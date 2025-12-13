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
    /// </summary>
    public class ReceptionStockSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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
        
        private void Start()
        {
            RefreshDisplay();
        }
        
        public void OnDrop(PointerEventData eventData)
        {
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
            
            // Transfer all from warehouse
            int qty = warehouseSource.Quantity;
            itemId = dropItemId;
            quantity += qty;
            
            // Remove from warehouse
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.RemoveFromSlot(warehouseSource.SlotIndex, qty);
            }
            
            warehouseSource.RefreshFromWarehouse();
            RefreshDisplay();
            
            // Sync with ReceptionPanel ExtraItemSlots
            SyncWithReceptionPanel();
            
            Debug.Log($"[ReceptionStockSlotUI] Added {qty}x {itemId}. Total: {quantity}");
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
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
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
    }
}
