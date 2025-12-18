using UnityEngine;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// ScriptableObject defining an individual staff profile that can be hired
    /// </summary>
    [CreateAssetMenu(fileName = "StaffProfile_", menuName = "HairRemovalSim/Staff Profile")]
    public class StaffProfileData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this staff profile")]
        public string staffId;
        
        [Tooltip("Staff name")]
        public string staffName;
        
        [Tooltip("Portrait image for UI display")]
        public Sprite portrait;
        
        [Header("Rank")]
        [Tooltip("Reference to rank data (defines stats)")]
        public StaffRankData rankData;
        
        [Header("Personality (Optional Modifiers)")]
        [Tooltip("Speed modifier (1.0 = normal, 1.2 = 20% faster)")]
        [Range(0.8f, 1.2f)]
        public float speedModifier = 1.0f;
        
        [Tooltip("Friendliness affects review bonus")]
        [Range(-1f, 1f)]
        public float friendlinessModifier = 0f;
        
        /// <summary>
        /// Get the staff rank
        /// </summary>
        public StaffRank Rank => rankData?.rank ?? StaffRank.College;
        
        /// <summary>
        /// Get daily salary
        /// </summary>
        public int DailySalary => rankData?.dailySalary ?? 100;
        
        /// <summary>
        /// Calculate actual treatment time with personality modifier
        /// </summary>
        public float GetTreatmentTime(int partCount)
        {
            float baseTime = rankData?.CalculateTreatmentTime(partCount) ?? (20f * partCount);
            return baseTime / speedModifier;
        }
        
        /// <summary>
        /// Generate review stars with friendliness modifier
        /// </summary>
        public int GenerateReviewStars()
        {
            int baseStars = rankData?.GenerateReviewStars() ?? 3;
            float modified = baseStars + friendlinessModifier;
            return Mathf.Clamp(Mathf.RoundToInt(modified), 1, 5);
        }
    }
}
