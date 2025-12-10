using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Treatment;
using HairRemovalSim.Customer;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    public class TreatmentPanel : MonoBehaviour
    {
        [Header("References")]
        public Transform bodyPartListContainer;
        public GameObject bodyPartEntryPrefab;
        public Slider overallProgressSlider;
        public TextMeshProUGUI overallProgressText;
        public GameObject panelRoot;
        public PainGauge painGauge; // Pain gauge UI

        private TreatmentSession currentSession;
        private HairTreatmentController treatmentController;
        private Dictionary<string, Slider> partSliders = new Dictionary<string, Slider>();

        public void Setup(TreatmentSession session)
        {
            currentSession = session;
            partSliders.Clear();
            
            // Setup pain gauge
            if (painGauge != null && session.Customer != null)
            {
                painGauge.SetCustomer(session.Customer);
            }
            
            // Clear existing entries
            foreach (Transform child in bodyPartListContainer)
            {
                Destroy(child.gameObject);
            }

            if (session.TargetBodyPart == null || session.Customer == null) return;
            
            // Get HairTreatmentController for per-part completion
            treatmentController = session.TargetBodyPart.GetComponent<HairTreatmentController>();
            
            if (treatmentController == null)
            {
                Debug.LogWarning("[TreatmentPanel] No HairTreatmentController found on BodyPart");
                return;
            }
            
            // Get individual body part names from the treatment controller
            var partNames = treatmentController.GetTargetPartNames();
            
            if (partNames.Count == 0)
            {
                // Fallback: Create single entry with treatment plan name
                CreateEntry(session.Customer.data.selectedTreatmentPlan.GetDisplayName(), "overall");
            }
            else
            {
                // Create an entry for each individual body part
                foreach (var partName in partNames)
                {
                    CreateEntry(partName, partName);
                }
            }

            UpdateUI();
            panelRoot.SetActive(false); // Start hidden, proximity detection will show it
        }
        
        private void CreateEntry(string displayName, string partKey)
        {
            GameObject entry = Instantiate(bodyPartEntryPrefab, bodyPartListContainer);
            entry.SetActive(true);
            
            TextMeshProUGUI nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            Slider slider = entry.transform.Find("ProgressSlider")?.GetComponent<Slider>();
            
            if (nameText != null)
            {
                nameText.text = displayName;
            }
            
            if (slider != null)
            {
                slider.value = 0f;
                partSliders[partKey] = slider;
            }
        }

        public void UpdateUI()
        {
            if (currentSession == null) return;

            // Update individual sliders per body part
            if (treatmentController != null)
            {
                var partCompletions = treatmentController.GetPerPartCompletion();
                
                foreach (var kvp in partSliders)
                {
                    float completion = 0f;
                    
                    if (kvp.Key == "overall")
                    {
                        // Calculate average completion from all parts
                        if (partCompletions.Count > 0)
                        {
                            float total = 0f;
                            foreach (var pc in partCompletions)
                            {
                                total += pc.Value;
                            }
                            completion = total / partCompletions.Count;
                        }
                    }
                    else if (partCompletions.TryGetValue(kvp.Key, out float partCompletion))
                    {
                        completion = partCompletion;
                    }
                    
                    kvp.Value.value = completion / 100f;
                    
                    // Change slider fill color to yellow when completed
                    if (completion >= 100f)
                    {
                        var fillImage = kvp.Value.fillRect?.GetComponent<Image>();
                        if (fillImage != null)
                        {
                            fillImage.color = new Color(1f, 0.9f, 0.2f, 1f); // Yellow
                        }
                    }
                }
            }

            // Hide overall progress UI (no longer needed)
            if (overallProgressSlider != null)
            {
                overallProgressSlider.gameObject.SetActive(false);
            }
            if (overallProgressText != null)
            {
                overallProgressText.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
            currentSession = null;
            treatmentController = null;
            partSliders.Clear();
        }
    }
}
