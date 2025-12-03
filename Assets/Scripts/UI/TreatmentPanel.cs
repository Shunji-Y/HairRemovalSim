using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Treatment;
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

        private Dictionary<Core.BodyPart, Slider> bodyPartSliders = new Dictionary<Core.BodyPart, Slider>();
        private TreatmentSession currentSession;

        public void Setup(TreatmentSession session)
        {
            currentSession = session;
            bodyPartSliders.Clear();
            
            // Clear existing entries
            foreach (Transform child in bodyPartListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new entries
            foreach (var part in session.TargetBodyParts)
            {
                GameObject entry = Instantiate(bodyPartEntryPrefab, bodyPartListContainer);
                entry.SetActive(true); // Ensure the entry is active
                
                TextMeshProUGUI nameText = entry.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                Slider slider = entry.transform.Find("ProgressSlider").GetComponent<Slider>();
                
                if (nameText != null) nameText.text = part.partName;
                if (slider != null)
                {
                    slider.value = part.CompletionPercentage / 100f;
                    bodyPartSliders.Add(part, slider);
                }
            }

            UpdateUI();
            panelRoot.SetActive(false); // Start hidden, proximity detection will show it
        }

        public void UpdateUI()
        {
            if (currentSession == null) return;

            // Update individual sliders
            foreach (var kvp in bodyPartSliders)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Value.value = kvp.Key.CompletionPercentage / 100f;
                }
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
