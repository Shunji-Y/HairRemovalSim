using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Review panel showing scrollable list of customer reviews
    /// </summary>
    public class ReviewPanel : MonoBehaviour
    {
        public static ReviewPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text starsDisplayText;
        [SerializeField] private Slider starProgressSlider;
        [SerializeField] private TMP_Text progressText;
        
        [Header("Review List")]
        [SerializeField] private Transform reviewsContainer;
        [SerializeField] private GameObject reviewCardPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("Empty State")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private TMP_Text emptyStateText;
        
        private List<GameObject> reviewCards = new List<GameObject>();
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void OnEnable()
        {
            RefreshDisplay();
            
            // Subscribe to events
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnReviewAdded += OnReviewAdded;
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnReviewAdded -= OnReviewAdded;
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
            
            // Complete tut_review_check when panel is closed
            Core.TutorialManager.Instance?.CompleteByAction("review_panel_closed");
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshDisplay();
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
        
        private void OnReviewAdded(CustomerReview review)
        {
            RefreshDisplay();
        }
        
        public void RefreshDisplay()
        {
            ClearReviewCards();
            
            if (ShopManager.Instance == null) return;
            
            // Update header
            UpdateHeader();
            
            // Get reviews
            var reviews = ShopManager.Instance.Reviews;
            
            // Show empty state or reviews
            if (reviews == null || reviews.Count == 0)
            {
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(true);
                if (emptyStateText != null)
                    emptyStateText.text = L?.Get("review.empty") ?? "No reviews yet.";
            }
            else
            {
                if (emptyStateObject != null)
                    emptyStateObject.SetActive(false);
                
                // Create review cards
                foreach (var review in reviews)
                {
                    CreateReviewCard(review);
                }
            }
        }
        
        private void UpdateHeader()
        {
            // Title
            if (titleText != null)
            {
                titleText.text = L?.Get("review.title") ?? "SALON REVIEWS";
            }
            
            // Stars display - 30 star system with 3 tiers (10 visible stars: 5x2)
            // Yellow (★1-10), Orange (★11-20), Purple (★21-30)
            if (starsDisplayText != null)
            {
                int currentStars = ShopManager.Instance?.StarRating ?? 1;
                
                // Determine tier and display stars
                int tier = (currentStars - 1) / 10; // 0=yellow, 1=orange, 2=purple
                int starsInTier = ((currentStars - 1) % 10) + 1; // 1-10 within tier
                
                string row1 = "";
                string row2 = "";
                
                // Build 5x2 star display
                for (int i = 0; i < 5; i++)
                {
                    row1 += (i < starsInTier) ? "★" : "☆";
                }
                for (int i = 5; i < 10; i++)
                {
                    row2 += (i < starsInTier) ? "★" : "☆";
                }
                
                starsDisplayText.text = $"{row1}\n{row2}";
                
                // Color by tier
                switch (tier)
                {
                    case 0: // Yellow (★1-10)
                        starsDisplayText.color = new Color(1f, 0.85f, 0.2f); // Gold/Yellow
                        break;
                    case 1: // Orange (★11-20)
                        starsDisplayText.color = new Color(1f, 0.5f, 0.1f); // Orange
                        break;
                    case 2: // Purple (★21-30)
                    default:
                        starsDisplayText.color = new Color(0.7f, 0.3f, 0.9f); // Purple
                        break;
                }
            }
            
            // Progress to next star
            if (starProgressSlider != null)
            {
                int currentStars = ShopManager.Instance?.StarRating ?? 1;
                float progress = ShopManager.Instance?.StarProgress ?? 0f;
                
                if (currentStars >= 30)
                {
                    starProgressSlider.value = 1f;
                    starProgressSlider.gameObject.SetActive(false);
                }
                else
                {
                    starProgressSlider.value = progress;
                    starProgressSlider.gameObject.SetActive(true);
                }
            }
            
            // Progress text (e.g., "500/1200 → ★2")
            if (progressText != null)
            {
                int currentStars = ShopManager.Instance?.StarRating ?? 1;
                
                if (currentStars >= 30)
                {
                    progressText.text = L?.Get("review.max_stars") ?? "Max Rating!";
                }
                else
                {
                    var progress = ShopManager.Instance?.GetProgressToNextStar() ?? (0, 1000, 2);
                    progressText.text = $"{progress.current}/{progress.total} → ★{progress.nextStar}";
                }
            }
        }
        
        private void CreateReviewCard(CustomerReview review)
        {
            if (reviewCardPrefab == null || reviewsContainer == null) return;
            
            var cardObj = Instantiate(reviewCardPrefab, reviewsContainer);
            var cardUI = cardObj.GetComponent<ReviewCardUI>();
            
            if (cardUI != null)
            {
                cardUI.Setup(review);
            }
            
            reviewCards.Add(cardObj);
        }
        
        private void ClearReviewCards()
        {
            foreach (var card in reviewCards)
            {
                if (card != null)
                    Destroy(card);
            }
            reviewCards.Clear();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Generate UI Structure")]
        private void GenerateUIStructure()
        {
            // Colors
            Color headerBg = new Color(0.2f, 0.25f, 0.35f);
            Color panelBg = new Color(0.95f, 0.95f, 0.97f);
            Color gold = new Color(1f, 0.8f, 0.2f);
            
            // Create main panel
            var panelObj = new GameObject("ReviewPanel");
            panelObj.transform.SetParent(transform, false);
            
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = panelBg;
            
            panel = panelObj;
            
            // Header
            var header = CreateUIElement("Header", panelObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(0, 80));
            var headerImg = header.AddComponent<Image>();
            headerImg.color = headerBg;
            
            // Title
            var title = CreateText("Title", header.transform, "SALON REVIEWS", 28, Color.white, TextAlignmentOptions.Center);
            title.anchoredPosition = new Vector2(0, 10);
            titleText = title.GetComponent<TMP_Text>();
            
            // Stars
            var stars = CreateText("Stars", header.transform, "★★★★☆", 24, gold, TextAlignmentOptions.Center);
            stars.anchoredPosition = new Vector2(0, -20);
            starsDisplayText = stars.GetComponent<TMP_Text>();
            
            // Scroll View
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(panelObj.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.offsetMin = new Vector2(20, 20);
            scrollViewRect.offsetMax = new Vector2(-20, -90);
            
            var scrollRectComp = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            scrollView.AddComponent<Mask>().showMaskGraphic = false;
            
            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRectComp.content = contentRect;
            scrollRectComp.vertical = true;
            scrollRectComp.horizontal = false;
            
            scrollRect = scrollRectComp;
            reviewsContainer = content.transform;
            
            // Empty state
            var empty = CreateText("EmptyState", content.transform, "No reviews yet.", 18, Color.gray, TextAlignmentOptions.Center);
            emptyStateObject = empty.gameObject;
            emptyStateText = empty.GetComponent<TMP_Text>();
            
            // Create sample review card prefab
            CreateSampleReviewCard(content.transform);
            
            Debug.Log("[ReviewPanel] UI structure generated!");
        }
        
        private void CreateSampleReviewCard(Transform parent)
        {
            var card = new GameObject("ReviewCard_Sample");
            card.transform.SetParent(parent, false);
            
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0, 120);
            
            var hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(5, 5, 5, 5);
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            // Avatar container
            var avatarContainer = new GameObject("AvatarContainer");
            avatarContainer.transform.SetParent(card.transform, false);
            var avatarContainerRect = avatarContainer.AddComponent<RectTransform>();
            avatarContainerRect.sizeDelta = new Vector2(60, 60);
            var avatarLE = avatarContainer.AddComponent<LayoutElement>();
            avatarLE.minWidth = 60;
            avatarLE.preferredWidth = 60;
            
            // Avatar background (circle)
            var avatarBg = new GameObject("AvatarBg");
            avatarBg.transform.SetParent(avatarContainer.transform, false);
            var avatarBgRect = avatarBg.AddComponent<RectTransform>();
            avatarBgRect.anchorMin = Vector2.zero;
            avatarBgRect.anchorMax = Vector2.one;
            avatarBgRect.sizeDelta = Vector2.zero;
            var avatarBgImg = avatarBg.AddComponent<Image>();
            avatarBgImg.color = new Color(0.9f, 0.9f, 0.9f);
            
            // Avatar image
            var avatar = new GameObject("Avatar");
            avatar.transform.SetParent(avatarContainer.transform, false);
            var avatarRect = avatar.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.1f, 0.1f);
            avatarRect.anchorMax = new Vector2(0.9f, 0.9f);
            avatarRect.sizeDelta = Vector2.zero;
            avatar.AddComponent<Image>().color = Color.white;
            
            // Bubble
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(card.transform, false);
            var bubbleRect = bubble.AddComponent<RectTransform>();
            var bubbleLE = bubble.AddComponent<LayoutElement>();
            bubbleLE.flexibleWidth = 1;
            
            var bubbleImg = bubble.AddComponent<Image>();
            bubbleImg.color = new Color(0.91f, 0.96f, 0.91f);
            
            var bubbleVlg = bubble.AddComponent<VerticalLayoutGroup>();
            bubbleVlg.padding = new RectOffset(15, 15, 10, 10);
            bubbleVlg.spacing = 5;
            bubbleVlg.childForceExpandWidth = true;
            bubbleVlg.childForceExpandHeight = false;
            
            // Stars + Title row
            var starsTitle = CreateText("StarsTitle", bubble.transform, "★★★★★ So Smooth! No Pain!", 16, Color.black, TextAlignmentOptions.Left);
            starsTitle.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            
            // Content
            var contentText = CreateText("Content", bubble.transform, "Best decision ever. The staff was super gentle and the new laser machine is magic! Highly recommend the full leg course.", 14, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.Left);
            
            Debug.Log("[ReviewPanel] Sample review card created. Save as prefab!");
        }
        
        private GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            return obj;
        }
        
        private RectTransform CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(0, fontSize + 10);
            
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            
            return rect;
        }
#endif
    }
}
