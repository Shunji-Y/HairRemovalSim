using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Tool card for ToolShopPanel
    /// Shows: Icon, Title, Stats (Scope/Pain/Power/Speed sliders), Description, Price, Purchase button
    /// </summary>
    public class ToolShopCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        
        [Header("Stats Sliders")]
        [SerializeField] private Slider scopeSlider;
        [SerializeField] private Slider painSlider;
        [SerializeField] private Slider powerSlider;
        [SerializeField] private Slider speedSlider;
        
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
        private bool isLocked;
        private bool isOwned;
        
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
        
        public void Setup(ItemData data, int currentStarLevel, System.Action<ItemData> onPurchase, bool owned = false)
        {
            itemData = data;
            isLocked = !data.IsUnlockedForStarLevel(currentStarLevel);
            isOwned = owned;
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
            
            // Name (localized)
            if (nameText != null)
            {
                nameText.text = itemData.GetLocalizedName();
            }
            
            // Description (localized)
            if (descriptionText != null)
            {
                descriptionText.text = itemData.GetLocalizedDescription();
            }
            
            // Price
            if (priceText != null)
                priceText.text = $"${itemData.price:N0}";
            
            // Stats sliders (0-100)
            UpdateStatSliders();
            
            // Lock state
            UpdateLockState();
            
            // Purchase button
            UpdatePurchaseButton();
            
            // Background color
            if (cardBackground != null)
            {
                if (isLocked)
                    cardBackground.color = lockedColor;
                else
                    cardBackground.color = normalColor;
            }
        }
        
        private void UpdateStatSliders()
        {
            // Stat fields have been removed from ItemData.
            // Hide all stat sliders if they exist.
            if (scopeSlider != null)
                scopeSlider.gameObject.SetActive(false);
            if (painSlider != null)
                painSlider.gameObject.SetActive(false);
            if (powerSlider != null)
                powerSlider.gameObject.SetActive(false);
            if (speedSlider != null)
                speedSlider.gameObject.SetActive(false);
        }
        
        private void UpdateLockState()
        {
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            
            if (lockedText != null && isLocked)
            {
                lockedText.text = L?.Get("tool.locked_star", itemData.requiredStarLevel) 
                    ?? $"★{itemData.requiredStarLevel} Required";
            }
        }
        
        private void UpdatePurchaseButton()
        {
            if (purchaseButton == null) return;
            
            if (isOwned)
            {
                purchaseButton.interactable = false;
                if (purchaseButtonText != null)
                    purchaseButtonText.text = L?.Get("tool.owned") ?? "OWNED";
            }
            else if (isLocked)
            {
                purchaseButton.interactable = false;
                if (purchaseButtonText != null)
                    purchaseButtonText.text = L?.Get("tool.locked") ?? "LOCKED";
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
            if (itemData == null || isLocked) return;
            onPurchaseCallback?.Invoke(itemData);
        }
        
        public ItemData GetItemData() => itemData;
        
#if UNITY_EDITOR
     //   [UnityEngine.ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            Color cardBg = new Color(0.85f, 0.9f, 0.95f);
            Color sliderBg = new Color(0.7f, 0.75f, 0.8f);
            Color sliderFill = new Color(0.5f, 0.6f, 0.7f);
            Color buttonColor = new Color(0.3f, 0.7f, 0.3f);
            
            // Card background
            var cardRect = GetComponent<RectTransform>();
            if (cardRect == null) cardRect = gameObject.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(180, 280);
            
            cardBackground = GetComponent<Image>();
            if (cardBackground == null) cardBackground = gameObject.AddComponent<Image>();
            cardBackground.color = cardBg;
            
            var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            
            // Icon container
            var iconContainer = CreateUIElement("IconContainer", transform);
            iconContainer.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var iconObj = CreateUIElement("Icon", iconContainer);
            SetRect(iconObj, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconImage = iconObj.gameObject.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // Name
            nameText = CreateText("Name", transform, "Gentle Pro Max", 14, Color.black);
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            
            // Stats container
            var statsContainer = CreateUIElement("StatsContainer", transform);
            var statsVlg = statsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            statsVlg.spacing = 3;
            statsVlg.childForceExpandWidth = true;
            statsVlg.childForceExpandHeight = false;
            
            // Scope
            scopeSlider = CreateStatRow("Scope", statsContainer, "Scope", sliderBg, sliderFill);
            painSlider = CreateStatRow("Pain", statsContainer, "Pain", sliderBg, sliderFill);
            powerSlider = CreateStatRow("Power", statsContainer, "Power", sliderBg, sliderFill);
            speedSlider = CreateStatRow("Speed", statsContainer, "Speed", sliderBg, sliderFill);
            
            // Description
            descriptionText = CreateText("Description", transform, "汎用的な脱毛レーザー", 10, new Color(0.3f, 0.3f, 0.3f));
            descriptionText.alignment = TextAlignmentOptions.Left;
            
            // Price
            priceText = CreateText("Price", transform, "¥12,000,000", 16, new Color(0.8f, 0.2f, 0.5f));
            priceText.fontStyle = FontStyles.Bold;
            priceText.alignment = TextAlignmentOptions.Right;
            
            // Purchase button
            var btnObj = CreateUIElement("PurchaseButton", transform);
            btnObj.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
            var btnImg = btnObj.gameObject.AddComponent<Image>();
            btnImg.color = buttonColor;
            purchaseButton = btnObj.gameObject.AddComponent<Button>();
            
            purchaseButtonText = CreateText("ButtonText", btnObj, "PURCHASE", 12, Color.white);
            SetRect(purchaseButtonText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            purchaseButtonText.alignment = TextAlignmentOptions.Center;
            
            Debug.Log("[ToolShopCardUI] UI structure generated!");
        }
        
        private Slider CreateStatRow(string name, RectTransform parent, string label, Color bgColor, Color fillColor)
        {
            var row = CreateUIElement(name + "Row", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            
            // Label
            var labelText = CreateText("Label", row, label, 10, Color.black);
            labelText.GetComponent<LayoutElement>().preferredWidth = 40;
            
            // Slider
            var sliderObj = CreateUIElement("Slider", row);
            sliderObj.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            var sliderBg = CreateUIElement("Background", sliderObj);
            SetRect(sliderBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            sliderBg.gameObject.AddComponent<Image>().color = bgColor;
            
            var fillArea = CreateUIElement("Fill Area", sliderObj);
            SetRect(fillArea, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            
            var fill = CreateUIElement("Fill", fillArea);
            SetRect(fill, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            fill.gameObject.AddComponent<Image>().color = fillColor;
            
            var slider = sliderObj.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.targetGraphic = sliderBg.gameObject.GetComponent<Image>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.interactable = false;
            
            return slider;
        }
        
        private RectTransform CreateUIElement(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            return rect;
        }
        
        private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            obj.AddComponent<LayoutElement>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            return tmp;
        }
        
        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
#endif
    }
}
