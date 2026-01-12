using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Checkout item slot UI for EXTRA ITEMS at cash register.
    /// Syncs with CheckoutStockSlotUI in Warehouse panel.
    /// Allows dragging items to payment panel.
    /// Supports slot-to-slot movement.
    /// </summary>
    public class CheckoutItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image backgroundImage;
        
        [Header("Settings")]
        [Tooltip("Index to sync with CheckoutStockSlotUI")]
        [SerializeField] private int syncSlotIndex = 0;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.7f);
        
        // Item state
        private string itemId;
        private int quantity;
        
        // Drag state
        private GameObject dragIcon;
        private Canvas canvas;
        private RectTransform canvasRect;
        
        // Static drag events for highlight system
        public static event System.Action OnCheckoutDragStarted;
        public static event System.Action OnCheckoutDragEnded;
        public static CheckoutItemSlotUI DragSource { get; private set; }
        
        public string ItemId => itemId;
        public int Quantity => quantity;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
        public int SyncSlotIndex => syncSlotIndex;
        
        private void Start()
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRect = canvas.GetComponent<RectTransform>();
            
            RefreshDisplay();
        }
        
        /// <summary>
        /// Set item from CheckoutStockSlotUI sync
        /// </summary>
        public void SetItemFromStock(string id, int qty)
        {
            itemId = id;
            quantity = qty;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Remove one item from this slot (for dragging to PaymentItemDropTarget)
        /// </summary>
        public void RemoveOne()
        {
            if (quantity > 0)
            {
                quantity--;
                if (quantity <= 0)
                {
                    itemId = null;
                }
                RefreshDisplay();
                SyncToStockSlot(); // Keep CheckoutStockSlotUI in sync
            }
        }
        
        /// <summary>
        /// Add one item to this slot (for returning from PaymentItemDropTarget)
        /// </summary>
        public void AddOne(string addItemId)
        {
            if (string.IsNullOrEmpty(itemId) || itemId == addItemId)
            {
                // Apply max stack limit
                var itemData = ItemDataRegistry.Instance?.GetItem(addItemId);
                int maxStack = itemData?.maxStackOnShelf ?? 99;
                
                if (quantity < maxStack)
                {
                    itemId = addItemId;
                    quantity++;
                    RefreshDisplay();
                    SyncToStockSlot();
                }
            }
        }
        
        /// <summary>
        /// Add item with ID and quantity (for staff restocking)
        /// </summary>
        public void AddItem(string id, int amount)
        {
            var itemData = ItemDataRegistry.Instance?.GetItem(id);
            int maxStack = itemData?.maxStackOnShelf ?? 99;
            
            if (string.IsNullOrEmpty(itemId) || itemId == id)
            {
                int currentQty = string.IsNullOrEmpty(itemId) ? 0 : quantity;
                int space = maxStack - currentQty;
                if (space > 0)
                {
                    int toAdd = Mathf.Min(amount, space);
                    itemId = id;
                    quantity = currentQty + toAdd;
                    RefreshDisplay();
                    SyncToStockSlot();
                }
            }
        }
        
        /// <summary>
        /// Get slot index (alias for SyncSlotIndex)
        /// </summary>
        public int SlotIndex => syncSlotIndex;
        /// Sync this slot's data to CheckoutStockSlotUI - DISABLED, now using Manager-based sync
        /// </summary>
        private void SyncToStockSlot()
        {
            // Manager-based sync is now used. Panel saves to Manager on close.
            // Individual slot sync is disabled to prevent conflicts.
        }
        
        /// <summary>
        /// Refresh visual display
        /// </summary>
        private void RefreshDisplay()
        {
            if (iconImage != null)
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
                    else
                    {
                        iconImage.enabled = false;
                    }
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
            
            if (quantityText != null)
            {
                quantityText.text = quantity > 1 ? $"×{quantity}" : "";
                quantityText.enabled = quantity > 1;
            }
        }
        
        // Drag handlers for dragging TO payment panel
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            SoundManager.Instance?.PlaySFX("sfx_drag");

            // Create drag icon
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null || itemData.icon == null) return;
            
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(canvas.transform, false);
            
            var img = dragIcon.AddComponent<Image>();
            img.sprite = itemData.icon;
            img.raycastTarget = false;
            
            var rt = dragIcon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50, 50);
            
            // Set drag source and fire event
            DragSource = this;
            
            // Play drag sound
    
            
            OnCheckoutDragStarted?.Invoke();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, canvas.worldCamera, out Vector2 pos);
            dragIcon.GetComponent<RectTransform>().anchoredPosition = pos;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragIcon != null)
            {
                Destroy(dragIcon);
            }
                        SoundManager.Instance?.PlaySFX("sfx_drop");

            // Clear drag source and fire event
            DragSource = null;
            OnCheckoutDragEnded?.Invoke();
            
            // Note: PaymentItemDropTarget.OnDrop handles the item addition with proper slot index
            // We don't call OnItemAdded here to avoid duplicate calls
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;
            
            // Show tooltip via PaymentPanel
            if (!IsEmpty && PaymentPanel.Instance?.Tooltip != null)
            {
                PaymentPanel.Instance.Tooltip.Show(itemId);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
            
            // Hide tooltip
            if (PaymentPanel.Instance?.Tooltip != null)
            {
                PaymentPanel.Instance.Tooltip.Hide();
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Play drop sound
            SoundManager.Instance?.PlaySFX("sfx_drop");
            
             // Handle drop from another CheckoutItemSlotUI
            var sameTypeSource = eventData.pointerDrag?.GetComponent<CheckoutItemSlotUI>();
            if (sameTypeSource != null && sameTypeSource != this && !sameTypeSource.IsEmpty)
            {
                HandleSameSlotDrop(sameTypeSource);
            }
        }
        
        private void HandleSameSlotDrop(CheckoutItemSlotUI source)
        {
            string dropItemId = source.ItemId;
            int qty = source.Quantity;
            
            // If this slot is empty or has same item, merge with maxStack limit
            if (IsEmpty || itemId == dropItemId)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(dropItemId);
                int maxStack = itemData?.maxStackOnShelf ?? 99;
                
                int currentQty = IsEmpty ? 0 : quantity;
                int space = maxStack - currentQty;
                int toMove = Mathf.Min(qty, space);
                
                if (toMove > 0)
                {
                    itemId = dropItemId;
                    quantity = currentQty + toMove;
                    
                    // Leave excess in source
                    int remaining = qty - toMove;
                    if (remaining > 0)
                    {
                        source.itemId = dropItemId;
                        source.quantity = remaining;
                    }
                    else
                    {
                        source.itemId = null;
                        source.quantity = 0;
                    }
                    source.RefreshDisplay();
                    
                    RefreshDisplay();
                    Debug.Log($"[CheckoutItemSlotUI] Moved {toMove}x {dropItemId} (max: {maxStack}, remaining: {remaining})");
                }
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
                
                RefreshDisplay();
                Debug.Log($"[CheckoutItemSlotUI] Swapped items between slots");
            }
        }
        
        /// <summary>
        /// Clear the slot
        /// </summary>
        public void Clear()
        {
            itemId = null;
            quantity = 0;
            RefreshDisplay();
        }
    }
}
