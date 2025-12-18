using UnityEngine;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Runtime data for a hired staff member
    /// </summary>
    [System.Serializable]
    public class HiredStaffData
    {
        [Header("Profile Reference")]
        public StaffProfileData profile;
        
        [Header("Assignment")]
        public StaffAssignment assignment = StaffAssignment.None;
        
        [Tooltip("If assigned to Treatment, which bed index")]
        public int assignedBedIndex = -1;
        
        [Header("State")]
        public int hireDayNumber;  // Day when hired (starts working next day)
        public bool isActive = false;  // Currently working (after first day)
        
        [Header("Runtime Reference")]
        [System.NonSerialized]
        public StaffController controller;  // Reference to spawned StaffController
        
        /// <summary>
        /// Create new hired staff data
        /// </summary>
        public HiredStaffData(StaffProfileData staffProfile, int currentDay)
        {
            profile = staffProfile;
            hireDayNumber = currentDay;
            assignment = StaffAssignment.None;
            assignedBedIndex = -1;
            isActive = false;
        }
        
        /// <summary>
        /// Get staff name
        /// </summary>
        public string Name => profile?.staffName ?? "Unknown";
        
        /// <summary>
        /// Get staff rank
        /// </summary>
        public StaffRank Rank => profile?.Rank ?? StaffRank.College;
        
        /// <summary>
        /// Get daily salary
        /// </summary>
        public int DailySalary => profile?.DailySalary ?? 100;
        
        /// <summary>
        /// Check if staff starts working today (hired yesterday)
        /// </summary>
        public bool ShouldStartWorking(int currentDay)
        {
            return !isActive && currentDay > hireDayNumber;
        }
        
        /// <summary>
        /// Get assignment display text
        /// </summary>
        public string GetAssignmentDisplayText()
        {
            switch (assignment)
            {
                case StaffAssignment.None:
                    return "未配置";
                case StaffAssignment.Reception:
                    return "受付";
                case StaffAssignment.Cashier:
                    return "レジ";
                case StaffAssignment.Treatment:
                    return assignedBedIndex >= 0 ? $"施術 (ベッド{assignedBedIndex + 1})" : "施術";
                case StaffAssignment.Restock:
                    return "在庫補充";
                default:
                    return "不明";
            }
        }
    }
}
