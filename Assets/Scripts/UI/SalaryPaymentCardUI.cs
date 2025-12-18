using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI card for salary payment in PaymentListPanel.
    /// Shows staff name, amount, due date, and pay button.
    /// </summary>
    public class SalaryPaymentCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text dueDateText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button payButton;
        [SerializeField] private Image backgroundImage;
        
        [Header("Status Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color overdueColor = new Color(1f, 0.8f, 0.8f);
        
        private SalaryRecord salaryRecord;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        public SalaryRecord Record => salaryRecord;
        
        /// <summary>
        /// Setup the card with salary record
        /// </summary>
        public void Setup(SalaryRecord record)
        {
            salaryRecord = record;
            
            // Title
            if (titleText != null)
                titleText.text = L?.Get("ui.salary_title", record.staffName) ?? $"Salary: {record.staffName}";
            
            // Amount
            if (amountText != null)
                amountText.text = $"¥{record.amount:N0}";
            
            // Due date
            if (dueDateText != null)
            {
                int currentDay = GameManager.Instance?.DayCount ?? 1;
                int daysRemaining = record.dueDay - currentDay;
                
                if (daysRemaining > 0)
                    dueDateText.text = L?.Get("ui.due_in_days", daysRemaining) ?? $"Due in {daysRemaining} day(s)";
                else if (daysRemaining == 0)
                    dueDateText.text = L?.Get("ui.due_today") ?? "Due Today";
                else
                    dueDateText.text = L?.Get("ui.overdue_days", -daysRemaining) ?? $"Overdue by {-daysRemaining} day(s)";
            }
            
            // Status
            if (statusText != null)
            {
                if (record.isOverdue)
                    statusText.text = $"<color=red>{L?.Get("ui.overdue") ?? "OVERDUE"}</color>";
                else
                    statusText.text = L?.Get("ui.pending") ?? "Pending";
            }
            
            // Background color
            if (backgroundImage != null)
            {
                backgroundImage.color = record.isOverdue ? overdueColor : normalColor;
            }
            
            // Pay button
            if (payButton != null)
            {
                payButton.onClick.RemoveAllListeners();
                payButton.onClick.AddListener(OnPayClicked);
                
                // Check if can afford
                bool canAfford = EconomyManager.Instance?.CurrentMoney >= record.amount;
                payButton.interactable = canAfford;
            }
        }
        
        /// <summary>
        /// Refresh display (e.g., after day change)
        /// </summary>
        public void Refresh()
        {
            if (salaryRecord != null)
                Setup(salaryRecord);
        }
        
        private void OnPayClicked()
        {
            if (SalaryManager.Instance != null && salaryRecord != null)
            {
                bool success = SalaryManager.Instance.PaySalary(salaryRecord);
                if (success)
                {
                    // Card will be destroyed by PaymentListPanel refresh
                    Debug.Log($"[SalaryPaymentCardUI] Paid salary for {salaryRecord.staffName}");
                }
            }
        }
    }
}
