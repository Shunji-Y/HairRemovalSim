using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// PC Panel for shop upgrades
    /// Shows current grade, next upgrade benefits, cost, and upgrade button
    /// </summary>
    public class ShopUpgradePanel : MonoBehaviour
    {
        public static ShopUpgradePanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text currentGradeText;
        [SerializeField] private TMP_Text moneyText;
        
        [Header("Preview")]
        [SerializeField] private Image previewImage;
        
        [Header("Benefits")]
        [SerializeField] private Transform leftBenefitContainer;
        [SerializeField] private Transform rightBenefitContainer;
        [SerializeField] private GameObject leftBubblePrefab;
        [SerializeField] private GameObject rightBubblePrefab;
        
        [Header("Cost & Button")]
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonText;
        
        [Header("Confirm Dialog")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private TMP_Text confirmMessageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Max Grade Display")]
        [SerializeField] private GameObject maxGradeOverlay;
        [SerializeField] private TMP_Text maxGradeText;
        
        private List<GameObject> benefitBubbles = new List<GameObject>();
        private ShopUpgradeData currentUpgradeData;
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
            
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmUpgrade);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CloseConfirmDialog);
        }
        
        private void OnEnable()
        {
            RefreshDisplay();
            
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
            
            GameEvents.OnMoneyChanged += OnMoneyChanged;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
            
            GameEvents.OnMoneyChanged -= OnMoneyChanged;
        }
        
        private void OnMoneyChanged(int newMoney)
        {
            UpdateMoneyDisplay();
            UpdateUpgradeButton();
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            CloseConfirmDialog();
            RefreshDisplay();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            CloseConfirmDialog();
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void RefreshDisplay()
        {
            if (ShopManager.Instance == null) return;
            
            UpdateHeader();
            UpdateMoneyDisplay();
            
            // Check if at max grade
            if (ShopManager.Instance.IsMaxGrade)
            {
                ShowMaxGradeState();
                return;
            }
            
            // Get next upgrade data
            currentUpgradeData = ShopManager.Instance.GetNextUpgradeData();
            if (currentUpgradeData == null)
            {
                ShowMaxGradeState();
                return;
            }
            
            // Show normal upgrade state
            if (maxGradeOverlay != null) maxGradeOverlay.SetActive(false);
            
            UpdatePreview();
            UpdateBenefits();
            UpdateCost();
            UpdateUpgradeButton();
        }
        
        private void UpdateHeader()
        {
            if (titleText != null)
                titleText.text = L?.Get("upgrade.title") ?? "Shop Upgrade";
            
            if (currentGradeText != null)
            {
                int grade = ShopManager.Instance?.ShopGrade ?? 1;
                currentGradeText.text = L?.Get("upgrade.current_grade", grade) ?? $"Current Grade: {grade}";
            }
        }
        
        private void UpdateMoneyDisplay()
        {
            if (moneyText != null)
            {
                int money = EconomyManager.Instance?.CurrentMoney ?? 0;
                moneyText.text = $"${money:N0}";
            }
        }
        
        private void UpdatePreview()
        {
            if (previewImage != null && currentUpgradeData != null)
            {
                if (currentUpgradeData.previewImage != null)
                {
                    previewImage.sprite = currentUpgradeData.previewImage;
                    previewImage.enabled = true;
                }
                else
                {
                    previewImage.enabled = false;
                }
            }
        }
        
        private void UpdateBenefits()
        {
            // Clear existing bubbles
            foreach (var bubble in benefitBubbles)
            {
                if (bubble != null) Destroy(bubble);
            }
            benefitBubbles.Clear();
            
            if (currentUpgradeData == null) return;
            if (leftBenefitContainer == null || rightBenefitContainer == null) return;
            if (leftBubblePrefab == null || rightBubblePrefab == null) return;
            
            int count = currentUpgradeData.BenefitCount;
            if (count == 0) return;
            
            // Distribute: left gets ceiling, right gets floor
            // e.g., 5 benefits: left=3, right=2
            int leftCount = (count + 1) / 2;
            int rightCount = count / 2;
            
            // Create left bubbles
            for (int i = 0; i < leftCount; i++)
            {
                CreateBenefitBubble(leftBenefitContainer, leftBubblePrefab, currentUpgradeData.benefitKeys[i]);
            }
            
            // Create right bubbles
            for (int i = 0; i < rightCount; i++)
            {
                CreateBenefitBubble(rightBenefitContainer, rightBubblePrefab, currentUpgradeData.benefitKeys[leftCount + i]);
            }
        }
        
        private void CreateBenefitBubble(Transform container, GameObject prefab, string key)
        {
            var bubbleObj = Instantiate(prefab, container);
            bubbleObj.SetActive(true);
            
            var text = bubbleObj.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                string localizedText = L?.Get(key);
                text.text = (!string.IsNullOrEmpty(localizedText) && !localizedText.StartsWith("["))
                    ? localizedText
                    : key;
            }
            
            benefitBubbles.Add(bubbleObj);
        }
        
        private void UpdateCost()
        {
            if (costText != null && currentUpgradeData != null)
            {
                costText.text = L?.Get("upgrade.cost", currentUpgradeData.upgradeCost) 
                    ?? $"Required: ${currentUpgradeData.upgradeCost:N0}";
            }
        }
        
        private void UpdateUpgradeButton()
        {
            if (upgradeButton == null) return;
            
            bool canAfford = ShopManager.Instance?.CanAffordNextUpgrade() ?? false;
            upgradeButton.interactable = canAfford && !ShopManager.Instance.IsMaxGrade;
            
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = L?.Get("upgrade.button") ?? "UPGRADE";
            }
        }
        
        private void ShowMaxGradeState()
        {
            if (maxGradeOverlay != null) maxGradeOverlay.SetActive(true);
            if (maxGradeText != null)
                maxGradeText.text = L?.Get("upgrade.max_grade") ?? "Maximum Grade Reached!";
            
            if (upgradeButton != null)
                upgradeButton.interactable = false;
            
            // Clear benefits
            foreach (var bubble in benefitBubbles)
            {
                if (bubble != null) Destroy(bubble);
            }
            benefitBubbles.Clear();
        }
        
        private void OnUpgradeClicked()
        {
            if (currentUpgradeData == null) return;
            ShowConfirmDialog();
        }
        
        private void ShowConfirmDialog()
        {
            if (confirmDialog == null) return;
            
            confirmDialog.SetActive(true);
            
            if (confirmMessageText != null && currentUpgradeData != null)
            {
                confirmMessageText.text = L?.Get("upgrade.confirm_message", ShopManager.Instance.ShopGrade+1,currentUpgradeData.upgradeCost)
                    ?? $"Upgrade to Grade {currentUpgradeData.targetGrade} for ${currentUpgradeData.upgradeCost:N0}?";
            }
        }
        
        private void CloseConfirmDialog()
        {
            if (confirmDialog != null)
                confirmDialog.SetActive(false);
        }
        
        private void OnConfirmUpgrade()
        {
            if (ShopManager.Instance == null || currentUpgradeData == null) return;
            
            CloseConfirmDialog();
            
            // Start whiteout transition
            if (ScreenTransitionManager.Instance != null)
            {
                ScreenTransitionManager.Instance.DoWhiteout(
                    onPeak: () =>
                    {
                        // Perform upgrade during white screen
                        ShopManager.Instance.UpgradeShop();
                    },
                    onComplete: () =>
                    {
                        // Refresh display after transition
                        RefreshDisplay();
                    }
                );
            }
            else
            {
                // No transition manager, just upgrade directly
                ShopManager.Instance.UpgradeShop();
                RefreshDisplay();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            // Colors
            Color headerColor = new Color(0.3f, 0.2f, 0.5f);
            Color contentBg = new Color(0.4f, 0.35f, 0.55f, 0.95f);
            Color buttonGreen = new Color(0.2f, 0.7f, 0.3f);
            Color bubbleBg = new Color(1f, 1f, 0.9f, 0.95f);
            
            // Clear existing children
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            // === MAIN PANEL ===
            var panelObj = CreatePanel("Panel", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, contentBg);
            panel = panelObj;
            var panelRect = panelObj.GetComponent<RectTransform>();
            
            // === HEADER ===
            var header = CreatePanel("Header", panelObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 0), headerColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 60);
            
            var title = CreateText("TitleText", header.transform, "店舗アップグレード", 28, Color.white, TextAlignmentOptions.Center);
            SetRect(title, new Vector2(0.2f, 0), new Vector2(0.8f, 1), Vector2.zero, Vector2.zero);
            titleText = title;
            
            var gradeText = CreateText("CurrentGradeText", header.transform, "現在のグレード: 1", 16, Color.white, TextAlignmentOptions.Left);
            SetRect(gradeText, new Vector2(0, 0), new Vector2(0.2f, 1), new Vector2(20, 0), new Vector2(0, 0));
            currentGradeText = gradeText;
            
            var money = CreateText("MoneyText", header.transform, "$10,000", 18, Color.yellow, TextAlignmentOptions.Right);
            SetRect(money, new Vector2(0.8f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-20, 0));
            moneyText = money;
            
            // === CONTENT AREA ===
            var content = CreatePanel("Content", panelObj.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 80), new Vector2(-20, -70), Color.clear);
            
            // === LEFT BENEFITS ===
            var leftPanel = CreatePanel("LeftBenefitContainer", content.transform, new Vector2(0, 0.3f), new Vector2(0.25f, 0.9f), Vector2.zero, Vector2.zero, Color.clear);
            var leftVLG = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftVLG.spacing = 10;
            leftVLG.childAlignment = TextAnchor.MiddleRight;
            leftVLG.childControlWidth = true;
            leftVLG.childControlHeight = false;
            leftVLG.childForceExpandWidth = true;
            leftVLG.childForceExpandHeight = false;
            leftBenefitContainer = leftPanel.transform;
            
            // === CENTER PREVIEW ===
            var centerPanel = CreatePanel("CenterPanel", content.transform, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero, Color.clear);
            
            var preview = CreatePanel("PreviewImage", centerPanel.transform, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero, new Color(0.8f, 0.8f, 1f));
            previewImage = preview.GetComponent<Image>();
            
            // === RIGHT BENEFITS ===
            var rightPanel = CreatePanel("RightBenefitContainer", content.transform, new Vector2(0.75f, 0.3f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero, Color.clear);
            var rightVLG = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightVLG.spacing = 10;
            rightVLG.childAlignment = TextAnchor.MiddleLeft;
            rightVLG.childControlWidth = true;
            rightVLG.childControlHeight = false;
            rightVLG.childForceExpandWidth = true;
            rightVLG.childForceExpandHeight = false;
            rightBenefitContainer = rightPanel.transform;
            
            // === BOTTOM AREA ===
            var bottomPanel = CreatePanel("BottomPanel", content.transform, new Vector2(0.2f, 0), new Vector2(0.8f, 0.2f), Vector2.zero, Vector2.zero, Color.clear);
            
            var cost = CreateText("CostText", bottomPanel.transform, "必要金額: $5,000", 24, Color.white, TextAlignmentOptions.Center);
            SetRect(cost, new Vector2(0, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            costText = cost;
            
            var upgradeBtn = CreateButton("UpgradeButton", bottomPanel.transform, "アップグレード", buttonGreen, Color.white, 22);
            SetRect(upgradeBtn, new Vector2(0.2f, 0), new Vector2(0.8f, 0.5f), Vector2.zero, new Vector2(0, -5));
            upgradeButton = upgradeBtn;
            upgradeButtonText = upgradeBtn.GetComponentInChildren<TMP_Text>();
            
            // === LEFT BUBBLE PREFAB (pointing right) ===
            var leftBubble = CreatePanel("LeftBubblePrefab", panelObj.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, bubbleBg);
            var leftBubbleRect = leftBubble.GetComponent<RectTransform>();
            leftBubbleRect.sizeDelta = new Vector2(180, 50);
            var leftBubbleText = CreateText("Text", leftBubble.transform, "特典テキスト", 14, Color.black, TextAlignmentOptions.Right);
            SetRect(leftBubbleText, Vector2.zero, Vector2.one, new Vector2(10, 5), new Vector2(-20, -5));
            leftBubble.SetActive(false);
            leftBubblePrefab = leftBubble;
            
            // === RIGHT BUBBLE PREFAB (pointing left) ===
            var rightBubble = CreatePanel("RightBubblePrefab", panelObj.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, bubbleBg);
            var rightBubbleRect = rightBubble.GetComponent<RectTransform>();
            rightBubbleRect.sizeDelta = new Vector2(180, 50);
            var rightBubbleText = CreateText("Text", rightBubble.transform, "特典テキスト", 14, Color.black, TextAlignmentOptions.Left);
            SetRect(rightBubbleText, Vector2.zero, Vector2.one, new Vector2(20, 5), new Vector2(-10, -5));
            rightBubble.SetActive(false);
            rightBubblePrefab = rightBubble;
            
            // === MAX GRADE OVERLAY ===
            var maxOverlay = CreatePanel("MaxGradeOverlay", panelObj.transform, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.8f));
            maxOverlay.SetActive(false);
            maxGradeOverlay = maxOverlay;
            
            var maxText = CreateText("MaxGradeText", maxOverlay.transform, "最高グレード達成！", 32, Color.yellow, TextAlignmentOptions.Center);
            SetRect(maxText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            maxGradeText = maxText;
            
            // === CONFIRM DIALOG ===
            var dialog = CreatePanel("ConfirmDialog", panelObj.transform, new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.2f, 0.3f, 0.98f));
            dialog.SetActive(false);
            confirmDialog = dialog;
            
            var confirmMsg = CreateText("ConfirmMessage", dialog.transform, "グレード2にアップグレードしますか？\n費用: $5,000", 20, Color.white, TextAlignmentOptions.Center);
            SetRect(confirmMsg, new Vector2(0, 0.4f), new Vector2(1, 0.9f), new Vector2(20, 0), new Vector2(-20, 0));
            confirmMessageText = confirmMsg;
            
            var confirmBtn = CreateButton("ConfirmButton", dialog.transform, "確定", buttonGreen, Color.white, 18);
            SetRect(confirmBtn, new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.35f), Vector2.zero, Vector2.zero);
            confirmButton = confirmBtn;
            
            var cancelBtn = CreateButton("CancelButton", dialog.transform, "キャンセル", Color.gray, Color.white, 18);
            SetRect(cancelBtn, new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.35f), Vector2.zero, Vector2.zero);
            cancelButton = cancelBtn;
            
            Debug.Log("[ShopUpgradePanel] UI Structure generated!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }
        
        private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            return tmp;
        }
        
        private Button CreateButton(string name, Transform parent, string text, Color bgColor, Color textColor, int fontSize)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            var img = obj.AddComponent<Image>();
            img.color = bgColor;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }
        
        private void SetRect(Component comp, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = comp.GetComponent<RectTransform>();
            if (rect == null) rect = comp.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
#endif
    }
}
