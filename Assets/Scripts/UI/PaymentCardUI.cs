using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual payment card for daily loan payments
    /// </summary>
    public class PaymentCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text loanNameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text deadlineText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image cardBackground;
        
        [Header("Buttons")]
        [SerializeField] private Button payButton;
        [SerializeField] private TMP_Text payButtonText;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color overdueColor = new Color(1f, 0.85f, 0.85f);
        
        private LoanPaymentCard card;
        private int currentDay;
        private System.Action onPaymentMade;
        
        private void Awake()
        {
            if (payButton != null)
                payButton.onClick.AddListener(OnPayClicked);
        }
        
        public void Setup(LoanPaymentCard paymentCard, System.Action callback)
        {
            card = paymentCard;
            currentDay = GameManager.Instance?.DayCount ?? 1;
            onPaymentMade = callback;
            RefreshDisplay();
        }
        
        public void Setup(LoanPaymentCard paymentCard, int day, System.Action callback)
        {
            card = paymentCard;
            currentDay = day;
            onPaymentMade = callback;
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (card == null) return;
            
            // Loan name
            if (loanNameText != null)
                loanNameText.text = card.parentLoan.loanData.displayName;
            
            // Amount
            if (amountText != null)
            {
                if (card.isOverdue && card.lateFee > 0)
                {
                    amountText.text = $"${card.baseAmount:N0} <color=red>+${card.lateFee:N0}</color>";
                }
                else
                {
                    amountText.text = $"${card.baseAmount:N0}";
                }
            }
            
            // Deadline
            int daysUntilDue = card.GetDaysUntilDue(currentDay);
            if (deadlineText != null)
            {
                if (card.isOverdue)
                {
                    deadlineText.text = "<color=red>Overdue</color>";
                }
                else if (daysUntilDue == 0)
                {
                    deadlineText.text = "<color=orange>Due: Today</color>";
                }
                else
                {
                    deadlineText.text = $"Due: {daysUntilDue} days";
                }
            }
            
            // Status
            if (statusText != null)
            {
                statusText.gameObject.SetActive(card.isOverdue);
                if (card.isOverdue)
                {
                    if (card.lateFee > 0)
                    {
                        statusText.text = $"<color=red>⚠ Overdue (Late Fee: ${card.lateFee:N0})</color>";
                    }
                    else
                    {
                        statusText.text = "<color=red>⚠ Overdue</color>";
                    }
                }
            }
            
            // Background
            if (cardBackground != null)
            {
                cardBackground.color = card.isOverdue ? overdueColor : normalColor;
            }
            
            // Pay button
            if (payButton != null)
            {
                int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
                payButton.interactable = currentMoney >= card.TotalAmount;
            }
            if (payButtonText != null)
            {
                payButtonText.text = $"Pay Now (${card.TotalAmount:N0})";
            }
        }
        
        private void OnPayClicked()
        {
            if (card == null || LoanManager.Instance == null) return;
            
            if (LoanManager.Instance.PayCard(card))
            {
                onPaymentMade?.Invoke();
            }
        }
    }
}
