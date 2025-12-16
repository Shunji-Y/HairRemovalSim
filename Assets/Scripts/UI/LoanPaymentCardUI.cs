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
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            if (payButton != null)
                payButton.onClick.AddListener(OnPayClicked);
            
            if (prepaymentToggle != null)
                prepaymentToggle.onValueChanged.AddListener(OnPrepaymentToggleChanged);
        }
        
        private void OnEnable()
        {
            // Subscribe to locale changes
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
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
            
            // Loan name (not localized - uses LoanData.displayName)
            if (loanNameText != null)
                loanNameText.text = card.parentLoan.loanData.displayName;
            
            // Day info
            if (dayInfoText != null)
            {
                if (isPrepaymentMode)
                {
                    int remaining = card.parentLoan.termDays - card.parentLoan.paidDays;
                    dayInfoText.text = L?.Get("loan.prepay_days", remaining) ?? $"Prepay ({remaining} days left)";
                }
                else
                {
                    dayInfoText.text = L?.Get("loan.today_amount", card.TotalAmount) ?? $"Today: ${card.TotalAmount:N0}";
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
            
            // Amount display (numbers don't need localization, just formatting)
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
                    deadlineText.text = L?.Get("loan.full_payment") ?? "Full Payment";
                }
                else if (card.isOverdue)
                {
                    deadlineText.text = $"<color=red>{L?.Get("loan.overdue") ?? "Overdue"}</color>";
                }
                else if (daysUntilDue == 0)
                {
                    deadlineText.text = $"<color=orange>{L?.Get("loan.due_today") ?? "Due: Today"}</color>";
                }
                else
                {
                    deadlineText.text = L?.Get("loan.due_days", daysUntilDue) ?? $"Due: {daysUntilDue} days";
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
                    
                    string principal = L?.Get("loan.principal") ?? "Principal";
                    string feeLabel = L?.Get("loan.fee") ?? "Fee";
                    string lateLabel = L?.Get("loan.late") ?? "Late";
                    
                    if (lateFees > 0)
                    {
                        statusText.text = $"{principal}: ${remainingPrincipal:N0} + {feeLabel}: ${fee:N0} + <color=red>{lateLabel}: ${lateFees:N0}</color>";
                    }
                    else
                    {
                        statusText.text = $"{principal}: ${remainingPrincipal:N0} + {feeLabel}: ${fee:N0}";
                    }
                    statusText.gameObject.SetActive(true);
                }
                else if (card.isOverdue)
                {
                    if (card.lateFee > 0)
                    {
                        statusText.text = $"<color=red>⚠ {L?.Get("loan.late_fee", card.lateFee) ?? $"Late Fee: ${card.lateFee:N0}"}</color>";
                    }
                    else
                    {
                        statusText.text = $"<color=red>⚠ {L?.Get("loan.overdue") ?? "Overdue"}</color>";
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
                    payButtonText.text = L?.Get("loan.pay_all", prepaymentAmount) ?? $"Pay All (${prepaymentAmount:N0})";
                }
                else
                {
                    payButtonText.text = L?.Get("loan.pay_now", normalPayment) ?? $"Pay Now (${normalPayment:N0})";
                }
            }
            
            // Prepayment info
            if (prepaymentInfoText != null)
            {
                string prepayLabel = L?.Get("loan.prepay") ?? "Prepay";
                if (isPrepaymentMode)
                {
                    prepaymentInfoText.text = $"▽ {prepayLabel}";
                }
                else
                {
                    prepaymentInfoText.text = $"△ {prepayLabel}";
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
