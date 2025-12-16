using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual review card UI component
    /// Shows: avatar icon, star rating, title, content
    /// Uses localization keys for title and content
    /// </summary>
    public class ReviewCardUI : MonoBehaviour
    {
        [Header("Avatar")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image avatarBackground;
        
        [Header("Bubble")]
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text contentText;
        
        [Header("Bubble Colors by Rating")]
        [SerializeField] private Color fiveStarColor = new Color(0.91f, 0.96f, 0.91f);   // #E8F5E9 green
        [SerializeField] private Color fourStarColor = new Color(0.95f, 0.97f, 0.91f);   // #F1F8E9 light green
        [SerializeField] private Color threeStarColor = new Color(1f, 0.97f, 0.88f);     // #FFF8E1 yellow
        [SerializeField] private Color twoStarColor = new Color(1f, 0.95f, 0.88f);       // #FFF3E0 orange
        [SerializeField] private Color oneStarColor = new Color(1f, 0.92f, 0.93f);       // #FFEBEE pink
        
        private CustomerReview review;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void OnEnable()
        {
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Setup(CustomerReview reviewData)
        {
            review = reviewData;
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (review == null) return;
            
            // Avatar
            if (avatarImage != null && ShopManager.Instance?.Templates != null)
            {
                var sprite = ShopManager.Instance.Templates.GetAvatarSprite(review.iconIndex);
                if (sprite != null)
                {
                    avatarImage.sprite = sprite;
                }
            }
            
            // Stars (★★★☆☆ style)
            if (starsText != null)
            {
                string stars = "";
                for (int i = 0; i < 5; i++)
                {
                    stars += i < review.stars ? "★" : "☆";
                }
                starsText.text = stars;
                starsText.color = GetStarColor(review.stars);
            }
            
            // Title (localized)
            if (titleText != null)
            {
                string localizedTitle = L?.Get(review.titleKey);
                // If localization returns the key itself (not found), use fallback
                if (string.IsNullOrEmpty(localizedTitle) || localizedTitle == $"[{review.titleKey}]")
                {
                    localizedTitle = $"{review.stars} Star Review";
                }
                titleText.text = localizedTitle;
            }
            
            // Content (localized)
            if (contentText != null)
            {
                string localizedContent = L?.Get(review.contentKey);
                // If localization returns the key itself (not found), use fallback
                if (string.IsNullOrEmpty(localizedContent) || localizedContent == $"[{review.contentKey}]")
                {
                    localizedContent = "Thank you for your feedback.";
                }
                contentText.text = localizedContent;
            }
            
            // Bubble background color
            if (bubbleBackground != null)
            {
                bubbleBackground.color = GetBubbleColor(review.stars);
            }
        }
        
        private Color GetBubbleColor(int stars)
        {
            return stars switch
            {
                5 => fiveStarColor,
                4 => fourStarColor,
                3 => threeStarColor,
                2 => twoStarColor,
                1 => oneStarColor,
                _ => threeStarColor
            };
        }
        
        private Color GetStarColor(int stars)
        {
            // Golden yellow for filled stars
            return new Color(1f, 0.8f, 0.2f);
        }
    }
}
