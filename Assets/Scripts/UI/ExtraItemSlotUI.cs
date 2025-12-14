using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI slot for extra items in reception panel (e.g., anesthesia cream)
    /// Can be dragged to the EXTRA ITEM drop target
    /// Supports slot-to-slot movement
    /// </summary>
    public class ExtraItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        // Item data
        private string itemId;
        private int quantity;
        
        // Drag state
        public static ExtraItemSlotUI DragSource { get; private set; }
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public string ItemId => itemId;
        public int Quantity => quantity;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
        
        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        /// <summary>
        /// Set item data for this slot
        /// </summary>
        public void SetItem(string id, int qty)
        {
            itemId = id;
            quantity = qty;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Clear this slot
        /// </summary>
        public void Clear()
        {
            itemId = null;
            quantity = 0;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Use one item from this slot
        /// </summary>
        public bool UseOne()
        {
            if (quantity > 0)
            {
                quantity--;
                RefreshDisplay();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Add items to this slot
        /// </summary>
        public void AddQuantity(int amount)
        {
            quantity += amount;
            RefreshDisplay();
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
        
        #region Drag and Drop
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            
            DragSource = this;
            
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
            
            if (DragSource == this)
            {
                iconImage.color = Color.white;
            }
            
            DragSource = null;
        }
        
        #endregion
        
        #region Drop Handler
        
        public void OnDrop(PointerEventData eventData)
        {
            // Handle drop from another ExtraItemSlotUI
            var sameTypeSource = eventData.pointerDrag?.GetComponent<ExtraItemSlotUI>();
            if (sameTypeSource != null && sameTypeSource != this && !sameTypeSource.IsEmpty)
            {
                HandleSameSlotDrop(sameTypeSource);
            }
        }
        
        private void HandleSameSlotDrop(ExtraItemSlotUI source)
        {
            string dropItemId = source.ItemId;
            int qty = source.Quantity;
            
            // If this slot is empty or has same item, merge
            if (IsEmpty || itemId == dropItemId)
            {
                itemId = dropItemId;
                quantity += qty;
                
                source.itemId = null;
                source.quantity = 0;
                source.RefreshDisplay();
                
                RefreshDisplay();
                Debug.Log($"[ExtraItemSlotUI] Moved {qty}x {dropItemId} from another slot");
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
                Debug.Log($"[ExtraItemSlotUI] Swapped items between slots");
            }
        }
        
        #endregion
        
        #region Hover Effects
        
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
        
        #endregion
    }
}
