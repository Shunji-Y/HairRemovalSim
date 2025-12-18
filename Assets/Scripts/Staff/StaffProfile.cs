using UnityEngine;
using System;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Individual staff profile data.
    /// Contains name, photo, rank reference, and employment state.
    /// </summary>
    [Serializable]
    public class StaffProfile
    {
        [Header("Identity")]
        public string staffId;
        public string displayName;
        public Sprite photo;
        
        [Header("Rank")]
        public StaffRankData rankData;
        
        [Header("Employment")]
        public bool isHired = false;
        public StaffAssignment assignment = StaffAssignment.None;
        public int assignedBedIndex = -1; // For Treatment assignment
        public int hireDay = -1; // Game day when hired
        public bool isWorking = false; // Currently working (arrived at shop)
        
        [Header("Salary")]
        public int pendingSalaryDays = 0;
        public int lastPaidDay = -1;
        
        /// <summary>
        /// Get daily salary from rank data
        /// </summary>
        public int DailySalary => rankData?.dailySalary ?? 0;
        
        /// <summary>
        /// Get localized rank name
        /// </summary>
        public string GetRankDisplayName()
        {
            return rankData?.GetDisplayName() ?? "Unknown";
        }
        
        /// <summary>
        /// Check if staff can be assigned to given position
        /// </summary>
        public bool CanAssignTo(StaffAssignment newAssignment)
        {
            // All staff can be assigned to any position
            return true;
        }
        
        /// <summary>
        /// Clone for candidate display (not yet hired)
        /// </summary>
        public StaffProfile Clone()
        {
            return new StaffProfile
            {
                staffId = staffId,
                displayName = displayName,
                photo = photo,
                rankData = rankData,
                isHired = false,
                assignment = StaffAssignment.None,
                assignedBedIndex = -1,
                hireDay = -1,
                isWorking = false
            };
        }
    }
}
