using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual warehouse slot UI with drag and drop support.
    /// </summary>
    public class WarehouseSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private GameObject emptyLabel;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        [SerializeField] private Color invalidDropColor = new Color(1f, 0.8f, 0.8f);
        
        // Slot data
        private int slotIndex;
        private string currentItemId;
        private int currentQuantity;
        
        // Drag state
        private static WarehouseSlotUI dragSource;
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public int SlotIndex => slotIndex;
        public string ItemId => currentItemId;
        public int Quantity => currentQuantity;
        public bool IsEmpty => string.IsNullOrEmpty(currentItemId) || currentQuantity <= 0;
        
        private void Awake()
        {
            EnsureCanvas();
        }
        
        private void EnsureCanvas()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }
        
        public void Initialize(int index)
        {
            slotIndex = index;
            EnsureCanvas();
            ClearSlot();
        }
        
        /// <summary>
        /// Update slot display from warehouse data
        /// </summary>
        public void RefreshFromWarehouse()
        {
            if (WarehouseManager.Instance == null) return;
            
            var slot = WarehouseManager.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                ClearSlot();
            }
            else
            {
                SetItem(slot.itemId, slot.quantity);
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
                if (emptyLabel != null) emptyLabel.SetActive(false);
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
            if (emptyLabel != null) emptyLabel.SetActive(true);
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
            image.SetNativeSize();
            
            // Scale to reasonable size
            var rt = dragIcon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);
            
            // Make original semi-transparent
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
            if (dragSource != null && dragSource != this)
            {
                HandleWarehouseDrop(dragSource);
                return;
            }
            
            // Handle drop from ShelfSlotUI
            var shelfSource = eventData.pointerDrag?.GetComponent<ShelfSlotUI>();
            if (shelfSource != null && !shelfSource.IsEmpty)
            {
                HandleShelfDrop(shelfSource);
            }
        }
        
        private void HandleWarehouseDrop(WarehouseSlotUI source)
        {
            if (WarehouseManager.Instance == null) return;
            
            // Move items from source slot to this slot
            int moved = WarehouseManager.Instance.MoveToSlot(source.slotIndex, slotIndex, source.currentQuantity);
            
            if (moved > 0)
            {
                source.RefreshFromWarehouse();
                RefreshFromWarehouse();
            }
        }
        
        private void HandleShelfDrop(ShelfSlotUI shelfSource)
        {
            if (WarehouseManager.Instance == null) return;
            if (shelfSource.LinkedShelf == null) return;
            
            string itemId = shelfSource.ItemId;
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return;
            
            int shelfQty = shelfSource.Quantity;
            int maxStack = itemData.maxWarehouseStack;
            
            // Get the actual warehouse slot data
            var slot = WarehouseManager.Instance.GetSlot(slotIndex);
            if (slot == null) return;
            
            // If warehouse slot is empty
            if (slot.IsEmpty)
            {
                int toMove = Mathf.Min(shelfQty, maxStack);
                
                // Add directly to THIS slot
                slot.itemId = itemId;
                slot.quantity = toMove;
                
                // Remove from shelf
                shelfSource.LinkedShelf.RemoveFromSlot(shelfSource.Row, shelfSource.Col, toMove);
                
                RefreshFromWarehouse();
                shelfSource.RefreshFromShelf();
                
                Debug.Log($"[WarehouseSlotUI] Moved {toMove}x {itemId} from shelf to slot {slotIndex}");
            }
            // If same item, try to fill
            else if (slot.itemId == itemId)
            {
                int space = maxStack - slot.quantity;
                if (space > 0)
                {
                    int toMove = Mathf.Min(shelfQty, space);
                    slot.quantity += toMove;
                    shelfSource.LinkedShelf.RemoveFromSlot(shelfSource.Row, shelfSource.Col, toMove);
                    
                    RefreshFromWarehouse();
                    shelfSource.RefreshFromShelf();
                    
                    Debug.Log($"[WarehouseSlotUI] Added {toMove}x {itemId} to slot {slotIndex}, now {slot.quantity}");
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
                if (dragSource != null && dragSource != this)
                {
                    // Check if valid drop target
                    if (IsEmpty || currentItemId == dragSource.currentItemId)
                        backgroundImage.color = highlightColor;
                    else
                        backgroundImage.color = invalidDropColor;
                }
                else
                {
                    backgroundImage.color = highlightColor;
                }
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
