using UnityEngine;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Staff rank levels
    /// </summary>
    public enum StaffRank
    {
        College,        // 大学生
        NewGrad,        // 新卒社員
        MidCareer,      // 中堅社員
        Veteran,        // ベテラン
        Professional    // プロフェッショナル
    }
    
    /// <summary>
    /// Staff assignment locations
    /// </summary>
    public enum StaffAssignment
    {
        None,           // 未配置
        Reception,      // 受付
        Cashier,        // レジ
        Treatment,      // 施術（ベッド指定必要）
        Restock         // 在庫補充
    }
    
    /// <summary>
    /// ScriptableObject defining stats for each staff rank
    /// </summary>
    [CreateAssetMenu(fileName = "StaffRank_", menuName = "HairRemovalSim/Staff Rank Data")]
    public class StaffRankData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Staff rank level")]
        public StaffRank rank;
        
        [Tooltip("Display name (default)")]
        public string displayName;
        
        [Tooltip("Localization key for name")]
        public string nameKey;
        
        [Header("Performance Stats")]
        [Tooltip("Treatment time per body part (seconds)")]
        [Min(1f)]
        public float treatmentTimePerPart = 20f;
        
        [Tooltip("Item usage / upsell probability (0-100%)")]
        [Range(0f, 100f)]
        public float itemUsageProbability = 30f;
        
        [Tooltip("Number of item slots staff can carry")]
        [Min(1)]
        public int itemSlotCount = 5;
        
        [Header("Review Impact")]
        [Tooltip("Average star rating when this staff handles customer (1-5)")]
        [Range(1f, 5f)]
        public float averageReviewStars = 3f;
        
        [Tooltip("Review star variance (+/-)")]
        [Range(0f, 2f)]
        public float reviewStarVariance = 1f;
        
        [Header("Reception/Cashier")]
        [Tooltip("Time to process customer at reception/cashier (seconds)")]
        [Min(1f)]
        public float processingTime = 10f;
        
        [Tooltip("Review coefficient (0.9 = 90%, 1.1 = 110%)")]
        [Range(0.5f, 1.5f)]
        public float reviewCoefficient = 0.9f;
        
        [Header("Success Rates (by Customer Wealth)")]
        [Tooltip("Success rate for Poorest (極貧) customers")]
        [Range(0f, 1f)]
        public float successRatePoorest = 1f;
        
        [Tooltip("Success rate for Poor (貧乏) customers")]
        [Range(0f, 1f)]
        public float successRatePoor = 0.8f;
        
        [Tooltip("Success rate for Normal (普通) customers")]
        [Range(0f, 1f)]
        public float successRateNormal = 0.6f;
        
        [Tooltip("Success rate for Rich (富豪) customers")]
        [Range(0f, 1f)]
        public float successRateRich = 0.4f;
        
        [Tooltip("Success rate for Richest (大富豪) customers")]
        [Range(0f, 1f)]
        public float successRateRichest = 0.2f;
        
        [Header("Treatment Item Usage")]
        [Tooltip("Probability that staff will use treatment item (冷却ジェル等). 大学生:80%, 新卒:60%, 中堅:40%, ベテラン:20%, プロ:10%")]
        [Range(0f, 1f)]
        public float treatmentItemUsageRate = 0.8f;
        
        [Tooltip("Success rate penalty when item should be used but is missing (0.3 = 30% reduction)")]
        [Range(0f, 1f)]
        public float missingItemPenalty = 0.3f;
        
        [Header("Cost")]
        [Tooltip("Daily salary cost")]
        [Min(0)]
        public int dailySalary = 100;
        
        /// <summary>
        /// Get localized display name
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(nameKey))
            {
                string localized = Core.LocalizationManager.Instance?.Get(nameKey);
                if (!string.IsNullOrEmpty(localized) && localized != nameKey && !localized.StartsWith("["))
                    return localized;
            }
            return displayName;
        }
        
        /// <summary>
        /// Calculate treatment time for given body part count
        /// </summary>
        public float CalculateTreatmentTime(int partCount)
        {
            return treatmentTimePerPart * Mathf.Max(1, partCount);
        }
        
        /// <summary>
        /// Roll for item usage / upsell success
        /// </summary>
        public bool RollItemUsage()
        {
            return Random.value * 100f < itemUsageProbability;
        }
        
        /// <summary>
        /// Generate review star rating based on this rank
        /// </summary>
        public int GenerateReviewStars()
        {
            float stars = averageReviewStars + Random.Range(-reviewStarVariance, reviewStarVariance);
            return Mathf.Clamp(Mathf.RoundToInt(stars), 1, 5);
        }
        
        /// <summary>
        /// Get success rate based on customer wealth level
        /// </summary>
        public float GetSuccessRate(Customer.WealthLevel wealth)
        {
            switch (wealth)
            {
                case Customer.WealthLevel.Poorest: return successRatePoorest;
                case Customer.WealthLevel.Poor: return successRatePoor;
                case Customer.WealthLevel.Normal: return successRateNormal;
                case Customer.WealthLevel.Rich: return successRateRich;
                case Customer.WealthLevel.Richest: return successRateRichest;
                default: return successRateNormal;
            }
        }
        
        /// <summary>
        /// Roll for success based on customer wealth
        /// Returns true if successful, false if failed
        /// </summary>
        public bool RollSuccess(Customer.WealthLevel wealth)
        {
            float successRate = GetSuccessRate(wealth);
            bool success = Random.value < successRate;
            Debug.Log($"[StaffRankData] Success roll: {successRate * 100}% -> {(success ? "SUCCESS" : "FAILED")}");
            return success;
        }
    }
}
