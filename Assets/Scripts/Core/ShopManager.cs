using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages shop-wide statistics and data (non-financial).
    /// For money/finances, use EconomyManager.
    /// For online store UI, see Store.StorePanel.
    /// Singleton pattern for global access.
    /// </summary>
    public class ShopManager : Singleton<ShopManager>
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
            
            Debug.Log($"[ShopManager] Review added: {totalReview} (base: {baseReview}, painMax: {painMaxCount}x-{PAIN_MAX_PENALTY}). Total: {reviewScore} (★{StarRating})");
            
            // Check for star change
            if (StarRating != previousStars)
            {
                Debug.Log($"[ShopManager] Star rating changed: ★{previousStars} → ★{StarRating}");
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
            Debug.Log("[ShopManager] Shop reset to initial state");
        }
        
        #region Treatment Shelf System
        
        [Header("Treatment Shelves")]
        [SerializeField] private Environment.TreatmentShelf shelfPrefab;
        
        // Pending shelf installations (applied at day start)
        private int pendingShelfPurchases = 0;
        
        /// <summary>
        /// Check if any bed has available shelf slot
        /// </summary>
        public bool CanPurchaseShelf()
        {
            var beds = FindObjectsOfType<Environment.BedController>();
            int availableSlots = 0;
            
            foreach (var bed in beds)
            {
                for (int i = 0; i < bed.shelfSlots.Length; i++)
                {
                    if (bed.shelfSlots[i] != null && bed.installedShelves[i] == null)
                    {
                        availableSlots++;
                    }
                }
            }
            
            return availableSlots > pendingShelfPurchases;
        }
        
        /// <summary>
        /// Purchase a shelf (will be installed next day)
        /// </summary>
        public bool PurchaseShelf(int cost)
        {
            if (!CanPurchaseShelf())
            {
                Debug.LogWarning("[ShopManager] No available shelf slots");
                return false;
            }
            
            if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendMoney(cost))
            {
                Debug.LogWarning("[ShopManager] Not enough money for shelf");
                return false;
            }
            
            pendingShelfPurchases++;
            Debug.Log($"[ShopManager] Shelf purchased! Will be installed tomorrow. ({pendingShelfPurchases} pending)");
            return true;
        }
        
        /// <summary>
        /// Install pending shelves (call at day start)
        /// </summary>
        public void InstallPendingShelves()
        {
            if (pendingShelfPurchases <= 0 || shelfPrefab == null) return;
            
            var beds = FindObjectsOfType<Environment.BedController>();
            
            foreach (var bed in beds)
            {
                if (pendingShelfPurchases <= 0) break;
                
                if (bed.HasAvailableShelfSlot())
                {
                    if (bed.InstallShelf(shelfPrefab))
                    {
                        pendingShelfPurchases--;
                    }
                }
            }
            
            if (pendingShelfPurchases > 0)
            {
                Debug.LogWarning($"[ShopManager] Could not install {pendingShelfPurchases} shelf(s) - no slots available");
                pendingShelfPurchases = 0;
            }
        }
        
        /// <summary>
        /// Get pending shelf purchase count
        /// </summary>
        public int PendingShelfPurchases => pendingShelfPurchases;
        
        #endregion
    }
}
