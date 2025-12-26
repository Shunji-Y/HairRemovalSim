using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Checkout stock slot in Warehouse UI.
    /// Accepts drops from warehouse slots and syncs with PaymentPanel/CheckoutItemSlotUI.
    /// Can drag items back to warehouse.
    /// </summary>
    public class CheckoutStockSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        [Header("Sync Settings")]
        [Tooltip("Index of CheckoutItemSlotUI to sync with")]
        [SerializeField] private int syncSlotIndex = 0;
        
        // Item data
        private string itemId;
        private int quantity;
        
        public string ItemId => itemId;
        public int Quantity => quantity;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
        public int SyncSlotIndex => syncSlotIndex;
        
        private void Start()
        {
            RefreshDisplay();
        }
        
        /// <summary>
        /// Set from CheckoutItemSlotUI (called when checkout changes item counts)
        /// </summary>
        public void SetFromCheckout(string id, int qty)
        {
            itemId = id;
            quantity = qty;
            RefreshDisplay();
            Debug.Log($"[CheckoutStockSlotUI] Synced from checkout: {id} x{qty}");
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Check for drop from same type (CheckoutStockSlotUI)
            var sameTypeSource = eventData.pointerDrag?.GetComponent<CheckoutStockSlotUI>();
            if (sameTypeSource != null && sameTypeSource != this && !sameTypeSource.IsEmpty)
            {
                HandleSameSlotDrop(sameTypeSource);
                return;
            }
            
            // Check for drop from WarehouseSlotUI
            var warehouseSource = eventData.pointerDrag?.GetComponent<WarehouseSlotUI>();
            if (warehouseSource == null || warehouseSource.IsEmpty) return;
            
            string dropItemId = warehouseSource.ItemId;
            
            // Check if item can be used at checkout
            var itemData = ItemDataRegistry.Instance?.GetItem(dropItemId);
            if (itemData == null || !itemData.canUseAtCheckout)
            {
                Debug.Log($"[CheckoutStockSlotUI] Item '{dropItemId}' cannot be used at checkout (canUseAtCheckout=false)");
                return;
            }
            
            // If slot has different item, reject
            if (!IsEmpty && itemId != dropItemId)
            {
                Debug.Log($"[CheckoutStockSlotUI] Slot already has {itemId}, cannot add {dropItemId}");
                return;
            }
            
            // Apply max stack limit
            int maxStack = itemData.maxStackOnShelf;
            int currentQty = IsEmpty ? 0 : quantity;
            int space = maxStack - currentQty;
            
            if (space <= 0)
            {
                Debug.Log($"[CheckoutStockSlotUI] Slot is full (max {maxStack})");
                return;
            }
            
            // Transfer from warehouse (respecting max stack)
            int warehouseQty = warehouseSource.Quantity;
            int toMove = Mathf.Min(warehouseQty, space);
            
            itemId = dropItemId;
            quantity = currentQty + toMove;
            
            // Remove from warehouse
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.RemoveFromSlot(warehouseSource.SlotIndex, toMove);
            }
            
            warehouseSource.RefreshFromWarehouse();
            RefreshDisplay();
            
            // Sync with CheckoutItemSlotUI
            SyncWithCheckoutPanel();
            
            Debug.Log($"[CheckoutStockSlotUI] Added {toMove}x {itemId}. Total: {quantity} (max: {maxStack})");
        }
        
        /// <summary>
        /// Handle drop from another CheckoutStockSlotUI
        /// </summary>
        private void HandleSameSlotDrop(CheckoutStockSlotUI source)
        {
            string dropItemId = source.ItemId;
            int qty = source.Quantity;
            
            // If this slot is empty or has same item, merge
            if (IsEmpty || itemId == dropItemId)
            {
                itemId = dropItemId;
                quantity += qty;
                
                // Clear source
                source.itemId = null;
                source.quantity = 0;
                source.RefreshDisplay();
                source.SyncWithCheckoutPanel();
                
                RefreshDisplay();
                SyncWithCheckoutPanel();
                
                Debug.Log($"[CheckoutStockSlotUI] Moved {qty}x {dropItemId} from another slot");
            }
            // If different item, swap
            else
            {
                string tempId = itemId;
                int tempQty = quantity;
                
                itemId = dropItemId;
                quantity = qty;
                
                source.itemId = tempId;
                source.quantity = tempQty;
                
                source.RefreshDisplay();
                source.SyncWithCheckoutPanel();
                
                RefreshDisplay();
                SyncWithCheckoutPanel();
                
                Debug.Log($"[CheckoutStockSlotUI] Swapped items between slots");
            }
        }
        
        private void RefreshDisplay()
        {
            if (!IsEmpty)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                if (itemData != null && itemData.icon != null)
                {
                    iconImage.sprite = itemData.icon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
                quantityText.text = $"×{quantity}";
                quantityText.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
                quantityText.enabled = false;
            }
        }
        
        /// <summary>
        /// Sync this slot's data with PaymentPanel
        /// </summary>
        private void SyncWithCheckoutPanel()
        {
            if (PaymentPanel.Instance != null)
            {
                PaymentPanel.Instance.SetCheckoutItemSlot(syncSlotIndex, itemId, quantity);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;
            
            // Show item detail in panel
            if (!IsEmpty && WarehousePanel.Instance != null)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                if (itemData != null)
                {
                    WarehousePanel.Instance.ShowItemDetail(itemData);
                }
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
            
            // Hide item detail
            if (WarehousePanel.Instance != null)
            {
                WarehousePanel.Instance.HideItemDetail();
            }
        }
        
        /// <summary>
        /// Use stock (called when item is consumed)
        /// </summary>
        public bool UseStock(int amount = 1)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                RefreshDisplay();
                SyncWithCheckoutPanel();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clear the slot
        /// </summary>
        public void Clear()
        {
            itemId = null;
            quantity = 0;
            RefreshDisplay();
            SyncWithCheckoutPanel();
        }
        
        /// <summary>
        /// Sync this stock slot FROM CheckoutItemSlotUI (called when warehouse panel opens)
        /// This ensures Stock reflects the current state of Checkout after operations
        /// </summary>
        public void SyncFromCheckoutPanel()
        {
            if (PaymentPanel.Instance != null && PaymentPanel.Instance.CheckoutItemSlots != null)
            {
                var slots = PaymentPanel.Instance.CheckoutItemSlots;
                if (syncSlotIndex >= 0 && syncSlotIndex < slots.Length)
                {
                    var checkoutSlot = slots[syncSlotIndex];
                    if (checkoutSlot != null)
                    {
                        itemId = checkoutSlot.ItemId;
                        quantity = checkoutSlot.Quantity;
                        RefreshDisplay();
                    }
                }
            }
        }
        
        #region Drag to Warehouse
        
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null || itemData.icon == null) return;
            
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(rootCanvas.transform, false);
            
            var img = dragIcon.AddComponent<Image>();
            img.sprite = itemData.icon;
            img.raycastTarget = false;
            
            var rt = dragIcon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);
            
            iconImage.color = new Color(1, 1, 1, 0.5f);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon == null) return;
            
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out pos
            );
            dragIcon.transform.localPosition = pos;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragIcon != null)
            {
                Destroy(dragIcon);
                dragIcon = null;
            }
            
            iconImage.color = Color.white;
            
            // Check if dropped on warehouse slot
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                var warehouseSlot = result.gameObject.GetComponent<WarehouseSlotUI>();
                if (warehouseSlot != null)
                {
                    TransferToWarehouse(warehouseSlot);
                    break;
                }
            }
        }
        
        private void TransferToWarehouse(WarehouseSlotUI targetSlot)
        {
            if (IsEmpty || WarehouseManager.Instance == null) return;
            
            var slot = WarehouseManager.Instance.GetSlot(targetSlot.SlotIndex);
            if (slot == null) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return;
            
            int maxStack = itemData.maxWarehouseStack;
            
            // If target is empty or has same item
            if (slot.IsEmpty)
            {
                int toMove = Mathf.Min(quantity, maxStack);
                slot.itemId = itemId;
                slot.quantity = toMove;
                quantity -= toMove;
            }
            else if (slot.itemId == itemId)
            {
                int space = maxStack - slot.quantity;
                int toMove = Mathf.Min(quantity, space);
                slot.quantity += toMove;
                quantity -= toMove;
            }
            
            if (quantity <= 0)
            {
                itemId = null;
            }
            
            targetSlot.RefreshFromWarehouse();
            RefreshDisplay();
            SyncWithCheckoutPanel();
            
            Debug.Log($"[CheckoutStockSlotUI] Transferred to warehouse slot {targetSlot.SlotIndex}");
        }
        
        #endregion
    }
}
