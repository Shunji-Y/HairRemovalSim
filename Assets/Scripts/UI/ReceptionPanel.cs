using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Customer;
using HairRemovalSim.Player;

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
        
        [Header("Body Part Buttons (Left Side)")]
        [SerializeField] private Toggle armsToggle;
        [SerializeField] private Toggle armpitsToggle;
        [SerializeField] private Toggle legsToggle;
        [SerializeField] private Toggle chestToggle;
        [SerializeField] private Toggle absToggle;
        [SerializeField] private Toggle beardToggle;
        [SerializeField] private Toggle backToggle;
        
        [Header("Extra Items")]
        [SerializeField] private ExtraItemDropTarget extraItemDropTarget;
        [SerializeField] private ExtraItemSlotUI[] extraItemSlots;
        
        [Header("Price & Confirm")]
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button confirmButton;
        
        [Header("References")]
        [SerializeField] private TreatmentPriceTable priceTable;
        
        // Current state
        private CustomerController currentCustomer;
        private TreatmentBodyPart selectedParts = TreatmentBodyPart.None;
        private int calculatedPrice = 0;
        
        // Callbacks
        private System.Action<CustomerController, TreatmentBodyPart, TreatmentMachine, bool, int> onConfirm;
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            
            // Setup button listeners
            if (armsToggle != null) armsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Arms, v));
            if (armpitsToggle != null) armpitsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Armpits, v));
            if (legsToggle != null) legsToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Legs, v));
            if (chestToggle != null) chestToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Chest, v));
            if (absToggle != null) absToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Abs, v));
            if (beardToggle != null) beardToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Beard, v));
            if (backToggle != null) backToggle.onValueChanged.AddListener(v => OnPartToggled(TreatmentBodyPart.Back, v));
            
            // Extra item drop target callback
            if (extraItemDropTarget != null) extraItemDropTarget.OnItemSet = OnExtraItemSet;
            
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
            
            // Display customer info
            DisplayCustomerInfo();
            
            // Sync extra item slots with warehouse stock
            SyncExtraItemSlots();
            
            // Initial price calculation
            RecalculatePrice();
            
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
            
            // Reset all toggles WITHOUT triggering events
            if (armsToggle != null) armsToggle.SetIsOnWithoutNotify(false);
            if (armpitsToggle != null) armpitsToggle.SetIsOnWithoutNotify(false);
            if (legsToggle != null) legsToggle.SetIsOnWithoutNotify(false);
            if (chestToggle != null) chestToggle.SetIsOnWithoutNotify(false);
            if (absToggle != null) absToggle.SetIsOnWithoutNotify(false);
            if (beardToggle != null) beardToggle.SetIsOnWithoutNotify(false);
            if (backToggle != null) backToggle.SetIsOnWithoutNotify(false);
            
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
            Debug.Log($"[ReceptionPanel] Extra item set: {itemId}");
        }
        
        private void RecalculatePrice()
        {
            if (currentCustomer == null) return;
            
            // Base price from plan
            int planPrice = CustomerPlanHelper.GetPlanPrice(currentCustomer.data.requestPlan);
            
            // Add extra item price if set
            int extraItemPrice = 0;
            if (extraItemDropTarget != null && extraItemDropTarget.HasItem)
            {
                var itemData = Core.ItemDataRegistry.Instance?.GetItem(extraItemDropTarget.ItemId);
                if (itemData != null)
                {
                    extraItemPrice = itemData.price;
                }
            }
            
            calculatedPrice = planPrice + extraItemPrice;
            
            if (priceText != null)
                priceText.text = $"${calculatedPrice}";
            
            // Enable confirm button if at least one part is selected
            if (confirmButton != null)
                confirmButton.interactable = selectedParts != TreatmentBodyPart.None;
        }
        
        private void OnConfirmClicked()
        {
            if (currentCustomer == null) return;
            
            var data = currentCustomer.data;
            bool hasExtraItem = extraItemDropTarget != null && extraItemDropTarget.HasItem;
            string extraItemId = hasExtraItem ? extraItemDropTarget.ItemId : null;
            
            // Check if extra item is anesthesia cream
            bool useAnesthesia = extraItemId == "anesthesia_cream";
            data.useAnesthesiaCream = useAnesthesia;
            
            // Save confirmed selections to customer data
            data.confirmedParts = selectedParts;
            data.confirmedMachine = TreatmentMachine.Shaver; // Default, no toggle now
            data.confirmedPrice = calculatedPrice;
            
            Debug.Log($"[ReceptionPanel] Confirmed - Parts: {selectedParts}, Price: ${calculatedPrice}");
            
            // Invoke callback
            onConfirm?.Invoke(currentCustomer, selectedParts, TreatmentMachine.Shaver, useAnesthesia, calculatedPrice);
            
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
    }
}

