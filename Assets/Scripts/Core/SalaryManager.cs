using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Staff;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages staff salary payments, due dates, and penalties.
    /// Creates salary cards in PaymentListPanel.
    /// </summary>
    public class SalaryManager : MonoBehaviour
    {
        public static SalaryManager Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private StaffHiringConfig hiringConfig;
        
        // Active salary records
        private List<SalaryRecord> pendingSalaries = new List<SalaryRecord>();
        
        // Events
        public System.Action<SalaryRecord> OnSalaryCreated;
        public System.Action<SalaryRecord> OnSalaryPaid;
        public System.Action<StaffProfile> OnStaffQuitUnpaid;
        
        public List<SalaryRecord> PendingSalaries => pendingSalaries;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        /// <summary>
        /// Generate salary for all hired staff (called at day end)
        /// </summary>
        public void GenerateDailySalaries(List<StaffProfile> hiredStaff, int currentDay)
        {
            foreach (var staff in hiredStaff)
            {
                if (staff == null || !staff.isHired) continue;
                if (staff.hireDay == currentDay) continue; // Don't charge on hire day
                
                CreateSalaryRecord(staff, currentDay);
            }
        }
        
        /// <summary>
        /// Create a salary record for staff
        /// </summary>
        private void CreateSalaryRecord(StaffProfile staff, int currentDay)
        {
            int dueDays = hiringConfig?.salaryDueDays ?? 3;
            
            var record = new SalaryRecord
            {
                staffId = staff.staffId,
                staffName = staff.displayName,
                amount = staff.DailySalary,
                createdDay = currentDay,
                dueDay = currentDay + dueDays,
                isPaid = false,
                isOverdue = false
            };
            
            pendingSalaries.Add(record);
            OnSalaryCreated?.Invoke(record);
            
            Debug.Log($"[SalaryManager] Created salary record: {staff.displayName} ¥{staff.DailySalary} due day {record.dueDay}");
        }
        
        /// <summary>
        /// Process pending salaries (called at day start)
        /// </summary>
        public void ProcessSalaries(int currentDay, List<StaffProfile> hiredStaff)
        {
            int graceDays = hiringConfig?.salaryGraceDays ?? 3;
            
            for (int i = pendingSalaries.Count - 1; i >= 0; i--)
            {
                var record = pendingSalaries[i];
                if (record.isPaid) continue;
                
                // Check if overdue
                if (currentDay > record.dueDay && !record.isOverdue)
                {
                    record.isOverdue = true;
                    Debug.LogWarning($"[SalaryManager] Salary overdue for {record.staffName}");
                }
                
                // Check if past grace period - force payment or staff quits
                if (currentDay > record.dueDay + graceDays)
                {
                    HandleUnpaidSalary(record, hiredStaff);
                    pendingSalaries.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Handle unpaid salary - force payment, review penalty, staff quits
        /// </summary>
        private void HandleUnpaidSalary(SalaryRecord record, List<StaffProfile> hiredStaff)
        {
            float penaltyMultiplier = hiringConfig?.overduePenaltyMultiplier ?? 1.1f;
            int penalty = hiringConfig?.quitReviewPenalty ?? -1000;
            
            int penalizedAmount = Mathf.RoundToInt(record.amount * penaltyMultiplier);
            
            // Force payment
            if (EconomyManager.Instance != null)
            {
                bool canPay = EconomyManager.Instance.SpendMoney(penalizedAmount);
                
                if (!canPay)
                {
                    // Game over - not enough money
                    Debug.LogError($"[SalaryManager] Cannot pay forced salary ¥{penalizedAmount} - GAME OVER");
                    // TODO: Trigger game over
                    return;
                }
            }
            
            // Apply review penalty
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReviewScore(penalty);
            }
            
            // Staff quits
            var staff = FindStaffById(record.staffId, hiredStaff);
            if (staff != null)
            {
                OnStaffQuitUnpaid?.Invoke(staff);
                Debug.LogWarning($"[SalaryManager] {record.staffName} quit due to unpaid salary! Review: {penalty}, Paid: ¥{penalizedAmount}");
            }
        }
        
        /// <summary>
        /// Pay a specific salary record
        /// </summary>
        public bool PaySalary(SalaryRecord record)
        {
            if (record.isPaid) return true;
            
            if (EconomyManager.Instance != null)
            {
                bool success = EconomyManager.Instance.SpendMoney(record.amount);
                if (success)
                {
                    record.isPaid = true;
                    OnSalaryPaid?.Invoke(record);
                    pendingSalaries.Remove(record);
                    return true;
                }
            }
            
            return false;
        }
        
        private StaffProfile FindStaffById(string staffId, List<StaffProfile> hiredStaff)
        {
            foreach (var staff in hiredStaff)
            {
                if (staff.staffId == staffId) return staff;
            }
            return null;
        }
        
        /// <summary>
        /// Get all unpaid salaries for a specific staff
        /// </summary>
        public List<SalaryRecord> GetUnpaidSalariesForStaff(string staffId)
        {
            var result = new List<SalaryRecord>();
            foreach (var record in pendingSalaries)
            {
                if (record.staffId == staffId && !record.isPaid)
                {
                    result.Add(record);
                }
            }
            return result;
        }
    }
    
    /// <summary>
    /// Individual salary payment record
    /// </summary>
    [System.Serializable]
    public class SalaryRecord
    {
        public string staffId;
        public string staffName;
        public int amount;
        public int createdDay;
        public int dueDay;
        public bool isPaid;
        public bool isOverdue;
    }
}
