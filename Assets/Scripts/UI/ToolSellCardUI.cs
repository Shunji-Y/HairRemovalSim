using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Card UI for selling tools in the tool shop
    /// Shows: Icon, Name, Quantity, Sell Price, Sell Button
    /// </summary>
    public class ToolSellCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text sellPriceText;
        
        [Header("Sell Button")]
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellButtonText;
        
        private ItemData itemData;
        private int quantity;
        private int sellPrice;
        private System.Action<ItemData> onSellCallback;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void OnEnable()
        {
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Setup(ItemData data, int qty, int price, System.Action<ItemData> onSell)
        {
            itemData = data;
            quantity = qty;
            sellPrice = price;
            onSellCallback = onSell;
            
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(OnSellClicked);
            }
            
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (itemData == null) return;
            
            // Icon
            if (iconImage != null && itemData.icon != null)
                iconImage.sprite = itemData.icon;
            
            // Name (localized)
            if (nameText != null)
            {
                string localizedName = L?.Get(itemData.nameKey);
                nameText.text = string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")
                    ? itemData.displayName
                    : localizedName;
            }
            
            // Quantity
            if (quantityText != null)
                quantityText.text = $"x{quantity}";
            
            // Sell price (half of original)
            if (sellPriceText != null)
                sellPriceText.text = $"+${sellPrice:N0}";
            
            // Button text
            if (sellButtonText != null)
                sellButtonText.text = L?.Get("tool.sell") ?? "SELL";
        }
        
        private void OnSellClicked()
        {
            if (itemData == null || quantity <= 0) return;
            onSellCallback?.Invoke(itemData);
        }
        
        public ItemData GetItemData() => itemData;
    }
}
