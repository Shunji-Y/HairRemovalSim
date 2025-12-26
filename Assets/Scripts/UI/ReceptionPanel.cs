using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HairRemovalSim.Customer;
using HairRemovalSim.Player;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Reception panel for customer consultation before treatment
    /// </summary>
    public class ReceptionPanel : MonoBehaviour
    {
        public static ReceptionPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Customer Info (Right Side)")]
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text toleranceText;
        
        [Header("Body Part Buttons (Legacy 7-toggle)")]
        [SerializeField] private Toggle armsToggle;
        [SerializeField] private Toggle armpitsToggle;
        [SerializeField] private Toggle legsToggle;
        [SerializeField] private Toggle chestToggle;
        [SerializeField] private Toggle absToggle;
        [SerializeField] private Toggle beardToggle;
        [SerializeField] private Toggle backToggle;
        
        [Header("Body Part Toggles (New 14-part visual system)")]
        [SerializeField] private BodyPartToggleUI[] bodyPartToggles;
        [SerializeField] private Core.BodyPartsDatabase bodyPartsDatabase;
        
        [Header("Extra Items")]
        [SerializeField] private ExtraItemDropTarget extraItemDropTarget;
        [SerializeField] private ExtraItemSlotUI[] extraItemSlots;
        
        [Header("Price & Confirm")]
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button confirmButton;
        
        [Header("Upsell Display")]
        [SerializeField] private TMP_Text additionalBudgetText;
        [SerializeField] private TMP_Text successRateText;
        
        [Header("References")]
        [SerializeField] private TreatmentPriceTable priceTable;
        
        // Current state
        private CustomerController currentCustomer;
        private TreatmentBodyPart selectedParts = TreatmentBodyPart.None;
        private int calculatedPrice = 0;
        private HashSet<string> selectedDetailedParts = new HashSet<string>();
        private HashSet<string> requestedDetailedParts = new HashSet<string>();
        private bool useNewToggleSystem = false;
        
        // Callbacks
        private System.Action<CustomerController, TreatmentBodyPart, TreatmentMachine, bool, int> onConfirm;
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            
            // Check which system to use
            useNewToggleSystem = bodyPartToggles != null && bodyPartToggles.Length > 0;
            
            // Setup legacy button listeners
            if (armsToggle != null) armsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Arms, v));
            if (armpitsToggle != null) armpitsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Armpits, v));
            if (legsToggle != null) legsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Legs, v));
            if (chestToggle != null) chestToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Chest, v));
            if (absToggle != null) absToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Abs, v));
            if (beardToggle != null) beardToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Beard, v));
            if (backToggle != null) backToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Back, v));
            
            // Setup new toggle system listeners
            if (useNewToggleSystem)
            {
                foreach (var toggle in bodyPartToggles)
                {
                    if (toggle != null)
                    {
                        toggle.OnSelectionChanged += OnBodyPartToggleChanged;
                    }
                }
            }
            
            // Extra item drop target callbacks
            if (extraItemDropTarget != null)
            {
                extraItemDropTarget.OnItemSet = OnExtraItemSet;
                extraItemDropTarget.OnItemCleared = OnExtraItemCleared;
            }
            
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            // Close on ESC or right-click (cancel, not confirm)
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                Cancel();
            }
        }
        
        /// <summary>
        /// Show the reception panel for a customer
        /// </summary>
        public void Show(CustomerController customer, System.Action<CustomerController, TreatmentBodyPart, TreatmentMachine, bool, int> confirmCallback)
        {
            currentCustomer = customer;
            onConfirm = confirmCallback;
            
            if (panel != null) panel.SetActive(true);
            
            // Reset selections
            ResetSelections();
            
            // Setup new toggle system if available
            if (useNewToggleSystem && customer != null)
            {
                SetupBodyPartToggles(customer.data.requestPlan);
            }
            
            // Display customer info
            DisplayCustomerInfo();
            
            // Sync extra item slots with warehouse stock
            SyncExtraItemSlots();
            
            // Initial price calculation
            RecalculatePrice();
            
            // Display total budget (plan price + additional budget)
            if (additionalBudgetText != null && customer != null)
            {
                additionalBudgetText.text = $"${customer.GetTotalBudget()}";
            }
            
            // Hide success rate (no item dropped yet)
            if (successRateText != null)
            {
                successRateText.gameObject.SetActive(false);
            }
            
            // Show cursor (don't pause game)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        /// <summary>
        /// Cancel reception (ESC/right-click) - just close panel, keep DropItem
        /// </summary>
        public void Cancel()
        {
            Debug.Log($"[ReceptionPanel] Cancelled for {currentCustomer?.data?.customerName ?? "NULL"}");
            
            // Resume waiting timer from current value - reception was cancelled
            if (currentCustomer != null)
            {
                currentCustomer.ResumeWaiting();
            }
            
            // Don't clear dropTarget - keep item there for next open
            
            if (panel != null) panel.SetActive(false);
            currentCustomer = null;
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        /// <summary>
        /// Hide after confirm - item was consumed, don't return to stock
        /// </summary>
        public void Hide()
        {
            // Just clear display without returning item to slot (item was used)
            if (extraItemDropTarget != null)
            {
                extraItemDropTarget.ClearWithoutReturn();
            }
            
            if (panel != null) panel.SetActive(false);
            currentCustomer = null;
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void ResetSelections()
        {
            selectedParts = TreatmentBodyPart.None;
            selectedDetailedParts.Clear();
            requestedDetailedParts.Clear();
            
            // Reset legacy toggles WITHOUT triggering events
            if (armsToggle != null) armsToggle.SetIsOnWithoutNotify(false);
            if (armpitsToggle != null) armpitsToggle.SetIsOnWithoutNotify(false);
            if (legsToggle != null) legsToggle.SetIsOnWithoutNotify(false);
            if (chestToggle != null) chestToggle.SetIsOnWithoutNotify(false);
            if (absToggle != null) absToggle.SetIsOnWithoutNotify(false);
            if (beardToggle != null) beardToggle.SetIsOnWithoutNotify(false);
            if (backToggle != null) backToggle.SetIsOnWithoutNotify(false);
            
            // Reset new toggle system
            if (useNewToggleSystem && bodyPartToggles != null)
            {
                foreach (var toggle in bodyPartToggles)
                {
                    if (toggle != null) toggle.Reset();
                }
            }
            
            // Don't clear extra item drop target - keep it across panel opens
            
            if (confirmButton != null) confirmButton.interactable = false;
        }
        
        private void DisplayCustomerInfo()
        {
            if (currentCustomer == null) return;
            var data = currentCustomer.data;
            
            if (targetText != null)
                targetText.text = CustomerPlanHelper.GetRequiredPartsDisplay(data.requestPlan);
            
            if (toleranceText != null)
                toleranceText.text = data.painToleranceLevel.ToString();
        }
        
        private void OnPartToggled(TreatmentBodyPart part, bool isOn)
        {
            Debug.Log($"[ReceptionPanel] OnPartToggled: {part} = {isOn}, before selectedParts = {selectedParts}");
            
            if (isOn)
                selectedParts |= part;
            else
                selectedParts &= ~part;
            
            Debug.Log($"[ReceptionPanel] OnPartToggled: after selectedParts = {selectedParts}");
            RecalculatePrice();
        }
        
        private void OnExtraItemSet(string itemId)
        {
            RecalculatePrice();
            UpdateSuccessRateDisplay(itemId);
            Debug.Log($"[ReceptionPanel] Extra item set: {itemId}");
        }
        
        private void OnExtraItemCleared()
        {
            RecalculatePrice();
            
            // Hide success rate when no item is dropped
            if (successRateText != null)
            {
                successRateText.gameObject.SetActive(false);
            }
            
            Debug.Log($"[ReceptionPanel] Extra item cleared, price recalculated");
        }
        
        /// <summary>
        /// Update success rate display based on dropped item
        /// </summary>
        private void UpdateSuccessRateDisplay(string itemId)
        {
            if (successRateText == null || currentCustomer == null) return;
            
            var itemData = Core.ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null)
            {
                successRateText.gameObject.SetActive(false);
                return;
            }
            
            // Calculate success rate
            float successRate = currentCustomer.CalculateUpsellSuccessRate(itemData.upsellPrice);
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
        
        private void RecalculatePrice()
        {
            if (currentCustomer == null) return;
            
            // Base price from plan
            int planPrice = CustomerPlanHelper.GetPlanPrice(currentCustomer.data.requestPlan);
            
            // Add extra item upsell price if set
            int extraItemPrice = 0;
            if (extraItemDropTarget != null && extraItemDropTarget.HasItem)
            {
                var itemData = Core.ItemDataRegistry.Instance?.GetItem(extraItemDropTarget.ItemId);
                if (itemData != null)
                {
                    extraItemPrice = itemData.upsellPrice;
                }
            }
            
            calculatedPrice = planPrice + extraItemPrice;
            
            // Display price with upsell increment if applicable
            if (priceText != null)
            {
                if (extraItemPrice > 0)
                {
                    // Show: "$55 (↑$20)"
                    priceText.text = $"${calculatedPrice} (<color=green>↑${extraItemPrice})";
                }
                else
                {
                    priceText.text = $"${calculatedPrice}";
                }
            }
            
            // Enable confirm button if at least one part is selected
            if (confirmButton != null)
                confirmButton.interactable = selectedParts != TreatmentBodyPart.None;
        }
        
        private void OnConfirmClicked()
        {
            if (currentCustomer == null) return;
            
            // Stop waiting timer and hide gauge - confirmed
            currentCustomer.StopWaiting();
            
            var data = currentCustomer.data;
            bool hasExtraItem = extraItemDropTarget != null && extraItemDropTarget.HasItem;
            string extraItemId = hasExtraItem ? extraItemDropTarget.ItemId : null;
            
            // Upsell judgment if extra item is set
            bool upsellAttempted = false;
            bool upsellSucceeded = false;
            int upsellPrice = 0;
            
            if (hasExtraItem && !string.IsNullOrEmpty(extraItemId))
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(extraItemId);
                if (itemData != null && itemData.upsellPrice > 0)
                {
                    upsellAttempted = true;
                    upsellPrice = itemData.upsellPrice;
                    float successRate = currentCustomer.CalculateUpsellSuccessRate(itemData.upsellPrice);
                    float roll = Random.value;
                    upsellSucceeded = roll <= successRate;
                    
                    Debug.Log($"[ReceptionPanel] Upsell attempt: {extraItemId}, Rate: {successRate:P0}, Roll: {roll:F2}, Success: {upsellSucceeded}");
                    
                    if (upsellSucceeded)
                    {
                        // Success: apply effects (budget NOT consumed at reception, only at checkout)
                        currentCustomer.ApplyReceptionEffects(itemData);
                    }
                    else
                    {
                        // Failure: apply review penalty, but item is still consumed
                        int penalty = currentCustomer.CalculateUpsellFailurePenalty(successRate);
                        data.reviewPenalty += penalty;
                        Debug.Log($"[ReceptionPanel] Upsell failed! Review penalty: {penalty}");
                    }
                }
                else if (itemData != null)
                {
                    // Item with no upsell price - auto success, just apply effects
                    currentCustomer.ApplyReceptionEffects(itemData);
                    upsellSucceeded = true;
                }
            }
            
            // Legacy: Check if extra item is anesthesia cream
            bool useAnesthesia = extraItemId == "anesthesia_cream";
            data.useAnesthesiaCream = useAnesthesia;
            
            // Save confirmed selections to customer data
            data.confirmedParts = selectedParts;
            data.confirmedMachine = TreatmentMachine.Shaver; // Default, no toggle now
            
            // Only include upsell price if upsell succeeded
            int basePlanPrice = CustomerPlanHelper.GetPlanPrice(data.requestPlan);
            data.confirmedPrice = basePlanPrice + (upsellSucceeded ? upsellPrice : 0);
            
            Debug.Log($"[ReceptionPanel] Confirmed - Parts: {selectedParts}, Price: ${data.confirmedPrice} (upsell: {upsellSucceeded})");
            
            // Invoke callback
            onConfirm?.Invoke(currentCustomer, selectedParts, TreatmentMachine.Shaver, useAnesthesia, data.confirmedPrice);
            
            Hide();
        }
        
        /// <summary>
        /// Sync extra item slots with warehouse reception stock
        /// </summary>
        private void SyncExtraItemSlots()
        {
            // This will be synced from ReceptionStockSlotUI via Warehouse
            // For now, just refresh display
        }
        
        /// <summary>
        /// Set item in an extra item slot (called from ReceptionStockSlotUI sync)
        /// </summary>
        public void SetExtraItemSlot(int index, string itemId, int quantity)
        {
            if (extraItemSlots != null && index >= 0 && index < extraItemSlots.Length)
            {
                extraItemSlots[index].SetItem(itemId, quantity);
            }
        }
        
        /// <summary>
        /// Get extra item slots for syncing
        /// </summary>
        public ExtraItemSlotUI[] GetExtraItemSlots() => extraItemSlots;
        
        /// <summary>
        /// Generate a negative review when customer leaves at reception
        /// </summary>
        private void GenerateReceptionLeaveReview(CustomerController customer)
        {
            if (customer == null || customer.data == null) return;
            
            var shopManager = Core.ShopManager.Instance;
            if (shopManager == null)
            {
                Debug.LogWarning("[ReceptionPanel] ShopManager not found, cannot generate review");
                return;
            }
            
            var data = customer.data;
            
            // Calculate review value: base value + penalty (penalty is already negative)
            // e.g. baseReview=30, penalty=-50 → reviewValue=-20 → 1 star
            int baseReview = customer.GetBaseReviewValue();
            int reviewValue = baseReview + data.reviewPenalty;
            
            // Convert review value (-50 to 50) to stars (1-5)
            // -50~-20 = 1 star, -20~0 = 2 stars, 0~20 = 3 stars, 20~35 = 4 stars, 35~50 = 5 stars
            int stars;
            if (reviewValue <= -20)
                stars = 1;
            else if (reviewValue <= 0)
                stars = 2;
            else if (reviewValue <= 20)
                stars = 3;
            else if (reviewValue <= 35)
                stars = 4;
            else
                stars = 5;
            
            Debug.Log($"[ReceptionPanel] Generating reception leave review for {data.customerName}: base={baseReview}, penalty={data.reviewPenalty}, value={reviewValue}, stars={stars}");
            
            // Add review to ShopManager
            shopManager.AddCustomerReview(stars);
        }
        
        /// <summary>
        /// Consume a random item from extra item slots (for staff upsell)
        /// Returns the item ID and price, or null if no items available
        /// </summary>
        public (string itemId, int price)? ConsumeRandomExtraItem()
        {
            if (extraItemSlots == null || extraItemSlots.Length == 0) return null;
            
            // Collect non-empty slots
            var availableSlots = new System.Collections.Generic.List<ExtraItemSlotUI>();
            foreach (var slot in extraItemSlots)
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
            
            // Get item price
            int price = 0;
            var itemData = Core.ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData != null)
            {
                price = itemData.price;
            }
            
            // Consume one item
            selectedSlot.UseOne();
            
            Debug.Log($"[ReceptionPanel] Staff consumed extra item: {itemId}, price: ${price}");
            
            return (itemId, price);
        }
        
        #region New 14-part Toggle System
        
        /// <summary>
        /// Setup body part toggles based on customer's request plan
        /// </summary>
        private void SetupBodyPartToggles(CustomerRequestPlan plan)
        {
            if (bodyPartToggles == null) return;
            
            // Get detailed part names for this plan
            var requestedParts = CustomerPlanHelper.GetDetailedPartNamesForPlan(plan);
            requestedDetailedParts.Clear();
            foreach (var partName in requestedParts)
            {
                requestedDetailedParts.Add(partName);
            }
            
            // Setup each toggle
            foreach (var toggle in bodyPartToggles)
            {
                if (toggle == null) continue;
                
                bool isRequested = requestedDetailedParts.Contains(toggle.PartName);
                toggle.SetRequested(isRequested);
            }
        }
        
        /// <summary>
        /// Handle body part toggle changed event
        /// </summary>
        private void OnBodyPartToggleChanged(BodyPartToggleUI toggle, bool isSelected)
        {
            if (toggle == null) return;
            
            if (isSelected)
            {
                selectedDetailedParts.Add(toggle.PartName);
            }
            else
            {
                selectedDetailedParts.Remove(toggle.PartName);
            }
            
            // Convert detailed parts to TreatmentBodyPart flags for backward compatibility
            selectedParts = ConvertSelectionToBodyParts(selectedDetailedParts);
            
            Debug.Log($"[ReceptionPanel] Toggle changed: {toggle.PartName} = {isSelected}, selectedParts = {selectedParts}");
            
            RecalculatePrice();
        }
        
        /// <summary>
        /// Convert 14-part selection to 7-part TreatmentBodyPart flags
        /// </summary>
        private TreatmentBodyPart ConvertSelectionToBodyParts(HashSet<string> detailedParts)
        {
            var result = TreatmentBodyPart.None;
            
            // Arms (any arm part)
            if (detailedParts.Contains("LeftUpperArm") || detailedParts.Contains("LeftLowerArm") ||
                detailedParts.Contains("RightUpperArm") || detailedParts.Contains("RightLowerArm"))
            {
                result |= TreatmentBodyPart.Arms;
            }
            
            // Armpits
            if (detailedParts.Contains("LeftArmpit") || detailedParts.Contains("RightArmpit"))
            {
                result |= TreatmentBodyPart.Armpits;
            }
            
            // Legs (any leg part)
            if (detailedParts.Contains("LeftThigh") || detailedParts.Contains("LeftCalf") ||
                detailedParts.Contains("RightThigh") || detailedParts.Contains("RightCalf"))
            {
                result |= TreatmentBodyPart.Legs;
            }
            
            // Single parts
            if (detailedParts.Contains("Chest")) result |= TreatmentBodyPart.Chest;
            if (detailedParts.Contains("Abs")) result |= TreatmentBodyPart.Abs;
            if (detailedParts.Contains("Beard")) result |= TreatmentBodyPart.Beard;
            if (detailedParts.Contains("Back")) result |= TreatmentBodyPart.Back;
            
            return result;
        }
        
        /// <summary>
        /// Check if all requested parts are selected
        /// </summary>
        private bool AreAllRequestedPartsSelected()
        {
            foreach (var part in requestedDetailedParts)
            {
                if (!selectedDetailedParts.Contains(part))
                    return false;
            }
            return requestedDetailedParts.Count > 0;
        }
        
        #endregion
    }
}

