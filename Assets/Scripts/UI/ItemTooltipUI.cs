using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Item tooltip UI for Reception and Payment panels.
    /// Shows item name, description, upsell price, and review bonus.
    /// Attach to a tooltip panel GameObject and reference from slots.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text upsellPriceText;
        [SerializeField] private TMP_Text reviewBonusText;
        
        [Header("Optional")]
        [SerializeField] private Image itemIconImage;
        
        private void Awake()
        {
            Hide();
        }
        
        /// <summary>
        /// Show tooltip for an item
        /// </summary>
        public void Show(ItemData item)
        {
            if (item == null || tooltipRoot == null) return;
            
            tooltipRoot.SetActive(true);
            
            // Item name (localized)
            if (itemNameText != null)
            {
                itemNameText.text = item.GetLocalizedName();
            }
            
            // Item description (localized)
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = item.GetLocalizedDescription();
            }
            
            // Upsell price
            if (upsellPriceText != null)
            {
                if (item.upsellPrice > 0)
                    upsellPriceText.text = $"${item.upsellPrice}";
                else
                    upsellPriceText.text = "-";
            }
            
            // Review bonus
            if (reviewBonusText != null)
            {
                if (item.reviewBonus != 0)
                    reviewBonusText.text = item.reviewBonus > 0 ? $"+{item.reviewBonus}" : $"{item.reviewBonus}";
                else
                    reviewBonusText.text = "-";
            }
            
            // Icon (optional)
            if (itemIconImage != null)
            {
                if (item.icon != null)
                {
                    itemIconImage.sprite = item.icon;
                    itemIconImage.enabled = true;
                }
                else
                {
                    itemIconImage.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// Show tooltip for an item ID
        /// </summary>
        public void Show(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData != null)
            {
                Show(itemData);
            }
        }
        
        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public void Hide()
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
            }
        }
    }
}
