using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Main payment panel showing all pending payments (loans + rent)
    /// </summary>
    public class PaymentListPanel : MonoBehaviour
    {
        public static PaymentListPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text warningText;
        
        [Header("Payment Cards Container")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private GameObject loanPaymentCardPrefab;
        [SerializeField] private GameObject rentPaymentCardPrefab;
        [SerializeField] private GameObject salaryPaymentCardPrefab;
        
        [Header("Footer")]
        [SerializeField] private TMP_Text totalDueText;
        [SerializeField] private Button payAllButton;
        
        private List<GameObject> paymentCards = new List<GameObject>();
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
            
            if (payAllButton != null)
                payAllButton.onClick.AddListener(OnPayAllClicked);
        }
        
        private void OnEnable()
        {
            // Re-register listeners
            if (payAllButton != null)
            {
                payAllButton.onClick.RemoveAllListeners();
                payAllButton.onClick.AddListener(OnPayAllClicked);
            }
            
            RefreshDisplay();
            
            // Subscribe to locale changes
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
            // Don't hide cursor here - PCUIManager handles cursor visibility
            // when switching between PC apps or closing PC
        }
        
        public void RefreshDisplay()
        {
            ClearCards();
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            int totalDue = 0;
            int overdueCount = 0;
            
            // Create rent payment cards (multiple can accumulate)
            if (RentManager.Instance != null)
            {
                foreach (var rentCard in RentManager.Instance.GetUnpaidCards())
                {
                    CreateRentCard(rentCard, currentDay);
                    totalDue += rentCard.amount;
                    if (rentCard.isOverdue)
                    {
                        overdueCount++;
                    }
                }
            }
            
            // Create loan payment cards (daily cards)
            if (LoanManager.Instance != null)
            {
                foreach (var card in LoanManager.Instance.GetUnpaidCards())
                {
                    CreateLoanCard(card, currentDay);
                    totalDue += card.TotalAmount;
                    if (card.isOverdue)
                    {
                        overdueCount++;
                    }
                }
            }
            
            // Create salary payment cards
            if (SalaryManager.Instance != null)
            {
                foreach (var record in SalaryManager.Instance.PendingSalaries)
                {
                    if (record.isPaid) continue;
                    CreateSalaryCard(record, currentDay);
                    totalDue += record.amount;
                    if (record.isOverdue)
                    {
                        overdueCount++;
                    }
                }
            }
            
            // Update header warning
            if (warningText != null)
            {
                if (overdueCount > 0)
                {
                    int maxCards = LoanManager.Instance?.MaxOverdueCards ?? 3;
                    string warningStr = L?.Get("ui.overdue_warning", overdueCount, maxCards) ?? $"⚠ Overdue ({overdueCount}/{maxCards})";
                    warningText.text = $"<color=red>{warningStr}</color>";
                    warningText.gameObject.SetActive(true);
                }
                else
                {
                    warningText.gameObject.SetActive(false);
                }
            }
            
            // Update total
            if (totalDueText != null)
                totalDueText.text = L?.Get("ui.total", totalDue) ?? $"Total: ${totalDue:N0}";
            
            // Update pay all button
            if (payAllButton != null)
            {
                int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
                payAllButton.interactable = currentMoney >= totalDue && totalDue > 0;
            }
        }
        
        private void ClearCards()
        {
            foreach (var card in paymentCards)
            {
                if (card != null) Destroy(card);
            }
            paymentCards.Clear();
        }
        
        private void CreateRentCard(RentPaymentCard rentCard, int currentDay)
        {
            if (rentPaymentCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(rentPaymentCardPrefab, cardsContainer);
            var cardUI = cardObj.GetComponent<RentPaymentCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(rentCard, currentDay, OnPaymentMade);
            }
            paymentCards.Add(cardObj);
        }
        
        private void CreateLoanCard(LoanPaymentCard loanCard, int currentDay)
        {
            if (loanPaymentCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(loanPaymentCardPrefab, cardsContainer);
            var cardUI = cardObj.GetComponent<LoanPaymentCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(loanCard, currentDay, OnPaymentMade);
            }
            paymentCards.Add(cardObj);
        }
        
        private void CreateSalaryCard(SalaryRecord record, int currentDay)
        {
            if (salaryPaymentCardPrefab == null || cardsContainer == null) return;
            
            var cardObj = Instantiate(salaryPaymentCardPrefab, cardsContainer);
            var cardUI = cardObj.GetComponent<SalaryPaymentCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(record);
            }
            paymentCards.Add(cardObj);
        }
        
        private void OnPaymentMade()
        {
            RefreshDisplay();
        }
        
        private void OnPayAllClicked()
        {
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            
            // Pay all rent cards
            if (RentManager.Instance != null)
            {
                var rentCards = RentManager.Instance.GetUnpaidCards();
                foreach (var card in rentCards.ToArray())
                {
                    RentManager.Instance.PayRentCard(card, currentDay);
                }
            }
            
            // Pay all loan cards
            if (LoanManager.Instance != null)
            {
                var loanCards = LoanManager.Instance.GetUnpaidCards();
                foreach (var card in loanCards.ToArray())
                {
                    LoanManager.Instance.PayCard(card);
                }
            }
            
            // Pay all salary cards
            if (SalaryManager.Instance != null)
            {
                var salaries = SalaryManager.Instance.PendingSalaries.ToArray();
                foreach (var record in salaries)
                {
                    SalaryManager.Instance.PaySalary(record);
                }
            }
            
            RefreshDisplay();
        }
        
#if UNITY_EDITOR
      //  [ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            // Colors
            Color headerBg = new Color(0.2f, 0.25f, 0.35f);
            Color cardBg = new Color(0.95f, 0.95f, 0.92f);
            Color buttonColor = new Color(0.3f, 0.6f, 0.4f);
            
            // Clear existing
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            // === HEADER ===
            var header = CreatePanel("Header", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0), 80, headerBg);
            
            var title = CreateText("Title", header.transform, "今日の支払い", 28, Color.white, TextAlignmentOptions.Center);
            SetRectFill(title.gameObject);
            titleText = title;
            
            var warning = CreateText("Warning", header.transform, "", 16, Color.red, TextAlignmentOptions.Right);
            SetRectTransform(warning.gameObject, new Vector2(0.6f, 0), new Vector2(1, 1), new Vector2(0, 10), new Vector2(-20, -10));
            warningText = warning;
            warning.gameObject.SetActive(false);
            
            // === CARDS CONTAINER ===
            var cardsArea = CreatePanel("CardsArea", transform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(20, 100), new Vector2(-20, -80), 0, new Color(0.9f, 0.9f, 0.88f));
            
            var container = CreatePanel("CardsContainer", cardsArea.transform, Vector2.zero, Vector2.one,
                new Vector2(10, 10), new Vector2(-10, -10), 0, Color.clear);
            
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            
            var csf = container.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            cardsContainer = container.transform;
            
            // Sample cards
            CreateSampleLoanCard(container.transform, "Bad Loan", "$2,100", "本日");
            CreateSampleRentCard(container.transform, "$50,000", "2日後");
            
            // === FOOTER ===
            var footer = CreatePanel("Footer", transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 100), 100, headerBg);
            
            var totalText = CreateText("TotalDue", footer.transform, "合計: $0", 24, Color.white, TextAlignmentOptions.Left);
            SetRectTransform(totalText.gameObject, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(30, 10), new Vector2(-10, -10));
            totalDueText = totalText;
            
            var payAllBtn = CreateButton("PayAllButton", footer.transform, "すべて支払う", buttonColor, Color.white);
            SetRectTransform(payAllBtn.gameObject, new Vector2(0.6f, 0.2f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
            payAllButton = payAllBtn;
            
            panel = gameObject;
            
            Debug.Log("[PaymentListPanel] UI generated!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private void CreateSampleLoanCard(Transform parent, string name, string amount, string deadline)
        {
            var card = CreatePanel($"LoanCard_{name}", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0, new Color(0.98f, 0.98f, 0.95f));
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 300;
            
            CreateText("Name", card.transform, name, 18, Color.black, TextAlignmentOptions.Center);
            CreateText("Amount", card.transform, $"今日: {amount}", 16, Color.gray, TextAlignmentOptions.Center);
            CreateText("Deadline", card.transform, $"期限: {deadline}", 14, Color.gray, TextAlignmentOptions.Center);
            
            var payBtn = CreateButton("PayButton", card.transform, "支払う", new Color(0.3f, 0.6f, 0.4f), Color.white);
            SetRectTransform(payBtn.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.2f), Vector2.zero, Vector2.zero);
            
            var prepayBtn = CreateButton("PrepayButton", card.transform, "▼ 繰り上げ返済", new Color(0.5f, 0.5f, 0.6f), Color.white);
            SetRectTransform(prepayBtn.gameObject, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.32f), Vector2.zero, Vector2.zero);
        }
        
        private void CreateSampleRentCard(Transform parent, string amount, string deadline)
        {
            var card = CreatePanel("RentCard", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0, new Color(0.95f, 0.95f, 0.98f));
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 300;
            
            CreateText("Name", card.transform, "家賃", 18, Color.black, TextAlignmentOptions.Center);
            CreateText("Amount", card.transform, amount, 20, Color.black, TextAlignmentOptions.Center);
            CreateText("Deadline", card.transform, $"期限: {deadline}", 14, Color.gray, TextAlignmentOptions.Center);
            
            var payBtn = CreateButton("PayButton", card.transform, "支払う", new Color(0.3f, 0.6f, 0.4f), Color.white);
            SetRectTransform(payBtn.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.2f), Vector2.zero, Vector2.zero);
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
            if (height > 0) rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }
        
        private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions align)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            return tmp;
        }
        
        private Button CreateButton(string name, Transform parent, string text, Color bgColor, Color textColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var img = obj.AddComponent<Image>();
            img.color = bgColor;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            SetRectFill(txtObj);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }
        
        private void SetRectFill(GameObject obj)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
#endif
    }
}
