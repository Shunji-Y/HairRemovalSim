using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using HairRemovalSim.Core;
using HairRemovalSim.Staff;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Debug panel for testing - Press F12 to toggle
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public static DebugPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        
        [Header("Status Display")]
        [SerializeField] private TMP_Text statusText;
        
        [Header("Time Controls")]
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Button timeSpeedButton;
        
        [Header("Money Controls")]
        [SerializeField] private Button addMoney1kButton;
        [SerializeField] private Button addMoney10kButton;
        [SerializeField] private Button resetMoneyButton;
        
        [Header("Loan Controls")]
        [SerializeField] private Button payAllLoansButton;
        [SerializeField] private Button resetOverdueButton;
        
        [Header("UI Controls")]
        [SerializeField] private Button openPCButton;
        [SerializeField] private Button openPaymentButton;
        [SerializeField] private Button openReceptionButton;
        [SerializeField] private Button openRegisterButton;
        
        [Header("Customer Controls")]
        [SerializeField] private Button spawnCustomerButton;
        [SerializeField] private Button clearCustomersButton;
        
        [Header("Review Controls")]
        [SerializeField] private Button addReview1StarButton;
        [SerializeField] private Button addReview3StarButton;
        [SerializeField] private Button addReview5StarButton;
        [SerializeField] private Button addReviewScoreButton;
        
        [Header("Staff Controls")]
        [SerializeField] private Button spawnStaffButton;
        [SerializeField] private Button assignReceptionButton;
        [SerializeField] private Button assignCashierButton;
        [SerializeField] private Button assignBedButton;
        [SerializeField] private Button assignRestockButton;
        [SerializeField] private Button selectPrevStaffButton;
        [SerializeField] private Button selectNextStaffButton;
        [SerializeField] private TMP_Text staffStatusText;
        
        private float timeScale = 1f;
        private int selectedStaffIndex = 0; // Current staff index for assignment
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }
        
        private void Start()
        {
            SetupButtonListeners();
        }
        
        private void Update()
        {
            // Toggle with F12 (InputSystem)
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Toggle();
            }
            
            if (IsOpen)
            {
                UpdateStatus();
            }
        }
        
        public void Toggle()
        {
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
                
                if (panel.activeSelf)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    // Restore cursor for gameplay
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        
        private void SetupButtonListeners()
        {
            // Time
            if (nextDayButton != null)
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            if (timeSpeedButton != null)
                timeSpeedButton.onClick.AddListener(OnTimeSpeedClicked);
            
            // Money
            if (addMoney1kButton != null)
                addMoney1kButton.onClick.AddListener(() => AddMoney(1000));
            if (addMoney10kButton != null)
                addMoney10kButton.onClick.AddListener(() => AddMoney(10000));
            if (resetMoneyButton != null)
                resetMoneyButton.onClick.AddListener(ResetMoney);
            
            // Loans
            if (payAllLoansButton != null)
                payAllLoansButton.onClick.AddListener(PayAllLoans);
            if (resetOverdueButton != null)
                resetOverdueButton.onClick.AddListener(ResetOverdue);
            
            // UI
            if (openPCButton != null)
                openPCButton.onClick.AddListener(OpenPC);
            if (openPaymentButton != null)
                openPaymentButton.onClick.AddListener(OpenPayment);
            if (openReceptionButton != null)
                openReceptionButton.onClick.AddListener(OpenReception);
            if (openRegisterButton != null)
                openRegisterButton.onClick.AddListener(OpenRegister);
            
            // Customers
            if (spawnCustomerButton != null)
                spawnCustomerButton.onClick.AddListener(SpawnCustomer);
            if (clearCustomersButton != null)
                clearCustomersButton.onClick.AddListener(ClearCustomers);
            
            // Reviews
            if (addReview1StarButton != null)
                addReview1StarButton.onClick.AddListener(() => AddReview(1));
            if (addReview3StarButton != null)
                addReview3StarButton.onClick.AddListener(() => AddReview(3));
            if (addReview5StarButton != null)
                addReview5StarButton.onClick.AddListener(() => AddReview(5));
            if (addReviewScoreButton != null)
                addReviewScoreButton.onClick.AddListener(AddReviewScore);
            
            // Staff
            if (spawnStaffButton != null)
                spawnStaffButton.onClick.AddListener(SpawnDebugStaff);
            if (assignReceptionButton != null)
                assignReceptionButton.onClick.AddListener(() => AssignDebugStaff(StaffAssignment.Reception));
            if (assignCashierButton != null)
                assignCashierButton.onClick.AddListener(() => AssignDebugStaff(StaffAssignment.Cashier));
            if (assignBedButton != null)
                assignBedButton.onClick.AddListener(() => AssignDebugStaffToBed(-1)); // Auto-find first available bed
            if (assignRestockButton != null)
                assignRestockButton.onClick.AddListener(() => AssignDebugStaff(StaffAssignment.Restock));
            if (selectPrevStaffButton != null)
                selectPrevStaffButton.onClick.AddListener(SelectPrevStaff);
            if (selectNextStaffButton != null)
                selectNextStaffButton.onClick.AddListener(SelectNextStaff);
        }
        
        private void UpdateStatus()
        {
            if (statusText == null) return;
            
            int day = GameManager.Instance?.DayCount ?? 0;
            string state = GameManager.Instance?.CurrentState.ToString() ?? "Unknown";
            int money = EconomyManager.Instance?.CurrentMoney ?? 0;
            int loanCount = LoanManager.Instance?.ActiveLoans.Count ?? 0;
            int totalDebt = LoanManager.Instance?.GetTotalDebt() ?? 0;
            int overdue = LoanManager.Instance?.GetOverdueCount() ?? 0;
            bool rentDue = RentManager.Instance?.HasPendingPayment(day) ?? false;
            
            statusText.text = $"Day: {day} | State: {state}\n" +
                              $"Money: ${money:N0}\n" +
                              $"Loans: {loanCount} | Debt: ${totalDebt:N0}\n" +
                              $"Overdue: {overdue} | Rent Due: {rentDue}\n" +
                              $"TimeScale: {Time.timeScale}x";
        }
        
        // === TIME ===
        private void OnNextDayClicked()
        {
            if (GameManager.Instance != null)
            {
                // If currently in Day or Preparation, trigger day end events first
                if (GameManager.Instance.CurrentState == GameManager.GameState.Day ||
                    GameManager.Instance.CurrentState == GameManager.GameState.Preparation)
                {
                    Debug.Log("[DebugPanel] Triggering day end events...");
                    GameEvents.TriggerShopClosed();
                }
                
                GameManager.Instance.StartNextDay();
                Debug.Log("[DebugPanel] Skipped to next day");
            }
        }
        
        private void OnTimeSpeedClicked()
        {
            timeScale = timeScale >= 10f ? 1f : timeScale * 2f;
            Time.timeScale = timeScale;
            
            if (timeSpeedButton != null)
            {
                var text = timeSpeedButton.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = $"Speed: {timeScale}x";
            }
        }
        
        // === MONEY ===
        private void AddMoney(int amount)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(amount);
                Debug.Log($"[DebugPanel] Added ${amount}");
            }
        }
        
        private void ResetMoney()
        {
            // Set money to 0 then add starting amount
            if (EconomyManager.Instance != null)
            {
                int current = EconomyManager.Instance.CurrentMoney;
                EconomyManager.Instance.SpendMoney(current);
                EconomyManager.Instance.AddMoney(10000);
                Debug.Log("[DebugPanel] Money reset to $10,000");
            }
        }
        
        // === LOANS ===
        private void PayAllLoans()
        {
            if (LoanManager.Instance == null) return;
            
            // Pay all cards
            foreach (var card in LoanManager.Instance.PaymentCards.ToArray())
            {
                card.isPaid = true;
            }
            LoanManager.Instance.PaymentCards.Clear();
            LoanManager.Instance.ActiveLoans.Clear();
            Debug.Log("[DebugPanel] All loans and cards cleared");
        }
        
        private void ResetOverdue()
        {
            if (LoanManager.Instance == null) return;
            
            foreach (var card in LoanManager.Instance.PaymentCards)
            {
                card.isOverdue = false;
            }
            Debug.Log("[DebugPanel] Overdue status reset");
        }
        
        // === REVIEWS ===
        private void AddReview(int stars)
        {
            if (ShopManager.Instance == null) return;
            
            // Add customer review (with template)
            ShopManager.Instance.AddCustomerReview(stars);
            
            // Also add score based on stars
            int scoreChange = (stars - 3) * 100; // 1★=-200, 3★=0, 5★=+200
            AddReviewScoreInternal(scoreChange);
            
            Debug.Log($"[DebugPanel] Added {stars}-star review");
        }
        
        private void AddReviewScore()
        {
            AddReviewScoreInternal(200);
            Debug.Log("[DebugPanel] Added +200 review score");
        }
        
        private void AddReviewScoreInternal(int amount)
        {
            if (ShopManager.Instance == null) return;
            
            // Use reflection or direct field access - need to add a method to ShopManager
            ShopManager.Instance.AddReviewScore(amount);
        }
        
        // === UI ===
        private void OpenPC()
        {
            var pcUI = FindFirstObjectByType<Environment.PCUIManager>();
            if (pcUI != null)
            {
                pcUI.ShowDesktop();
                Debug.Log("[DebugPanel] PC opened");
            }
        }
        
        private void OpenPayment()
        {
            if (PaymentListPanel.Instance != null)
            {
                PaymentListPanel.Instance.gameObject.SetActive(true);
                Debug.Log("[DebugPanel] Payment panel opened");
            }
        }
        
        private void OpenReception()
        {
            var reception = FindFirstObjectByType<ReceptionPanel>();
            if (reception != null)
            {
                // Need to pass a customer, so just show the panel for now
                reception.gameObject.SetActive(true);
                Debug.Log("[DebugPanel] Reception panel opened");
            }
        }
        
        private void OpenRegister()
        {
            var paymentPanel = FindFirstObjectByType<PaymentPanel>();
            if (paymentPanel != null)
            {
                paymentPanel.gameObject.SetActive(true);
                Debug.Log("[DebugPanel] Register panel opened");
            }
        }
        
        // === CUSTOMERS ===
        private void SpawnCustomer()
        {
            var spawner = FindFirstObjectByType<Customer.CustomerSpawner>();
            if (spawner != null)
            {
                // Call via SendMessage to access private/protected method
                spawner.SendMessage("SpawnCustomer", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[DebugPanel] Customer spawn requested");
            }
        }
        
        private void ClearCustomers()
        {
            var customers = FindObjectsByType<Customer.CustomerController>(FindObjectsSortMode.None);
            foreach (var c in customers)
            {
                c.LeaveShop();
            }
            Debug.Log($"[DebugPanel] Cleared {customers.Length} customers");
        }
        
        // === STAFF ===
        private void SpawnDebugStaff()
        {
            var staffManager = StaffManager.Instance;
            if (staffManager == null)
            {
                Debug.LogWarning("[DebugPanel] StaffManager not found!");
                return;
            }
            
            // Get first available profile
            if (staffManager.availableProfiles.Count > 0)
            {
                var profile = staffManager.availableProfiles[0];
                var hiredData = staffManager.DebugHireAndActivate(profile);
                if (hiredData != null)
                {
                    Debug.Log($"[DebugPanel] Spawned staff: {hiredData.Name}");
                    UpdateStaffStatus();
                }
            }
            else
            {
                Debug.LogWarning("[DebugPanel] No staff profiles available!");
            }
        }
        
        private void AssignDebugStaff(StaffAssignment assignment)
        {
            var staffManager = StaffManager.Instance;
            if (staffManager == null) return;
            
            var hiredStaff = staffManager.GetHiredStaff();
            if (selectedStaffIndex >= 0 && selectedStaffIndex < hiredStaff.Count)
            {
                staffManager.SetStaffAssignment(hiredStaff[selectedStaffIndex], assignment);
                Debug.Log($"[DebugPanel] Assigned staff {selectedStaffIndex} ({hiredStaff[selectedStaffIndex].Name}) to {assignment}");
                UpdateStaffStatus();
            }
            else
            {
                Debug.LogWarning($"[DebugPanel] No staff at index {selectedStaffIndex}");
            }
        }
        
        private void AssignDebugStaffToBed(int bedIndex)
        {
            var staffManager = StaffManager.Instance;
            if (staffManager == null) return;
            
            var hiredStaff = staffManager.GetHiredStaff();
            if (selectedStaffIndex >= 0 && selectedStaffIndex < hiredStaff.Count)
            {
                // Find first available bed if -1 passed, or use specified index
                int targetBed = bedIndex >= 0 ? bedIndex : staffManager.GetFirstAvailableBedIndex();
                
                if (targetBed < 0)
                {
                    Debug.LogWarning("[DebugPanel] No available beds for assignment");
                    return;
                }
                
                bool success = staffManager.SetStaffAssignment(hiredStaff[selectedStaffIndex], StaffAssignment.Treatment, targetBed);
                if (success)
                {
                    Debug.Log($"[DebugPanel] Assigned staff {selectedStaffIndex} ({hiredStaff[selectedStaffIndex].Name}) to bed {targetBed}");
                }
                UpdateStaffStatus();
            }
        }
        
        private void SelectPrevStaff()
        {
            var staffManager = StaffManager.Instance;
            if (staffManager == null) return;
            
            var hiredStaff = staffManager.GetHiredStaff();
            if (hiredStaff.Count == 0) return;
            
            selectedStaffIndex--;
            if (selectedStaffIndex < 0)
                selectedStaffIndex = hiredStaff.Count - 1;
            
            UpdateStaffStatus();
            Debug.Log($"[DebugPanel] Selected staff index: {selectedStaffIndex}");
        }
        
        private void SelectNextStaff()
        {
            var staffManager = StaffManager.Instance;
            if (staffManager == null) return;
            
            var hiredStaff = staffManager.GetHiredStaff();
            if (hiredStaff.Count == 0) return;
            
            selectedStaffIndex++;
            if (selectedStaffIndex >= hiredStaff.Count)
                selectedStaffIndex = 0;
            
            UpdateStaffStatus();
            Debug.Log($"[DebugPanel] Selected staff index: {selectedStaffIndex}");
        }
        
        private void UpdateStaffStatus()
        {
            if (staffStatusText == null) return;
            
            var staffManager = StaffManager.Instance;
            if (staffManager == null)
            {
                staffStatusText.text = "StaffManager: N/A";
                return;
            }
            
            var hiredStaff = staffManager.GetHiredStaff();
            string text = $"Staff: {hiredStaff.Count} (Select: {selectedStaffIndex})\n";
            for (int i = 0; i < hiredStaff.Count; i++)
            {
                var staff = hiredStaff[i];
                string marker = (i == selectedStaffIndex) ? "→ " : "  ";
                text += $"{marker}{staff.Name}: {staff.GetAssignmentDisplayText()}\n";
            }
            staffStatusText.text = text;
        }
        
#if UNITY_EDITOR
      //  [ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            Color btnColor = new Color(0.3f, 0.3f, 0.4f);
            
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
            
            // Panel background - fixed size, left-aligned
            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(10, 0);
            bgRect.sizeDelta = new Vector2(280, 0);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = bgColor;
            
            // Add ScrollRect for scrolling
            var scrollRect = bg.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30;
            
            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(bg.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(5, 5);
            vpRect.offsetMax = new Vector2(-5, -5);
            var vpMask = viewport.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;
            scrollRect.viewport = vpRect;
            
            // Content container with VerticalLayoutGroup
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            
            // Title
            var title = CreateTextElement(content.transform, "DEBUG PANEL (F12)", 18, Color.yellow, 30);
            
            // Status
            var status = CreateTextElement(content.transform, "Status...", 11, Color.white, 80);
            status.alignment = TextAlignmentOptions.TopLeft;
            statusText = status;
            
            // === TIME ===
            CreateHeader(content.transform, "[ TIME ]");
            nextDayButton = CreateBtn(content.transform, "Next Day", btnColor);
            timeSpeedButton = CreateBtn(content.transform, "Speed: 1x", btnColor);
            
            // === MONEY ===
            CreateHeader(content.transform, "[ MONEY ]");
            addMoney1kButton = CreateBtn(content.transform, "+$1,000", btnColor);
            addMoney10kButton = CreateBtn(content.transform, "+$10,000", btnColor);
            resetMoneyButton = CreateBtn(content.transform, "Reset Money", btnColor);
            
            // === LOANS ===
            CreateHeader(content.transform, "[ LOANS ]");
            payAllLoansButton = CreateBtn(content.transform, "Pay All Loans", btnColor);
            resetOverdueButton = CreateBtn(content.transform, "Reset Overdue", btnColor);
            
            // === UI ===
            CreateHeader(content.transform, "[ UI ]");
            openPCButton = CreateBtn(content.transform, "Open PC", btnColor);
            openPaymentButton = CreateBtn(content.transform, "Open Payment", btnColor);
            openReceptionButton = CreateBtn(content.transform, "Open Reception", btnColor);
            openRegisterButton = CreateBtn(content.transform, "Open Register", btnColor);
            
            // === CUSTOMERS ===
            CreateHeader(content.transform, "[ CUSTOMERS ]");
            spawnCustomerButton = CreateBtn(content.transform, "Spawn Customer", btnColor);
            clearCustomersButton = CreateBtn(content.transform, "Clear All", btnColor);
            
            panel = bg;
            bg.SetActive(false);
            
            Debug.Log("[DebugPanel] UI generated!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private TMP_Text CreateTextElement(Transform parent, string text, int size, Color color, float height)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
        
        private void CreateHeader(Transform parent, string text)
        {
            CreateTextElement(parent, text, 12, new Color(0.5f, 0.8f, 1f), 20);
        }
        
        private Button CreateBtn(Transform parent, string text, Color color)
        {
            var obj = new GameObject($"Btn_{text}");
            obj.transform.SetParent(parent, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 28;
            var img = obj.AddComponent<Image>();
            img.color = color;
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
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }
        
        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
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
        
        private TMP_Text CreateText(string name, Transform parent, string text, int size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
        
        private void CreateSectionHeader(Transform parent, string text, ref float y)
        {
            var header = CreateText($"Header_{text}", parent, $"[ {text} ]", 12, new Color(0.5f, 0.8f, 1f));
            SetRect(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, y - 20), new Vector2(0, y));
            y -= 25;
        }
        
        private Button CreateButton(Transform parent, string text, ref float y, Color color)
        {
            var obj = new GameObject($"Btn_{text}");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            SetRect(obj, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, y - 25), new Vector2(-10, y));
            
            var img = obj.AddComponent<Image>();
            img.color = color;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(obj.transform, false);
            SetRect(txtObj, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            y -= 30;
            return btn;
        }
        
        private void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        
        private void SetRect(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            SetRect(text.gameObject, anchorMin, anchorMax, offsetMin, offsetMax);
        }
#endif
    }
}
