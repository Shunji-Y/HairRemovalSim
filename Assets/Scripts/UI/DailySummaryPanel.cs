using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;
using HairRemovalSim.Core;
using HairRemovalSim.Store;
using HairRemovalSim.Environment;
using HairRemovalSim.Customer;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Daily summary panel shown at end of business day.
    /// Displays day's statistics before proceeding to next day.
    /// </summary>
    public class DailySummaryPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        
        [Header("Stats Text (Auto-generated if null)")]
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private TMP_Text expensesText;
        [SerializeField] private TMP_Text profitText;
        [SerializeField] private TMP_Text loanText;
        [SerializeField] private TMP_Text customersText;
        [SerializeField] private TMP_Text angryCustomersText;
        [SerializeField] private TMP_Text debrisText;
        [SerializeField] private TMP_Text reviewText;
        [SerializeField] private TMP_Text averageReviewText;
        [SerializeField] private TMP_Text pendingOrdersText;
        [SerializeField] private TMP_Text gradeText;
        
        [Header("Graph (for future implementation)")]
        [SerializeField] private RectTransform graphContainer;
        [SerializeField] private TMP_Text graphTotalText;
        
        [Header("Button")]
        [SerializeField] private Button nextDayButton;
        
        [Header("Colors")]
        [SerializeField] private Color headerColor = new Color(0.4f, 0.2f, 0.6f);
        [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color profitPositiveColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color profitNegativeColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color graphLineColor = new Color(0.3f, 0.9f, 0.3f);
        
        [Header("Effects")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        // Graph data tracking
        private List<float> moneyHistory = new List<float>();
        private List<GameObject> graphPoints = new List<GameObject>();
        private List<GameObject> graphLines = new List<GameObject>();
        
        public static DailySummaryPanel Instance { get; private set; }
        
        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            
            // Auto-generate UI if panelRoot is null
            if (panelRoot == null)
            {
                GenerateUIStructure();
            }
            
            if (nextDayButton != null)
            {
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            }
            
            // Hide by default
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to money changes for graph
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;
            }
        }
        
        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
            }
        }
        
        private void OnMoneyChanged(int amount)
        {
            // Record money for graph
            if (EconomyManager.Instance != null)
            {
                moneyHistory.Add(EconomyManager.Instance.CurrentMoney);
            }
        }
        
        /// <summary>
        /// Auto-generate the UI structure matching the reference design
        /// </summary>
      //  [ContextMenu("Generate UI Structure")]
        public void GenerateUIStructure()
        {
            // Create Canvas if needed
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Create Fade Overlay
            var fadeObj = new GameObject("FadeOverlay");
            fadeObj.transform.SetParent(transform);
            var fadeRect = fadeObj.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            fadeRect.anchoredPosition = Vector2.zero;
            
            var fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = true; // Block clicks
            
            fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeObj.SetActive(false);

            // Main panel root
            panelRoot = CreatePanel("DailySummaryRoot", transform, new Vector2(900, 500));
            var rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchoredPosition = Vector2.zero;
            
            // Remove Image from root (just container)
            var rootImage = panelRoot.GetComponent<Image>();
            if (rootImage != null) DestroyImmediate(rootImage);
            
            // === LEFT PANEL: Today's Results ===
            var leftPanel = CreatePanel("LeftPanel", panelRoot.transform, new Vector2(440, 480));
            SetAnchors(leftPanel, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            leftPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(230, 0);
            leftPanel.GetComponent<Image>().color = panelColor;
            AddOutline(leftPanel, new Color(0.6f, 0.5f, 0.2f), 3);
            
            // Left header
            var leftHeader = CreateHeader("LeftHeader", leftPanel.transform, "TODAY'S RESULTS", 400, 40);
            SetAnchors(leftHeader, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            leftHeader.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            
            // Stats container
            var statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(leftPanel.transform);
            var statsRect = statsContainer.AddComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(400, 380);
            SetAnchors(statsContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            statsRect.anchoredPosition = new Vector2(0, -30);
            
            var vlg = statsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(15, 15, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            
            // Create stat rows
            revenueText = CreateStatRow(statsContainer.transform, "¥", "売り上げ", "¥0", Color.yellow).Item2;
            expensesText = CreateStatRow(statsContainer.transform, "↓", "損失", "¥0", Color.red).Item2;
            profitText = CreateStatRow(statsContainer.transform, "↑", "純利益", "¥0", profitPositiveColor, true).Item2;
            
            CreateSeparator(statsContainer.transform);
            
            loanText = CreateStatRow(statsContainer.transform, "🏛", "ローン残債", "¥0", Color.white).Item2;
            customersText = CreateStatRow(statsContainer.transform, "👤", "来店客数", "0", Color.white).Item2;
            angryCustomersText = CreateStatRow(statsContainer.transform, "😠", "怒って帰った客数", "0", Color.red).Item2;
            debrisText = CreateStatRow(statsContainer.transform, "🗑", "残りデブリ数", "0", Color.white).Item2;
            
            CreateSeparator(statsContainer.transform);
            
            reviewText = CreateStatRow(statsContainer.transform, "★", "現在のレビュー値", "★★★★☆ (4.0)", Color.yellow).Item2;
            averageReviewText = CreateStatRow(statsContainer.transform, "📊", "本日の平均レビュー値", "★★★★☆ (4.0)", Color.cyan).Item2;
            
            CreateSeparator(statsContainer.transform);
            
            pendingOrdersText = CreateStatRow(statsContainer.transform, "📦", "翌日届くアイテム", "なし", Color.white).Item2;
            gradeText = CreateStatRow(statsContainer.transform, "👑", "ショップグレード", "1", Color.yellow).Item2;
            
            // === RIGHT PANEL: Sales Trend Graph ===
            var rightPanel = CreatePanel("RightPanel", panelRoot.transform, new Vector2(420, 480));
            SetAnchors(rightPanel, new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            rightPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 0);
            rightPanel.GetComponent<Image>().color = panelColor;
            AddOutline(rightPanel, new Color(0.6f, 0.5f, 0.2f), 3);
            
            // Right header
            var rightHeader = CreateHeader("RightHeader", rightPanel.transform, "SALES TREND", 380, 40);
            SetAnchors(rightHeader, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rightHeader.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            
            // Graph container
            graphContainer = new GameObject("GraphContainer").AddComponent<RectTransform>();
            graphContainer.SetParent(rightPanel.transform);
            graphContainer.sizeDelta = new Vector2(360, 280);
            SetAnchors(graphContainer.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            graphContainer.anchoredPosition = new Vector2(0, 10);
            
            // Graph background
            var graphBg = graphContainer.gameObject.AddComponent<Image>();
            graphBg.color = new Color(0.05f, 0.1f, 0.05f, 0.8f);
            
            // Graph total text
            var totalObj = new GameObject("GraphTotal");
            totalObj.transform.SetParent(graphContainer);
            graphTotalText = totalObj.AddComponent<TextMeshProUGUI>();
            graphTotalText.text = "Total: ¥0";
            graphTotalText.fontSize = 16;
            graphTotalText.color = Color.white;
            graphTotalText.alignment = TextAlignmentOptions.TopRight;
            var totalRect = totalObj.GetComponent<RectTransform>();
            totalRect.sizeDelta = new Vector2(150, 30);
            SetAnchors(totalObj, new Vector2(1, 1), new Vector2(1, 1));
            totalRect.anchoredPosition = new Vector2(-10, -10);
            
            // Axis labels (placeholder)
            CreateAxisLabels(graphContainer);
            
            // NEXT DAY Button
            var buttonObj = new GameObject("NextDayButton");
            buttonObj.transform.SetParent(rightPanel.transform);
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(180, 50);
            SetAnchors(buttonObj, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            buttonRect.anchoredPosition = new Vector2(0, 40);
            
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonColor;
            AddOutline(buttonObj, Color.black, 2);
            
            nextDayButton = buttonObj.AddComponent<Button>();
            nextDayButton.targetGraphic = buttonImage;
            nextDayButton.onClick.AddListener(OnNextDayClicked);
            
            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObj.transform);
            var btnTmp = buttonText.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "NEXT DAY";
            btnTmp.fontSize = 24;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color = Color.white;
            btnTmp.alignment = TextAlignmentOptions.Center;
            var btnTextRect = buttonText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            btnTextRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("[DailySummaryPanel] UI structure generated!");
        }
        
        private GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = panelColor;
            return panel;
        }
        
        private GameObject CreateHeader(string name, Transform parent, string text, float width, float height)
        {
            var header = new GameObject(name);
            header.transform.SetParent(parent);
            var rect = header.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            var bg = header.AddComponent<Image>();
            bg.color = headerColor;
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(header.transform);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            return header;
        }
        
        private (GameObject, TMP_Text) CreateStatRow(Transform parent, string icon, string label, string value, Color valueColor, bool bold = false)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 28);
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            
            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform);
            var iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = icon;
            iconTmp.fontSize = 18;
            iconTmp.color = valueColor;
            var iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 30;
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform);
            var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 16;
            labelTmp.color = Color.white;
            var labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 180;
            
            // Value
            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform);
            var valueTmp = valueObj.AddComponent<TextMeshProUGUI>();
            valueTmp.text = value;
            valueTmp.fontSize = bold ? 20 : 16;
            valueTmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            valueTmp.color = valueColor;
            valueTmp.alignment = TextAlignmentOptions.Right;
            var valueLE = valueObj.AddComponent<LayoutElement>();
            valueLE.flexibleWidth = 1;
            
            return (row, valueTmp);
        }
        
        private void CreateSeparator(Transform parent)
        {
            var sep = new GameObject("Separator");
            sep.transform.SetParent(parent);
            var rect = sep.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 2);
            var img = sep.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f);
            var le = sep.AddComponent<LayoutElement>();
            le.preferredHeight = 2;
        }
        
        private void CreateAxisLabels(RectTransform container)
        {
            // Y-axis label
            var yAxis = new GameObject("YAxisLabel");
            yAxis.transform.SetParent(container);
            var yTmp = yAxis.AddComponent<TextMeshProUGUI>();
            yTmp.text = "AMOUNT (¥)";
            yTmp.fontSize = 10;
            yTmp.color = Color.gray;
            yTmp.alignment = TextAlignmentOptions.Center;
            var yRect = yAxis.GetComponent<RectTransform>();
            yRect.sizeDelta = new Vector2(100, 20);
            SetAnchors(yAxis, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            yRect.anchoredPosition = new Vector2(-30, 0);
            yRect.localRotation = Quaternion.Euler(0, 0, 90);
            
            // X-axis label
            var xAxis = new GameObject("XAxisLabel");
            xAxis.transform.SetParent(container);
            var xTmp = xAxis.AddComponent<TextMeshProUGUI>();
            xTmp.text = "TIME";
            xTmp.fontSize = 10;
            xTmp.color = Color.gray;
            xTmp.alignment = TextAlignmentOptions.Center;
            var xRect = xAxis.GetComponent<RectTransform>();
            xRect.sizeDelta = new Vector2(100, 20);
            SetAnchors(xAxis, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            xRect.anchoredPosition = new Vector2(0, -10);
            
            // Time labels
            string[] times = { "9:00", "12:00", "15:00", "18:00", "CLOSE" };
            for (int i = 0; i < times.Length; i++)
            {
                var timeLabel = new GameObject($"Time_{times[i]}");
                timeLabel.transform.SetParent(container);
                var tmp = timeLabel.AddComponent<TextMeshProUGUI>();
                tmp.text = times[i];
                tmp.fontSize = 9;
                tmp.color = Color.gray;
                tmp.alignment = TextAlignmentOptions.Center;
                var rect = timeLabel.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(50, 15);
                float xPos = -150 + (i * 75);
                SetAnchors(timeLabel, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
                rect.anchoredPosition = new Vector2(xPos, 15);
            }
        }
        
        private void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
            }
        }
        
        private void AddOutline(GameObject obj, Color color, float width)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
        }
        
        /// <summary>
        /// Show the daily summary panel with fade effect and cursor unlock
        /// </summary>
        public void Show()
        {
            StartCoroutine(ShowRoutine());
        }
        
        private System.Collections.IEnumerator ShowRoutine()
        {
            // Toggle cursor
            ToggleCursor(true);
            
            // Disable player control if possible
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null) player.enabled = false;

            // Fade in black BG
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
                fadeCanvasGroup.alpha = 0f;
                float duration = fadeDuration;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }
            
            // Show panel
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            
            UpdateDisplay();
            UpdateGraph();
        }
        
        private void ToggleCursor(bool show)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
        
        /// <summary>
        /// Hide the panel
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        
        private void UpdateDisplay()
        {
            var stats = DailyStatsManager.Instance;
            
            // Revenue & Expenses
            if (revenueText != null)
                revenueText.text = $"¥{stats?.TodayRevenue ?? 0:N0}";
            
            if (expensesText != null)
                expensesText.text = $"¥{stats?.TodayExpenses ?? 0:N0}";
            
            if (profitText != null)
            {
                int profit = stats?.TodayProfit ?? 0;
                profitText.text = $"¥{profit:N0}";
                profitText.color = profit >= 0 ? profitPositiveColor : profitNegativeColor;
            }
            
            // Loan
            if (loanText != null)
            {
                int debt = LoanManager.Instance?.GetTotalPrincipal() ?? 0;
                loanText.text = $"¥{debt:N0}";
            }
            
            // Customers
            if (customersText != null)
                customersText.text = $"{stats?.CustomersToday ?? 0}";
            
            if (angryCustomersText != null)
                angryCustomersText.text = $"{stats?.AngryCustomersToday ?? 0}";
            
            // Debris
            if (debrisText != null)
            {
                int remaining = HairDebrisManager.Instance?.GetRemainingCount() ?? 0;
                debrisText.text = $"{remaining}";
            }
            
            // Review
            if (reviewText != null && ShopManager.Instance != null)
            {
                int score = ShopManager.Instance.ReviewScore;
                int stars = 1;
                
                // Use PaymentPanel logic if available
                if (PaymentPanel.Instance != null)
                {
                    PaymentPanel.MoodLevel mood = PaymentPanel.Instance.GetMoodFromReview(score);
                    stars = PaymentPanel.Instance.GetStarsFromMood(mood);
                }
                else
                {
                    // Basic fallback
                    stars = Mathf.Clamp(Mathf.RoundToInt(score / 20f), 1, 5);
                }
                
                reviewText.text = $"{score}";
            }
            
            if (averageReviewText != null)
            {
                float avg = FindFirstObjectByType<CustomerSpawner>().GetDailyAverageReview();
                var avg2 = (avg + 50) / 20;
                int avgStars = 3;
                
                if (PaymentPanel.Instance != null)
                {
                    PaymentPanel.MoodLevel mood = PaymentPanel.Instance.GetMoodFromReview((int)avg);
                    avgStars = PaymentPanel.Instance.GetStarsFromMood(mood);
                }
                else
                {
                    avgStars = Mathf.Clamp(Mathf.RoundToInt(avg / 20f), 1, 5);
                }
                
                averageReviewText.text = $"{GetStarString(avgStars)} ({avg2:F1})";
            }
            
            // Pending orders
            if (pendingOrdersText != null && InventoryManager.Instance != null)
            {
                var orders = InventoryManager.Instance.GetPendingOrders();
                if (orders.Count == 0)
                {
                    pendingOrdersText.text = "なし";
                }
                else
                {
                    var sb = new StringBuilder();
                    int count = 0;
                    foreach (var order in orders)
                    {
                        if (count > 0) sb.Append("\n");
                        var itemData = ItemDataRegistry.Instance?.GetItem(order.Key);
                        string name = itemData?.name ?? order.Key;
                        sb.Append($"{name} x{order.Value}");
                        count++;
                        if (count >= 3) { sb.Append("..."); break; }
                    }
                    pendingOrdersText.text = sb.ToString();
                }
            }
            
            // Grade
            if (gradeText != null && ShopManager.Instance != null)
            {
                gradeText.text = $"{ShopManager.Instance.ShopGrade}";
            }
            
            // Graph total
            if (graphTotalText != null && EconomyManager.Instance != null)
            {
                graphTotalText.text = $"Total: ¥{EconomyManager.Instance.CurrentMoney:N0}";
            }
        }
        
        private string GetStarString(int stars)
        {
            string result = "";
            for (int i = 0; i < 5; i++)
            {
                result += i < stars ? "★" : "☆";
            }
            return result;
        }
        
        private void UpdateGraph()
        {
            if (graphContainer == null) return;
            
            // Clear old graph elements
            foreach (var point in graphPoints)
            {
                if (point != null) Destroy(point);
            }
            graphPoints.Clear();
            
            foreach (var line in graphLines)
            {
                if (line != null) Destroy(line);
            }
            graphLines.Clear();
            
            if (moneyHistory.Count < 2) return;
            
            // Find min/max for scaling
            float minMoney = float.MaxValue;
            float maxMoney = float.MinValue;
            foreach (var m in moneyHistory)
            {
                if (m < minMoney) minMoney = m;
                if (m > maxMoney) maxMoney = m;
            }
            
            float range = maxMoney - minMoney;
            if (range < 1000) range = 1000;
            
            float graphWidth = graphContainer.sizeDelta.x - 40;
            float graphHeight = graphContainer.sizeDelta.y - 60;
            
            Vector2 prevPos = Vector2.zero;
            
            for (int i = 0; i < moneyHistory.Count; i++)
            {
                float xNorm = (float)i / (moneyHistory.Count - 1);
                float yNorm = (moneyHistory[i] - minMoney) / range;
                
                float x = -graphWidth / 2 + xNorm * graphWidth;
                float y = -graphHeight / 2 + yNorm * graphHeight + 10;
                
                Vector2 pos = new Vector2(x, y);
                
                // Create point
                var point = new GameObject($"Point_{i}");
                point.transform.SetParent(graphContainer);
                var pointRect = point.AddComponent<RectTransform>();
                pointRect.sizeDelta = new Vector2(10, 10);
                pointRect.anchoredPosition = pos;
                var pointImg = point.AddComponent<Image>();
                pointImg.color = graphLineColor;
                graphPoints.Add(point);
                
                // Create line to previous point
                if (i > 0)
                {
                    var line = CreateLine(prevPos, pos, graphLineColor);
                    graphLines.Add(line);
                }
                
                prevPos = pos;
            }
        }
        
        private GameObject CreateLine(Vector2 start, Vector2 end, Color color)
        {
            var line = new GameObject("Line");
            line.transform.SetParent(graphContainer);
            var rect = line.AddComponent<RectTransform>();
            
            Vector2 dir = end - start;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            rect.sizeDelta = new Vector2(distance, 3);
            rect.anchoredPosition = start + dir * 0.5f;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            var img = line.AddComponent<Image>();
            img.color = color;
            
            return line;
        }
        
        public void ClearMoneyHistory()
        {
            moneyHistory.Clear();
        }
        
        private void OnNextDayClicked()
        {
            StartCoroutine(NextDayRoutine());
        }

        private System.Collections.IEnumerator NextDayRoutine()
        {
            // 1. Bring fade overlay to front and ensure it's active
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
                fadeCanvasGroup.transform.SetAsLastSibling(); // Cover the panel
                
                // Get Image to manipulate alpha directly (as per user request: original is 0.95)
                var fadeImage = fadeCanvasGroup.GetComponent<Image>();
                Color originalColor = Color.black; 
                if (fadeImage != null)
                {
                    originalColor = fadeImage.color;
                    
                    // Fade Image Alpha to 1.0 (Solid Black)
                    float fadeToBlackDuration = 0.5f;
                    float elapsed = 0f;
                    float startAlpha = originalColor.a;
                    
                    while (elapsed < fadeToBlackDuration)
                    {
                        elapsed += Time.deltaTime;
                        float newAlpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeToBlackDuration);
                        fadeImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
                        yield return null;
                    }
                    fadeImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
                }
                
                // 2. Wait 1 second (User request: "1秒待ってから")
                yield return new WaitForSeconds(1.0f);

                // 3. Process Day Transition
                if (panelRoot != null) panelRoot.SetActive(false); // Hide panel behind the black screen
                
                if (GameManager.Instance != null)
                {
                    if (DailyStatsManager.Instance != null) DailyStatsManager.Instance.ResetForNewDay();
                    GameManager.Instance.StartNextDay();
                }
                
                ClearMoneyHistory();
                
                // Wait a frame for initialization
                yield return null;

                // 4. Fade Out (FadeCanvasGroup 1 -> 0)
                float fadeOutDuration = fadeDuration;
                float fadeOutElapsed = 0f;
                
                while (fadeOutElapsed < fadeOutDuration)
                {
                    fadeOutElapsed += Time.deltaTime;
                    fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOutElapsed / fadeOutDuration);
                    yield return null;
                }
                
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.gameObject.SetActive(false);
                
                // 5. Restore original state (Sibling index and Image alpha)
                fadeCanvasGroup.transform.SetAsFirstSibling(); // Send back
                if (fadeImage != null)
                {
                    fadeImage.color = originalColor; // Restore 0.95 alpha
                }
            }
            else
            {
                // Fallback if no fade group
                if (panelRoot != null) panelRoot.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.StartNextDay();
            }
            
            // Re-enable controls
            ToggleCursor(false);
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null) player.enabled = true;
        }
    }
}
