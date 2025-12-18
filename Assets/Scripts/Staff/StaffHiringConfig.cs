using UnityEngine;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Configuration for staff hiring system.
    /// Defines max staff count per shop grade.
    /// </summary>
    [CreateAssetMenu(fileName = "StaffHiringConfig", menuName = "HairRemovalSim/Staff Hiring Config")]
    public class StaffHiringConfig : ScriptableObject
    {
        [Header("Max Staff by Grade")]
        [Tooltip("Maximum staff count for each shop grade (index = grade - 1)")]
        [SerializeField] private int[] maxStaffByGrade = { 0, 2, 5, 12, 14, 20 };
        
        [Header("Candidate Generation")]
        [Tooltip("Number of candidates to show in hire panel")]
        [Range(3, 12)]
        public int candidateCount = 6;
        
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
        /// Get max staff count for given shop grade
        /// </summary>
        public int GetMaxStaffForGrade(int grade)
        {
            int index = Mathf.Clamp(grade - 1, 0, maxStaffByGrade.Length - 1);
            return maxStaffByGrade[index];
        }
        
        /// <summary>
        /// Check if player can hire more staff at current grade
        /// </summary>
        public bool CanHireMore(int currentStaffCount, int shopGrade)
        {
            return currentStaffCount < GetMaxStaffForGrade(shopGrade);
        }
    }
}
