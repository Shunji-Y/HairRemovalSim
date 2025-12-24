using UnityEngine;
using System;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Configuration data for a single grade level
    /// </summary>
    [Serializable]
    public class GradeConfig
    {
        [Tooltip("Grade number (1-7)")]
        public int grade = 1;
        
        [Tooltip("Maximum customers per day at max attraction level (before facility boost)")]
        public int maxCustomers = 18;
        
        [Tooltip("Maximum attraction level cap for this grade")]
        public int attractionCap = 100;
        
        [Tooltip("Cumulative review score threshold to reach this star rating")]
        public int reviewThreshold = 0;
        
        [Tooltip("Rent cost every 3 days")]
        public int rent = 50;
        
        [Tooltip("Required stars to upgrade to this grade")]
        public int requiredStars = 1;
    }
    
    /// <summary>
    /// Database containing all grade configurations
    /// Single ScriptableObject for easy balance adjustments
    /// </summary>
    [CreateAssetMenu(fileName = "GradeConfigDatabase", menuName = "HairRemovalSim/Grade Config Database")]
    public class GradeConfigDatabase : ScriptableObject
    {
        [Header("Grade Configurations")]
        [Tooltip("Configuration for each grade level (index 0 = Grade 1/★1, etc.)")]
        [SerializeField] private GradeConfig[] gradeConfigs = new GradeConfig[]
        {
            new GradeConfig { grade = 1, maxCustomers = 18, attractionCap = 100, reviewThreshold = 0, rent = 50, requiredStars = 1 },
            new GradeConfig { grade = 2, maxCustomers = 36, attractionCap = 250, reviewThreshold = 1200, rent = 150, requiredStars = 2 },
            new GradeConfig { grade = 3, maxCustomers = 72, attractionCap = 400, reviewThreshold = 4500, rent = 400, requiredStars = 3 },
            new GradeConfig { grade = 4, maxCustomers = 108, attractionCap = 600, reviewThreshold = 22000, rent = 800, requiredStars = 4 },
            new GradeConfig { grade = 5, maxCustomers = 144, attractionCap = 800, reviewThreshold = 50000, rent = 1200, requiredStars = 5 },
            new GradeConfig { grade = 6, maxCustomers = 220, attractionCap = 1200, reviewThreshold = 140000, rent = 1800, requiredStars = 6 },
            new GradeConfig { grade = 7, maxCustomers = 220, attractionCap = 1200, reviewThreshold = 400000, rent = 2500, requiredStars = 7 },
        };
        
        /// <summary>
        /// Get configuration for a specific grade
        /// </summary>
        public GradeConfig GetConfig(int grade)
        {
            if (grade < 1 || grade > gradeConfigs.Length)
            {
                Debug.LogWarning($"[GradeConfigDatabase] Invalid grade {grade}, returning grade 1 defaults");
                return gradeConfigs[0];
            }
            return gradeConfigs[grade - 1];
        }
        
        /// <summary>
        /// Get max customers for a grade
        /// </summary>
        public int GetMaxCustomers(int grade)
        {
            return GetConfig(grade).maxCustomers;
        }
        
        /// <summary>
        /// Get attraction cap for a grade
        /// </summary>
        public int GetAttractionCap(int grade)
        {
            return GetConfig(grade).attractionCap;
        }
        
        /// <summary>
        /// Get rent cost for a grade
        /// </summary>
        public int GetRent(int grade)
        {
            return GetConfig(grade).rent;
        }
        
        /// <summary>
        /// Get required stars to upgrade to a grade
        /// </summary>
        public int GetRequiredStars(int targetGrade)
        {
            return GetConfig(targetGrade).requiredStars;
        }
        
        /// <summary>
        /// Get review threshold for a star rating (1-7)
        /// </summary>
        public int GetReviewThreshold(int starRating)
        {
            return GetConfig(starRating).reviewThreshold;
        }
        
        /// <summary>
        /// Calculate star rating from cumulative review score
        /// </summary>
        public int GetStarRatingFromReview(int reviewScore)
        {
            int stars = 1;
            for (int i = gradeConfigs.Length - 1; i >= 0; i--)
            {
                if (reviewScore >= gradeConfigs[i].reviewThreshold)
                {
                    stars = gradeConfigs[i].grade;
                    break;
                }
            }
            return Mathf.Clamp(stars, 1, 7);
        }
        
        /// <summary>
        /// Get progress towards next star (0-1)
        /// </summary>
        public float GetStarProgress(int reviewScore, int currentStars)
        {
            if (currentStars >= 7) return 1f;
            
            int currentThreshold = GetReviewThreshold(currentStars);
            int nextThreshold = GetReviewThreshold(currentStars + 1);
            int range = nextThreshold - currentThreshold;
            if (range <= 0) return 1f;
            
            return Mathf.Clamp01((float)(reviewScore - currentThreshold) / range);
        }
    }
}
