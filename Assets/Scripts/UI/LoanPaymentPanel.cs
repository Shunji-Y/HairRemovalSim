using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Legacy loan payment panel - now uses PaymentListPanel
    /// Kept for backwards compatibility
    /// </summary>
    [System.Obsolete("Use PaymentListPanel instead")]
    public class LoanPaymentPanel : MonoBehaviour
    {
        public static LoanPaymentPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text overdueWarningText;
        
        [Header("Payment Cards")]
        [SerializeField] private Transform paymentCardsContainer;
        [SerializeField] private GameObject paymentCardPrefab;
        
        [Header("Buttons")]
        [SerializeField] private Button payAllButton;
        [SerializeField] private Button closeButton;
        
        private List<GameObject> paymentCards = new List<GameObject>();
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            
            if (payAllButton != null)
                payAllButton.onClick.AddListener(OnPayAllClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        public void ShowIfNeeded()
        {
            if (LoanManager.Instance == null) return;
            
            if (LoanManager.Instance.HasPaymentsDue())
            {
                Show();
            }
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshDisplay();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
        
        private void RefreshDisplay()
        {
            ClearCards();
            
            if (LoanManager.Instance == null) return;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            var cards = LoanManager.Instance.GetUnpaidCards();
            
            // Update title
            if (titleText != null)
                titleText.text = $"Day {currentDay} - Loan Payment";
            
            // Show warning if overdue
            int overdueCount = LoanManager.Instance.GetOverdueCount();
            if (overdueWarningText != null)
            {
                if (overdueCount > 0)
                {
                    overdueWarningText.text = $"⚠ {overdueCount} overdue payment(s)!";
                    overdueWarningText.gameObject.SetActive(true);
                }
                else
                {
                    overdueWarningText.gameObject.SetActive(false);
                }
            }
            
            // Create cards
            foreach (var card in cards)
            {
                CreateCard(card, currentDay);
            }
            
            UpdatePayAllButton();
        }
        
        private void ClearCards()
        {
            foreach (var card in paymentCards)
            {
                if (card != null) Destroy(card);
            }
            paymentCards.Clear();
        }
        
        private void CreateCard(LoanPaymentCard loanCard, int currentDay)
        {
            if (paymentCardPrefab == null || paymentCardsContainer == null) return;
            
            var cardObj = Instantiate(paymentCardPrefab, paymentCardsContainer);
            var cardUI = cardObj.GetComponent<LoanPaymentCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(loanCard, currentDay, OnPaymentMade);
            }
            paymentCards.Add(cardObj);
        }
        
        private void UpdatePayAllButton()
        {
            if (payAllButton == null || LoanManager.Instance == null) return;
            
            int totalDue = LoanManager.Instance.GetTotalDebt();
            int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
            payAllButton.interactable = currentMoney >= totalDue;
        }
        
        private void OnPayAllClicked()
        {
            if (LoanManager.Instance == null) return;
            
            var cards = LoanManager.Instance.GetUnpaidCards().ToArray();
            foreach (var card in cards)
            {
                LoanManager.Instance.PayCard(card);
            }
            
            RefreshDisplay();
            
            if (!LoanManager.Instance.HasPaymentsDue())
            {
                Hide();
            }
        }
        
        private void OnPaymentMade()
        {
            RefreshDisplay();
            
            if (LoanManager.Instance != null && !LoanManager.Instance.HasPaymentsDue())
            {
                Hide();
            }
        }
        
        private void OnCloseClicked()
        {
            Hide();
        }
    }
}
