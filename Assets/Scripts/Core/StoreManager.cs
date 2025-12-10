using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages store-wide statistics and data (non-financial).
    /// For money/finances, use EconomyManager.
    /// Singleton pattern for global access.
    /// </summary>
    public class StoreManager : Singleton<StoreManager>
    {
        [Header("Review System")]
        [SerializeField] private int reviewScore = 0; // 0=★1, 1000=★2, 2000=★3, 3000=★4, 4000=★5
        public const int REVIEW_PER_STAR = 1000;
        public const int MAX_STARS = 5;
        public const int PAIN_MAX_PENALTY = 25;
        
        [Header("Store Info")]
        [SerializeField] private int storeLevel = 1;
        
        [Header("Staff (Placeholder)")]
        [SerializeField] private int staffCount = 0;
        
        /// <summary>
        /// Current review score (0-4000+)
        /// </summary>
        public int ReviewScore => reviewScore;
        
        /// <summary>
        /// Current star rating (1-5)
        /// </summary>
        public int StarRating
        {
            get
            {
                int stars = 1 + (reviewScore / REVIEW_PER_STAR);
                return Mathf.Clamp(stars, 1, MAX_STARS);
            }
        }
        
        /// <summary>
        /// Progress to next star (0-1)
        /// </summary>
        public float StarProgress
        {
            get
            {
                if (StarRating >= MAX_STARS) return 1f;
                int currentStarBase = (StarRating - 1) * REVIEW_PER_STAR;
                return (float)(reviewScore - currentStarBase) / REVIEW_PER_STAR;
            }
        }
        
        /// <summary>
        /// Add review points from completed treatment
        /// </summary>
        public void AddReview(int baseReview, int painMaxCount)
        {
            int totalReview = baseReview - (painMaxCount * PAIN_MAX_PENALTY);
            totalReview = Mathf.Max(totalReview, -baseReview); // Can't lose more than base
            
            int previousStars = StarRating;
            reviewScore += totalReview;
            reviewScore = Mathf.Max(0, reviewScore); // Don't go below 0
            
            Debug.Log($"[StoreManager] Review added: {totalReview} (base: {baseReview}, painMax: {painMaxCount}x-{PAIN_MAX_PENALTY}). Total: {reviewScore} (★{StarRating})");
            
            // Check for star change
            if (StarRating != previousStars)
            {
                Debug.Log($"[StoreManager] Star rating changed: ★{previousStars} → ★{StarRating}");
                // TODO: Trigger star change event/notification
            }
        }
        
        /// <summary>
        /// Reset store to initial state (new game)
        /// </summary>
        public void ResetStore()
        {
            reviewScore = 0;
            storeLevel = 1;
            staffCount = 0;
            Debug.Log("[StoreManager] Store reset to initial state");
        }
    }
}
