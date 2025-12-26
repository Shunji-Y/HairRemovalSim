using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Reception desk stock slot in Warehouse UI
    /// Accepts drops from warehouse slots and syncs with ReceptionPanel ExtraItemSlots
    /// Can drag items back to warehouse
    /// </summary>
    public class ReceptionStockSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        [Header("Sync Settings")]
        [Tooltip("Index of ExtraItemSlot in ReceptionPanel to sync with")]
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
        /// Set from ExtraItemSlotUI (called when extra item slot changes)
        /// </summary>
        public void SetFromExtraItem(string id, int qty)
        {
            itemId = id;
            quantity = qty;
            RefreshDisplay();
            Debug.Log($"[ReceptionStockSlotUI] Synced from extra item: {id} x{qty}");
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Check for drop from same type (ReceptionStockSlotUI)
            var sameTypeSource = eventData.pointerDrag?.GetComponent<ReceptionStockSlotUI>();
            if (sameTypeSource != null && sameTypeSource != this && !sameTypeSource.IsEmpty)
            {
                HandleSameSlotDrop(sameTypeSource);
                return;
            }
            
            // Check for drop from WarehouseSlotUI
            var warehouseSource = eventData.pointerDrag?.GetComponent<WarehouseSlotUI>();
            if (warehouseSource == null || warehouseSource.IsEmpty) return;
            
            string dropItemId = warehouseSource.ItemId;
            
            // Check if item can be placed at reception
            var itemData = ItemDataRegistry.Instance?.GetItem(dropItemId);
            if (itemData == null || !itemData.canPlaceAtReception)
            {
                Debug.Log($"[ReceptionStockSlotUI] Item '{dropItemId}' cannot be placed at reception (canPlaceAtReception=false)");
                return;
            }
            
            // If slot has different item, reject
            if (!IsEmpty && itemId != dropItemId)
            {
                Debug.Log($"[ReceptionStockSlotUI] Slot already has {itemId}, cannot add {dropItemId}");
                return;
            }
            
            // Apply max stack limit
            int maxStack = itemData.maxStackOnShelf;
            int currentQty = IsEmpty ? 0 : quantity;
            int space = maxStack - currentQty;
            
            if (space <= 0)
            {
                Debug.Log($"[ReceptionStockSlotUI] Slot is full (max {maxStack})");
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
            
            // Sync with ReceptionPanel ExtraItemSlots
            SyncWithReceptionPanel();
            
            Debug.Log($"[ReceptionStockSlotUI] Added {toMove}x {itemId}. Total: {quantity} (max: {maxStack})");
        }
        
        /// <summary>
        /// Handle drop from another ReceptionStockSlotUI
        /// </summary>
        private void HandleSameSlotDrop(ReceptionStockSlotUI source)
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
                source.SyncWithReceptionPanel();
                
                RefreshDisplay();
                SyncWithReceptionPanel();
                
                Debug.Log($"[ReceptionStockSlotUI] Moved {qty}x {dropItemId} from another slot");
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
                source.SyncWithReceptionPanel();
                
                RefreshDisplay();
                SyncWithReceptionPanel();
                
                Debug.Log($"[ReceptionStockSlotUI] Swapped items between slots");
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
        /// Sync this slot's data with ReceptionPanel's ExtraItemSlot
        /// </summary>
        private void SyncWithReceptionPanel()
        {
            if (ReceptionPanel.Instance != null)
            {
                ReceptionPanel.Instance.SetExtraItemSlot(syncSlotIndex, itemId, quantity);
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
                SyncWithReceptionPanel();
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
            SyncWithReceptionPanel();
        }
        
        /// <summary>
        /// Sync this stock slot FROM ReceptionPanel's ExtraItemSlot (called when warehouse panel opens)
        /// This ensures Stock reflects the current state of Reception after operations
        /// </summary>
        public void SyncFromReceptionPanel()
        {
            if (ReceptionPanel.Instance != null)
            {
                var slots = ReceptionPanel.Instance.GetExtraItemSlots();
                if (slots != null && syncSlotIndex >= 0 && syncSlotIndex < slots.Length)
                {
                    var extraSlot = slots[syncSlotIndex];
                    if (extraSlot != null)
                    {
                        itemId = extraSlot.ItemId;
                        quantity = extraSlot.Quantity;
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
            SyncWithReceptionPanel();
            
            Debug.Log($"[ReceptionStockSlotUI] Transferred to warehouse slot {targetSlot.SlotIndex}");
        }
        
        #endregion
    }
}
