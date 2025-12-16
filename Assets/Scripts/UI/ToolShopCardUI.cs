using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual tool card in the tool shop
    /// Shows: image, name, stats (range, pain, speed), price, purchase button, description
    /// No quantity controls - single purchase only
    /// </summary>
    public class ToolShopCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        
        [Header("Stats")]
        [SerializeField] private TMP_Text statsText; // Combined stats display
        
        [Header("Grade Lock")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TMP_Text lockedText;
        
        [Header("Purchase")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseButtonText;
        [SerializeField] private Image cardBackground;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color ownedColor = new Color(0.9f, 1f, 0.9f);
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.7f, 0.7f);
        
        private ItemData itemData;
        private System.Action<ItemData> onPurchaseCallback;
        private bool isOwned;
        private bool isLocked;
        
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
        
        public void Setup(ItemData data, int currentShopGrade, bool owned, System.Action<ItemData> onPurchase)
        {
            itemData = data;
            isOwned = owned;
            isLocked = !data.IsUnlockedForGrade(currentShopGrade);
            onPurchaseCallback = onPurchase;
            
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
            
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (itemData == null) return;
            
            // Icon
            if (iconImage != null && itemData.icon != null)
                iconImage.sprite = itemData.icon;
            
            // Name
            if (nameText != null)
                nameText.text = itemData.displayName;
            
            // Description
            if (descriptionText != null)
                descriptionText.text = itemData.storeDescription ?? itemData.description;
            
            // Price
            if (priceText != null)
                priceText.text = $"¥{itemData.price:N0}";
            
            // Stats - combined display
            UpdateStats();
            
            // Lock state
            UpdateLockState();
            
            // Purchase button
            UpdatePurchaseButton();
            
            // Background color
            if (cardBackground != null)
            {
                if (isLocked)
                    cardBackground.color = lockedColor;
                else if (isOwned)
                    cardBackground.color = ownedColor;
                else
                    cardBackground.color = normalColor;
            }
        }
        
        private void UpdateStats()
        {
            if (statsText == null) return;
            
            // Build stats line like: [Scope: Wide] [Pain: Low] [Speed: Fast]
            string rangeLabel = GetRangeLabel();
            string painLabel = GetPainLabel();
            string speedLabel = GetSpeedLabel();
            
            statsText.text = $"[{L?.Get("tool.range") ?? "Scope"}: {rangeLabel}] [{L?.Get("tool.pain") ?? "Pain"}: {painLabel}] [{L?.Get("tool.fire_rate") ?? "Speed"}: {speedLabel}]";
        }
        
        private string GetRangeLabel()
        {
            return itemData.effectRange switch
            {
                <= 0.5f => L?.Get("tool.range_narrow") ?? "Narrow",
                <= 1.0f => L?.Get("tool.range_normal") ?? "Normal",
                <= 2.0f => L?.Get("tool.range_wide") ?? "Wide",
                _ => L?.Get("tool.range_ultra") ?? "Ultra"
            };
        }
        
        private string GetPainLabel()
        {
            return itemData.painLevel switch
            {
                0f => L?.Get("tool.pain_none") ?? "None",
                <= 0.3f => L?.Get("tool.pain_low") ?? "Low",
                <= 0.6f => L?.Get("tool.pain_medium") ?? "Medium",
                _ => L?.Get("tool.pain_high") ?? "Extreme"
            };
        }
        
        private string GetSpeedLabel()
        {
            return itemData.fireRate switch
            {
                < 0f => L?.Get("tool.rate_continuous") ?? "Rapid",
                <= 0.5f => L?.Get("tool.rate_slow") ?? "Slow",
                <= 1.0f => L?.Get("tool.rate_normal") ?? "Normal",
                _ => L?.Get("tool.rate_fast") ?? "Fast"
            };
        }
        
        private void UpdateLockState()
        {
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            
            if (lockedText != null && isLocked)
            {
                lockedText.text = L?.Get("tool.locked_grade", itemData.requiredShopGrade) 
                    ?? $"Requires Grade {itemData.requiredShopGrade}";
            }
        }
        
        private void UpdatePurchaseButton()
        {
            if (purchaseButton == null) return;
            
            if (isLocked)
            {
                purchaseButton.interactable = false;
                if (purchaseButtonText != null)
                    purchaseButtonText.text = L?.Get("tool.locked") ?? "LOCKED";
            }
            else if (isOwned)
            {
                purchaseButton.interactable = false;
                if (purchaseButtonText != null)
                    purchaseButtonText.text = L?.Get("tool.owned") ?? "OWNED";
            }
            else
            {
                int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
                bool canAfford = currentMoney >= itemData.price;
                purchaseButton.interactable = canAfford;
                
                if (purchaseButtonText != null)
                {
                    if (canAfford)
                        purchaseButtonText.text = L?.Get("tool.purchase") ?? "PURCHASE";
                    else
                        purchaseButtonText.text = L?.Get("tool.out_of_stock") ?? "OUT OF STOCK";
                }
            }
        }
        
        private void OnPurchaseClicked()
        {
            if (itemData == null || isLocked || isOwned) return;
            onPurchaseCallback?.Invoke(itemData);
        }
        
        /// <summary>
        /// Get the item data for this card
        /// </summary>
        public ItemData GetItemData() => itemData;
    }
}
