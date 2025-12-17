using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Editor script to setup Advertising Panel UI programmatically
    /// Attach to an empty GameObject and call Setup from context menu
    /// </summary>
    public class AdvertisingPanelSetup : MonoBehaviour
    {
        [Header("Colors")]
        public Color headerColor = new Color(0.2f, 0.4f, 0.7f, 1f);  // Dark blue
        public Color panelColor = new Color(0.15f, 0.35f, 0.6f, 1f); // Slightly darker
        public Color cardColor = new Color(0.3f, 0.5f, 0.8f, 1f);    // Light blue
        public Color accentColor = new Color(0.1f, 0.6f, 0.3f, 1f);  // Green
        public Color textColor = Color.white;
        public Color freeColor = new Color(0.2f, 0.6f, 0.2f, 1f);    // Green for free
        public Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);  // Gray

        [Header("Fonts")]
        public TMP_FontAsset font;
        
        [Header("References (Auto-populated)")]
        public AdvertisingPanel advertisingPanel;
        public RectTransform availableAdsContainer;
        public RectTransform activeAdsContainer;

        private Canvas canvas;
        private RectTransform mainPanel;

        [ContextMenu("Setup Advertising Panel UI")]
        public void SetupUI()
        {
            // Get or create canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("AdvertisingPanelSetup must be under a Canvas!");
                return;
            }

            // Create main container
            CreateMainPanel();
            
            // Create sample prefabs inside containers
            CreateSampleAdCard();
            CreateSampleActiveAdCard();
            
            // Setup AdvertisingPanel component
            advertisingPanel = gameObject.GetComponent<AdvertisingPanel>();
            if (advertisingPanel == null)
                advertisingPanel = gameObject.AddComponent<AdvertisingPanel>();

            Debug.Log("[AdvertisingPanelSetup] UI Setup complete!");
        }

        private void CreateMainPanel()
        {
            // Main background panel
            mainPanel = CreatePanel("AdvertisingPanel", transform, panelColor);
            mainPanel.anchorMin = Vector2.zero;
            mainPanel.anchorMax = Vector2.one;
            mainPanel.offsetMin = new Vector2(50, 50);
            mainPanel.offsetMax = new Vector2(-50, -50);

            // Header
            CreateHeader();

            // Content area with two columns
            var contentArea = CreatePanel("ContentArea", mainPanel, Color.clear);
            contentArea.anchorMin = new Vector2(0, 0);
            contentArea.anchorMax = new Vector2(1, 0.9f);
            contentArea.offsetMin = new Vector2(10, 10);
            contentArea.offsetMax = new Vector2(-10, -10);

            // Left column - Marketing Status (35% width)
            CreateMarketingStatusPanel(contentArea);

            // Right column - Ad Campaigns (65% width)
            CreateAdCampaignsPanel(contentArea);
        }

        private void CreateHeader()
        {
            var header = CreatePanel("Header", mainPanel, headerColor);
            header.anchorMin = new Vector2(0, 0.9f);
            header.anchorMax = new Vector2(1, 1);
            header.offsetMin = Vector2.zero;
            header.offsetMax = Vector2.zero;

            // Title
            var titleObj = CreateText("Title", header, "Marketing & Ad Management", 24);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = Vector2.zero;
            titleObj.alignment = TextAlignmentOptions.MidlineLeft;

            // Money display
            var moneyObj = CreateText("MoneyText", header, "¥5,200,000", 20);
            var moneyRect = moneyObj.GetComponent<RectTransform>();
            moneyRect.anchorMin = new Vector2(0.8f, 0);
            moneyRect.anchorMax = new Vector2(1, 1);
            moneyRect.offsetMin = Vector2.zero;
            moneyRect.offsetMax = new Vector2(-20, 0);
            moneyObj.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void CreateMarketingStatusPanel(RectTransform parent)
        {
            var leftPanel = CreatePanel("MarketingStatusPanel", parent, new Color(0.2f, 0.2f, 0.25f, 1f));
            leftPanel.anchorMin = new Vector2(0, 0);
            leftPanel.anchorMax = new Vector2(0.35f, 1);
            leftPanel.offsetMin = Vector2.zero;
            leftPanel.offsetMax = new Vector2(-5, 0);

            // Section title
            var sectionTitle = CreateText("SectionTitle", leftPanel, "Current Marketing Status", 16);
            var titleRect = sectionTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.92f);
            titleRect.anchorMax = new Vector2(1, 1);
            sectionTitle.alignment = TextAlignmentOptions.Center;

            // Attraction Rate Section
            CreateAttractionRateSection(leftPanel);

            // VIP Factor Section
            CreateVIPFactorSection(leftPanel);

            // Targeting Section
            CreateTargetingSection(leftPanel);
        }

        private void CreateAttractionRateSection(RectTransform parent)
        {
            var section = CreatePanel("AttractionSection", parent, Color.clear);
            section.anchorMin = new Vector2(0.05f, 0.5f);
            section.anchorMax = new Vector2(0.95f, 0.9f);
            section.offsetMin = Vector2.zero;
            section.offsetMax = Vector2.zero;

            // Background circle (placeholder for radial gauge)
            var circleBg = CreatePanel("CircleBackground", section, new Color(0.3f, 0.3f, 0.35f, 1f));
            circleBg.anchorMin = new Vector2(0.15f, 0.1f);
            circleBg.anchorMax = new Vector2(0.85f, 0.9f);
            var circleImg = circleBg.GetComponent<Image>();
            // Note: For actual circular gauge, use UI.Extensions or custom shader

            // Percentage text
            var percentText = CreateText("AttractionRateText", section, "35%", 32);
            var percentRect = percentText.GetComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0, 0.3f);
            percentRect.anchorMax = new Vector2(1, 0.7f);
            percentText.alignment = TextAlignmentOptions.Center;
            percentText.fontStyle = FontStyles.Bold;

            // Label
            var labelText = CreateText("AttractionLabel", section, "Customer Attraction Rate", 12);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.7f);
            labelRect.anchorMax = new Vector2(1, 0.85f);
            labelText.alignment = TextAlignmentOptions.Center;

            // Max label
            var maxText = CreateText("MaxLabel", section, "/ 100% MAX", 10);
            var maxRect = maxText.GetComponent<RectTransform>();
            maxRect.anchorMin = new Vector2(0, 0.15f);
            maxRect.anchorMax = new Vector2(1, 0.3f);
            maxText.alignment = TextAlignmentOptions.Center;
            maxText.color = new Color(0.4f, 0.9f, 0.4f, 1f);

            // Review effect
            var reviewText = CreateText("ReviewEffectText", section, "Review Effect: +3% (Avg ★3.8)", 10);
            var reviewRect = reviewText.GetComponent<RectTransform>();
            reviewRect.anchorMin = new Vector2(0, 0);
            reviewRect.anchorMax = new Vector2(1, 0.12f);
            reviewText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateVIPFactorSection(RectTransform parent)
        {
            var section = CreatePanel("VIPSection", parent, Color.clear);
            section.anchorMin = new Vector2(0.05f, 0.3f);
            section.anchorMax = new Vector2(0.95f, 0.48f);
            section.offsetMin = Vector2.zero;
            section.offsetMax = Vector2.zero;

            // Label
            var labelText = CreateText("VIPLabel", section, "VIP Customer Factor: 45/100", 12);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelText.alignment = TextAlignmentOptions.Left;

            // Slider background
            var sliderBg = CreatePanel("VIPSliderBg", section, new Color(0.2f, 0.2f, 0.2f, 1f));
            sliderBg.anchorMin = new Vector2(0, 0.2f);
            sliderBg.anchorMax = new Vector2(1, 0.5f);
            
            // Slider fill
            var sliderFill = CreatePanel("VIPSliderFill", sliderBg, accentColor);
            sliderFill.anchorMin = new Vector2(0, 0);
            sliderFill.anchorMax = new Vector2(0.45f, 1); // 45%
            sliderFill.offsetMin = Vector2.zero;
            sliderFill.offsetMax = Vector2.zero;
        }

        private void CreateTargetingSection(RectTransform parent)
        {
            var section = CreatePanel("TargetingSection", parent, Color.clear);
            section.anchorMin = new Vector2(0.05f, 0.05f);
            section.anchorMax = new Vector2(0.95f, 0.28f);
            section.offsetMin = Vector2.zero;
            section.offsetMax = Vector2.zero;

            // Customer type icons (5 columns)
            string[] typeNames = { "Poor", "Low", "Mid", "High", "Ultra" };
            for (int i = 0; i < 5; i++)
            {
                float x = i / 5f;
                float xEnd = (i + 1) / 5f;
                
                var iconPanel = CreatePanel($"CustomerType_{i}", section, new Color(0.4f, 0.4f, 0.45f, 1f));
                iconPanel.anchorMin = new Vector2(x + 0.01f, 0.35f);
                iconPanel.anchorMax = new Vector2(xEnd - 0.01f, 0.95f);
                
                var typeLabel = CreateText($"TypeLabel_{i}", section, typeNames[i], 9);
                var labelRect = typeLabel.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(x, 0);
                labelRect.anchorMax = new Vector2(xEnd, 0.3f);
                typeLabel.alignment = TextAlignmentOptions.Center;
            }

            // Targeting label
            var targetLabel = CreateText("TargetingLabel", parent, "Targeting: Mid-High Income", 10);
            var targetRect = targetLabel.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.05f, 0);
            targetRect.anchorMax = new Vector2(0.95f, 0.05f);
            targetLabel.alignment = TextAlignmentOptions.Center;
        }

        private void CreateAdCampaignsPanel(RectTransform parent)
        {
            var rightPanel = CreatePanel("AdCampaignsPanel", parent, new Color(0.25f, 0.25f, 0.3f, 1f));
            rightPanel.anchorMin = new Vector2(0.35f, 0);
            rightPanel.anchorMax = new Vector2(1, 1);
            rightPanel.offsetMin = new Vector2(5, 0);
            rightPanel.offsetMax = Vector2.zero;

            // Section title
            var sectionTitle = CreateText("SectionTitle", rightPanel, "Ad Campaigns Available", 16);
            var titleRect = sectionTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.92f);
            titleRect.anchorMax = new Vector2(1, 1);
            sectionTitle.alignment = TextAlignmentOptions.Center;

            // Available ads grid container
            var adsGrid = CreatePanel("AvailableAdsContainer", rightPanel, Color.clear);
            adsGrid.anchorMin = new Vector2(0.02f, 0.25f);
            adsGrid.anchorMax = new Vector2(0.98f, 0.9f);
            adsGrid.offsetMin = Vector2.zero;
            adsGrid.offsetMax = Vector2.zero;
            var gridLayout = adsGrid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 140);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            
            // Store reference
            availableAdsContainer = adsGrid;

            // Active campaigns section
            CreateActiveCampaignsSection(rightPanel);
        }

        private void CreateActiveCampaignsSection(RectTransform parent)
        {
            var section = CreatePanel("ActiveCampaignsSection", parent, new Color(0.2f, 0.35f, 0.5f, 1f));
            section.anchorMin = new Vector2(0.02f, 0.02f);
            section.anchorMax = new Vector2(0.98f, 0.22f);
            section.offsetMin = Vector2.zero;
            section.offsetMax = Vector2.zero;

            // Section title
            var sectionTitle = CreateText("ActiveTitle", section, "Active Campaigns", 14);
            var titleRect = sectionTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.75f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            sectionTitle.alignment = TextAlignmentOptions.MidlineLeft;

            // Active ads container
            var activeContainer = CreatePanel("ActiveAdsContainer", section, Color.clear);
            activeContainer.anchorMin = new Vector2(0.01f, 0.05f);
            activeContainer.anchorMax = new Vector2(0.99f, 0.72f);
            activeContainer.offsetMin = Vector2.zero;
            activeContainer.offsetMax = Vector2.zero;
            var horizLayout = activeContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.spacing = 10;
            horizLayout.childAlignment = TextAnchor.MiddleLeft;
            horizLayout.childForceExpandWidth = false;
            horizLayout.childForceExpandHeight = true;
            horizLayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Store reference
            activeAdsContainer = activeContainer;
        }

        // Helper methods
        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            
            var rect = obj.GetComponent<RectTransform>();
            var img = obj.GetComponent<Image>();
            img.color = color;
            
            return rect;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            if (font != null) tmp.font = font;
            
            return tmp;
        }

        /// <summary>
        /// Create a sample ad card inside AvailableAdsContainer
        /// </summary>
        private void CreateSampleAdCard()
        {
            if (availableAdsContainer == null)
            {
                Debug.LogWarning("AvailableAdsContainer not found!");
                return;
            }

            var cardObj = new GameObject("AdCardPrefab", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(availableAdsContainer, false);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(200, 140);
            
            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = cardColor;

            // Icon area
            var iconArea = CreatePanel("IconArea", cardRect, new Color(0.35f, 0.55f, 0.85f, 1f));
            iconArea.anchorMin = new Vector2(0, 0.4f);
            iconArea.anchorMax = new Vector2(0.35f, 0.95f);
            iconArea.offsetMin = new Vector2(5, 0);
            iconArea.offsetMax = new Vector2(0, -5);

            // Icon image
            var iconImg = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconImg.transform.SetParent(iconArea, false);
            var iconImgRect = iconImg.GetComponent<RectTransform>();
            iconImgRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconImgRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconImgRect.offsetMin = Vector2.zero;
            iconImgRect.offsetMax = Vector2.zero;

            // Name text
            var nameText = CreateText("NameText", cardRect, "Free SNS Posts", 12);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.38f, 0.75f);
            nameRect.anchorMax = new Vector2(1, 0.95f);
            nameRect.offsetMax = new Vector2(-5, 0);
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.fontStyle = FontStyles.Bold;

            // Stats text
            var statsText = CreateText("StatsText", cardRect, "Attraction  +5%\nDuration:   1 Day\nVIP:        -", 9);
            var statsRect = statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.38f, 0.35f);
            statsRect.anchorMax = new Vector2(1, 0.75f);
            statsRect.offsetMax = new Vector2(-5, 0);
            statsText.alignment = TextAlignmentOptions.TopLeft;

            // Cost text
            var costText = CreateText("CostText", cardRect, "¥0", 14);
            var costRect = costText.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(0.5f, 0.3f);
            costRect.offsetMin = new Vector2(5, 5);
            costText.alignment = TextAlignmentOptions.BottomLeft;
            costText.fontStyle = FontStyles.Bold;

            // Purchase button
            var buttonObj = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(cardRect, false);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(1, 0.3f);
            buttonRect.offsetMin = new Vector2(5, 5);
            buttonRect.offsetMax = new Vector2(-5, -5);
            var buttonImg = buttonObj.GetComponent<Image>();
            buttonImg.color = accentColor;

            var buttonText = CreateText("ButtonText", buttonRect, "EXECUTE", 11);
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            // Locked overlay
            var lockedOverlay = CreatePanel("LockedOverlay", cardRect, new Color(0, 0, 0, 0.6f));
            lockedOverlay.anchorMin = Vector2.zero;
            lockedOverlay.anchorMax = Vector2.one;
            lockedOverlay.offsetMin = Vector2.zero;
            lockedOverlay.offsetMax = Vector2.zero;
            lockedOverlay.gameObject.SetActive(false); // Hidden by default

            var lockedText = CreateText("LockedText", lockedOverlay, "★3 Required", 14);
            lockedText.alignment = TextAlignmentOptions.Center;
            lockedText.fontStyle = FontStyles.Bold;

            // Add AdCardUI component
            cardObj.AddComponent<AdCardUI>();

            Debug.Log("[AdvertisingPanelSetup] Sample Ad Card created in AvailableAdsContainer.");
        }

        /// <summary>
        /// Create a sample active ad card inside ActiveAdsContainer
        /// </summary>
        private void CreateSampleActiveAdCard()
        {
            if (activeAdsContainer == null)
            {
                Debug.LogWarning("ActiveAdsContainer not found!");
                return;
            }

            var cardObj = new GameObject("ActiveAdCardPrefab", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardObj.transform.SetParent(activeAdsContainer, false);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(250, 45);
            
            // Layout element for horizontal layout
            var layoutElem = cardObj.GetComponent<LayoutElement>();
            layoutElem.minWidth = 250;
            layoutElem.preferredWidth = 250;
            
            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            // Name text
            var nameText = CreateText("NameText", cardRect, "Flyer Distribution", 12);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0);
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = new Vector2(10, 0);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;

            // Remaining days
            var daysText = CreateText("RemainingDaysText", cardRect, "(Remaining: 2 Days)", 10);
            var daysRect = daysText.GetComponent<RectTransform>();
            daysRect.anchorMin = new Vector2(0.6f, 0);
            daysRect.anchorMax = new Vector2(1, 1);
            daysRect.offsetMax = new Vector2(-10, 0);
            daysText.alignment = TextAlignmentOptions.MidlineRight;
            daysText.color = new Color(0.8f, 0.9f, 1f, 1f);

            // Add ActiveAdCardUI component
            cardObj.AddComponent<ActiveAdCardUI>();

            Debug.Log("[AdvertisingPanelSetup] Sample Active Ad Card created in ActiveAdsContainer.");
        }
    }
}
