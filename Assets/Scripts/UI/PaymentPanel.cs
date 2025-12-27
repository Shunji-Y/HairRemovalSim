using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;
using HairRemovalSim.Core.Effects;
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
        
        [Header("Upsell Display")]
        [SerializeField] private TextMeshProUGUI additionalBudgetText;
        [SerializeField] private TextMeshProUGUI successRateText;
        
        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI itemTooltip;
        public ItemTooltipUI Tooltip => itemTooltip;
        
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
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
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
            
            // Pause waiting timer - gauge stays visible until confirm
            customer.PauseWaiting();
            
            // Get treatment fee from confirmed price
            treatmentFee = data.confirmedPrice;
            addedItemPrice = 0;
            addedItemReviewBonus = 0;
            addedItemId = null;
            
            // Display total budget (plan price + additional budget)
            if (additionalBudgetText != null)
            {
                additionalBudgetText.text = $"${customer.GetTotalBudget()}";
            }
            
            // Hide success rate (no item dropped yet)
            if (successRateText != null)
            {
                successRateText.gameObject.SetActive(false);
            }
            
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
            
            // Resume waiting timer from current value - payment was cancelled
            if (currentCustomer != null)
            {
                currentCustomer.ResumeWaiting();
            }
            
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
            
            // Total amount - show total with upsell increment if item is added
            int total = treatmentFee + addedItemPrice;
            if (totalAmountText != null)
            {
                if (addedItemPrice > 0)
                {
                    // Show: "$55 (↑$20)"
                    totalAmountText.text = $"${total} (<color=green>↑${addedItemPrice})";
                }
                else
                {
                    totalAmountText.text = $"${total}";
                }
            }
            
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
            
            // Apply debris penalty (1 debris = -1 point)
            if (Environment.HairDebrisManager.Instance != null)
            {
                int debrisPenalty = Environment.HairDebrisManager.Instance.GetRemainingCount();
                totalReview -= debrisPenalty;
            }
            
            // Apply placement item review percent boost (e.g., 5% boost)
            if (ShopManager.Instance != null && ShopManager.Instance.ReviewPercentBoost > 0f)
            {
                totalReview = Mathf.RoundToInt(totalReview * (1f + ShopManager.Instance.ReviewPercentBoost));
            }
            
            return totalReview;
        }
        
        /// <summary>
        /// Get mood level from review value
        /// </summary>
        public MoodLevel GetMoodFromReview(int review)
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
        public int GetStarsFromMood(MoodLevel mood)
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
            UpdateCheckoutSuccessRateDisplay(itemId);
            
            Debug.Log($"[PaymentPanel] Added item: {itemId}, price: ${addedItemPrice}, reviewBonus: {addedItemReviewBonus}");
        }
        
        /// <summary>
        /// Update success rate display based on dropped item (checkout)
        /// </summary>
        private void UpdateCheckoutSuccessRateDisplay(string itemId)
        {
            if (successRateText == null || currentCustomer == null) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null)
            {
                successRateText.gameObject.SetActive(false);
                return;
            }
            
            // Calculate success rate using remaining budget after treatment fee
            // Formula: 2 - (UpsellPrice / (TotalBudget - TreatmentFee))
            float successRate = CalculateCheckoutUpsellRate(itemData.upsellPrice);
            int successPercent = Mathf.RoundToInt(successRate * 100f);
            
            // Display success rate
            successRateText.text = $"{successPercent}%";
            successRateText.gameObject.SetActive(true);
            
            // Color based on success rate
            if (successRate >= 0.8f)
                successRateText.color = Color.green;
            else if (successRate >= 0.5f)
                successRateText.color = Color.yellow;
            else
                successRateText.color = Color.red;
        }
        
        /// <summary>
        /// Calculate upsell success rate at checkout
        /// Formula: Clamp(2 - (UpsellPrice / (TotalBudget - TreatmentFee)), 0, 1)
        /// </summary>
        private float CalculateCheckoutUpsellRate(int upsellPrice)
        {
            if (currentCustomer == null) return 0f;
            
            // Free items always succeed (check this FIRST)
            if (upsellPrice <= 0) return 1f;
            
            int totalBudget = currentCustomer.GetTotalBudget();
            int remainingBudget = totalBudget - treatmentFee;
            
            if (remainingBudget <= 0) return 0f;
            
            float rate = 2f - ((float)upsellPrice / remainingBudget);
            return Mathf.Clamp01(rate);
        }
        
        /// <summary>
        /// Apply effects from checkout item (attraction boosts, etc.)
        /// </summary>
        private void ApplyCheckoutItemEffects(ItemData itemData)
        {
            if (itemData == null || itemData.effects == null || itemData.effects.Count == 0) return;
            
            var ctx = EffectContext.CreateForRegister();
            EffectHelper.ApplyEffects(itemData, ctx);
            
            // Apply attraction boost to CustomerSpawner
            var spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                if (ctx.AttractionBoost > 0f)
                {
                    spawner.AddAttractionBoost(ctx.AttractionBoost);
                }
                if (ctx.NextDayAttractionBoost > 0f)
                {
                    spawner.AddNextDayAttractionBoost(ctx.NextDayAttractionBoost);
                }
            }
            
            Debug.Log($"[PaymentPanel] Applied checkout effects: AttractionBoost={ctx.AttractionBoost}, NextDay={ctx.NextDayAttractionBoost}");
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
            
            // Hide success rate when item is cleared
            if (successRateText != null)
            {
                successRateText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Confirm payment button handler
        /// </summary>
        private void OnConfirmPayment()
        {
            if (currentCustomer == null) return;
            var data = currentCustomer.data;
            
            // Stop waiting timer and hide gauge - payment confirmed
            currentCustomer.StopWaiting();
            
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
            
            // Upsell judgment for checkout item - MUST happen before finalReview calculation
            bool checkoutUpsellSucceeded = false;
            if (!string.IsNullOrEmpty(addedItemId))
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(addedItemId);
                if (itemData != null && itemData.upsellPrice > 0)
                {
                    // Use checkout-specific formula: 2 - (UpsellPrice / (TotalBudget - TreatmentFee))
                    float successRate = CalculateCheckoutUpsellRate(itemData.upsellPrice);
                    float roll = Random.value;
                    checkoutUpsellSucceeded = roll <= successRate;
                    
                    Debug.Log($"[PaymentPanel] Checkout upsell attempt: {addedItemId}, Rate: {successRate:P0}, Roll: {roll:F2}, Success: {checkoutUpsellSucceeded}");
                    
                    if (checkoutUpsellSucceeded)
                    {
                        // Success: consume budget, apply effects
                        currentCustomer.ConsumeAdditionalBudget(itemData.upsellPrice);
                        ApplyCheckoutItemEffects(itemData);
                    }
                    else
                    {
                        // Failure: apply review penalty BEFORE calculating finalReview
                        int penalty = currentCustomer.CalculateUpsellFailurePenalty(successRate);
                        data.reviewPenalty += penalty;
                        Debug.Log($"[PaymentPanel] Checkout upsell failed! Review penalty: {penalty}");
                    }
                }
                else if (itemData != null)
                {
                    // Item with no upsell price - auto success
                    ApplyCheckoutItemEffects(itemData);
                    checkoutUpsellSucceeded = true;
                }
            }
            
            // Calculate final review AFTER upsell judgment (includes penalty if failed)
            int finalReview = CalculateCurrentReview();
            
            // Only charge addedItemPrice if upsell succeeded
            int totalAmount = treatmentFee + (checkoutUpsellSucceeded ? addedItemPrice : 0);
            
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
                
                // Record for daily attraction update
                var spawner = FindObjectOfType<CustomerSpawner>();
                spawner?.RecordDailyReview(finalReview);
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
        /// Staff upsell: Find and consume checkout item with 80%+ success rate
        /// Uses checkout-specific formula: 2 - (price / (totalBudget - treatmentFee))
        /// </summary>
        public (string itemId, int price, int reviewBonus, float successRate)? ConsumeHighSuccessRateCheckoutItem(CustomerController customer, int treatmentFee)
        {
            if (checkoutItemSlots == null || checkoutItemSlots.Length == 0 || customer == null) return null;
            
            const float MIN_SUCCESS_RATE = 0.8f;
            
            // Calculate remaining budget for checkout upsell
            int totalBudget = customer.GetTotalBudget();
            int remainingBudget = totalBudget - treatmentFee;
            
            if (remainingBudget <= 0)
            {
                Debug.Log("[PaymentPanel] Staff: No remaining budget for checkout upsell");
                return null;
            }
            
            // Collect items with 80%+ success rate
            var eligibleItems = new System.Collections.Generic.List<(CheckoutItemSlotUI slot, Core.ItemData itemData, float successRate)>();
            
            foreach (var slot in checkoutItemSlots)
            {
                if (slot == null || slot.IsEmpty) continue;
                
                var itemData = Core.ItemDataRegistry.Instance?.GetItem(slot.ItemId);
                if (itemData == null) continue;
                
                // Calculate success rate using checkout formula
                float successRate;
                if (itemData.upsellPrice <= 0)
                {
                    successRate = 1f; // Free items always succeed
                }
                else
                {
                    successRate = 2f - ((float)itemData.upsellPrice / remainingBudget);
                    successRate = Mathf.Clamp01(successRate);
                }
                
                if (successRate >= MIN_SUCCESS_RATE)
                {
                    eligibleItems.Add((slot, itemData, successRate));
                }
            }
            
            if (eligibleItems.Count == 0)
            {
                Debug.Log("[PaymentPanel] Staff: No checkout items with 80%+ success rate available");
                return null;
            }
            
            // Pick random from eligible items
            int randomIndex = UnityEngine.Random.Range(0, eligibleItems.Count);
            var selected = eligibleItems[randomIndex];
            
            // Consume one item
            selected.slot.RemoveOne();
            
            Debug.Log($"[PaymentPanel] Staff selected checkout item: {selected.itemData.itemId}, price: ${selected.itemData.upsellPrice}, successRate: {selected.successRate:P0}");
            
            return (selected.itemData.itemId, selected.itemData.upsellPrice, selected.itemData.reviewBonus, selected.successRate);
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
