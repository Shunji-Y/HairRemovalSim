using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Payment panel shown at checkout when customer is ready to pay.
    /// Displays mood, budget, treatment fee, allows adding items, and processes payment.
    /// </summary>
    public class PaymentPanel : MonoBehaviour
    {
        public static PaymentPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Customer Info")]
        [SerializeField] private Image moodIcon;
        [SerializeField] private TextMeshProUGUI budgetText;
        
        [Header("Mood Icons (0=Angry, 4=VeryHappy)")]
        [SerializeField] private Sprite[] moodSprites; // 5 sprites for mood levels
        
        [Header("Payment Info")]
        [SerializeField] private TextMeshProUGUI treatmentFeeText;
        [SerializeField] private TextMeshProUGUI totalAmountText;
        
        [Header("Added Item")]
        [SerializeField] private Image addedItemIcon;
        
        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        
        [Header("Checkout Item Slots (synced from CheckoutStockSlotUI)")]
        [SerializeField] private CheckoutItemSlotUI[] checkoutItemSlots;
        
        // Current state
        private CustomerController currentCustomer;
        private int treatmentFee;
        private int addedItemPrice;
        private int addedItemReviewBonus;
        private string addedItemId;
        
        // Mood levels and review ranges
        public enum MoodLevel
        {
            VeryAngry = 0,  // Review: -49 to -30
            Angry = 1,      // Review: -29 to -10
            Neutral = 2,    // Review: -10 to +10
            Happy = 3,      // Review: +11 to +30
            VeryHappy = 4   // Review: +31 to +50
        }
        
        public bool IsOpen => panel != null && panel.activeSelf;
        public CheckoutItemSlotUI[] CheckoutItemSlots => checkoutItemSlots;
        
        private void Awake()
        {
            Instance = this;
            
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmPayment);
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            // Close on ESC
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }
        
        /// <summary>
        /// Show payment panel for a customer
        /// </summary>
        public void Show(CustomerController customer)
        {
            if (customer == null) return;
            
            currentCustomer = customer;
            var data = customer.data;
            
            // Get treatment fee from confirmed price
            treatmentFee = data.confirmedPrice;
            addedItemPrice = 0;
            addedItemReviewBonus = 0;
            addedItemId = null;
            
            // Update display
            UpdateDisplay();
            
            // Show panel
            if (panel != null)
                panel.SetActive(true);
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log($"[PaymentPanel] Opened for {data.customerName}, fee: ${treatmentFee}, budget: ${data.baseBudget}");
        }
        
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            
            currentCustomer = null;
            
            // Re-lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplay()
        {
            if (currentCustomer == null) return;
            var data = currentCustomer.data;
            
            // Budget
            if (budgetText != null)
                budgetText.text = $"${data.baseBudget}";
            
            // Treatment fee
            if (treatmentFeeText != null)
                treatmentFeeText.text = $"${treatmentFee}";
            
            // Total amount
            int total = treatmentFee + addedItemPrice;
            if (totalAmountText != null)
                totalAmountText.text = $"${total}";
            
            // Calculate and display mood
            int currentReview = CalculateCurrentReview();
            MoodLevel mood = GetMoodFromReview(currentReview);
            UpdateMoodIcon(mood);
            
            // Added item display
            if (addedItemIcon != null)
            {
                if (!string.IsNullOrEmpty(addedItemId))
                {
                    var itemData = ItemDataRegistry.Instance?.GetItem(addedItemId);
                    if (itemData != null && itemData.icon != null)
                    {
                        addedItemIcon.sprite = itemData.icon;
                        addedItemIcon.enabled = true;
                    }
                }
                else
                {
                    addedItemIcon.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// Calculate current review value based on all factors
        /// </summary>
        private int CalculateCurrentReview()
        {
            if (currentCustomer == null) return 0;
            var data = currentCustomer.data;
            
            // Base review from customer controller
            int baseReview = currentCustomer.GetBaseReviewValue(); // 10-50
            
            // Subtract accumulated penalties (pain, reception issues)
            int totalReview = baseReview - data.reviewPenalty;
            
            // Add item bonus
            totalReview += addedItemReviewBonus;
            
            // Calculate budget overage penalty
            int totalPrice = treatmentFee + addedItemPrice;
            int overBudget = totalPrice - data.baseBudget;
            if (overBudget > 0)
            {
                // Penalty scales with overage
                int overagePenalty = Mathf.RoundToInt(overBudget * 0.5f);
                totalReview -= overagePenalty;
            }
            
            return totalReview;
        }
        
        /// <summary>
        /// Get mood level from review value
        /// </summary>
        private MoodLevel GetMoodFromReview(int review)
        {
            if (review <= -30) return MoodLevel.VeryAngry;      // -49 to -30
            if (review <= -10) return MoodLevel.Angry;          // -29 to -10
            if (review <= 10) return MoodLevel.Neutral;         // -10 to +10
            if (review <= 30) return MoodLevel.Happy;           // +11 to +30
            return MoodLevel.VeryHappy;                          // +31 to +50
        }
        
        /// <summary>
        /// Update mood icon based on level
        /// </summary>
        private void UpdateMoodIcon(MoodLevel mood)
        {
            if (moodIcon == null || moodSprites == null || moodSprites.Length == 0) return;
            
            int index = (int)mood;
            if (index >= 0 && index < moodSprites.Length && moodSprites[index] != null)
            {
                moodIcon.sprite = moodSprites[index];
            }
        }
        
        /// <summary>
        /// Called when an item is dropped onto the added item slot
        /// </summary>
        public void OnItemAdded(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null || !itemData.canUseAtCheckout) return;
            
            addedItemId = itemId;
            addedItemPrice = itemData.upsellPrice;
            addedItemReviewBonus = itemData.reviewBonus;
            
            UpdateDisplay();
            
            Debug.Log($"[PaymentPanel] Added item: {itemId}, price: ${addedItemPrice}, reviewBonus: {addedItemReviewBonus}");
        }
        
        /// <summary>
        /// Clear added item
        /// </summary>
        public void ClearAddedItem()
        {
            addedItemId = null;
            addedItemPrice = 0;
            addedItemReviewBonus = 0;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Confirm payment button handler
        /// </summary>
        private void OnConfirmPayment()
        {
            if (currentCustomer == null) return;
            var data = currentCustomer.data;
            
            int finalReview = CalculateCurrentReview();
            int totalAmount = treatmentFee + addedItemPrice;
            
            Debug.Log($"[PaymentPanel] Confirming payment - Amount: ${totalAmount}, Review: {finalReview}");
            
            // Check if customer leaves without paying
            if (finalReview <= -50)
            {
                Debug.Log($"[PaymentPanel] Customer {data.customerName} leaves without paying! Review too low: {finalReview}");
                
                // Submit negative review
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.AddReview(finalReview, currentCustomer.GetPainMaxCount());
                }
                
                currentCustomer.LeaveShop();
                Hide();
                
                // Notify CashRegister
                OnPaymentComplete(false);
                return;
            }
            
            // Process payment
            EconomyManager.Instance.AddMoney(totalAmount);
            
            // Submit review
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReview(finalReview, currentCustomer.GetPainMaxCount());
            }
            
            Debug.Log($"[PaymentPanel] {data.customerName} paid ${totalAmount}, review: {finalReview}");
            
            // NOTE: Item is already consumed when dropped onto PaymentItemDropTarget
            // No need to consume here again
            
            currentCustomer.LeaveShop();
            Hide();
            
            // Notify CashRegister
            OnPaymentComplete(true);
        }
        
        /// <summary>
        /// Consume item from checkout stock
        /// </summary>
        private void ConsumeItemFromStock(string itemId)
        {
            if (checkoutItemSlots == null) return;
            
            foreach (var slot in checkoutItemSlots)
            {
                if (slot != null && slot.ItemId == itemId && slot.Quantity > 0)
                {
                    slot.RemoveOne();
                    Debug.Log($"[PaymentPanel] Consumed 1x {itemId} from checkout stock");
                    break;
                }
            }
        }
        
        /// <summary>
        /// Called when payment is complete (success or customer left)
        /// </summary>
        private void OnPaymentComplete(bool paid)
        {
            // Find CashRegister and notify
            var cashRegister = FindObjectOfType<CashRegister>();
            if (cashRegister != null)
            {
                cashRegister.OnPaymentProcessed(currentCustomer);
            }
        }
        
        /// <summary>
        /// Set checkout item slot from CheckoutStockSlotUI sync
        /// </summary>
        public void SetCheckoutItemSlot(int index, string itemId, int quantity)
        {
            if (checkoutItemSlots == null || index < 0 || index >= checkoutItemSlots.Length) return;
            
            var slot = checkoutItemSlots[index];
            if (slot != null)
            {
                slot.SetItemFromStock(itemId, quantity);
            }
        }
    }
}
