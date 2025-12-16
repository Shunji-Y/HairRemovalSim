using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Individual customer review data
    /// Stores localization keys for title and content
    /// </summary>
    [System.Serializable]
    public class CustomerReview
    {
        public int stars;           // 1-5
        public string titleKey;     // Localization key for title (e.g., "review.5star.title.1")
        public string contentKey;   // Localization key for content (e.g., "review.5star.content.1")
        public int iconIndex;       // Index into avatar array
        public int dayPosted;       // Game day when posted
        
        public CustomerReview(int stars, string titleKey, string contentKey, int iconIndex, int dayPosted)
        {
            this.stars = Mathf.Clamp(stars, 1, 5);
            this.titleKey = titleKey;
            this.contentKey = contentKey;
            this.iconIndex = iconIndex;
            this.dayPosted = dayPosted;
        }
    }
}
