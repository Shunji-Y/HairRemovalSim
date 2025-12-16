using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Rent payment card UI
    /// Shows: rent amount, days until due, overdue status
    /// </summary>
    public class RentPaymentCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text deadlineText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text dayInfoText;
        [SerializeField] private Image cardBackground;
        
        [Header("Buttons")]
        [SerializeField] private Button payButton;
        [SerializeField] private TMP_Text payButtonText;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.92f, 0.95f, 0.95f);
        [SerializeField] private Color warningColor = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private Color overdueColor = new Color(1f, 0.85f, 0.85f);
        
        private RentPaymentCard card;
        private int currentDay;
        private System.Action onPaymentMade;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            if (payButton != null)
                payButton.onClick.AddListener(OnPayClicked);
        }
        
        private void OnEnable()
        {
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Setup(RentPaymentCard rentCard, int day, System.Action callback)
        {
            card = rentCard;
            currentDay = day;
            onPaymentMade = callback;
            
            if (payButton != null)
            {
                payButton.onClick.RemoveAllListeners();
                payButton.onClick.AddListener(OnPayClicked);
            }
            
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (card == null) return;
            
            // Title
            if (titleText != null)
                titleText.text = L?.Get("rent.title") ?? "Rent";
            
            // Day info
            if (dayInfoText != null)
                dayInfoText.text = L?.Get("rent.bill", card.generatedDay) ?? $"Day {card.generatedDay} Bill";
            
            // Amount
            if (amountText != null)
                amountText.text = $"${card.amount:N0}";
            
            // Deadline
            int daysUntilDue = card.GetDaysUntilDue(currentDay);
            if (deadlineText != null)
            {
                if (card.isOverdue)
                {
                    int daysOverdue = currentDay - card.overdueSinceDay;
                    string overdueText = L?.Get("loan.overdue") ?? "Overdue";
                    deadlineText.text = $"<color=red>{overdueText} ({daysOverdue}d)</color>";
                }
                else if (daysUntilDue <= 0)
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
                if (card.isOverdue)
                {
                    statusText.text = $"<color=red>⚠ {L?.Get("rent.overdue_pay") ?? "Overdue - Pay immediately!"}</color>";
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
                if (card.isOverdue)
                {
                    cardBackground.color = overdueColor;
                }
                else if (daysUntilDue <= 1)
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
                payButton.interactable = currentMoney >= card.amount;
            }
            if (payButtonText != null)
            {
                payButtonText.text = L?.Get("loan.pay_now", card.amount) ?? $"Pay Now (${card.amount:N0})";
            }
        }
        
        private void OnPayClicked()
        {
            if (card == null) return;
            
            if (RentManager.Instance != null && RentManager.Instance.PayRentCard(card, currentDay))
            {
                Debug.Log($"[RentPaymentCardUI] Rent from day {card.generatedDay} paid!");
                onPaymentMade?.Invoke();
            }
            else
            {
                Debug.Log("[RentPaymentCardUI] Payment failed");
            }
        }
    }
}
