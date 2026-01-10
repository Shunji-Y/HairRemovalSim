using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HairRemovalSim.Staff;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Main staff panel with Hire and Manage tabs.
    /// Connected to PCUIManager as staffPanel.
    /// </summary>
    public class StaffPanel : MonoBehaviour
    {
        public static StaffPanel Instance { get; private set; }
        
        [Header("Tab Buttons")]
        [SerializeField] private Button hireTabButton;
        [SerializeField] private Button manageTabButton;
        
        [Header("Tab Content")]
        [SerializeField] private GameObject hireTabContent;
        [SerializeField] private GameObject manageTabContent;
        
        [Header("Hire Tab - Card Container")]
        [SerializeField] private Transform candidateCardContainer;
        [SerializeField] private GameObject candidateCardPrefab;
        
        [Header("Manage Tab - Card Container")]
        [SerializeField] private Transform hiredCardContainer;
        [SerializeField] private GameObject hiredCardPrefab;
        
        [Header("Hire Dialog")]
        [SerializeField] private StaffHireDialog hireDialog;
        
        [Header("Fire Confirm Dialog")]
        [SerializeField] private StaffFireConfirmDialog fireConfirmDialog;
        
        [Header("Info Display")]
        [SerializeField] private TMP_Text staffCountText;
        [SerializeField] private TMP_Text gradeInfoText;
        
        private List<StaffCandidateCardUI> candidateCards = new List<StaffCandidateCardUI>();
        private List<StaffManageCardUI> hiredCards = new List<StaffManageCardUI>();
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            // Setup tab buttons
            if (hireTabButton != null)
                hireTabButton.onClick.AddListener(() => ShowTab(true));
            if (manageTabButton != null)
                manageTabButton.onClick.AddListener(() => ShowTab(false));
        }
        
        private void OnEnable()
        {
            // Refresh candidates in case grade changed
            if (StaffCandidateGenerator.Instance != null)
            {
                StaffCandidateGenerator.Instance.RefreshCandidates();
            }
            
            RefreshAll();
            ShowTab(true); // Default to hire tab
            
            // Complete tut_staff_open when panel is opened
            Core.TutorialManager.Instance?.CompleteByAction("staff_panel_opened");
            
            // Tutorial trigger for staff hiring
            Core.TutorialManager.Instance?.TryShowTutorial("tut_staff_about");
        }
        
        private void OnDisable()
        {
            // Complete tut_staff_about when panel is closed
            Core.TutorialManager.Instance?.CompleteByAction("staff_panel_closed");
        }
        
        /// <summary>
        /// Show either hire or manage tab
        /// </summary>
        public void ShowTab(bool showHire)
        {
            if (hireTabContent != null)
                hireTabContent.SetActive(showHire);
            if (manageTabContent != null)
                manageTabContent.SetActive(!showHire);
            
            // Update tab button visuals
            UpdateTabButtonVisuals(showHire);
            
            if (showHire)
                RefreshCandidateCards();
            else
                RefreshHiredCards();
        }
        
        private void UpdateTabButtonVisuals(bool hireSelected)
        {
            // Visual feedback for selected tab (could be color change, underline, etc.)
            if (hireTabButton != null)
                hireTabButton.interactable = !hireSelected;
            if (manageTabButton != null)
                manageTabButton.interactable = hireSelected;
        }
        
        /// <summary>
        /// Refresh all displays
        /// </summary>
        public void RefreshAll()
        {
            UpdateInfoDisplay();
            RefreshCandidateCards();
            RefreshHiredCards();
        }
        
        private void UpdateInfoDisplay()
        {
            int starRating = ShopManager.Instance?.StarRating ?? 1;
            int currentCount = StaffManager.Instance?.HiredStaffCount ?? 0;
            int maxCount = StaffCandidateGenerator.Instance?.HiringConfig?.GetMaxStaffForStarRating(starRating) ?? 0;
            
            if (staffCountText != null)
                staffCountText.text = L?.Get("ui.staff_count", currentCount, maxCount) ?? $"Staff: {currentCount}/{maxCount}";
            
            if (gradeInfoText != null)
                gradeInfoText.text = L?.Get("ui.star_rating", starRating) ?? $"★{starRating}";
        }
        
        /// <summary>
        /// Refresh candidate cards from generator
        /// </summary>
        private void RefreshCandidateCards()
        {
            // Clear existing
            foreach (var card in candidateCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            candidateCards.Clear();
            
            if (StaffCandidateGenerator.Instance == null)
            {
                Debug.LogWarning("[StaffPanel] StaffCandidateGenerator.Instance is null!");
                return;
            }
            if (candidateCardPrefab == null)
            {
                Debug.LogWarning("[StaffPanel] candidateCardPrefab is not assigned!");
                return;
            }
            if (candidateCardContainer == null)
            {
                Debug.LogWarning("[StaffPanel] candidateCardContainer is not assigned!");
                return;
            }
            
            var candidates = StaffCandidateGenerator.Instance.CurrentCandidates;
            Debug.Log($"[StaffPanel] Generating {candidates.Count} candidate cards");
            
            foreach (var candidate in candidates)
            {
                var cardObj = Instantiate(candidateCardPrefab, candidateCardContainer);
                cardObj.SetActive(true); // Ensure prefab is active
                var card = cardObj.GetComponent<StaffCandidateCardUI>();
                if (card != null)
                {
                    card.Setup(candidate, OnHireClicked);
                    candidateCards.Add(card);
                }
            }
        }
        
        /// <summary>
        /// Refresh hired staff cards
        /// </summary>
        private void RefreshHiredCards()
        {
            // Clear existing
            foreach (var card in hiredCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            hiredCards.Clear();
            
            if (StaffManager.Instance == null || hiredCardPrefab == null || hiredCardContainer == null)
                return;
            
            var hired = StaffManager.Instance.GetHiredStaff();
            
            foreach (var staffData in hired)
            {
                var cardObj = Instantiate(hiredCardPrefab, hiredCardContainer);
                var card = cardObj.GetComponent<StaffManageCardUI>();
                if (card != null)
                {
                    card.Setup(staffData, OnFireClicked, OnReassignClicked);
                    hiredCards.Add(card);
                }
            }
        }
        
        /// <summary>
        /// Called when hire button on candidate card is clicked
        /// </summary>
        private void OnHireClicked(StaffProfile candidate)
        {
            Debug.Log($"[StaffPanel] OnHireClicked called for {candidate?.displayName}, hireDialog={hireDialog}");
            if (hireDialog != null)
            {
                hireDialog.Show(candidate, OnHireConfirmed, OnHireCancelled);
            }
        }
        
        /// <summary>
        /// Called when hire is confirmed with assignment
        /// </summary>
        private void OnHireConfirmed(StaffProfile candidate, StaffAssignment assignment, int bedIndex)
        {
            if (StaffManager.Instance == null) return;
            
            // Use the sourceProfileData from the candidate (linked to the original ScriptableObject)
            var profileData = candidate.sourceProfileData;
            if (profileData == null)
            {
                Debug.LogError($"[StaffPanel] Candidate {candidate.displayName} has no sourceProfileData!");
                return;
            }
            
            bool success = StaffManager.Instance.HireStaff(profileData, assignment, bedIndex);
            
            if (success)
            {
                // Remove from candidates
                StaffCandidateGenerator.Instance?.RemoveCandidate(candidate);
                RefreshAll();
            }
        }
        
        private void OnHireCancelled()
        {
            // Nothing to do
        }
        
        /// <summary>
        /// Called when fire button on hired card is clicked
        /// </summary>
        private void OnFireClicked(HiredStaffData staffData)
        {
            if (fireConfirmDialog != null)
            {
                fireConfirmDialog.Show(staffData, OnFireConfirmed);
            }
        }
        
        /// <summary>
        /// Called when fire is confirmed
        /// </summary>
        private void OnFireConfirmed(HiredStaffData staffData)
        {
            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.FireStaff(staffData);
                RefreshAll();
            }
        }
        
        /// <summary>
        /// Called when reassign button on hired card is clicked
        /// </summary>
        private void OnReassignClicked(HiredStaffData staffData)
        {
            if (hireDialog != null)
            {
                hireDialog.ShowForReassignment(staffData, OnReassignConfirmed, OnReassignCancelled);
            }
        }
        
        /// <summary>
        /// Called when reassignment is confirmed
        /// </summary>
        private void OnReassignConfirmed(HiredStaffData staffData, StaffAssignment newAssignment, int bedIndex)
        {
            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.SetStaffAssignment(staffData, newAssignment, bedIndex);
                RefreshAll();
            }
        }
        
        private void OnReassignCancelled()
        {
            // Nothing to do
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Staff Panel (Generate)", false, 10)]
        private static void CreateStaffPanel()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var panelObj = new GameObject("StaffPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            var panel = panelObj.AddComponent<StaffPanel>();
            panel.GenerateUIStructure();
            
            UnityEditor.Selection.activeGameObject = panelObj;
        }
        
        //[ContextMenu("Generate UI Structure")]
        public void GenerateUIStructure()
        {
            // Colors
            Color headerBg = new Color(0.4f, 0.25f, 0.5f); // Purple theme
            Color tabSelectedBg = new Color(0.5f, 0.3f, 0.6f);
            Color tabNormalBg = new Color(0.35f, 0.2f, 0.45f);
            Color cardBg = new Color(0.95f, 0.95f, 0.95f);
            Color buttonColor = new Color(0.2f, 0.7f, 0.7f); // Cyan
            Color fireButtonColor = new Color(0.8f, 0.3f, 0.3f);
            
            // Clear existing
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            // Setup RectTransform for this panel
            var mainRect = GetComponent<RectTransform>();
            if (mainRect == null) mainRect = gameObject.AddComponent<RectTransform>();
            SetRectFill(gameObject);
            
            // === HEADER ===
            var header = CreatePanel("Header", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -60), new Vector2(0, 0), 60, headerBg);
            
            var title = CreateText("Title", header.transform, "HIRE STAFF", 28, Color.white, TextAlignmentOptions.Center);
            SetRectFill(title.gameObject);
            
            // === TAB BAR ===
            var tabBar = CreatePanel("TabBar", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -110), new Vector2(0, -60), 50, new Color(0.3f, 0.18f, 0.4f));
            
            hireTabButton = CreateButton("HireTabButton", tabBar.transform, "HIRE STAFF", tabSelectedBg, Color.white);
            SetRectTransform(hireTabButton.gameObject, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 0));
            
            manageTabButton = CreateButton("ManageTabButton", tabBar.transform, "MANAGE STAFF", tabNormalBg, Color.gray);
            SetRectTransform(manageTabButton.gameObject, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            
            // === INFO BAR ===
            var infoBar = CreatePanel("InfoBar", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -140), new Vector2(0, -110), 30, new Color(0.25f, 0.15f, 0.35f));
            
            staffCountText = CreateText("StaffCount", infoBar.transform, "Staff: 0/2", 16, Color.white, TextAlignmentOptions.Left);
            SetRectTransform(staffCountText.gameObject, new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(20, 0), new Vector2(0, 0));
            
            gradeInfoText = CreateText("GradeInfo", infoBar.transform, "Grade 2", 16, Color.white, TextAlignmentOptions.Right);
            SetRectTransform(gradeInfoText.gameObject, new Vector2(0.7f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-20, 0));
            
            // === HIRE TAB CONTENT ===
            hireTabContent = CreatePanel("HireTabContent", transform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(20, 20), new Vector2(-20, -150), 0, new Color(0.2f, 0.2f, 0.25f)).gameObject;
            
            var hireScroll = CreateScrollView("CandidateScroll", hireTabContent.transform);
            candidateCardContainer = hireScroll.transform;
            
            // Create sample candidate cards
            CreateSampleCandidateCard(candidateCardContainer, "Yuki Tanaka", "Rank: A ★★★", "¥71,429");
            CreateSampleCandidateCard(candidateCardContainer, "Kenji Sato", "Rank: B ★★", "¥71,429");
            CreateSampleCandidateCard(candidateCardContainer, "Emi Suzuki", "Rank: C ★", "¥71,429");
            
            // === MANAGE TAB CONTENT ===
            manageTabContent = CreatePanel("ManageTabContent", transform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(20, 20), new Vector2(-20, -150), 0, new Color(0.2f, 0.2f, 0.25f)).gameObject;
            manageTabContent.SetActive(false);
            
            var manageScroll = CreateScrollView("HiredScroll", manageTabContent.transform);
            hiredCardContainer = manageScroll.transform;
            
            // === HIRE DIALOG ===
            var hireDialogObj = CreateDialog("HireDialog", transform, "Select Assignment");
            hireDialog = hireDialogObj.AddComponent<StaffHireDialog>();
            CreateHireDialogContent(hireDialogObj.transform);
            hireDialogObj.SetActive(false);
            
            // === FIRE CONFIRM DIALOG ===
            var fireDialogObj = CreateDialog("FireConfirmDialog", transform, "Confirm Fire");
            fireConfirmDialog = fireDialogObj.AddComponent<StaffFireConfirmDialog>();
            CreateFireDialogContent(fireDialogObj.transform);
            fireDialogObj.SetActive(false);
            
            // Generate prefabs
            GenerateCandidateCardPrefab();
            GenerateHiredCardPrefab();
            
            Debug.Log("[StaffPanel] UI generated!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private GameObject CreateScrollView(string name, Transform parent)
        {
            var scrollObj = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0, Color.clear);
            
            var content = CreatePanel("Content", scrollObj.transform, new Vector2(0, 1), new Vector2(1, 1),
                Vector2.zero, Vector2.zero, 0, Color.clear);
            
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0, 1);
            
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 280);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            return content;
        }
        
        private void CreateSampleCandidateCard(Transform parent, string cardName, string rank, string cost)
        {
            var card = CreatePanel("Card_" + cardName, parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 0, new Color(0.9f, 0.88f, 0.85f));
            
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 280;
            
            // Photo placeholder
            var photo = CreatePanel("Photo", card.transform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.95f),
                Vector2.zero, Vector2.zero, 0, new Color(0.7f, 0.7f, 0.7f));
            
            // Name
            var nameText = CreateText("Name", card.transform, cardName, 16, Color.black, TextAlignmentOptions.Left);
            SetRectTransform(nameText.gameObject, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.38f), Vector2.zero, Vector2.zero);
            
            // Rank
            var rankText = CreateText("Rank", card.transform, rank, 12, Color.gray, TextAlignmentOptions.Left);
            SetRectTransform(rankText.gameObject, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero);
            
            // Cost
            var costText = CreateText("Cost", card.transform, "Cost/Day: " + cost, 12, Color.gray, TextAlignmentOptions.Left);
            SetRectTransform(costText.gameObject, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.2f), Vector2.zero, Vector2.zero);
            
            // Hire button
            var hireBtn = CreateButton("HireButton", card.transform, "HIRE", new Color(0.2f, 0.7f, 0.7f), Color.white);
            SetRectTransform(hireBtn.gameObject, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.1f), Vector2.zero, Vector2.zero);
        }
        
        private GameObject CreateDialog(string name, Transform parent, string titleText)
        {
            var overlay = CreatePanel(name, parent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 0, new Color(0, 0, 0, 0.5f));
            
            var dialog = CreatePanel("DialogBox", overlay.transform, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f),
                Vector2.zero, Vector2.zero, 0, new Color(0.95f, 0.95f, 0.95f));
            
            var title = CreateText("Title", dialog.transform, titleText, 24, Color.black, TextAlignmentOptions.Center);
            SetRectTransform(title.gameObject, new Vector2(0, 0.85f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            
            return overlay;
        }
        
        private void CreateHireDialogContent(Transform dialogOverlay)
        {
            var dialog = dialogOverlay.GetChild(0);
            
            var info = CreateText("CandidateInfo", dialog, "Staff Name\nRank\n¥Cost/day", 16, Color.gray, TextAlignmentOptions.Center);
            SetRectTransform(info.gameObject, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
            
            // Assignment buttons
            var receptionBtn = CreateButton("ReceptionButton", dialog, "Reception", new Color(0.3f, 0.5f, 0.7f), Color.white);
            SetRectTransform(receptionBtn.gameObject, new Vector2(0.05f, 0.4f), new Vector2(0.45f, 0.55f), Vector2.zero, Vector2.zero);
            
            var cashierBtn = CreateButton("CashierButton", dialog, "Cashier", new Color(0.3f, 0.5f, 0.7f), Color.white);
            SetRectTransform(cashierBtn.gameObject, new Vector2(0.55f, 0.4f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero);
            
            var treatmentBtn = CreateButton("TreatmentButton", dialog, "Treatment", new Color(0.3f, 0.5f, 0.7f), Color.white);
            SetRectTransform(treatmentBtn.gameObject, new Vector2(0.05f, 0.22f), new Vector2(0.45f, 0.37f), Vector2.zero, Vector2.zero);
            
            var restockBtn = CreateButton("RestockButton", dialog, "Restock", new Color(0.3f, 0.5f, 0.7f), Color.white);
            SetRectTransform(restockBtn.gameObject, new Vector2(0.55f, 0.22f), new Vector2(0.95f, 0.37f), Vector2.zero, Vector2.zero);
            
            // Confirm/Cancel
            var confirmBtn = CreateButton("ConfirmButton", dialog, "Confirm", new Color(0.2f, 0.7f, 0.4f), Color.white);
            SetRectTransform(confirmBtn.gameObject, new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.17f), Vector2.zero, Vector2.zero);
            
            var cancelBtn = CreateButton("CancelButton", dialog, "Cancel", new Color(0.6f, 0.6f, 0.6f), Color.white);
            SetRectTransform(cancelBtn.gameObject, new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.17f), Vector2.zero, Vector2.zero);
        }
        
        private void CreateFireDialogContent(Transform dialogOverlay)
        {
            var dialog = dialogOverlay.GetChild(0);
            
            var message = CreateText("Message", dialog, "Are you sure you want to fire this staff?", 18, Color.black, TextAlignmentOptions.Center);
            SetRectTransform(message.gameObject, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.8f), Vector2.zero, Vector2.zero);
            
            var confirmBtn = CreateButton("ConfirmButton", dialog, "Fire", new Color(0.8f, 0.3f, 0.3f), Color.white);
            SetRectTransform(confirmBtn.gameObject, new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.3f), Vector2.zero, Vector2.zero);
            
            var cancelBtn = CreateButton("CancelButton", dialog, "Cancel", new Color(0.6f, 0.6f, 0.6f), Color.white);
            SetRectTransform(cancelBtn.gameObject, new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.3f), Vector2.zero, Vector2.zero);
        }
        
        private void GenerateCandidateCardPrefab()
        {
            var prefab = CreatePanel("CandidateCardPrefab", transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 0, new Color(0.9f, 0.88f, 0.85f));
            
            prefab.AddComponent<StaffCandidateCardUI>();
            
            var le = prefab.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 280;
            
            // Photo
            var photoObj = CreatePanel("Photo", prefab.transform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.95f),
                Vector2.zero, Vector2.zero, 0, Color.gray);
            
            // Info texts
            CreateText("NameText", prefab.transform, "Name", 16, Color.black, TextAlignmentOptions.Left);
            CreateText("RankText", prefab.transform, "Rank", 12, Color.gray, TextAlignmentOptions.Left);
            CreateText("CostText", prefab.transform, "Cost", 12, Color.gray, TextAlignmentOptions.Left);
            
            // Hire button
            CreateButton("HireButton", prefab.transform, "HIRE", new Color(0.2f, 0.7f, 0.7f), Color.white);
            
            candidateCardPrefab = prefab;
            prefab.SetActive(false);
        }
        
        private void GenerateHiredCardPrefab()
        {
            var prefab = CreatePanel("HiredCardPrefab", transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 0, new Color(0.85f, 0.9f, 0.88f));
            
            prefab.AddComponent<StaffManageCardUI>();
            
            var le = prefab.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 280;
            
            // Photo
            CreatePanel("Photo", prefab.transform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.95f),
                Vector2.zero, Vector2.zero, 0, Color.gray);
            
            // Info texts
            CreateText("NameText", prefab.transform, "Name", 16, Color.black, TextAlignmentOptions.Left);
            CreateText("RankText", prefab.transform, "Rank", 12, Color.gray, TextAlignmentOptions.Left);
            CreateText("AssignmentText", prefab.transform, "Assignment", 12, Color.gray, TextAlignmentOptions.Left);
            CreateText("StatusText", prefab.transform, "Status", 12, Color.gray, TextAlignmentOptions.Left);
            
            // Fire button
            CreateButton("FireButton", prefab.transform, "FIRE", new Color(0.8f, 0.3f, 0.3f), Color.white);
            
            hiredCardPrefab = prefab;
            prefab.SetActive(false);
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
