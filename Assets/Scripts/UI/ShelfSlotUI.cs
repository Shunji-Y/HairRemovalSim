using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI representation of a single shelf slot. Supports drag and drop with warehouse.
    /// </summary>
    public class ShelfSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        // Linked shelf data
        private TreatmentShelf linkedShelf;
        private int row;
        private int col;
        
        // Current display data
        private string currentItemId;
        private int currentQuantity;
        
        // Drag state
        private static ShelfSlotUI dragSource;
        private static WarehouseSlotUI warehouseDragSource; // For cross-type drops
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public TreatmentShelf LinkedShelf => linkedShelf;
        public int Row => row;
        public int Col => col;
        public string ItemId => currentItemId;
        public int Quantity => currentQuantity;
        public bool IsEmpty => string.IsNullOrEmpty(currentItemId) || currentQuantity <= 0;
        
        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        public void Initialize(TreatmentShelf shelf, int slotRow, int slotCol)
        {
            linkedShelf = shelf;
            row = slotRow;
            col = slotCol;
            RefreshFromShelf();
        }
        
        /// <summary>
        /// Sync UI from actual TreatmentShelf data
        /// </summary>
        public void RefreshFromShelf()
        {
            if (linkedShelf == null)
            {
                ClearSlot();
                return;
            }
            
            var slotData = linkedShelf.GetSlotData(row, col);
            if (slotData == null || string.IsNullOrEmpty(slotData.itemId) || slotData.quantity <= 0)
            {
                ClearSlot();
            }
            else
            {
                SetItem(slotData.itemId, slotData.quantity);
            }
        }
        
        public void SetItem(string itemId, int quantity)
        {
            currentItemId = itemId;
            currentQuantity = quantity;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData != null && itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
                quantityText.text = $"×{quantity}";
                quantityText.enabled = true;
            }
            else
            {
                ClearSlot();
            }
        }
        
        public void ClearSlot()
        {
            currentItemId = null;
            currentQuantity = 0;
            iconImage.enabled = false;
            quantityText.enabled = false;
        }
        
        #region Drag and Drop
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            
            dragSource = this;
            
            // Create drag icon
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(rootCanvas.transform, false);
            
            var image = dragIcon.AddComponent<Image>();
            image.sprite = iconImage.sprite;
            image.raycastTarget = false;
            
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
            
            if (dragSource == this)
            {
                iconImage.color = Color.white;
            }
            
            dragSource = null;
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Handle drop from WarehouseSlotUI
            var warehouseSource = eventData.pointerDrag?.GetComponent<WarehouseSlotUI>();
            if (warehouseSource != null && !warehouseSource.IsEmpty)
            {
                HandleWarehouseToShelfDrop(warehouseSource);
                return;
            }
            
            // Handle drop from another ShelfSlotUI (return to warehouse)
            if (dragSource != null && dragSource != this)
            {
                // For shelf-to-shelf, we don't support direct move
                // Items must go through warehouse
            }
        }
        
        /// <summary>
        /// Handle drop from warehouse to shelf
        /// </summary>
        private void HandleWarehouseToShelfDrop(WarehouseSlotUI warehouseSource)
        {
            if (linkedShelf == null || WarehouseManager.Instance == null) return;
            
            string itemId = warehouseSource.ItemId;
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return;
            
            // Check if this item can be placed on shelf
            if (!itemData.canPlaceOnShelf)
            {
                Debug.Log($"[ShelfSlotUI] {itemData.displayName} cannot be placed on treatment shelf");
                return;
            }
            
            int maxOnShelf = itemData.maxStackOnShelf;
            int warehouseQty = warehouseSource.Quantity;
            
            // If shelf slot is empty
            if (IsEmpty)
            {
                int toMove = Mathf.Min(warehouseQty, maxOnShelf);
                
                // Add to shelf
                linkedShelf.PlaceItem(row, col, itemId, toMove);
                
                // Remove from warehouse
                WarehouseManager.Instance.RemoveFromSlot(warehouseSource.SlotIndex, toMove);
                
                RefreshFromShelf();
                warehouseSource.RefreshFromWarehouse();
            }
            // If same item, try to fill
            else if (currentItemId == itemId)
            {
                int space = maxOnShelf - currentQuantity;
                if (space > 0)
                {
                    int toMove = Mathf.Min(warehouseQty, space);
                    
                    linkedShelf.AddToSlot(row, col, toMove);
                    WarehouseManager.Instance.RemoveFromSlot(warehouseSource.SlotIndex, toMove);
                    
                    RefreshFromShelf();
                    warehouseSource.RefreshFromWarehouse();
                }
            }
            // Different item - no action
        }
        
        #endregion
        
        #region Hover Effects
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlightColor;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }
        
        #endregion
    }
}
