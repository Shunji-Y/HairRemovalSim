using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Configuration for 30-star level system.
    /// Star levels are determined by cumulative review score.
    /// Independent from ShopGrade (which requires manual upgrade).
    /// </summary>
    [CreateAssetMenu(fileName = "StarLevelConfig", menuName = "HairRemovalSim/Star Level Config")]
    public class StarLevelConfig : ScriptableObject
    {
        public const int MAX_STAR_LEVEL = 30;
        
        [Header("Review Thresholds")]
        [Tooltip("Cumulative review score required for each star level (1-30)")]
        [SerializeField] private List<int> reviewThresholds = new List<int>
        {
            0,       // ★1
            180,     // ★2
            430,     // ★3
            770,     // ★4
            1200,    // ★5
            1800,    // ★6
            2500,    // ★7
            3400,    // ★8
            4500,    // ★9
            5600,    // ★10
            7100,    // ★11
            9000,    // ★12
            11200,   // ★13
            13800,   // ★14
            16400,   // ★15
            20000,   // ★16
            24500,   // ★17
            29800,   // ★18
            35700,   // ★19
            42700,   // ★20
            52500,   // ★21
            63000,   // ★22
            75000,   // ★23
            90000,   // ★24
            107000,  // ★25
            129000,  // ★26
            154000,  // ★27
            182000,  // ★28
            214000,  // ★29
            250000,  // ★30
        };
        
        /// <summary>
        /// Get star level from cumulative review score
        /// </summary>
        public int GetStarLevelFromReview(int reviewScore)
        {
            for (int i = reviewThresholds.Count - 1; i >= 0; i--)
            {
                if (reviewScore >= reviewThresholds[i])
                {
                    return i + 1; // Star levels are 1-indexed
                }
            }
            return 1;
        }
        
        /// <summary>
        /// Get review threshold for a specific star level
        /// </summary>
        public int GetThresholdForStarLevel(int starLevel)
        {
            int index = Mathf.Clamp(starLevel - 1, 0, reviewThresholds.Count - 1);
            return reviewThresholds[index];
        }
        
        /// <summary>
        /// Get progress towards next star (0-1)
        /// </summary>
        public float GetProgressToNextStar(int reviewScore, int currentStarLevel)
        {
            if (currentStarLevel >= MAX_STAR_LEVEL) return 1f;
            
            int currentThreshold = GetThresholdForStarLevel(currentStarLevel);
            int nextThreshold = GetThresholdForStarLevel(currentStarLevel + 1);
            int range = nextThreshold - currentThreshold;
            
            if (range <= 0) return 1f;
            
            float progress = (float)(reviewScore - currentThreshold) / range;
            return Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// Get detailed progress info for UI display
        /// </summary>
        public (int current, int total, int nextStar) GetProgressDetails(int reviewScore, int currentStarLevel)
        {
            if (currentStarLevel >= MAX_STAR_LEVEL)
            {
                return (0, 0, MAX_STAR_LEVEL);
            }
            
            int currentThreshold = GetThresholdForStarLevel(currentStarLevel);
            int nextThreshold = GetThresholdForStarLevel(currentStarLevel + 1);
            int current = reviewScore - currentThreshold;
            int total = nextThreshold - currentThreshold;
            
            return (current, total, currentStarLevel + 1);
        }
    }
}
