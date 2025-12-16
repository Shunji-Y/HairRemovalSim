using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Loan payment card UI for daily payment cards
    /// Shows: loan name, daily amount, days until due, overdue status
    /// Includes prepayment toggle to pay off entire loan early
    /// </summary>
    public class LoanPaymentCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text loanNameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text deadlineText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text dayInfoText;
        [SerializeField] private Image cardBackground;
        
        [Header("Buttons")]
        [SerializeField] private Button payButton;
        [SerializeField] private TMP_Text payButtonText;
        
        [Header("Prepayment")]
        [SerializeField] private Toggle prepaymentToggle;
        [SerializeField] private TMP_Text prepaymentInfoText;
        [SerializeField] private GameObject prepaymentPanel;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color warningColor = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private Color overdueColor = new Color(1f, 0.85f, 0.85f);
        
        private LoanPaymentCard card;
        private int currentDay;
        private System.Action onPaymentMade;
        private bool isPrepaymentMode = false;
        
        private void Awake()
        {
            if (payButton != null)
                payButton.onClick.AddListener(OnPayClicked);
            
            if (prepaymentToggle != null)
                prepaymentToggle.onValueChanged.AddListener(OnPrepaymentToggleChanged);
        }
        
        public void Setup(LoanPaymentCard paymentCard, int day, System.Action callback)
        {
            card = paymentCard;
            currentDay = day;
            onPaymentMade = callback;
            isPrepaymentMode = false;
            
            // Re-register button listeners
            if (payButton != null)
            {
                payButton.onClick.RemoveAllListeners();
                payButton.onClick.AddListener(OnPayClicked);
            }
            
            if (prepaymentToggle != null)
            {
                prepaymentToggle.onValueChanged.RemoveAllListeners();
                prepaymentToggle.isOn = false;
                prepaymentToggle.onValueChanged.AddListener(OnPrepaymentToggleChanged);
            }
            
            RefreshDisplay();
        }
        
        private void OnPrepaymentToggleChanged(bool isOn)
        {
            isPrepaymentMode = isOn;
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (card == null) return;
            
            // Loan name
            if (loanNameText != null)
                loanNameText.text = card.parentLoan.loanData.displayName;
            
            // Day info
            if (dayInfoText != null)
            {
                if (isPrepaymentMode)
                {
                    int remaining = card.parentLoan.termDays - card.parentLoan.paidDays;
                    dayInfoText.text = $"Prepay ({remaining} days left)";
                }
                else
                {
                    dayInfoText.text = $"Today: ${card.TotalAmount:N0}";
                }
            }
            
            // Calculate amounts
            int normalPayment = card.TotalAmount;
            int prepaymentAmount = 0;
            
            if (isPrepaymentMode && LoanManager.Instance != null)
            {
                prepaymentAmount = LoanManager.Instance.CalculatePrepaymentAmount(card.parentLoan);
            }
            
            int paymentAmount = isPrepaymentMode ? prepaymentAmount : normalPayment;
            
            // Amount display
            if (amountText != null)
            {
                if (isPrepaymentMode)
                {
                    amountText.text = $"${prepaymentAmount:N0}";
                }
                else if (card.isOverdue && card.lateFee > 0)
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
                if (isPrepaymentMode)
                {
                    deadlineText.text = "Full Payment";
                }
                else if (card.isOverdue)
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
                if (isPrepaymentMode)
                {
                    // Show prepayment breakdown including late fees
                    int remainingPrincipal = card.parentLoan.remainingPrincipal;
                    int fee = LoanManager.Instance?.GetPrepaymentFee(card.parentLoan) ?? 0;
                    int lateFees = LoanManager.Instance?.GetAccumulatedLateFees(card.parentLoan) ?? 0;
                    
                    if (lateFees > 0)
                    {
                        statusText.text = $"Principal: ${remainingPrincipal:N0} + Fee: ${fee:N0} + <color=red>Late: ${lateFees:N0}</color>";
                    }
                    else
                    {
                        statusText.text = $"Principal: ${remainingPrincipal:N0} + Fee: ${fee:N0}";
                    }
                    statusText.gameObject.SetActive(true);
                }
                else if (card.isOverdue)
                {
                    if (card.lateFee > 0)
                    {
                        statusText.text = $"<color=red>⚠ Late Fee: ${card.lateFee:N0}</color>";
                    }
                    else
                    {
                        statusText.text = "<color=red>⚠ Overdue</color>";
                    }
                    statusText.gameObject.SetActive(true);
                }
                else
                {
                    statusText.gameObject.SetActive(false);
                }
            }
            
            // Background color
            if (cardBackground != null)
            {
                if (card.isOverdue && !isPrepaymentMode)
                {
                    cardBackground.color = overdueColor;
                }
                else if (daysUntilDue <= 1 && !isPrepaymentMode)
                {
                    cardBackground.color = warningColor;
                }
                else
                {
                    cardBackground.color = normalColor;
                }
            }
            
            // Pay button
            if (payButton != null)
            {
                int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
                payButton.interactable = currentMoney >= paymentAmount;
            }
            if (payButtonText != null)
            {
                if (isPrepaymentMode)
                {
                    payButtonText.text = $"Pay All (${prepaymentAmount:N0})";
                }
                else
                {
                    payButtonText.text = $"Pay Now (${normalPayment:N0})";
                }
            }
            
            // Prepayment info
            if (prepaymentInfoText != null)
            {
                if (isPrepaymentMode)
                {
                    prepaymentInfoText.text = "▽ Prepay";
                }
                else
                {
                    prepaymentInfoText.text = "△ Prepay";
                }
            }
        }
        
        private int CalculateSavings()
        {
            if (card?.parentLoan == null || LoanManager.Instance == null) return 0;
            
            var loan = card.parentLoan;
            int remainingDays = loan.termDays - loan.paidDays;
            int totalIfNormal = remainingDays * loan.dailyPayment;
            int prepaymentCost = LoanManager.Instance.CalculatePrepaymentAmount(loan);
            
            return totalIfNormal - prepaymentCost;
        }
        
        private void OnPayClicked()
        {
            if (card == null || LoanManager.Instance == null) return;
            
            bool success;
            if (isPrepaymentMode)
            {
                success = LoanManager.Instance.Prepay(card.parentLoan);
                if (success)
                {
                    Debug.Log($"[LoanPaymentCardUI] Prepaid entire loan: {card.parentLoan.loanData.displayName}");
                }
            }
            else
            {
                success = LoanManager.Instance.PayCard(card);
                if (success)
                {
                    Debug.Log($"[LoanPaymentCardUI] Paid card for {card.parentLoan.loanData.displayName}");
                }
            }
            
            if (success)
            {
                onPaymentMade?.Invoke();
            }
            else
            {
                Debug.Log("[LoanPaymentCardUI] Payment failed - not enough money?");
            }
        }
    }
}
