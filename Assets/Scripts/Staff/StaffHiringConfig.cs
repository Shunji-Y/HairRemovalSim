using UnityEngine;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Configuration for staff hiring system.
    /// Defines max staff count per star rating.
    /// </summary>
    [CreateAssetMenu(fileName = "StaffHiringConfig", menuName = "HairRemovalSim/Staff Hiring Config")]
    public class StaffHiringConfig : ScriptableObject
    {
        [Header("Max Staff by Star Rating")]
        [Tooltip("Maximum staff count for each star rating (index = starRating - 1)")]
        [SerializeField] private int[] maxStaffByStarRating = { 
            0,  // ★1
            0,  // ★2
            0,  // ★3
            1,  // ★4
            1,  // ★5
            2,  // ★6
            2,  // ★7
            3,  // ★8
            3,  // ★9
            4,  // ★10
            4,  // ★11
            5,  // ★12
            5,  // ★13
            6,  // ★14
            6,  // ★15
            7,  // ★16
            7,  // ★17
            8,  // ★18
            8,  // ★19
            9,  // ★20
            10, // ★21
            11, // ★22
            12, // ★23
            13, // ★24
            15  // ★25+
        };
        
        [Header("Salary System")]
        [Tooltip("Days until salary is due")]
        public int salaryDueDays = 3;
        
        [Tooltip("Grace period after due date before penalty")]
        public int salaryGraceDays = 3;
        
        [Tooltip("Penalty multiplier for overdue salary (1.1 = 10% extra)")]
        public float overduePenaltyMultiplier = 1.1f;
        
        [Tooltip("Review penalty when staff quits due to unpaid salary")]
        public int quitReviewPenalty = -1000;
        
        /// <summary>
        /// Get max staff count for given star rating
        /// </summary>
        public int GetMaxStaffForStarRating(int starRating)
        {
            int index = Mathf.Clamp(starRating - 1, 0, maxStaffByStarRating.Length - 1);
            return maxStaffByStarRating[index];
        }
        
        /// <summary>
        /// [Deprecated] Use GetMaxStaffForStarRating instead
        /// </summary>
        public int GetMaxStaffForGrade(int grade)
        {
            // For backward compatibility, convert grade to approximate star rating
            // Grade 1->★1, Grade 2->★5, Grade 3->★10, etc.
            int approxStarRating = (grade - 1) * 5 + 1;
            return GetMaxStaffForStarRating(approxStarRating);
        }
        
        /// <summary>
        /// Check if player can hire more staff at current star rating
        /// </summary>
        public bool CanHireMore(int currentStaffCount, int starRating)
        {
            return currentStaffCount < GetMaxStaffForStarRating(starRating);
        }
    }
}
