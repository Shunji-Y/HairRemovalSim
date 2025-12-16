using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Drop target for EXTRA ITEM in reception panel
    /// Accepts drops from ExtraItemSlotUI
    /// Can drag item back to ExtraItemSlotUI
    /// </summary>
    public class ExtraItemDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
        
        // Current item
        private string currentItemId;
        private ExtraItemSlotUI sourceSlot;
        
        // Drag state
        private static GameObject dragIcon;
        private Canvas rootCanvas;
        
        public string ItemId => currentItemId;
        public bool HasItem => !string.IsNullOrEmpty(currentItemId);
        
        // Event when item is set
        public System.Action<string> OnItemSet;
        
        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            var source = ExtraItemSlotUI.DragSource;
            if (source == null || source.IsEmpty) return;
            
            // If already has item, return previous to source
            if (HasItem && sourceSlot != null)
            {
                sourceSlot.AddQuantity(1);
            }
            
            // Set new item
            currentItemId = source.ItemId;
            sourceSlot = source;
            source.UseOne();
            
            RefreshDisplay();
            OnItemSet?.Invoke(currentItemId);
            
            Debug.Log($"[ExtraItemDropTarget] Set extra item: {currentItemId}");
        }
        
        /// <summary>
        /// Clear the drop target (return item to source if any)
        /// </summary>
        public void Clear()
        {
            if (HasItem && sourceSlot != null)
            {
                sourceSlot.AddQuantity(1);
            }
            
            currentItemId = null;
            sourceSlot = null;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Clear the drop target WITHOUT returning item to source (item was consumed)
        /// </summary>
        public void ClearWithoutReturn()
        {
            currentItemId = null;
            sourceSlot = null;
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (HasItem)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(currentItemId);
                if (itemData != null && itemData.icon != null)
                {
                    iconImage.sprite = itemData.icon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        #region Drag back to ExtraItemSlotUI
        
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
            
            if (!HasItem) return;
            
            // Check if dropped on ExtraItemSlotUI
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                var extraSlot = result.gameObject.GetComponent<ExtraItemSlotUI>();
                if (extraSlot != null)
                {
                    // Return item to this slot if it's empty or has same item
                    if (extraSlot.IsEmpty)
                    {
                        // Empty slot - set the item
                        extraSlot.SetItem(currentItemId, 1);
                        
                        // Clear this drop target
                        currentItemId = null;
                        sourceSlot = null;
                        RefreshDisplay();
                        
                        Debug.Log($"[ExtraItemDropTarget] Dragged item back to empty ExtraItemSlotUI");
                        return;
                    }
                    else if (extraSlot.ItemId == currentItemId)
                    {
                        // Same item - just add quantity
                        extraSlot.AddQuantity(1);
                        
                        // Clear this drop target
                        currentItemId = null;
                        sourceSlot = null;
                        RefreshDisplay();
                        
                        Debug.Log($"[ExtraItemDropTarget] Dragged item back to same-item ExtraItemSlotUI");
                        return;
                    }
                    // Different item - skip this slot, don't clear
                }
            }
        }
        
        #endregion
        
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
    }
}
