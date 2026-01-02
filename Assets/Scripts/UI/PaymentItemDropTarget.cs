using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Drop target for adding items to payment in PaymentPanel.
    /// Shows the dropped item's icon. Supports drag back to CheckoutItemSlotUI.
    /// </summary>
    public class PaymentItemDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        
        [Header("Slot Settings")]
        [Tooltip("Slot index (0-4) for multiple upsell slots")]
        [SerializeField] private int slotIndex = 0;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        // Current item
        private string currentItemId;
        
        // Drag state
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public string ItemId => currentItemId;
        public bool HasItem => !string.IsNullOrEmpty(currentItemId);
        public int SlotIndex => slotIndex;
        
        private void Awake()
        {
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
            
            if (iconImage != null)
                iconImage.enabled = false;
                
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Check if dropped from checkout slot
            var checkoutSlot = eventData.pointerDrag?.GetComponent<CheckoutItemSlotUI>();
            if (checkoutSlot != null && !checkoutSlot.IsEmpty)
            {
                string newItemId = checkoutSlot.ItemId;
                
                // If already has an item, return it to checkout slot first
                if (HasItem)
                {
                    ReturnItemToCheckout(currentItemId);
                }
                
                // Set new item
                SetItem(newItemId);
                
                // Consume one item from the checkout slot
                checkoutSlot.RemoveOne();
            }
        }
        
        /// <summary>
        /// Return current item to any available checkout slot
        /// </summary>
        private void ReturnItemToCheckout(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            if (PaymentPanel.Instance != null && PaymentPanel.Instance.CheckoutItemSlots != null)
            {
                foreach (var slot in PaymentPanel.Instance.CheckoutItemSlots)
                {
                    if (slot != null)
                    {
                        // Find matching or empty slot
                        if (slot.ItemId == itemId || slot.IsEmpty)
                        {
                            slot.AddOne(itemId);
                            Debug.Log($"[PaymentItemDropTarget] Returned {itemId} to checkout slot");
                            
                            // Notify PaymentPanel to clear this slot's added item
                            PaymentPanel.Instance.ClearAddedItem(slotIndex);
                            return;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"[PaymentItemDropTarget] Could not find slot to return {itemId}");
        }
        
        /// <summary>
        /// Set item to display and notify PaymentPanel
        /// </summary>
        public void SetItem(string itemId)
        {
            currentItemId = itemId;
            
            // Update icon display
            if (iconImage != null)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                    if (itemData != null && itemData.icon != null)
                    {
                        iconImage.sprite = itemData.icon;
                        iconImage.enabled = true;
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
            
            // Notify PaymentPanel with slot index
            if (PaymentPanel.Instance != null)
            {
                PaymentPanel.Instance.OnItemAdded(slotIndex, itemId);
            }
        }
        
        public void ClearSlot()
        {
            currentItemId = null;
            if (iconImage != null)
                iconImage.enabled = false;
        }
        
        /// <summary>
        /// Return item to stock and clear (for ESC/Cancel - item is not consumed)
        /// </summary>
        public void ReturnToStockAndClear()
        {
            if (HasItem)
            {
                Debug.Log($"[PaymentItemDropTarget] Returning {currentItemId} to stock before clearing");
                ReturnItemToCheckout(currentItemId);
            }
            ClearSlot();
        }
        
        #region Drag back to CheckoutItemSlotUI
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!HasItem) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(currentItemId);
            if (itemData == null || itemData.icon == null) return;
            
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
            
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(rootCanvas.transform, false);
            
            var img = dragIcon.AddComponent<Image>();
            img.sprite = itemData.icon;
            img.raycastTarget = false;
            
            var rt = dragIcon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50, 50);
            
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
            
            if (!HasItem) return;
            
            // Check if dropped on checkout slot
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                var checkoutSlot = result.gameObject.GetComponent<CheckoutItemSlotUI>();
                if (checkoutSlot != null)
                {
                    // Return item to this slot
                    checkoutSlot.AddOne(currentItemId);
                    
                    // Clear and notify
                    ClearSlot();
                    if (PaymentPanel.Instance != null)
                    {
                        PaymentPanel.Instance.ClearAddedItem(slotIndex);
                    }
                    
                    Debug.Log($"[PaymentItemDropTarget] Dragged item back to checkout slot");
                    return;
                }
            }
        }
        
        #endregion
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;
            
            // Show tooltip via PaymentPanel
            if (HasItem && PaymentPanel.Instance?.Tooltip != null)
            {
                PaymentPanel.Instance.Tooltip.Show(currentItemId);
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
    }
}
