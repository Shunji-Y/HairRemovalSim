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
        public enum SlotMode { Shelf, FaceLaser, BodyLaser }
        
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        
        [Header("Laser Slot Settings")]
        [SerializeField] private SlotMode slotMode = SlotMode.Shelf;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        // Linked shelf data (for Shelf mode)
        private TreatmentShelf linkedShelf;
        private int row;
        private int col;
        
        // Linked bed data (for Laser mode)
        private BedController linkedBed;
        
        // Current display data
        private string currentItemId;
        private int currentQuantity;
        
        // Drag state
        private static ShelfSlotUI dragSource;
        private static WarehouseSlotUI warehouseDragSource; // For cross-type drops
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public TreatmentShelf LinkedShelf => linkedShelf;
        public BedController LinkedBed => linkedBed;
        public SlotMode Mode => slotMode;
        public int Row => row;
        public int Col => col;
        public string ItemId => currentItemId;
        public int Quantity => currentQuantity;
        public bool IsEmpty => string.IsNullOrEmpty(currentItemId) || currentQuantity <= 0;
        public bool IsLaserSlot => slotMode == SlotMode.FaceLaser || slotMode == SlotMode.BodyLaser;
        
        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        public void Initialize(TreatmentShelf shelf, int slotRow, int slotCol)
        {
            slotMode = SlotMode.Shelf;
            linkedShelf = shelf;
            row = slotRow;
            col = slotCol;
            RefreshFromShelf();
        }
        
        /// <summary>
        /// Initialize as a laser slot (Face or Body)
        /// </summary>
        public void InitializeAsLaserSlot(BedController bed, SlotMode mode)
        {
            if (mode == SlotMode.Shelf)
            {
                Debug.LogWarning("[ShelfSlotUI] InitializeAsLaserSlot called with Shelf mode");
                return;
            }
            
            slotMode = mode;
            linkedBed = bed;
            linkedShelf = null;
            RefreshFromLaserBody();
        }
        
        /// <summary>
        /// Sync UI from LaserBody data
        /// </summary>
        public void RefreshFromLaserBody()
        {
            if (linkedBed == null || linkedBed.laserBody == null)
            {
                ClearSlot();
                return;
            }
            
            var targetArea = slotMode == SlotMode.FaceLaser ? ToolTargetArea.Face : ToolTargetArea.Body;
            var itemData = linkedBed.laserBody.GetItem(targetArea);
            
            if (itemData != null)
            {
                SetItem(itemData.itemId, 1);
            }
            else
            {
                ClearSlot();
            }
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
                if (quantityText != null)
                {
                    quantityText.text = $"×{quantity}";
                    quantityText.enabled = true;
                }
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
            if (quantityText != null)
            {
                quantityText.enabled = false;
            }
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
            // Handle laser slot mode
            if (IsLaserSlot)
            {
                var warehouseSource = eventData.pointerDrag?.GetComponent<WarehouseSlotUI>();
                if (warehouseSource != null && !warehouseSource.IsEmpty)
                {
                    HandleWarehouseToLaserSlot(warehouseSource);
                }
                return;
            }
            
            // Handle drop from another ShelfSlotUI
            var shelfSource = eventData.pointerDrag?.GetComponent<ShelfSlotUI>();
            if (shelfSource != null && shelfSource != this && !shelfSource.IsEmpty)
            {
                HandleShelfToShelfDrop(shelfSource);
                return;
            }
            
            // Handle drop from WarehouseSlotUI
            var warehouseSource2 = eventData.pointerDrag?.GetComponent<WarehouseSlotUI>();
            if (warehouseSource2 != null && !warehouseSource2.IsEmpty)
            {
                HandleWarehouseToShelfDrop(warehouseSource2);
                return;
            }
        }
        
        /// <summary>
        /// Handle drop from another ShelfSlotUI
        /// </summary>
        private void HandleShelfToShelfDrop(ShelfSlotUI source)
        {
            string dropItemId = source.ItemId;
            int qty = source.Quantity;
            
            // Validate shaver slot restrictions
            var dropItemData = ItemDataRegistry.Instance?.GetItem(dropItemId);
            if (dropItemData != null)
            {
                bool isTargetSlotZero = (row == 0 && col == 0);
                bool isDropShaver = (dropItemData.toolType == TreatmentToolType.Shaver);
                
                // Slot [0,0] only accepts shaver
                if (isTargetSlotZero && !isDropShaver)
                {
                    Debug.Log($"[ShelfSlotUI] Slot [0,0] is shaver-only. Cannot drop {dropItemData.toolType}");
                    return;
                }
                // Shaver can only go to slot [0,0]
                if (isDropShaver && !isTargetSlotZero)
                {
                    Debug.Log($"[ShelfSlotUI] Shaver can only be placed at slot [0,0], not [{row},{col}]");
                    return;
                }
            }
            
            // If this slot is empty or has same item, merge (with max stack limit)
            if (IsEmpty || currentItemId == dropItemId)
            {
                // Get max stack limit
                int maxStack = dropItemData?.maxStackOnShelf ?? 99;
                int currentQty = IsEmpty ? 0 : currentQuantity;
                int space = maxStack - currentQty;
                int toMove = Mathf.Min(qty, space);
                
                if (toMove <= 0)
                {
                    Debug.Log($"[ShelfSlotUI] Slot is full (max {maxStack})");
                    return;
                }
                
                // Remove only the amount we can move from source
                if (source.LinkedShelf != null)
                {
                    source.LinkedShelf.RemoveFromSlot(source.Row, source.Col, toMove);
                }
                
                // Add to this slot
                if (linkedShelf != null)
                {
                    linkedShelf.PlaceItem(row, col, dropItemId, toMove);
                }
                
                source.RefreshFromShelf();
                RefreshFromShelf();
                
                Debug.Log($"[ShelfSlotUI] Moved {toMove}x {dropItemId} from another shelf slot (max stack: {maxStack})");
            }
            // If different item, swap
            else
            {
                string tempId = currentItemId;
                int tempQty = currentQuantity;
                
                // Clear both slots first
                if (linkedShelf != null)
                {
                    linkedShelf.RemoveFromSlot(row, col, tempQty);
                }
                if (source.LinkedShelf != null)
                {
                    source.LinkedShelf.RemoveFromSlot(source.Row, source.Col, qty);
                }
                
                // Place swapped items
                if (linkedShelf != null)
                {
                    linkedShelf.PlaceItem(row, col, dropItemId, qty);
                }
                if (source.LinkedShelf != null)
                {
                    source.LinkedShelf.PlaceItem(source.Row, source.Col, tempId, tempQty);
                }
                
                source.RefreshFromShelf();
                RefreshFromShelf();
                
                Debug.Log($"[ShelfSlotUI] Swapped items between shelf slots");
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
            if (!itemData.CanPlaceOnShelf)
            {
                Debug.Log($"[ShelfSlotUI] {itemData.name} cannot be placed on treatment shelf");
                return;
            }
            
            // Slot [0,0] is shaver-only, and shaver can only be at [0,0]
            bool isSlotZero = (row == 0 && col == 0);
            bool isShaver = (itemData.toolType == TreatmentToolType.Shaver);
            
            if (isSlotZero && !isShaver)
            {
                Debug.Log($"[ShelfSlotUI] Slot [0,0] is shaver-only. Cannot place {itemData.toolType}");
                return;
            }
            if (isShaver && !isSlotZero)
            {
                Debug.Log($"[ShelfSlotUI] Shaver can only be placed at slot [0,0], not [{row},{col}]");
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
        
        /// <summary>
        /// Handle drop from warehouse to laser slot - spawn laser on LaserBody
        /// </summary>
        private void HandleWarehouseToLaserSlot(WarehouseSlotUI warehouseSource)
        {
            if (linkedBed == null || linkedBed.laserBody == null) return;
            if (WarehouseManager.Instance == null) return;
            
            string itemId = warehouseSource.ItemId;
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return;
            
            // Validate this is a laser of correct type
            if (itemData.toolType != TreatmentToolType.Laser)
            {
                Debug.Log($"[ShelfSlotUI] Only lasers can be placed in laser slot. {itemData.name} is {itemData.toolType}");
                return;
            }
            
            var requiredArea = slotMode == SlotMode.FaceLaser ? ToolTargetArea.Face : ToolTargetArea.Body;
            if (itemData.targetArea != requiredArea && itemData.targetArea != ToolTargetArea.All)
            {
                Debug.Log($"[ShelfSlotUI] {itemData.name} is {itemData.targetArea}, slot requires {requiredArea}");
                return;
            }
            
            // Check if slot is already occupied
            if (linkedBed.laserBody.GetItem(requiredArea) != null)
            {
                Debug.Log($"[ShelfSlotUI] Laser slot already occupied");
                return;
            }
            
            // Get laser instance from pool
            var laserInstance = ItemPoolManager.Instance?.GetItem(itemId);
            if (laserInstance == null)
            {
                Debug.LogError($"[ShelfSlotUI] Failed to get laser from pool: {itemId}");
                return;
            }
            
            // Place on LaserBody
            if (linkedBed.laserBody.PlaceItem(requiredArea, itemData, laserInstance))
            {
                // Remove from warehouse
                WarehouseManager.Instance.RemoveFromSlot(warehouseSource.SlotIndex, 1);
                
                RefreshFromLaserBody();
                warehouseSource.RefreshFromWarehouse();
                
                Debug.Log($"[ShelfSlotUI] Placed {itemData.name} on LaserBody {requiredArea} slot");
            }
            else
            {
                // Failed - return to pool
                ItemPoolManager.Instance?.ReturnItem(itemId, laserInstance);
            }
        }
        
        /// <summary>
        /// Move laser from this slot back to warehouse
        /// </summary>
        public void ReturnLaserToWarehouse(int targetSlotIndex = -1)
        {
            if (!IsLaserSlot || linkedBed == null || linkedBed.laserBody == null) return;
            if (WarehouseManager.Instance == null) return;
            if (IsEmpty) return;
            
            var requiredArea = slotMode == SlotMode.FaceLaser ? ToolTargetArea.Face : ToolTargetArea.Body;
            var (itemData, instance) = linkedBed.laserBody.RemoveItem(requiredArea);
            
            if (itemData != null && instance != null)
            {
                bool success = false;
                
                if (targetSlotIndex >= 0)
                {
                    // Try to add to specific slot
                    var targetSlot = WarehouseManager.Instance.GetSlot(targetSlotIndex);
                    if (targetSlot != null && targetSlot.IsEmpty)
                    {
                        WarehouseManager.Instance.SetSlot(targetSlotIndex, itemData.itemId, 1);
                        success = true;
                    }
                    else if (targetSlot != null && targetSlot.itemId == itemData.itemId)
                    {
                        // Same item - stack it
                        var itemDataRef = ItemDataRegistry.Instance?.GetItem(itemData.itemId);
                        if (itemDataRef != null && targetSlot.quantity < itemDataRef.maxWarehouseStack)
                        {
                            WarehouseManager.Instance.SetSlot(targetSlotIndex, itemData.itemId, targetSlot.quantity + 1);
                            success = true;
                        }
                    }
                }
                
                // Fallback to any empty slot if specific slot failed or not specified
                if (!success)
                {
                    int added = WarehouseManager.Instance.AddItem(itemData.itemId, 1);
                    success = added > 0;
                }
                
                if (success)
                {
                    // Success - return to pool
                    ItemPoolManager.Instance?.ReturnItem(itemData.itemId, instance);
                    RefreshFromLaserBody();
                    Debug.Log($"[ShelfSlotUI] Returned {itemData.name} to warehouse");
                }
                else
                {
                    // Failed to add to warehouse - put back on LaserBody
                    linkedBed.laserBody.PlaceItem(requiredArea, itemData, instance);
                    Debug.LogWarning($"[ShelfSlotUI] Warehouse full, cannot return {itemData.name}");
                }
            }
        }
        
        #endregion
        
        #region Hover Effects
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlightColor;
            }
            
            // Show item detail in panel
            if (!IsEmpty && WarehousePanel.Instance != null)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(currentItemId);
                if (itemData != null)
                {
                    WarehousePanel.Instance.ShowItemDetail(itemData);
                }
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
            
            // Hide item detail
            if (WarehousePanel.Instance != null)
            {
                WarehousePanel.Instance.HideItemDetail();
            }
        }
        
        #endregion
    }
}
