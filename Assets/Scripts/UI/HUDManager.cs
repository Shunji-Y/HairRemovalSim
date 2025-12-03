using UnityEngine;
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
        public TextMeshProUGUI interactionPromptText;
        public TreatmentPanel treatmentPanel;

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
            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
                interactionPromptText.gameObject.SetActive(true);
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
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
                moneyText.text = $"Money: ${amount:N0}";
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
