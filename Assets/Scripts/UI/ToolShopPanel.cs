using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Tool shop panel for purchasing treatment tools
    /// Features: Category filter, confirmation dialog, next-day delivery
    /// </summary>
    public class ToolShopPanel : MonoBehaviour
    {
        public static ToolShopPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text moneyText;
        
        [Header("Category Tabs")]
        [SerializeField] private Button lasersTab;
        [SerializeField] private Button shaversTab;
        [SerializeField] private Button coolingTab;
        [SerializeField] private Button miscTab;
        
        [Header("Tool Cards")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private GameObject toolCardPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Image confirmItemIcon;
        [SerializeField] private TMP_Text confirmItemName;
        [SerializeField] private TMP_Text confirmPriceText;
        [SerializeField] private TMP_Text deliveryInfoText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private List<GameObject> toolCards = new List<GameObject>();
        private List<string> ownedToolIds = new List<string>();
        private ItemData pendingPurchase;
        private TreatmentToolType currentFilter = TreatmentToolType.Laser;
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
            
            // Setup tab buttons
            if (lasersTab != null)
                lasersTab.onClick.AddListener(() => SetFilter(TreatmentToolType.Laser));
            if (shaversTab != null)
                shaversTab.onClick.AddListener(() => SetFilter(TreatmentToolType.Shaver));
            if (coolingTab != null)
                coolingTab.onClick.AddListener(() => SetFilter(TreatmentToolType.None)); // For cooling items
            if (miscTab != null)
                miscTab.onClick.AddListener(() => SetFilter(TreatmentToolType.Other));
            
            // Setup dialog buttons
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmPurchase);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CloseConfirmDialog);
        }
        
        private void OnEnable()
        {
            RefreshDisplay();
            
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
                
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshDisplay();
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            CloseConfirmDialog();
        }
        
        private void SetFilter(TreatmentToolType filter)
        {
            currentFilter = filter;
            RefreshCards();
        }
        
        public void RefreshDisplay()
        {
            UpdateHeader();
            RefreshCards();
        }
        
        private void UpdateHeader()
        {
            if (titleText != null)
                titleText.text = L?.Get("toolshop.title") ?? "Medi-Supply Pro - Equipment Store";
            
            UpdateMoneyDisplay();
        }
        
        private void UpdateMoneyDisplay()
        {
            if (moneyText != null)
            {
                int money = EconomyManager.Instance?.CurrentMoney ?? 0;
                moneyText.text = $"¥{money:N0}";
            }
        }
        
        private void RefreshCards()
        {
            ClearCards();
            
            int currentGrade = GetCurrentShopGrade();
            var tools = GetFilteredTools();
            
            foreach (var tool in tools)
            {
                CreateToolCard(tool, currentGrade);
            }
        }
        
        private void ClearCards()
        {
            foreach (var card in toolCards)
            {
                if (card != null)
                    Destroy(card);
            }
            toolCards.Clear();
        }
        
        private void CreateToolCard(ItemData tool, int currentGrade)
        {
            if (toolCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(toolCardPrefab, cardsContainer);
            var cardUI = cardObj.GetComponent<ToolShopCardUI>();
            
            if (cardUI != null)
            {
                bool isOwned = ownedToolIds.Contains(tool.itemId);
                cardUI.Setup(tool, currentGrade, isOwned, OnToolPurchaseRequested);
            }
            
            toolCards.Add(cardObj);
        }
        
        private void OnToolPurchaseRequested(ItemData tool)
        {
            pendingPurchase = tool;
            ShowConfirmDialog(tool);
        }
        
        private void ShowConfirmDialog(ItemData tool)
        {
            if (confirmDialog == null) return;
            
            confirmDialog.SetActive(true);
            
            // Icon
            if (confirmItemIcon != null && tool.icon != null)
                confirmItemIcon.sprite = tool.icon;
            
            // Name
            if (confirmItemName != null)
                confirmItemName.text = tool.displayName;
            
            // Price
            if (confirmPriceText != null)
                confirmPriceText.text = $"¥{tool.price:N0}";
            
            // Delivery info
            if (deliveryInfoText != null)
            {
                int currentDay = GameManager.Instance?.DayCount ?? 1;
                deliveryInfoText.text = L?.Get("toolshop.delivery_info", currentDay + 1) 
                    ?? $"Delivery: Day {currentDay + 1}";
            }
        }
        
        private void CloseConfirmDialog()
        {
            if (confirmDialog != null)
                confirmDialog.SetActive(false);
            pendingPurchase = null;
        }
        
        private void OnConfirmPurchase()
        {
            if (pendingPurchase == null) return;
            
            // Check money
            if (EconomyManager.Instance == null || 
                !EconomyManager.Instance.SpendMoney(pendingPurchase.price))
            {
                Debug.Log("[ToolShopPanel] Not enough money");
                CloseConfirmDialog();
                return;
            }
            
            // Schedule delivery for next day
            ScheduleDelivery(pendingPurchase);
            
            Debug.Log($"[ToolShopPanel] Purchased: {pendingPurchase.displayName} - Delivery scheduled");
            
            CloseConfirmDialog();
            RefreshCards();
        }
        
        private void ScheduleDelivery(ItemData item)
        {
            // Add to owned tools (will be available after delivery)
            ownedToolIds.Add(item.itemId);
            
            // TODO: Add to pending deliveries list with delivery day
            int deliveryDay = (GameManager.Instance?.DayCount ?? 1) + 1;
            Debug.Log($"[ToolShopPanel] {item.displayName} scheduled for delivery on Day {deliveryDay}");
            
            // TODO: Integrate with a DeliveryManager or WarehouseManager
            // DeliveryManager.Instance?.ScheduleDelivery(item.itemId, deliveryDay);
        }
        
        private int GetCurrentShopGrade()
        {
            return ShopManager.Instance?.StarRating ?? 1;
        }
        
        private List<ItemData> GetFilteredTools()
        {
            if (ItemDataRegistry.Instance == null)
                return new List<ItemData>();
            
            // Get treatment tools
            var toolItems = ItemDataRegistry.Instance.GetItemsByCategory(ItemCategory.TreatmentTool);
            var legacyTools = ItemDataRegistry.Instance.GetItemsByCategory(ItemCategory.Tool);
            
            foreach (var tool in legacyTools)
            {
                if (tool.IsTreatmentTool && !toolItems.Contains(tool))
                    toolItems.Add(tool);
            }
            
            // Filter by current type
            var filtered = new List<ItemData>();
            foreach (var tool in toolItems)
            {
                if (tool.toolType == currentFilter)
                    filtered.Add(tool);
            }
            
            // Sort by grade then price
            filtered.Sort((a, b) => {
                int gradeCompare = a.requiredShopGrade.CompareTo(b.requiredShopGrade);
                return gradeCompare != 0 ? gradeCompare : a.price.CompareTo(b.price);
            });
            
            return filtered;
        }
        
        /// <summary>
        /// Check if player owns a specific tool
        /// </summary>
        public bool OwnsTool(string itemId)
        {
            return ownedToolIds.Contains(itemId);
        }
        
        /// <summary>
        /// Get list of owned tool IDs (for save/load)
        /// </summary>
        public List<string> GetOwnedToolIds()
        {
            return new List<string>(ownedToolIds);
        }
        
        /// <summary>
        /// Set owned tool IDs (for load)
        /// </summary>
        public void SetOwnedToolIds(List<string> ids)
        {
            ownedToolIds = ids ?? new List<string>();
            RefreshCards();
        }
        
#if UNITY_EDITOR
        [UnityEngine.ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            // Colors
            Color headerBg = new Color(0.15f, 0.35f, 0.55f);
            Color panelBg = new Color(0.2f, 0.4f, 0.6f);
            Color tabActive = new Color(0.3f, 0.5f, 0.7f);
            Color tabInactive = new Color(0.2f, 0.35f, 0.5f);
            Color cardBg = new Color(0.95f, 0.97f, 1f);
            Color buttonColor = new Color(0.3f, 0.7f, 0.3f);
            
            // Main Panel
            var panelObj = new GameObject("ToolShopPanel");
            panelObj.transform.SetParent(transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            SetRect(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panelObj.AddComponent<Image>().color = panelBg;
            panel = panelObj;
            
            // Header
            var header = CreateUIElement("Header", panelObj.transform);
            SetRect(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 50));
            header.gameObject.AddComponent<Image>().color = headerBg;
            
            // Title
            titleText = CreateText("Title", header, "Medi-Supply Pro - Equipment Store", 24, Color.white);
            SetRect(titleText.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(20, 0), Vector2.zero);
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Money display
            moneyText = CreateText("Money", header, "¥5,200,000", 20, Color.white);
            SetRect(moneyText.GetComponent<RectTransform>(), new Vector2(0.8f, 0), new Vector2(1, 1), new Vector2(-20, 0), Vector2.zero);
            moneyText.alignment = TextAlignmentOptions.MidlineRight;
            
            // Tab Container
            var tabContainer = CreateUIElement("TabContainer", panelObj.transform);
            SetRect(tabContainer, new Vector2(0, 1), new Vector2(0.15f, 1), new Vector2(0, -200), new Vector2(0, 150));
            var tabVlg = tabContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            tabVlg.spacing = 5;
            tabVlg.childForceExpandWidth = true;
            tabVlg.childForceExpandHeight = false;
            
            // Tabs
            lasersTab = CreateTabButton("LasersTab", tabContainer, "Lasers", tabActive);
            shaversTab = CreateTabButton("ShaversTab", tabContainer, "Shavers", tabInactive);
            coolingTab = CreateTabButton("CoolingTab", tabContainer, "Cooling", tabInactive);
            miscTab = CreateTabButton("MiscTab", tabContainer, "Misc", tabInactive);
            
            // Cards Container with ScrollView
            var scrollView = CreateUIElement("ScrollView", panelObj.transform);
            SetRect(scrollView, new Vector2(0.15f, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -60));
            scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            
            // Viewport (required for ScrollRect)
            var viewport = CreateUIElement("Viewport", scrollView);
            SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            
            // Content
            var content = CreateUIElement("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 0);
            
            var glg = content.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(200, 280);
            glg.spacing = new Vector2(15, 15);
            glg.padding = new RectOffset(15, 15, 15, 15);
            glg.childAlignment = TextAnchor.UpperLeft;
            
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Setup ScrollRect
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            
            cardsContainer = content;
            
            // Sample Tool Card
            CreateSampleToolCard(content, cardBg, buttonColor);
            
            // Confirmation Dialog
            CreateConfirmDialog(panelObj.transform);
            
            Debug.Log("[ToolShopPanel] UI structure generated!");
        }
        
        private void CreateSampleToolCard(Transform parent, Color cardBg, Color buttonColor)
        {
            var card = CreateUIElement("ToolCard_Sample", parent);
            card.sizeDelta = new Vector2(200, 280);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = cardBg;
            
            var cardVlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardVlg.padding = new RectOffset(10, 10, 10, 10);
            cardVlg.spacing = 5;
            cardVlg.childForceExpandWidth = true;
            cardVlg.childForceExpandHeight = false;
            cardVlg.childControlHeight = true;
            
            // Icon
            var iconContainer = CreateUIElement("IconContainer", card);
            iconContainer.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var iconImg = CreateUIElement("Icon", iconContainer);
            SetRect(iconImg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconImg.gameObject.AddComponent<Image>().color = Color.white;
            
            // Name
            var nameText = CreateText("Name", card, "Gentle Pro Max", 16, Color.black);
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            
            // Stats
            var statsText = CreateText("Stats", card, "[Scope: Wide] [Pain: Low] [Speed: Fast]", 10, new Color(0.3f, 0.3f, 0.3f));
            statsText.alignment = TextAlignmentOptions.Center;
            
            // Price
            var priceText = CreateText("Price", card, "¥12,000,000", 18, new Color(0.1f, 0.1f, 0.5f));
            priceText.fontStyle = FontStyles.Bold;
            priceText.alignment = TextAlignmentOptions.Center;
            
            // Button
            var btnObj = CreateUIElement("PurchaseButton", card);
            btnObj.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;
            var btnImg = btnObj.gameObject.AddComponent<Image>();
            btnImg.color = buttonColor;
            btnObj.gameObject.AddComponent<Button>();
            
            var btnText = CreateText("ButtonText", btnObj, "PURCHASE", 14, Color.white);
            SetRect(btnText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btnText.alignment = TextAlignmentOptions.Center;
            
            Debug.Log("[ToolShopPanel] Sample tool card created. Save as prefab!");
        }
        
        private void CreateConfirmDialog(Transform parent)
        {
            var dialog = CreateUIElement("ConfirmDialog", parent);
            SetRect(dialog, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), Vector2.zero, Vector2.zero);
            dialog.gameObject.AddComponent<Image>().color = Color.white;
            confirmDialog = dialog.gameObject;
            
            var vlg = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 15;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            
            // Title
            var titleText = CreateText("DialogTitle", dialog, "Order Details", 18, Color.black);
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            
            // Icon
            var iconContainer = CreateUIElement("IconContainer", dialog);
            var iconLE = iconContainer.gameObject.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 80;
            iconLE.preferredWidth = 80;
            confirmItemIcon = iconContainer.gameObject.AddComponent<Image>();
            confirmItemIcon.color = Color.white;
            
            // Item name
            confirmItemName = CreateText("ItemName", dialog, "Cooling Gel", 16, Color.black);
            confirmItemName.alignment = TextAlignmentOptions.Center;
            
            // Price
            confirmPriceText = CreateText("Price", dialog, "¥1,000", 20, new Color(0.8f, 0.2f, 0.5f));
            confirmPriceText.fontStyle = FontStyles.Bold;
            confirmPriceText.alignment = TextAlignmentOptions.Center;
            
            // Delivery info
            deliveryInfoText = CreateText("DeliveryInfo", dialog, "Delivery: Day 2", 12, Color.gray);
            deliveryInfoText.alignment = TextAlignmentOptions.Center;
            
            // Buttons row
            var btnRow = CreateUIElement("ButtonRow", dialog);
            btnRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            var hlg = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            
            // Cancel button
            var cancelObj = CreateUIElement("CancelButton", btnRow);
            var cancelImg = cancelObj.gameObject.AddComponent<Image>();
            cancelImg.color = new Color(0.8f, 0.8f, 0.8f);
            cancelButton = cancelObj.gameObject.AddComponent<Button>();
            var cancelText = CreateText("Text", cancelObj, "Cancel", 14, Color.black);
            SetRect(cancelText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cancelText.alignment = TextAlignmentOptions.Center;
            
            // Confirm button
            var confirmObj = CreateUIElement("ConfirmButton", btnRow);
            var confirmImg = confirmObj.gameObject.AddComponent<Image>();
            confirmImg.color = new Color(0.5f, 0.7f, 0.9f);
            confirmButton = confirmObj.gameObject.AddComponent<Button>();
            var confirmText = CreateText("Text", confirmObj, "Confirm", 14, Color.white);
            SetRect(confirmText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            confirmText.alignment = TextAlignmentOptions.Center;
            
            dialog.gameObject.SetActive(false);
        }
        
        private Button CreateTabButton(string name, Transform parent, string label, Color bgColor)
        {
            var tab = CreateUIElement(name, parent);
            tab.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;
            var img = tab.gameObject.AddComponent<Image>();
            img.color = bgColor;
            var btn = tab.gameObject.AddComponent<Button>();
            
            var text = CreateText("Text", tab, label, 14, Color.white);
            SetRect(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
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
