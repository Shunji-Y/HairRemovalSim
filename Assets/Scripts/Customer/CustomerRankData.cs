using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Customer tier (5 tiers)
    /// </summary>
    public enum CustomerTier
    {
        Poorest = 0,
        Poor = 1,
        Normal = 2,
        Rich = 3,
        Richest = 4
    }
    
    /// <summary>
    /// Data for a single customer rank (30 ranks total: 5 tiers × 6 sublevels)
    /// </summary>
    [CreateAssetMenu(fileName = "NewCustomerRank", menuName = "HairRemovalSim/Customer Rank Data")]
    public class CustomerRankData : ScriptableObject
    {
        [Header("Rank Identity")]
        [Tooltip("Display name (e.g., Poorest1, Rich3)")]
        public string rankName;
        
        [Tooltip("Tier of this customer")]
        public CustomerTier tier;
        
        [Tooltip("Sublevel within tier (1-6)")]
        [Range(1, 6)]
        public int subLevel = 1;
        
        [Header("Unlock Requirements")]
        [Tooltip("Required star level to unlock (1-30)")]
        [Range(1, 30)]
        public int requiredStarLevel = 1;
        
        [Tooltip("Required grade to access this tier")]
        [Range(1, 7)]
        public int requiredGrade = 1;
        
        [Header("Pricing")]
        [Tooltip("Base plan price")]
        public int planPrice = 30;
        
        [Tooltip("Additional budget range (min)")]
        public int budgetMin = 15;
        
        [Tooltip("Additional budget range (max)")]
        public int budgetMax = 23;
        
        [Header("Staff Requirement")]
        [Tooltip("Required staff rank (null = any staff can serve)")]
        public Staff.StaffRank? requiredStaffRank = null;
        
        [Header("Treatment Plan")]
        [Tooltip("Body parts included in this rank's plan")]
        public List<BodyPart> treatmentParts = new List<BodyPart>();
        
        [Tooltip("Plan description key for localization")]
        public string planDescriptionKey;
        
        /// <summary>
        /// Check if this rank is unlocked for given star level and grade
        /// </summary>
        public bool IsUnlocked(int starLevel, int grade)
        {
            return starLevel >= requiredStarLevel && grade >= requiredGrade;
        }
        
        /// <summary>
        /// Get random additional budget within range
        /// </summary>
        public int GetRandomBudget()
        {
            return Random.Range(budgetMin, budgetMax + 1);
        }
        
        /// <summary>
        /// Get total payment (plan price + random budget)
        /// </summary>
        public int GetTotalPayment()
        {
            return planPrice + GetRandomBudget();
        }
        
        /// <summary>
        /// Get tier-grade mapping
        /// </summary>
        public static int GetRequiredGradeForTier(CustomerTier tier)
        {
            switch (tier)
            {
                case CustomerTier.Poorest: return 1;
                case CustomerTier.Poor: return 2;
                case CustomerTier.Normal: return 4;
                case CustomerTier.Rich: return 5;
                case CustomerTier.Richest: return 6;
                default: return 1;
            }
        }
    }
}
