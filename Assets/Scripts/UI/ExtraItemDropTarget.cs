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
    /// </summary>
    public class ExtraItemDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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
        
        public string ItemId => currentItemId;
        public bool HasItem => !string.IsNullOrEmpty(currentItemId);
        
        // Event when item is set
        public System.Action<string> OnItemSet;
        
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
