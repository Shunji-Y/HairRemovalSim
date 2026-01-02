using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

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
        [SerializeField] private ReviewTemplates reviewTemplates;
        
        public const int REVIEW_PER_STAR = 1000;
        public const int MAX_STARS = 5;
        public const int PAIN_MAX_PENALTY = 25;
        
        // Review history
        private List<CustomerReview> reviewHistory = new List<CustomerReview>();
        
        [Header("Store Info")]
        [SerializeField] private int shopGrade = 1;
        
        [Header("Beds")]
        [Tooltip("All treatment beds in the shop")]
        [SerializeField] private List<Environment.BedController> beds = new List<Environment.BedController>();
        
        [Header("Shop Upgrade")]
        [Tooltip("Upgrade configs in scene for each grade level (2-6)")]
        [SerializeField] private List<ShopUpgradeConfig> upgradeConfigs = new List<ShopUpgradeConfig>();
        
        [Header("NavMesh")]
        [Tooltip("NavMeshSurface for runtime rebaking after expansion")]
        [SerializeField] private NavMeshSurface navMeshSurface;
        
        [Header("Staff (Placeholder)")]
        [SerializeField] private int staffCount = 0;
        
        [Header("Grade Configuration")]
        [Tooltip("Database containing all grade-specific settings")]
        [SerializeField] private GradeConfigDatabase gradeConfigDatabase;
        
        // Events
        public System.Action<CustomerReview> OnReviewAdded;
        public System.Action<int, int> OnStarRatingChanged; // old, new
        public System.Action<int> OnShopUpgraded; // new grade
        
        /// <summary>
        /// Current shop grade (1-6)
        /// </summary>
        public int ShopGrade => shopGrade;
        
        /// <summary>
        /// Grade configuration database
        /// </summary>
        public GradeConfigDatabase GradeConfig => gradeConfigDatabase;
        
        /// <summary>
        /// All beds in the shop
        /// </summary>
        public List<Environment.BedController> Beds => beds;
        
        /// <summary>
        /// Get total bed count
        /// </summary>
        public int BedCount => beds?.Count ?? 0;
        
        /// <summary>
        /// Get bed by index
        /// </summary>
        public Environment.BedController GetBed(int index)
        {
            if (beds == null || index < 0 || index >= beds.Count) return null;
            return beds[index];
        }
        
        /// <summary>
        /// Current review score (0-4000+)
        /// </summary>
        public int ReviewScore => reviewScore;
        
        /// <summary>
        /// Current star rating (1-7) based on cumulative review score
        /// </summary>
        public int StarRating
        {
            get
            {
                if (gradeConfigDatabase != null)
                    return gradeConfigDatabase.GetStarRatingFromReview(reviewScore);
                
                // Fallback to old logic
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
                if (gradeConfigDatabase != null)
                    return gradeConfigDatabase.GetStarProgress(reviewScore, StarRating);
                
                // Fallback to old logic
                if (StarRating >= MAX_STARS) return 1f;
                int currentStarBase = (StarRating - 1) * REVIEW_PER_STAR;
                return (float)(reviewScore - currentStarBase) / REVIEW_PER_STAR;
            }
        }
        
        /// <summary>
        /// Get progress details towards next star (for UI display)
        /// Returns: (currentProgress, rangeSize, nextStar)
        /// </summary>
        public (int current, int total, int nextStar) GetProgressToNextStar()
        {
            int currentStars = StarRating;
            if (currentStars >= 7)
                return (0, 0, 7);
            
            if (gradeConfigDatabase != null)
            {
                int currentThreshold = gradeConfigDatabase.GetReviewThreshold(currentStars);
                int nextThreshold = gradeConfigDatabase.GetReviewThreshold(currentStars + 1);
                int current = reviewScore - currentThreshold;
                int total = nextThreshold - currentThreshold;
                return (current, total, currentStars + 1);
            }
            
            // Fallback
            int currentBase = (currentStars - 1) * REVIEW_PER_STAR;
            return (reviewScore - currentBase, REVIEW_PER_STAR, currentStars + 1);
        }
        
        /// <summary>
        /// Get all reviews (newest first)
        /// </summary>
        public IReadOnlyList<CustomerReview> Reviews => reviewHistory;
        
        /// <summary>
        /// Get review templates asset
        /// </summary>
        public ReviewTemplates Templates => reviewTemplates;
        
        /// <summary>
        /// Get attraction rate bonus based on star rating
        /// ★5=+3, ★4=+1, ★3=0, ★2=-2, ★1=-5
        /// Returns 0 if no reviews yet (new shop)
        /// </summary>
        public float GetStarRatingBonus()
        {
            // No reviews = no bonus or penalty (treat as neutral ★3)
            if (reviewHistory == null || reviewHistory.Count == 0)
                return 0f;
            
            switch (StarRating)
            {
                case 5: return 3f;
                case 4: return 1f;
                case 3: return 0f;
                case 2: return -2f;
                case 1: return -5f;
                default: return 0f;
            }
        }
        
        /// <summary>
        /// Get average review score from last 3 days
        /// Returns value in range approximately -50 to 50
        /// </summary>
        public float GetAverageReviewScoreLast3Days()
        {
            if (reviewHistory == null || reviewHistory.Count == 0)
                return 0f;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            int startDay = Mathf.Max(1, currentDay - 2); // Last 3 days including today
            
            float totalScore = 0f;
            int count = 0;
            
            foreach (var review in reviewHistory)
            {
                if (review.dayPosted >= startDay && review.dayPosted <= currentDay)
                {
                    // Convert star rating to score: 1=-25, 2=-12, 3=0, 4=12, 5=25
                    float score = (review.stars - 3f) * 12.5f;
                    totalScore += score;
                    count++;
                }
            }
            
            return count > 0 ? totalScore / count : 0f;
        }
        
        /// <summary>
        /// Get VIP coefficient from past 3 days review average
        /// Normalized to 0-100 scale
        /// Score -50 -> VIP 0, Score 0 -> VIP 50, Score 50+ -> VIP 100
        /// </summary>
        public float GetVipCoefficientFromReviews()
        {
            float avgScore = GetAverageReviewScoreLast3Days();
            
            // Normalize: -50~50 to 0~100
            // -50 -> 0, 0 -> 50, 50 -> 100
            float vip = (avgScore + 50f) / 100f * 100f;
            return Mathf.Clamp(vip, 0f, 100f);
        }
        
        /// <summary>
        /// Add review from customer satisfaction (1-5 stars)
        /// Note: This only adds the review to history. Score is updated separately by AddReview().
        /// </summary>
        public void AddCustomerReview(int stars)
        {
            stars = Mathf.Clamp(stars, 1, 5);
            
            // Get localization key prefix for this star rating
            string keyPrefix = $"review.{stars}star.default";
            int iconIndex = 0;
            
            if (reviewTemplates != null)
            {
                keyPrefix = reviewTemplates.GetRandomKeyPrefix(stars);
                iconIndex = reviewTemplates.GetRandomAvatarIndex();
            }
            
            // Store keys: titleKey = prefix + ".title", contentKey = prefix + ".content"
            string titleKey = keyPrefix + ".title";
            string contentKey = keyPrefix + ".content";
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            var review = new CustomerReview(stars, titleKey, contentKey, iconIndex, currentDay);
            
            // Add to history (newest first)
            reviewHistory.Insert(0, review);
            
            Debug.Log($"[ShopManager] Customer review added: ★{stars} (keys: {titleKey}, {contentKey})");
            
            OnReviewAdded?.Invoke(review);
        }
        
        /// <summary>
        /// Legacy: Add review points from completed treatment
        /// </summary>
        public void AddReview(int baseReview, int painMaxCount)
        {
            int totalReview = baseReview - (painMaxCount * PAIN_MAX_PENALTY);
            
            // Clamp to prevent extreme values, but allow negative reviews
            // If base is positive, don't go below -baseReview (prevent double penalty)
            // If base is already negative, allow it to go as low as needed
            if (baseReview > 0)
            {
                totalReview = Mathf.Max(totalReview, -baseReview);
            }
            
            int previousStars = StarRating;
            reviewScore += totalReview;
            reviewScore = Mathf.Max(0, reviewScore);
            
            Debug.Log($"[ShopManager] Review added: {totalReview} (base: {baseReview}, painMax: {painMaxCount}x-{PAIN_MAX_PENALTY}). Total: {reviewScore} (★{StarRating})");
            
            if (StarRating != previousStars)
            {
                Debug.Log($"[ShopManager] Star rating changed: ★{previousStars} → ★{StarRating}");
                OnStarRatingChanged?.Invoke(previousStars, StarRating);
            }
        }
        
        /// <summary>
        /// Add or subtract review score directly (for debug/cheat)
        /// </summary>
        public void AddReviewScore(int amount)
        {
            int previousStars = StarRating;
            reviewScore += amount;
            reviewScore = Mathf.Max(0, reviewScore);
            
            Debug.Log($"[ShopManager] Review score changed: {amount:+#;-#;0} → Total: {reviewScore} (★{StarRating})");
            
            if (StarRating != previousStars)
            {
                Debug.Log($"[ShopManager] Star rating changed: ★{previousStars} → ★{StarRating}");
                OnStarRatingChanged?.Invoke(previousStars, StarRating);
            }
        }
        
        #region Placement Item Effects
        
        private float _reviewPercentBoost = 0f;
        
        /// <summary>
        /// Get review percent boost from placement items (0.05 = 5%)
        /// </summary>
        public float ReviewPercentBoost => _reviewPercentBoost;
        
        /// <summary>
        /// Add/remove review percent boost from placement items
        /// </summary>
        public void AddReviewPercentBoost(float percent)
        {
            _reviewPercentBoost += percent;
            Debug.Log($"[ShopManager] Review percent boost changed: {percent:+#.##%;-#.##%;0%} → Total: {_reviewPercentBoost:P0}");
        }
        
        #endregion
        
        /// <summary>
        /// Reset store to initial state (new game)
        /// </summary>
        public void ResetStore()
        {
            reviewScore = 0;
            reviewHistory.Clear();
            shopGrade = 1;
            staffCount = 0;
            _reviewPercentBoost = 0f;
            Debug.Log("[ShopManager] Shop reset to initial state");
        }
        
        #region Treatment Shelf System
        

        
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
        /// Get pending shelf purchase count
        /// </summary>
        public int PendingShelfPurchases => pendingShelfPurchases;
        
        #endregion
        
        #region Shop Upgrade System
        
        /// <summary>
        /// Auto-find upgrade configs in scene if not assigned
        /// </summary>
        private void FindUpgradeConfigs()
        {
            if (upgradeConfigs == null || upgradeConfigs.Count == 0)
            {
                upgradeConfigs = new List<ShopUpgradeConfig>(FindObjectsOfType<ShopUpgradeConfig>());
                upgradeConfigs.Sort((a, b) => a.TargetGrade.CompareTo(b.TargetGrade));
                Debug.Log($"[ShopManager] Auto-found {upgradeConfigs.Count} upgrade configs in scene");
            }
        }
        
        /// <summary>
        /// Get upgrade config for target grade
        /// </summary>
        public ShopUpgradeConfig GetUpgradeConfig(int targetGrade)
        {
            FindUpgradeConfigs();
            if (upgradeConfigs == null) return null;
            return upgradeConfigs.Find(c => c != null && c.TargetGrade == targetGrade);
        }
        
        /// <summary>
        /// Get upgrade data for target grade (from config)
        /// </summary>
        public ShopUpgradeData GetUpgradeData(int targetGrade)
        {
            var config = GetUpgradeConfig(targetGrade);
            return config?.upgradeData;
        }
        
        /// <summary>
        /// Get next available upgrade config (null if at max grade)
        /// </summary>
        public ShopUpgradeConfig GetNextUpgradeConfig()
        {
            return GetUpgradeConfig(shopGrade + 1);
        }
        
        /// <summary>
        /// Get next available upgrade data (null if at max grade)
        /// </summary>
        public ShopUpgradeData GetNextUpgradeData()
        {
            return GetUpgradeData(shopGrade + 1);
        }
        
        /// <summary>
        /// Get maximum customers for current grade (base value before facility boost)
        /// </summary>
        public int GetCurrentMaxCustomers()
        {
            if (gradeConfigDatabase != null)
                return gradeConfigDatabase.GetMaxCustomers(shopGrade);
            return 18; // Default fallback
        }
        
        /// <summary>
        /// Get attraction cap for current grade
        /// </summary>
        public int GetCurrentAttractionCap()
        {
            if (gradeConfigDatabase != null)
                return gradeConfigDatabase.GetAttractionCap(shopGrade);
            return 100; // Default fallback
        }
        
        /// <summary>
        /// Get max simultaneous customers for current grade
        /// </summary>
        public int GetCurrentMaxSimultaneous()
        {
            if (gradeConfigDatabase != null)
                return gradeConfigDatabase.GetMaxSimultaneous(shopGrade);
            return 3; // Default fallback
        }
        
        /// <summary>
        /// Get rent cost for current grade
        /// </summary>
        public int GetCurrentRent()
        {
            if (gradeConfigDatabase != null)
                return gradeConfigDatabase.GetRent(shopGrade);
            return 50; // Default fallback
        }
        
        /// <summary>
        /// Check if can afford next upgrade
        /// </summary>
        public bool CanAffordNextUpgrade()
        {
            var config = GetNextUpgradeConfig();
            if (config == null) return false;
            return EconomyManager.Instance != null && EconomyManager.Instance.CurrentMoney >= config.UpgradeCost;
        }
        
        /// <summary>
        /// Check if player has required stars for next upgrade
        /// </summary>
        public bool HasRequiredStarsForUpgrade()
        {
            int targetGrade = shopGrade + 1;
            if (gradeConfigDatabase == null) return true; // No database, skip check
            
            int requiredStars = gradeConfigDatabase.GetRequiredStars(targetGrade);
            return StarRating >= requiredStars;
        }
        
        /// <summary>
        /// Get required stars for next upgrade
        /// </summary>
        public int GetRequiredStarsForNextUpgrade()
        {
            int targetGrade = shopGrade + 1;
            if (gradeConfigDatabase == null) return targetGrade; // Default: same as grade
            return gradeConfigDatabase.GetRequiredStars(targetGrade);
        }
        
        /// <summary>
        /// Check if can upgrade (both cost and stars)
        /// </summary>
        public bool CanUpgradeShop()
        {
            if (IsMaxGrade) return false;
            return CanAffordNextUpgrade() && HasRequiredStarsForUpgrade();
        }
        
        /// <summary>
        /// Check if at max grade
        /// </summary>
        public bool IsMaxGrade => shopGrade >= 6;
        
        /// <summary>
        /// Perform shop upgrade (call after payment confirmation and during whiteout)
        /// </summary>
        public bool UpgradeShop()
        {
            if (IsMaxGrade)
            {
                Debug.LogWarning("[ShopManager] Already at max grade!");
                return false;
            }
            
            var config = GetNextUpgradeConfig();
            if (config == null)
            {
                Debug.LogError("[ShopManager] No upgrade config for next grade!");
                return false;
            }
            
            // Check star requirement
            if (!HasRequiredStarsForUpgrade())
            {
                int required = GetRequiredStarsForNextUpgrade();
                Debug.LogWarning($"[ShopManager] Not enough stars for upgrade! Need ★{required}, have ★{StarRating}");
                return false;
            }
            
            // Pay cost
            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendMoney(config.UpgradeCost))
                {
                    Debug.LogWarning("[ShopManager] Not enough money for upgrade!");
                    return false;
                }
            }
            
            int oldGrade = shopGrade;
            shopGrade++;
            
            // 1. Activate expansion room
            if (config.expansionRoom != null)
            {
                config.expansionRoom.SetActive(true);
                Debug.Log($"[ShopManager] Activated expansion room: {config.expansionRoom.name}");
            }
            
            // 2. Hide walls
            if (config.wallsToHide != null)
            {
                foreach (var wall in config.wallsToHide)
                {
                    if (wall != null)
                    {
                        wall.SetActive(false);
                        Debug.Log($"[ShopManager] Hidden wall: {wall.name}");
                    }
                }
            }
            
            // 3. Activate and add new beds
            if (config.newBeds != null)
            {
                foreach (var bed in config.newBeds)
                {
                    if (bed != null)
                    {
                        bed.transform.parent.gameObject.SetActive(true);
                        AddBed(bed);
                        Debug.Log($"[ShopManager] Activated bed: {bed.name}");
                    }
                }
            }
            
            // 4. Legacy: Activate shop model if set
            if (config.shopModel != null)
            {
                ActivateShopModel(shopGrade);
            }
            
            // 5. Move CashRegister to new position if specified
            if (config.cashierPosition != null)
            {
                var cashRegister = UI.CashRegister.Instance;
                if (cashRegister != null)
                {
                    cashRegister.transform.position = config.cashierPosition.position;
                    cashRegister.transform.rotation = config.cashierPosition.rotation;
                    Debug.Log($"[ShopManager] Moved CashRegister to {config.cashierPosition.name}");
                }
            }
            
            // Note: NavMesh is handled via NavMeshObstacle on walls (Carve enabled)
            // No runtime NavMesh rebuild needed
            
            // Update CustomerSpawner maxCustomers
            var customerSpawner = Customer.CustomerSpawner.FindObjectOfType<Customer.CustomerSpawner>();
            if (customerSpawner != null)
            {
                int newMax = GetCurrentMaxSimultaneous();
                customerSpawner.maxCustomers = newMax;
                Debug.Log($"[ShopManager] Updated CustomerSpawner.maxCustomers to {newMax}");
            }
            
            Debug.Log($"[ShopManager] Shop upgraded: Grade {oldGrade} → Grade {shopGrade}");
            OnShopUpgraded?.Invoke(shopGrade);
            
            return true;
        }
        
        /// <summary>
        /// Add a bed to the shop's bed list and sync with other managers
        /// </summary>
        public void AddBed(Environment.BedController bed)
        {
            if (bed == null || beds.Contains(bed)) return;
            
            beds.Add(bed);
            
            // Sync with ReceptionManager
            if (UI.ReceptionManager.Instance != null)
            {
                UI.ReceptionManager.Instance.RefreshBedReferences();
            }
            
            // Sync with StaffManager
            if (Staff.StaffManager.Instance != null)
            {
                Staff.StaffManager.Instance.RefreshBedAssignments();
            }
            
            // Sync with WarehousePanel (refresh shelf carts)
            if (UI.WarehousePanel.Instance != null)
            {
                UI.WarehousePanel.Instance.RefreshShelfCarts();
            }
            
            Debug.Log($"[ShopManager] Bed added: {bed.name}. Total beds: {beds.Count}");
        }
        
        /// <summary>
        /// Rebuild NavMesh after expansion (delayed by 1 frame for mesh initialization)
        /// </summary>
        public void RebuildNavMesh()
        {
            if (navMeshSurface != null)
            {
                StartCoroutine(RebuildNavMeshDelayed());
            }
            else
            {
                Debug.LogWarning("[ShopManager] NavMeshSurface not assigned, cannot rebuild NavMesh");
            }
        }
        
        private System.Collections.IEnumerator RebuildNavMeshDelayed()
        {
            // Wait for end of frame to ensure all objects are properly initialized
            yield return new WaitForEndOfFrame();
            
            // Additional frame for physics/colliders to update
            yield return null;
            
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[ShopManager] NavMesh rebuilt");
            }
        }
        
        /// <summary>
        /// Activate shop model for specified grade
        /// </summary>
        private void ActivateShopModel(int grade)
        {
            foreach (var config in upgradeConfigs)
            {
                if (config == null || config.shopModel == null) continue;
                config.shopModel.SetActive(config.TargetGrade == grade);
            }
        }
        
        #endregion
    }
}
