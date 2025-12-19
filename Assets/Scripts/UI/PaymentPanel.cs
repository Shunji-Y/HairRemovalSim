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
        
        [Header("Item Drop Target")]
        [SerializeField] private PaymentItemDropTarget itemDropTarget;
        
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
            
            // Close on ESC (cancel, not complete)
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cancel();
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
            
            Debug.Log($"[PaymentPanel] Opened for {data.customerName}, fee: ${treatmentFee}");
        }
        
        /// <summary>
        /// Cancel payment (ESC key) - does not complete payment, just closes UI
        /// </summary>
        public void Cancel()
        {
            Debug.Log($"[PaymentPanel] Cancelled for {currentCustomer?.data?.customerName ?? "NULL"} - customer still at register");
            
            // Don't clear dropTarget - keep item there for next open
            
            if (panel != null)
                panel.SetActive(false);
            
            // Do NOT clear currentCustomer - they're still at the register!
            // CashRegister.currentCustomer also remains
            
            // Re-lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        /// <summary>
        /// Hide after payment complete - clears current customer and all payment info
        /// </summary>
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            
            // Clear all payment info for next customer
            currentCustomer = null;
            treatmentFee = 0;
            addedItemPrice = 0;
            addedItemReviewBonus = 0;
            addedItemId = null;
            
            // Clear the item drop target display (item was consumed)
            if (itemDropTarget != null)
            {
                itemDropTarget.Clear();
            }
            
            // Re-lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[PaymentPanel] Hidden and cleared all payment info");
        }
        
        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplay()
        {
            if (currentCustomer == null) return;
            var data = currentCustomer.data;
            
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
            
            // Subtract accumulated penalties (pain, treatment issues)
            int totalReview = baseReview - data.reviewPenalty;
            
            // Add item bonus
            totalReview += addedItemReviewBonus;
            
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
        /// Convert mood level to star rating (1-5)
        /// </summary>
        private int GetStarsFromMood(MoodLevel mood)
        {
            return mood switch
            {
                MoodLevel.VeryAngry => 1,
                MoodLevel.Angry => 2,
                MoodLevel.Neutral => 3,
                MoodLevel.Happy => 4,
                MoodLevel.VeryHappy => 5,
                _ => 3
            };
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
            
            // Check wrong part penalty - 20% leave chance per wrong part
            int wrongPartCount = CountWrongParts(data.confirmedParts, data.requestPlan);
            if (wrongPartCount > 0)
            {
                float leaveChance = wrongPartCount * 0.20f;
                if (Random.value < leaveChance)
                {
                    Debug.Log($"[PaymentPanel] Customer {data.customerName} leaves due to wrong parts! ({wrongPartCount} wrong, {leaveChance*100:F0}% chance)");
                    
                    // Submit negative review
                    int angryReview = -30;
                    if (ShopManager.Instance != null)
                    {
                        ShopManager.Instance.AddReview(angryReview, currentCustomer.GetPainMaxCount());
                        ShopManager.Instance.AddCustomerReview(1); // 1 star
                    }
                    
                    OnPaymentComplete(currentCustomer, false);
                    currentCustomer.LeaveShop();
                    Hide();
                    return;
                }
            }
            
            int finalReview = CalculateCurrentReview();
            int totalAmount = treatmentFee + addedItemPrice;
            
            Debug.Log($"[PaymentPanel] Confirming payment - Amount: ${totalAmount}, Review: {finalReview}");
            
            // Save customer reference before Hide() clears it
            var customerToProcess = currentCustomer;
            
            // Check if customer leaves without paying
            if (finalReview <= -50)
            {
                Debug.Log($"[PaymentPanel] Customer {data.customerName} leaves without paying! Review too low: {finalReview}");
                
                // Submit negative review
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.AddReview(finalReview, customerToProcess.GetPainMaxCount());
                    // Add customer review for review panel
                    int stars = GetStarsFromMood(GetMoodFromReview(finalReview));
                    ShopManager.Instance.AddCustomerReview(stars);
                }
                
                // Don't clear dropTarget - keep item there
                
                // Notify CashRegister FIRST (before Hide clears currentCustomer)
                OnPaymentComplete(customerToProcess, false);
                
                customerToProcess.LeaveShop();
                Hide();
                return;
            }
            
            // Process payment
            EconomyManager.Instance.AddMoney(totalAmount);
            
            // Submit review
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReview(finalReview, customerToProcess.GetPainMaxCount());
                // Add customer review for review panel
                int stars = GetStarsFromMood(GetMoodFromReview(finalReview));
                ShopManager.Instance.AddCustomerReview(stars);
            }
            
            Debug.Log($"[PaymentPanel] {data.customerName} paid ${totalAmount}, review: {finalReview}");
            
            // NOTE: Item is already consumed when dropped onto PaymentItemDropTarget
            // No need to consume here again
            
            // Notify CashRegister FIRST (before Hide clears currentCustomer)
            OnPaymentComplete(customerToProcess, true);
            
            customerToProcess.LeaveShop();
            Hide();
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
        private void OnPaymentComplete(CustomerController customer, bool paid)
        {
            // Find CashRegister and notify
            var cashRegister = FindObjectOfType<CashRegister>();
            if (cashRegister != null)
            {
                cashRegister.OnPaymentProcessed(customer);
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
        
        /// <summary>
        /// Get checkout item slots for staff restocking
        /// </summary>
        public CheckoutItemSlotUI[] GetCheckoutItemSlots() => checkoutItemSlots;
        
        /// <summary>
        /// Consume a random item from checkout slots (for staff upsell at cashier)
        /// Returns item ID, price, and review bonus, or null if no items available
        /// </summary>
        public (string itemId, int price, int reviewBonus)? ConsumeRandomCheckoutItem()
        {
            if (checkoutItemSlots == null || checkoutItemSlots.Length == 0) return null;
            
            // Collect non-empty slots
            var availableSlots = new System.Collections.Generic.List<CheckoutItemSlotUI>();
            foreach (var slot in checkoutItemSlots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    availableSlots.Add(slot);
                }
            }
            
            if (availableSlots.Count == 0) return null;
            
            // Pick random slot
            int randomIndex = UnityEngine.Random.Range(0, availableSlots.Count);
            var selectedSlot = availableSlots[randomIndex];
            
            string itemId = selectedSlot.ItemId;
            
            // Get item price and review bonus
            int price = 0;
            int reviewBonus = 0;
            var itemData = Core.ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData != null)
            {
                price = itemData.price;
                reviewBonus = itemData.reviewBonus;
            }
            
            // Consume one item
            selectedSlot.RemoveOne();
            
            Debug.Log($"[PaymentPanel] Staff consumed checkout item: {itemId}, price: ${price}, review: +{reviewBonus}");
            
            return (itemId, price, reviewBonus);
        }
        
        /// <summary>
        /// Count the number of wrong parts (missing or extra) compared to the customer's request
        /// </summary>
        private int CountWrongParts(TreatmentBodyPart confirmedParts, CustomerRequestPlan requestPlan)
        {
            TreatmentBodyPart requiredParts = CustomerPlanHelper.GetRequiredParts(requestPlan);
            
            // Count missing parts (required but not selected)
            TreatmentBodyPart missing = requiredParts & ~confirmedParts;
            int missingCount = CountSetBits((int)missing);
            
            // Count extra parts (selected but not required)
            TreatmentBodyPart extra = confirmedParts & ~requiredParts;
            int extraCount = CountSetBits((int)extra);
            
            return missingCount + extraCount;
        }
        
        /// <summary>
        /// Count set bits in a flags enum
        /// </summary>
        private int CountSetBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            return count;
        }
    }
}
