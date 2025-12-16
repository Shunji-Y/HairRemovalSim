using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Bank panel UI for browsing and applying for loans
    /// </summary>
    public class BankPanel : MonoBehaviour
    {
        public static BankPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Overview Section")]
        [SerializeField] private TMP_Text currentLoansText;
        [SerializeField] private TMP_Text totalDebtText;
        
        [Header("Loan Cards Container")]
        [SerializeField] private Transform loanCardsContainer;
        [SerializeField] private GameObject loanCardPrefab;
        
        [Header("Application Dialog")]
        [SerializeField] private GameObject applicationDialog;
        [SerializeField] private TMP_Text dialogLoanNameText;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text dailyPaymentText;
        [SerializeField] private TMP_Text totalRepaymentText;
        [SerializeField] private TMP_Text termText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private LoanData selectedLoan;
        private List<LoanCardUI> loanCards = new List<LoanCardUI>();
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmApplication);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CloseApplicationDialog);
            if (amountSlider != null)
                amountSlider.onValueChanged.AddListener(OnAmountChanged);
        }
        
        private void OnEnable()
        {
            // Called when panel is set active (e.g., via PCUIManager)
            
            // Re-register button listeners (in case they weren't set in Awake)
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmApplication);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(CloseApplicationDialog);
            }
            if (amountSlider != null)
            {
                amountSlider.onValueChanged.RemoveAllListeners();
                amountSlider.onValueChanged.AddListener(OnAmountChanged);
            }
            
            RefreshDisplay();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (applicationDialog != null && applicationDialog.activeSelf)
                    CloseApplicationDialog();
                else
                    Hide();
            }
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshDisplay();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            CloseApplicationDialog();
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void RefreshDisplay()
        {
            if (LoanManager.Instance == null) return;
            
            // Update overview
            int activeCount = LoanManager.Instance.ActiveLoans.Count;
            int totalDebt = LoanManager.Instance.GetTotalDebt();
            
            if (currentLoansText != null)
                currentLoansText.text = $"${totalDebt:N0}";
            if (totalDebtText != null)
                totalDebtText.text = $"Active Loans: {activeCount}";
            
            // Refresh loan cards
            RefreshLoanCards();
        }
        
        private void RefreshLoanCards()
        {
            Debug.Log("[BankPanel] RefreshLoanCards called");
            
            // Clear existing
            foreach (var card in loanCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            loanCards.Clear();
            
            if (LoanManager.Instance == null)
            {
                Debug.LogError("[BankPanel] LoanManager.Instance is NULL!");
                return;
            }
            
            if (loanCardPrefab == null)
            {
                Debug.LogError("[BankPanel] loanCardPrefab is NULL!");
                return;
            }
            
            if (loanCardsContainer == null)
            {
                Debug.LogError("[BankPanel] loanCardsContainer is NULL!");
                return;
            }
            
            Debug.Log($"[BankPanel] Available loans count: {LoanManager.Instance.AvailableLoans.Count}");
            
            // Create cards for available loans
            foreach (var loanData in LoanManager.Instance.AvailableLoans)
            {
                Debug.Log($"[BankPanel] Creating card for: {loanData.displayName}");
                var cardObj = Instantiate(loanCardPrefab, loanCardsContainer);
                var card = cardObj.GetComponent<LoanCardUI>();
                if (card != null)
                {
                    bool isActive = LoanManager.Instance.HasActiveLoan(loanData.loanId);
                    card.Setup(loanData, isActive, OnApplyClicked);
                    loanCards.Add(card);
                }
                else
                {
                    Debug.LogError("[BankPanel] LoanCardUI component not found on prefab!");
                }
            }
            
            Debug.Log($"[BankPanel] Created {loanCards.Count} loan cards");
        }
        
        private void OnApplyClicked(LoanData loanData)
        {
            if (loanData == null) return;
            if (LoanManager.Instance.HasActiveLoan(loanData.loanId)) return;
            
            selectedLoan = loanData;
            ShowApplicationDialog();
        }
        
        private void ShowApplicationDialog()
        {
            if (applicationDialog == null || selectedLoan == null) return;
            
            applicationDialog.SetActive(true);
            
            if (dialogLoanNameText != null)
                dialogLoanNameText.text = selectedLoan.displayName;
            
            if (amountSlider != null)
            {
                amountSlider.minValue = 100;
                amountSlider.maxValue = selectedLoan.maxAmount;
                amountSlider.value = selectedLoan.maxAmount / 2;
            }
            
            if (termText != null)
                termText.text = $"Term: {selectedLoan.termDays} days";
            
            OnAmountChanged(amountSlider != null ? amountSlider.value : selectedLoan.maxAmount);
        }
        
        private void OnAmountChanged(float value)
        {
            if (selectedLoan == null) return;
            
            int amount = Mathf.RoundToInt(value / 100) * 100; // Round to 100
            
            if (amountText != null)
                amountText.text = $"${amount:N0}";
            
            int dailyPayment = selectedLoan.CalculateDailyPayment(amount);
            int totalRepayment = selectedLoan.CalculateTotalRepayment(amount);
            
            if (dailyPaymentText != null)
                dailyPaymentText.text = $"Daily: ${dailyPayment:N0}";
            if (totalRepaymentText != null)
                totalRepaymentText.text = $"Total: ${totalRepayment:N0}";
        }
        
        private void CloseApplicationDialog()
        {
            if (applicationDialog != null)
                applicationDialog.SetActive(false);
            selectedLoan = null;
        }
        
        private void OnConfirmApplication()
        {
            Debug.Log("[BankPanel] OnConfirmApplication called!");
            
            if (selectedLoan == null)
            {
                Debug.LogError("[BankPanel] selectedLoan is NULL!");
                return;
            }
            
            if (LoanManager.Instance == null)
            {
                Debug.LogError("[BankPanel] LoanManager.Instance is NULL!");
                return;
            }
            
            int amount = amountSlider != null 
                ? Mathf.RoundToInt(amountSlider.value / 100) * 100 
                : selectedLoan.maxAmount;
            
            Debug.Log($"[BankPanel] Attempting loan: {selectedLoan.displayName}, Amount: {amount}");
            
            if (LoanManager.Instance.TakeLoan(selectedLoan, amount))
            {
                Debug.Log($"[BankPanel] Loan approved: {selectedLoan.displayName}, Amount: {amount}");
                CloseApplicationDialog();
                RefreshDisplay();
            }
            else
            {
                Debug.LogError("[BankPanel] TakeLoan returned false!");
            }
        }
        
#if UNITY_EDITOR
        //[ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            // Colors
            Color darkBlue = new Color(0.1f, 0.15f, 0.25f);
            Color gold = new Color(0.85f, 0.7f, 0.3f);
            Color cardBg = new Color(0.95f, 0.95f, 0.92f);
            
            // Get or create panel
            var panelRect = GetComponent<RectTransform>();
            if (panelRect == null) return;
            
            // Clear existing children
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            // === HEADER ===
            var header = CreatePanel("Header", transform, new Vector2(0, 1), new Vector2(0, 1), 
                new Vector2(0, -80), new Vector2(0, 0), 80, darkBlue);
            
            // Logo text
            var logoText = CreateText("LogoText", header.transform, "Global Trust Bank", 28, gold, TextAlignmentOptions.Left);
            var logoRect = logoText.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0, 0);
            logoRect.anchorMax = new Vector2(0.3f, 1);
            logoRect.offsetMin = new Vector2(30, 10);
            logoRect.offsetMax = new Vector2(-10, -10);
            
            // === MAIN CONTENT ===
            var mainContent = CreatePanel("MainContent", transform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0, 250), new Vector2(0, -80), 0, new Color(0.2f, 0.25f, 0.35f));
            
            // Banner (left side)
            var banner = CreatePanel("Banner", mainContent.transform, new Vector2(0, 0), new Vector2(0.65f, 1),
                new Vector2(20, 20), new Vector2(-10, -20), 0, new Color(0.15f, 0.2f, 0.3f));
            
            var bannerText = CreateText("BannerText", banner.transform, 
                "Secure Your Future.\nTrustworthy Financing Solutions\nfor Your Business Growth.", 
                32, Color.white, TextAlignmentOptions.Left);
            var bannerTextRect = bannerText.GetComponent<RectTransform>();
            bannerTextRect.anchorMin = new Vector2(0, 0.3f);
            bannerTextRect.anchorMax = new Vector2(0.6f, 0.9f);
            bannerTextRect.offsetMin = new Vector2(40, 0);
            bannerTextRect.offsetMax = new Vector2(-20, 0);
            
            // Overview card (right side)
            var overviewCard = CreatePanel("OverviewCard", mainContent.transform, new Vector2(0.65f, 0), new Vector2(1, 1),
                new Vector2(10, 20), new Vector2(-20, -20), 0, cardBg);
            
            var overviewTitle = CreateText("OverviewTitle", overviewCard.transform, "Your Financial Overview", 
                22, darkBlue, TextAlignmentOptions.Center);
            SetRectTransform(overviewTitle, new Vector2(0, 0.8f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            // Current loans text (assign to field)
            var currentLoansLabel = CreateText("CurrentLoansLabel", overviewCard.transform, "Current Outstanding Loans:", 
                16, Color.gray, TextAlignmentOptions.Left);
            SetRectTransform(currentLoansLabel, new Vector2(0, 0.6f), new Vector2(1, 0.75f), new Vector2(20, 0), new Vector2(-20, 0));
            
            var currentLoansValue = CreateText("CurrentLoansValue", overviewCard.transform, "¥0", 
                28, darkBlue, TextAlignmentOptions.Left);
            SetRectTransform(currentLoansValue, new Vector2(0, 0.45f), new Vector2(1, 0.6f), new Vector2(20, 0), new Vector2(-20, 0));
            currentLoansText = currentLoansValue;
            
            // Total debt text
            var totalDebtValue = CreateText("TotalDebtValue", overviewCard.transform, "現在のローン数: 0", 
                16, Color.gray, TextAlignmentOptions.Left);
            SetRectTransform(totalDebtValue, new Vector2(0, 0.3f), new Vector2(1, 0.45f), new Vector2(20, 0), new Vector2(-20, 0));
            totalDebtText = totalDebtValue;
            
            // === LOAN CARDS SECTION ===
            var cardsSection = CreatePanel("LoanCardsSection", transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 250), 250, new Color(0.9f, 0.9f, 0.88f));
            
            var cardsSectionTitle = CreateText("CardsSectionTitle", cardsSection.transform, "Available Loan Products",
                20, darkBlue, TextAlignmentOptions.Left);
            SetRectTransform(cardsSectionTitle, new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(30, 0), new Vector2(-30, -5));
            
            // Cards container with HorizontalLayoutGroup
            var cardsContainer = CreatePanel("LoanCardsContainer", cardsSection.transform, new Vector2(0, 0), new Vector2(1, 0.85f),
                new Vector2(20, 20), new Vector2(-20, -10), 0, Color.clear);
            
            var hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            
            loanCardsContainer = cardsContainer.transform;
            
            // Create sample loan cards
            string[] loanNames = { "Startup Accelerator", "Equipment & Technology", "Business Expansion", "Real Estate", "Working Capital" };
            string[] amounts = { "¥50M", "¥200M", "¥500M", "¥1B", "¥30M" };
            
            for (int i = 0; i < 5; i++)
            {
                CreateSampleLoanCard(cardsContainer.transform, loanNames[i], amounts[i], cardBg, darkBlue, gold);
            }
            
            // === APPLICATION DIALOG ===
            var dialog = CreatePanel("ApplicationDialog", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300, -200), new Vector2(300, 200), 0, new Color(1, 1, 1, 0.98f));
            dialog.SetActive(false);
            applicationDialog = dialog;
            
            var dialogTitle = CreateText("DialogTitle", dialog.transform, "ローン申込", 24, darkBlue, TextAlignmentOptions.Center);
            SetRectTransform(dialogTitle, new Vector2(0, 0.85f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            var dialogLoanName = CreateText("DialogLoanName", dialog.transform, "Loan Name", 20, Color.black, TextAlignmentOptions.Center);
            SetRectTransform(dialogLoanName, new Vector2(0, 0.7f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);
            dialogLoanNameText = dialogLoanName;
            
            // Slider
            var sliderObj = new GameObject("AmountSlider");
            sliderObj.transform.SetParent(dialog.transform, false);
            var slider = sliderObj.AddComponent<Slider>();
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            SetRectTransform(sliderObj, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.6f), Vector2.zero, Vector2.zero);
            
            // Slider background
            var sliderBg = CreatePanel("Background", sliderObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0, Color.gray);
            slider.targetGraphic = sliderBg.GetComponent<Image>();
            
            var fillArea = CreatePanel("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-5, 0), 0, Color.clear);
            var fill = CreatePanel("Fill", fillArea.transform, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero, 0, gold);
            slider.fillRect = fill.GetComponent<RectTransform>();
            
            amountSlider = slider;
            
            // Amount text
            var amtText = CreateText("AmountText", dialog.transform, "¥100,000", 18, Color.black, TextAlignmentOptions.Center);
            SetRectTransform(amtText, new Vector2(0, 0.4f), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            amountText = amtText;
            
            // Daily/Total texts
            var dailyText = CreateText("DailyPaymentText", dialog.transform, "1日あたり: ¥0", 14, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(dailyText, new Vector2(0, 0.3f), new Vector2(0.5f, 0.4f), Vector2.zero, Vector2.zero);
            dailyPaymentText = dailyText;
            
            var totalText = CreateText("TotalRepaymentText", dialog.transform, "総返済額: ¥0", 14, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(totalText, new Vector2(0.5f, 0.3f), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            totalRepaymentText = totalText;
            
            var termTxt = CreateText("TermText", dialog.transform, "返済期間: 30日", 14, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(termTxt, new Vector2(0, 0.2f), new Vector2(1, 0.3f), Vector2.zero, Vector2.zero);
            termText = termTxt;
            
            // Buttons
            var confirmBtn = CreateButton("ConfirmButton", dialog.transform, "確定", gold, Color.white);
            SetRectTransform(confirmBtn.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.18f), Vector2.zero, Vector2.zero);
            confirmButton = confirmBtn;
            
            var cancelBtn = CreateButton("CancelButton", dialog.transform, "キャンセル", Color.gray, Color.white);
            SetRectTransform(cancelBtn.gameObject, new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.18f), Vector2.zero, Vector2.zero);
            cancelButton = cancelBtn;
            
            // Set panel reference
            panel = gameObject;
            
            Debug.Log("[BankPanel] UI Structure generated successfully!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 offsetMin, Vector2 offsetMax, float height, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            if (height > 0)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
            
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
        
        private Button CreateButton(string name, Transform parent, string text, Color bgColor, Color textColor)
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
            tmp.fontSize = 18;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }
        
        private void CreateSampleLoanCard(Transform parent, string loanName, string amount, Color bgColor, Color textColor, Color btnColor)
        {
            var card = CreatePanel($"LoanCard_{loanName}", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0, bgColor);
            
            var nameText = CreateText("Name", card.transform, loanName + "\nLoan", 16, textColor, TextAlignmentOptions.Center);
            SetRectTransform(nameText.gameObject, new Vector2(0, 0.6f), new Vector2(1, 0.95f), new Vector2(5, 0), new Vector2(-5, 0));
            
            var amountText = CreateText("Amount", card.transform, $"Max: {amount}", 12, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(amountText.gameObject, new Vector2(0, 0.4f), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            
            var rateText = CreateText("Rate", card.transform, "From 1.5%/day", 10, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(rateText.gameObject, new Vector2(0, 0.25f), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            
            var applyBtn = CreateButton("ApplyButton", card.transform, "Apply Now", btnColor, Color.white);
            SetRectTransform(applyBtn.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.22f), Vector2.zero, Vector2.zero);
        }
        
        private void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        
        private void SetRectTransform(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            SetRectTransform(text.gameObject, anchorMin, anchorMax, offsetMin, offsetMax);
        }
#endif
    }
}

