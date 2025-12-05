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

        private Slider bodyPartSlider;
        private TreatmentSession currentSession;

        public void Setup(TreatmentSession session)
        {
            currentSession = session;
            
            // Clear existing entries
            foreach (Transform child in bodyPartListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create single entry for the treatment plan
            if (session.TargetBodyPart != null && session.Customer != null)
            {
                GameObject entry = Instantiate(bodyPartEntryPrefab, bodyPartListContainer);
                entry.SetActive(true);
                
                TextMeshProUGUI nameText = entry.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                Slider slider = entry.transform.Find("ProgressSlider").GetComponent<Slider>();
                
                // Display treatment plan name instead of body part name
                if (nameText != null) 
                {
                    nameText.text = session.Customer.data.selectedTreatmentPlan.GetDisplayName();
                }
                
                if (slider != null)
                {
                    slider.value = session.TargetBodyPart.CompletionPercentage / 100f;
                    bodyPartSlider = slider;
                }
            }

            UpdateUI();
            panelRoot.SetActive(false); // Start hidden, proximity detection will show it
        }

        public void UpdateUI()
        {
            if (currentSession == null) return;

            // Update individual slider
            if (bodyPartSlider != null && currentSession.TargetBodyPart != null)
            {
                bodyPartSlider.value = currentSession.TargetBodyPart.CompletionPercentage / 100f;
            }

            // Update overall progress
            if (overallProgressSlider != null)
            {
                overallProgressSlider.value = currentSession.OverallProgress / 100f;
            }
            if (overallProgressText != null)
            {
                overallProgressText.text = $"{currentSession.OverallProgress:F0}%";
            }
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
            currentSession = null;
        }
    }
}
