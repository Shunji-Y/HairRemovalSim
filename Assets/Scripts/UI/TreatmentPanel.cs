using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Treatment;
using HairRemovalSim.Customer;
using System.Collections.Generic;
using HairRemovalSim.Core;

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
        
        [Header("Effect Item Icon")]
        [Tooltip("Image to display applied reception item icon (optional)")]
        public Image effectItemIcon;
        [Tooltip("Text to display applied reception item name (optional)")]
        public TextMeshProUGUI effectItemNameText;

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
            
            // Setup effect item icon (display applied reception item)
            SetupEffectItemIcon(session.Customer);
            
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
            
            // Use confirmedParts from reception if available
            var confirmedParts = session.Customer.data.confirmedParts;
            
            if (confirmedParts != TreatmentBodyPart.None)
            {
                // Get detailed 14-part breakdown from 7 reception selections
                var detailedParts = CustomerPlanHelper.GetDetailedTreatmentParts(confirmedParts);
                
                foreach (var partName in detailedParts)
                {
                    CreateEntry(partName, partName);
                }
            }
            else
            {
                // Fallback: Use old UV-based system
                var partNames = treatmentController.GetTargetPartNames();
                
                if (partNames.Count == 0)
                {
                    CreateEntry(session.Customer.data.selectedTreatmentPlan.GetDisplayName(), "overall");
                }
                else
                {
                    foreach (var partName in partNames)
                    {
                        CreateEntry(partName, partName);
                    }
                }
            }

            UpdateUI();
            panelRoot.SetActive(false); // Start hidden, proximity detection will show it
        }
        
        /// <summary>
        /// Setup the effect item icon based on customer's applied reception item
        /// </summary>
        private void SetupEffectItemIcon(CustomerController customer)
        {
            // Hide by default
            if (effectItemIcon != null) effectItemIcon.gameObject.SetActive(false);
            if (effectItemNameText != null) effectItemNameText.gameObject.SetActive(false);
            
            if (customer != null && customer.AppliedReceptionItem != null)
            {
                var item = customer.AppliedReceptionItem;
                
                // Show icon if available
                if (effectItemIcon != null && item.icon != null)
                {
                    effectItemIcon.sprite = item.icon;
                    effectItemIcon.gameObject.SetActive(true);
                }
                
                // Show localized name if available
                if (effectItemNameText != null)
                {
                    string localizedName = item.GetLocalizedName();
                    effectItemNameText.text = localizedName;
                    effectItemNameText.gameObject.SetActive(true);
                }
                
                Debug.Log($"[TreatmentPanel] Displaying effect item: {item.itemId}");
            }
        }
        
        private void CreateEntry(string displayName, string partKey)
        {
            GameObject entry = Instantiate(bodyPartEntryPrefab, bodyPartListContainer);
            entry.SetActive(true);
            
            TextMeshProUGUI nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            Slider slider = entry.transform.Find("ProgressSlider")?.GetComponent<Slider>();
            
            if (nameText != null)
            {
                var dName = LocalizationManager.Instance.Get("part." + displayName);
                nameText.text = dName;
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
            
            // Hide effect item icon and text
            if (effectItemIcon != null)
            {
                effectItemIcon.gameObject.SetActive(false);
            }
            if (effectItemNameText != null)
            {
                effectItemNameText.gameObject.SetActive(false);
            }
        }
    }
}
