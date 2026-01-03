using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;

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
        [SerializeField] private Button placementTab;
        [SerializeField] private Button usefulTab;
        
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
        
        [Header("Immediate Delivery Toggle")]
        [SerializeField] private Toggle immediateDeliveryToggle;
        [SerializeField] private TMP_Text immediateDeliveryFeeText;
        
        [Header("Sell Panel")]
        [SerializeField] private Button sellToolsButton;
        [SerializeField] private GameObject sellPanel;
        [SerializeField] private Transform sellCardsContainer;
        [SerializeField] private GameObject sellCardPrefab;
        
        [Header("Sell Confirmation Dialog")]
        [SerializeField] private GameObject sellConfirmDialog;
        [SerializeField] private Image sellConfirmIcon;
        [SerializeField] private TMP_Text sellConfirmName;
        [SerializeField] private TMP_Text sellConfirmPrice;
        [SerializeField] private Button sellConfirmButton;
        [SerializeField] private Button sellCancelButton;
        
        private List<GameObject> toolCards = new List<GameObject>();
        private List<GameObject> sellCards = new List<GameObject>();
        private ItemData pendingPurchase;
        private ItemData pendingSell;
        private TreatmentToolType currentFilter = TreatmentToolType.Laser;
        private bool isSellMode = false;
        private bool isPlacementMode = false;
        private bool isUsefulMode = false;
        private bool isImmediateDelivery = false;
        
        // Delivery plan item IDs
        private const string PREMIUM_DELIVERY_PLAN_ID = "premium_delivery_plan";
        private const string EXECUTIVE_DELIVERY_PLAN_ID = "executive_delivery_plan";
        
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
            if (placementTab != null)
                placementTab.onClick.AddListener(ShowPlacementItems);
            if (usefulTab != null)
                usefulTab.onClick.AddListener(ShowUsefulItems);
            
            // Setup purchase dialog buttons
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmPurchase);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CloseConfirmDialog);
            
            // Setup immediate delivery toggle
            if (immediateDeliveryToggle != null)
                immediateDeliveryToggle.onValueChanged.AddListener(OnImmediateDeliveryToggled);
            
            // Setup sell mode button
            if (sellToolsButton != null)
                sellToolsButton.onClick.AddListener(ToggleSellMode);
            
            // Setup sell confirm dialog buttons
            if (sellConfirmButton != null)
                sellConfirmButton.onClick.AddListener(OnConfirmSell);
            if (sellCancelButton != null)
                sellCancelButton.onClick.AddListener(CloseSellConfirmDialog);
        }
        
        private void OnEnable()
        {
            RefreshDisplay();
            
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
            
            // Subscribe to shop upgrade to refresh when grade changes
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnShopUpgraded += OnShopUpgraded;
                
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
            
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnShopUpgraded -= OnShopUpgraded;
            
            CloseSellMode();
        }
        
        private void OnShopUpgraded(int newGrade)
        {
            RefreshCards();
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
            CloseSellMode();
        }
        
        private void SetFilter(TreatmentToolType filter)
        {
            currentFilter = filter;
            isPlacementMode = false;
            isUsefulMode = false;
            CloseSellMode();
            RefreshCards();
        }
        
        private void ShowPlacementItems()
        {
            isPlacementMode = true;
            isUsefulMode = false;
            CloseSellMode();
            RefreshCards();
        }
        
        private void ShowUsefulItems()
        {
            isPlacementMode = false;
            isUsefulMode = true;
            CloseSellMode();
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
                moneyText.text = $"${money:N0}";
            }
        }
        
        private void RefreshCards()
        {
            ClearCards();
            
            int currentStarLevel = GetCurrentStarLevel();
            
            if (isPlacementMode)
            {
                RefreshPlacementCards(currentStarLevel);
            }
            else if (isUsefulMode)
            {
                RefreshUsefulCards(currentStarLevel);
            }
            else
            {
                var tools = GetFilteredTools();
                foreach (var tool in tools)
                {
                    CreateToolCard(tool, currentStarLevel);
                }
            }
        }
        
        private void RefreshUsefulCards(int currentGrade)
        {
            if (ItemDataRegistry.Instance == null) return;
            
            var items = ItemDataRegistry.Instance.GetItemsByCategory(ItemCategory.Useful);
            int currentStarLevel = GetCurrentStarLevel();
            
            foreach (var item in items)
            {
                // Star level filter: hide if requiredStarLevel > currentStarLevel + 5
                int starDiff = item.requiredStarLevel - currentStarLevel;
                if (starDiff > 5) continue;
                
                // Check if already owned (one-time purchase items)
                bool isOwned = IsUsefulItemOwned(item.itemId);
                
                // For Useful items, use ToolShopCardUI with owned state
                CreateToolCard(item, currentStarLevel);
            }
        }
        
        private bool IsUsefulItemOwned(string itemId)
        {
            // Check WarehouseManager for useful items
            if (HairRemovalSim.Core.WarehouseManager.Instance != null)
            {
                return HairRemovalSim.Core.WarehouseManager.Instance.GetTotalItemCount(itemId) > 0;
            }
            return false;
        }
        
        private void RefreshPlacementCards(int currentStarLevel)
        {
            if (PlacementManager.Instance == null || ItemDataRegistry.Instance == null) return;
            
            var items = PlacementManager.Instance.GetAllPlacementItems();
            
            foreach (var item in items)
            {
                // Star level filter: hide if requiredStarLevel > currentStarLevel + 5
                int starDiff = item.requiredStarLevel - currentStarLevel;
                if (starDiff > 5) continue;
                
                bool isOwned = PlacementManager.Instance.IsOwned(item.itemId);
                CreatePlacementCard(item, currentStarLevel, isOwned);
            }
        }
        
        private void CreatePlacementCard(ItemData item, int currentStarLevel, bool isOwned)
        {
            if (toolCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(toolCardPrefab, cardsContainer);
            toolCards.Add(cardObj);
            
            var card = cardObj.GetComponent<ToolShopCardUI>();
            if (card != null)
            {
                // Always pass callback, card will handle owned state via isOwned flag
                card.Setup(item, currentStarLevel, OnPlacementPurchaseCallback, isOwned);
            }
        }
        
        private void OnPlacementPurchaseCallback(ItemData item)
        {
            pendingPurchase = item;
            ShowConfirmDialog(item);
        }
        
        private void OnPlacementPurchaseRequested(ItemData item)
        {
            pendingPurchase = item;
            ShowConfirmDialog(item);
        }
        
        private void OnSellPlacementItem(ItemData item)
        {
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.RemoveItem(item.itemId);
                RefreshCards();
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
        
        private void CreateToolCard(ItemData tool, int currentStarLevel)
        {
            if (toolCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(toolCardPrefab, cardsContainer);
            var cardUI = cardObj.GetComponent<ToolShopCardUI>();
            
            if (cardUI != null)
            {
                cardUI.Setup(tool, currentStarLevel, OnToolPurchaseRequested);
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
            
            // Reset immediate delivery toggle
            isImmediateDelivery = false;
            if (immediateDeliveryToggle != null)
            {
                immediateDeliveryToggle.isOn = false;
                
                // Hide toggle for Placement and Useful items (they are instant/special)
                bool showToggle = !isPlacementMode && !isUsefulMode;
                immediateDeliveryToggle.gameObject.SetActive(showToggle);
                
                if (immediateDeliveryFeeText != null)
                    immediateDeliveryFeeText.gameObject.SetActive(showToggle);
            }
            
            // Icon
            if (confirmItemIcon != null && tool.icon != null)
                confirmItemIcon.sprite = tool.icon;

            // Name
            if (confirmItemName != null)
                confirmItemName.text = L?.Get(tool.nameKey); ;
            
            // Price
            UpdateConfirmDialogPrice(tool);
            
            // Delivery info
            UpdateDeliveryInfo(tool);
            
            // Hide delivery info and ensure toggle logic consistency for Placement/Useful
            if (isPlacementMode || isUsefulMode)
            {
                if (deliveryInfoText != null) deliveryInfoText.gameObject.SetActive(false);
                if (immediateDeliveryFeeText != null) immediateDeliveryFeeText.gameObject.SetActive(false);
            }
            else
            {
                if (deliveryInfoText != null) deliveryInfoText.gameObject.SetActive(true);
                // immediateDeliveryFeeText visibility is handled by toggle state logic or UpdateImmediateDeliveryFeeText if needed, 
                // but commonly we only show it if toggle is on.
                // Re-evaluate toggle visibility just in case
            }
        }
        
        private void UpdateConfirmDialogPrice(ItemData tool)
        {
            if (confirmPriceText == null) return;
            
            int basePrice = tool.price;
            int deliveryFee = isImmediateDelivery ? CalculateImmediateDeliveryFee(basePrice) : 0;
            int totalPrice = basePrice + deliveryFee;
            
            if (deliveryFee > 0)
            {
                float feePercent = GetImmediateDeliveryFeePercent() * 100f;
                confirmPriceText.text = $"${totalPrice:N0} (+{feePercent:F0}%)";
            }
            else
            {
                confirmPriceText.text = $"${basePrice:N0}";
            }
        }
        
        private void UpdateDeliveryInfo(ItemData tool)
        {
            if (deliveryInfoText == null) return;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            
            if (isImmediateDelivery)
            {
                deliveryInfoText.text = L?.Get("toolshop.delivery_immediate") ?? "Delivery: Immediate";
            }
            else
            {
                deliveryInfoText.text = L?.Get("toolshop.delivery_info", currentDay + 1) 
                    ?? $"Delivery: Day {currentDay + 1}";
            }
            
            // Update delivery fee text
            UpdateImmediateDeliveryFeeText(tool);
        }
        
        private void UpdateImmediateDeliveryFeeText(ItemData tool)
        {
            if (immediateDeliveryFeeText == null) return;
            
            int fee = CalculateImmediateDeliveryFee(tool.price);
            float feePercent = GetImmediateDeliveryFeePercent() * 100f;
            
            if (fee == 0)
            {
                immediateDeliveryFeeText.text = "$0";
            }
            else
            {
                immediateDeliveryFeeText.text = $"${fee:N0} (+{feePercent:F0}%)";
            }
        }
        
        private void OnImmediateDeliveryToggled(bool isOn)
        {
            isImmediateDelivery = isOn;
            if (pendingPurchase != null)
            {
                UpdateConfirmDialogPrice(pendingPurchase);
                UpdateDeliveryInfo(pendingPurchase);
            }
        }
        
        /// <summary>
        /// Get immediate delivery fee percentage based on owned delivery plans
        /// </summary>
        private float GetImmediateDeliveryFeePercent()
        {
            // Check for Executive plan (free delivery)
            if (IsUsefulItemOwned(EXECUTIVE_DELIVERY_PLAN_ID))
                return 0f;
            
            // Check for Premium plan (10% fee)
            if (IsUsefulItemOwned(PREMIUM_DELIVERY_PLAN_ID))
                return 0.10f;
            
            // Default: 20% fee
            return 0.20f;
        }
        
        /// <summary>
        /// Calculate immediate delivery fee for a given price
        /// </summary>
        private int CalculateImmediateDeliveryFee(int basePrice)
        {
            float percent = GetImmediateDeliveryFeePercent();
            return Mathf.RoundToInt(basePrice * percent);
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
            
            // Handle placement items differently
            if (isPlacementMode)
            {
                if (PlacementManager.Instance != null)
                {
                    if (PlacementManager.Instance.PlaceItem(pendingPurchase))
                    {
                        Debug.Log($"[ToolShopPanel] Placed: {pendingPurchase.name}");
                    }
                    else
                    {
                        Debug.Log("[ToolShopPanel] Failed to place item (not enough money or already owned)");
                    }
                }
                CloseConfirmDialog();
                RefreshCards();
                return;
            }
            
            // Calculate total price with delivery fee
            int basePrice = pendingPurchase.price;
            int deliveryFee = isImmediateDelivery ? CalculateImmediateDeliveryFee(basePrice) : 0;
            int totalPrice = basePrice + deliveryFee;
            
            // Check money
            if (EconomyManager.Instance == null || 
                !EconomyManager.Instance.SpendMoney(totalPrice))
            {
                Debug.Log("[ToolShopPanel] Not enough money");
                CloseConfirmDialog();
                return;
            }
            
            // Deliver immediately or schedule for next day
            if (isImmediateDelivery)
            {
                DeliverImmediately(pendingPurchase);
                Debug.Log($"[ToolShopPanel] Purchased: {pendingPurchase.name} - Immediate delivery");
            }
            else
            {
                ScheduleDelivery(pendingPurchase);
                Debug.Log($"[ToolShopPanel] Purchased: {pendingPurchase.name} - Delivery scheduled for tomorrow");
            }
            
            CloseConfirmDialog();
            RefreshCards();
        }
        
        private void DeliverImmediately(ItemData item)
        {
            // Deliver directly to warehouse
            if (HairRemovalSim.Core.WarehouseManager.Instance != null)
            {
                HairRemovalSim.Core.WarehouseManager.Instance.AddItem(item.itemId, 1);
                HairRemovalSim.Core.WarehouseManager.Instance.ShowNewIndicator();
                Debug.Log($"[ToolShopPanel] {item.name} delivered immediately to warehouse");
            }
            else
            {
                Debug.LogWarning($"[ToolShopPanel] WarehouseManager not found, immediate delivery failed");
            }
        }
        
        private void ScheduleDelivery(ItemData item)
        {
            // Use InventoryManager.AddPendingOrder for next-day delivery
            if (HairRemovalSim.Store.InventoryManager.Instance != null)
            {
                HairRemovalSim.Store.InventoryManager.Instance.AddPendingOrder(item, 1);
                Debug.Log($"[ToolShopPanel] {item.name} ordered - will be delivered tomorrow");
            }
            else
            {
                Debug.LogWarning($"[ToolShopPanel] InventoryManager not found, delivery not scheduled");
            }
        }
        
        private int GetCurrentShopGrade()
        {
            return ShopManager.Instance?.ShopGrade ?? 1;
        }
        
        private int GetCurrentStarLevel()
        {
            return ShopManager.Instance?.StarRating ?? 1;
        }
        
        private List<ItemData> GetFilteredTools()
        {
            if (ItemDataRegistry.Instance == null)
                return new List<ItemData>();
            
            int currentStarLevel = GetCurrentStarLevel();
            
            // Get treatment tools
            var toolItems = ItemDataRegistry.Instance.GetItemsByCategory(ItemCategory.TreatmentTool);
            var legacyTools = ItemDataRegistry.Instance.GetItemsByCategory(ItemCategory.Tool);
            
            foreach (var tool in legacyTools)
            {
                if (tool.IsTreatmentTool && !toolItems.Contains(tool))
                    toolItems.Add(tool);
            }
            
            // Filter by current type and star level visibility
            var filtered = new List<ItemData>();
            foreach (var tool in toolItems)
            {
                if (tool.toolType != currentFilter)
                    continue;
                
                // Star level filter: hide if requiredStarLevel > currentStarLevel + 5
                // Show locked if requiredStarLevel > currentStarLevel
                // Show unlocked if requiredStarLevel <= currentStarLevel
                int starDiff = tool.requiredStarLevel - currentStarLevel;
                if (starDiff > 5)
                    continue; // Hide completely
                
                filtered.Add(tool);
            }
            
            // Sort by star level then price
            filtered.Sort((a, b) => {
                int starCompare = a.requiredStarLevel.CompareTo(b.requiredStarLevel);
                return starCompare != 0 ? starCompare : a.price.CompareTo(b.price);
            });
            
            return filtered;
        }
        
        // ==========================================
        // Sell Mode Functions
        // ==========================================
        
        /// <summary>
        /// Toggle sell mode on/off
        /// </summary>
        public void ToggleSellMode()
        {
            isSellMode = !isSellMode;
            
            if (sellPanel != null)
                sellPanel.SetActive(isSellMode);
            
            if (isSellMode)
            {
                RefreshSellCards();
            }
            else
            {
                ClearSellCards();
            }
        }
        
        /// <summary>
        /// Close sell mode without toggle
        /// </summary>
        private void CloseSellMode()
        {
            if (!isSellMode) return;
            
            isSellMode = false;
            if (sellPanel != null)
                sellPanel.SetActive(false);
            ClearSellCards();
            CloseSellConfirmDialog();
        }
        
        /// <summary>
        /// Refresh the sell cards list from warehouse
        /// </summary>
        private void RefreshSellCards()
        {
            ClearSellCards();
            
            var warehouseManager = HairRemovalSim.Core.WarehouseManager.Instance;
            if (warehouseManager == null)
            {
                Debug.LogWarning("[ToolShopPanel] WarehouseManager not found!");
                return;
            }
            
            var warehouseItems = warehouseManager.GetAllItems();
            Debug.Log($"[ToolShopPanel] Found {warehouseItems.Count} items in warehouse");
            
            foreach (var kvp in warehouseItems)
            {
                var itemData = ItemDataRegistry.Instance?.GetItem(kvp.Key);
                Debug.Log($"[ToolShopPanel] Checking item: {kvp.Key}, qty: {kvp.Value}, itemData: {itemData?.name ?? "null"}, IsTool: {itemData?.IsTreatmentTool}");
                
                if (itemData != null && itemData.cantSell)
                {
                    Debug.Log($"[ToolShopPanel] Skipping {kvp.Key} - cantSell is true");
                    continue;
                }
                
                if (itemData != null && itemData.IsTreatmentTool && kvp.Value > 0)
                {
                    CreateSellCard(itemData, kvp.Value);
                }
            }
        }
        
        private void ClearSellCards()
        {
            foreach (var card in sellCards)
            {
                if (card != null) Destroy(card);
            }
            sellCards.Clear();
        }
        
        private void CreateSellCard(ItemData tool, int quantity)
        {
            if (sellCardPrefab == null || sellCardsContainer == null) return;
            
            var cardObj = Instantiate(sellCardPrefab, sellCardsContainer);
            var cardUI = cardObj.GetComponent<ToolSellCardUI>();
            
            if (cardUI != null)
            {
                int sellPrice = tool.price / 2; // Half price
                cardUI.Setup(tool, quantity, sellPrice, OnSellRequested);
            }
            
            sellCards.Add(cardObj);
        }
        
        private void OnSellRequested(ItemData tool)
        {
            pendingSell = tool;
            ShowSellConfirmDialog(tool);
        }
        
        private void ShowSellConfirmDialog(ItemData tool)
        {
            if (sellConfirmDialog == null) return;
            
            sellConfirmDialog.SetActive(true);
            
            if (sellConfirmIcon != null && tool.icon != null)
                sellConfirmIcon.sprite = tool.icon;
            
            if (sellConfirmName != null)
            {
                sellConfirmName.text = tool.GetLocalizedName();
            }
            
            if (sellConfirmPrice != null)
            {
                int sellPrice = tool.price / 2;
                sellConfirmPrice.text = $"+${sellPrice:N0}";
            }
        }
        
        private void CloseSellConfirmDialog()
        {
            if (sellConfirmDialog != null)
                sellConfirmDialog.SetActive(false);
            pendingSell = null;
        }
        
        private void OnConfirmSell()
        {
            if (pendingSell == null) return;
            
            // Remove from warehouse
            if (HairRemovalSim.Core.WarehouseManager.Instance != null)
            {
                int removed = HairRemovalSim.Core.WarehouseManager.Instance.RemoveItem(pendingSell.itemId, 1);
                if (removed > 0)
                {
                    // Add money (half price)
                    int sellPrice = pendingSell.price / 2;
                    EconomyManager.Instance?.AddMoney(sellPrice);
                    
                    Debug.Log($"[ToolShopPanel] Sold {pendingSell.name} for ${sellPrice}");
                    
                    // Update UI
                    UpdateMoneyDisplay();
                    RefreshSellCards();
                }
                else
                {
                    Debug.LogWarning("[ToolShopPanel] Failed to remove item from warehouse");
                }
            }
            
            CloseSellConfirmDialog();
        }
        
#if UNITY_EDITOR
       // [UnityEngine.ContextMenu("Generate UI Structure")]
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
            Color sliderBg = new Color(0.7f, 0.75f, 0.8f);
            Color sliderFill = new Color(0.5f, 0.6f, 0.7f);
            
            var card = CreateUIElement("ToolCard_Sample", parent);
            card.sizeDelta = new Vector2(180, 280);
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
            iconContainer.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var iconImg = CreateUIElement("Icon", iconContainer);
            SetRect(iconImg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconImg.gameObject.AddComponent<Image>().color = Color.white;
            
            // Name
            var nameText = CreateText("Name", card, "Gentle Pro Max", 14, Color.black);
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            
            // Stats Container
            var statsContainer = CreateUIElement("StatsContainer", card);
            var statsVlg = statsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            statsVlg.spacing = 3;
            statsVlg.childForceExpandWidth = true;
            statsVlg.childForceExpandHeight = false;
            
            // Stat rows
            CreateStatRow("Scope", statsContainer, sliderBg, sliderFill);
            CreateStatRow("Pain", statsContainer, sliderBg, sliderFill);
            CreateStatRow("Power", statsContainer, sliderBg, sliderFill);
            CreateStatRow("Speed", statsContainer, sliderBg, sliderFill);
            
            // Description
            var descText = CreateText("Description", card, "汎用的な脱毛レーザー", 10, new Color(0.3f, 0.3f, 0.3f));
            descText.alignment = TextAlignmentOptions.Left;
            
            // Price
            var priceText = CreateText("Price", card, "¥12,000,000", 14, new Color(0.8f, 0.2f, 0.5f));
            priceText.fontStyle = FontStyles.Bold;
            priceText.alignment = TextAlignmentOptions.Right;
            
            // Button
            var btnObj = CreateUIElement("PurchaseButton", card);
            btnObj.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
            var btnImg = btnObj.gameObject.AddComponent<Image>();
            btnImg.color = buttonColor;
            btnObj.gameObject.AddComponent<Button>();
            
            var btnText = CreateText("ButtonText", btnObj, "PURCHASE", 12, Color.white);
            SetRect(btnText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btnText.alignment = TextAlignmentOptions.Center;
            
            // Add ToolShopCardUI component
            card.gameObject.AddComponent<ToolShopCardUI>();
            
            Debug.Log("[ToolShopPanel] Sample tool card created. Save as prefab and assign references!");
        }
        
        private void CreateStatRow(string label, RectTransform parent, Color bgColor, Color fillColor)
        {
            var row = CreateUIElement(label + "Row", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            
            // Label
            var labelText = CreateText("Label", row, label, 10, Color.black);
            labelText.GetComponent<LayoutElement>().preferredWidth = 40;
            
            // Slider background
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
            slider.value = 50;
            slider.interactable = false;
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
