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
        [SerializeField] private TMP_Text budgetText;
        
        [Header("Body Part Buttons (Left Side)")]
        [SerializeField] private Toggle armsToggle;
        [SerializeField] private Toggle armpitsToggle;
        [SerializeField] private Toggle legsToggle;
        [SerializeField] private Toggle chestToggle;
        [SerializeField] private Toggle absToggle;
        [SerializeField] private Toggle beardToggle;
        [SerializeField] private Toggle backToggle;
        
        [Header("Machine Selection")]
        [SerializeField] private Toggle shaverToggle;
        [SerializeField] private Toggle laserToggle;
        
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
        private TreatmentMachine selectedMachine = TreatmentMachine.Shaver;
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
            
            if (shaverToggle != null) shaverToggle.onValueChanged.AddListener(v => { if (v) OnMachineSelected(TreatmentMachine.Shaver); });
            if (laserToggle != null) laserToggle.onValueChanged.AddListener(v => { if (v) OnMachineSelected(TreatmentMachine.Laser); });
            
            // Extra item drop target callback
            if (extraItemDropTarget != null) extraItemDropTarget.OnItemSet = OnExtraItemSet;
            
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            // Close on ESC or right-click
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                Hide();
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
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            currentCustomer = null;
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void ResetSelections()
        {
            selectedParts = TreatmentBodyPart.None;
            selectedMachine = TreatmentMachine.Shaver;
            
            // Reset all toggles WITHOUT triggering events
            if (armsToggle != null) armsToggle.SetIsOnWithoutNotify(false);
            if (armpitsToggle != null) armpitsToggle.SetIsOnWithoutNotify(false);
            if (legsToggle != null) legsToggle.SetIsOnWithoutNotify(false);
            if (chestToggle != null) chestToggle.SetIsOnWithoutNotify(false);
            if (absToggle != null) absToggle.SetIsOnWithoutNotify(false);
            if (beardToggle != null) beardToggle.SetIsOnWithoutNotify(false);
            if (backToggle != null) backToggle.SetIsOnWithoutNotify(false);
            
            if (shaverToggle != null) shaverToggle.SetIsOnWithoutNotify(true);
            if (laserToggle != null) laserToggle.SetIsOnWithoutNotify(false);
            
            // Clear extra item drop target
            if (extraItemDropTarget != null) extraItemDropTarget.Clear();
            
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
            
            if (budgetText != null)
                budgetText.text = $"${data.baseBudget}";
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
        
        private void OnMachineSelected(TreatmentMachine machine)
        {
            Debug.Log($"[ReceptionPanel] OnMachineSelected: {machine}, before selectedMachine = {selectedMachine}");
            selectedMachine = machine;
            Debug.Log($"[ReceptionPanel] OnMachineSelected: after selectedMachine = {selectedMachine}, selectedParts = {selectedParts}");
            RecalculatePrice();
        }
        
        private void OnExtraItemSet(string itemId)
        {
            RecalculatePrice();
            Debug.Log($"[ReceptionPanel] Extra item set: {itemId}");
        }
        
        private void RecalculatePrice()
        {
            if (priceTable == null || currentCustomer == null) return;
            
            bool hasExtraItem = extraItemDropTarget != null && extraItemDropTarget.HasItem;
            calculatedPrice = priceTable.CalculatePrice(selectedParts, selectedMachine, hasExtraItem);
            
            if (priceText != null)
                priceText.text = $"${calculatedPrice}";
            
            // Enable confirm button if at least one part is selected
            if (confirmButton != null)
                confirmButton.interactable = selectedParts != TreatmentBodyPart.None;
        }
        
        private void OnConfirmClicked()
        {
            if (currentCustomer == null || priceTable == null) return;
            
            var data = currentCustomer.data;
            bool hasExtraItem = extraItemDropTarget != null && extraItemDropTarget.HasItem;
            string extraItemId = hasExtraItem ? extraItemDropTarget.ItemId : null;
            
            // Check if extra item is anesthesia cream
            bool useAnesthesia = extraItemId == "anesthesia_cream";
            
            // Calculate penalty
            int penalty = priceTable.CalculatePenalty(selectedParts, data.requestPlan, calculatedPrice, data.baseBudget);
            data.reviewPenalty += penalty;
            data.useAnesthesiaCream = useAnesthesia;
            
            // Check if customer should leave
            if (priceTable.ShouldCustomerLeave(data.reviewPenalty))
            {
                Debug.Log($"[ReceptionPanel] Customer {data.customerName} leaving due to penalty: {data.reviewPenalty}");
                currentCustomer.LeaveShop();
                Hide();
                return;
            }
            
            // Save confirmed selections to customer data
            data.confirmedParts = selectedParts;
            data.confirmedMachine = selectedMachine;
            
            Debug.Log($"[ReceptionPanel] Confirmed - Parts: {selectedParts}, Machine: {selectedMachine}, Anesthesia: {useAnesthesia}");
            
            // Invoke callback
            onConfirm?.Invoke(currentCustomer, selectedParts, selectedMachine, useAnesthesia, calculatedPrice);
            
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
    }
}

