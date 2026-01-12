using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    public class HUDManager : Singleton<HUDManager>
    {
        [Header("UI References")]
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI dayText;
        public GameObject interactBackground;
        public TextMeshProUGUI interactionPromptText;
        public GameObject placementDescriptionRoot;
        public TextMeshProUGUI placementNameText;
        public TextMeshProUGUI placementDescriptionText;
        public TreatmentPanel treatmentPanel;

        public TextMeshProUGUI timeTextForPC;

        // No need for Awake override unless we have specific init logic, Singleton handles Instance.

        private void OnEnable()
        {
            GameEvents.OnMoneyChanged += UpdateMoney;
            GameEvents.OnTimeUpdated += HandleTimeUpdate;
            GameEvents.OnDayChanged += UpdateDay;

        }

        private void OnDisable()
        {
            GameEvents.OnMoneyChanged -= UpdateMoney;
            GameEvents.OnTimeUpdated -= HandleTimeUpdate;
            GameEvents.OnDayChanged -= UpdateDay;
        }

        public void ShowInteractionPrompt(string prompt)
        {
            if (interactionPromptText != null && !string.IsNullOrEmpty(prompt))
            {
                // Set text first
                interactionPromptText.text = prompt;
                
                // Show background
                if (interactBackground != null)
                {
                    interactBackground.SetActive(true);
                    
                    // Force ContentSizeFitter to recalculate
                    LayoutRebuilder.ForceRebuildLayoutImmediate(interactBackground.GetComponent<RectTransform>());
                }
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactBackground.SetActive(false);
            }
        }
        
        /// <summary>
        /// Show description for placement objects (water server, decorations, etc.)
        /// </summary>
        public void ShowPlacementDescription(string name, string description)
        {
            // Show the root panel
            if (placementDescriptionRoot != null)
            {
                placementDescriptionRoot.SetActive(true);
            }
            
            if (placementNameText != null)
            {
                placementNameText.text = name;
            }
            
            if (placementDescriptionText != null)
            {
                placementDescriptionText.text = description;
            }
        }
        
        /// <summary>
        /// Hide placement object description
        /// </summary>
        public void HidePlacementDescription()
        {
            // Hide the root panel (includes background)
            if (placementDescriptionRoot != null)
            {
                placementDescriptionRoot.SetActive(false);
            }
        }

        public void ShowTreatmentPanel(HairRemovalSim.Treatment.TreatmentSession session)
        {
            if (treatmentPanel != null)
            {
                treatmentPanel.Setup(session);
            }
        }

        public void HideTreatmentPanel()
        {
            if (treatmentPanel != null)
            {
                treatmentPanel.Hide();
            }
        }

        public void UpdateTreatmentPanel()
        {
            if (treatmentPanel != null)
            {
                treatmentPanel.UpdateUI();
            }
        }

        public bool IsTreatmentPanelVisible()
        {
            return treatmentPanel != null && treatmentPanel.gameObject.activeSelf;
        }

        private void UpdateMoney(int amount)
        {
            if (moneyText != null)
            {
                moneyText.text = $"${amount:N0}";
            }
        }

        private void HandleTimeUpdate(float normalizedTime)
        {
            // Default hours: 9 to 19
            UpdateTime(normalizedTime, 9, 19);
        }

        private void UpdateTime(float normalizedTime, int hourStart, int hourEnd)
        {
            if (timeText != null)
            {
                float totalHours = hourEnd - hourStart;
                float currentHour = hourStart + (normalizedTime * totalHours);
                int hours = Mathf.FloorToInt(currentHour);
                int minutes = Mathf.FloorToInt((currentHour - hours) * 60);

                timeText.text = $"{hours:00}:{minutes:00}";
            }

            if (timeTextForPC != null)
            {
                timeTextForPC.text = timeText.text;
            }
        }

        private void UpdateDay(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Day {day}";
            }
    
        }
    }
}
