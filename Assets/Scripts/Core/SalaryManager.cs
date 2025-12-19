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
        
        private void Start()
        {
            // Subscribe to day events
            GameEvents.OnDayChanged += OnDayChanged;
        }
        
        private void OnDestroy()
        {
            GameEvents.OnDayChanged -= OnDayChanged;
        }
        
        private void OnDayChanged(int newDay)
        {
            // Process existing salaries (check overdue, etc.)
            ProcessSalaries(newDay);
            
            // Generate new salary cards for today
            GenerateDailySalaries(newDay);
            
            // Refresh payment panel
            UI.PaymentListPanel.Instance?.RefreshDisplay();
        }
        
        /// <summary>
        /// Generate salary for all active hired staff
        /// </summary>
        public void GenerateDailySalaries(int currentDay)
        {
            var staffList = Staff.StaffManager.Instance?.GetHiredStaff();
            if (staffList == null) return;
            
            foreach (var staff in staffList)
            {
                if (staff == null || !staff.isActive) continue;
                if (staff.hireDayNumber >= currentDay) continue; // Don't charge on hire day
                
                CreateSalaryRecord(staff, currentDay);
            }
        }
        
        /// <summary>
        /// Create a salary record for staff
        /// </summary>
        private void CreateSalaryRecord(Staff.HiredStaffData staff, int currentDay)
        {
            int dueDays = hiringConfig?.salaryDueDays ?? 3;
            
            var record = new SalaryRecord
            {
                staffId = staff.profile?.staffName ?? "unknown",
                staffName = staff.Name,
                amount = staff.DailySalary,
                createdDay = currentDay,
                dueDay = currentDay + dueDays,
                isPaid = false,
                isOverdue = false
            };
            
            pendingSalaries.Add(record);
            OnSalaryCreated?.Invoke(record);
            
            Debug.Log($"[SalaryManager] Created salary card: {staff.Name} ${staff.DailySalary} due day {record.dueDay}");
        }
        
        /// <summary>
        /// Process pending salaries (called at day start)
        /// </summary>
        public void ProcessSalaries(int currentDay)
        {
            int graceDays = hiringConfig?.salaryGraceDays ?? 3;
            
            // Track which staff have expired cards
            var staffToProcess = new System.Collections.Generic.HashSet<string>();
            
            foreach (var record in pendingSalaries)
            {
                if (record.isPaid) continue;
                
                // Check if overdue
                if (currentDay > record.dueDay && !record.isOverdue)
                {
                    record.isOverdue = true;
                    Debug.LogWarning($"[SalaryManager] Salary overdue for {record.staffName}");
                }
                
                // Check if past grace period
                if (currentDay > record.dueDay + graceDays)
                {
                    staffToProcess.Add(record.staffName);
                }
            }
            
            // Process each staff with expired cards
            foreach (var staffName in staffToProcess)
            {
                HandleUnpaidSalaryForStaff(staffName);
            }
        }
        
        /// <summary>
        /// Handle unpaid salary for a staff - sum ALL unpaid cards, force payment ×1.1, review penalty, staff quits
        /// </summary>
        private void HandleUnpaidSalaryForStaff(string staffName)
        {
            float penaltyMultiplier = hiringConfig?.overduePenaltyMultiplier ?? 1.1f;
            int penalty = hiringConfig?.quitReviewPenalty ?? -1000;
            
            // Sum all unpaid salaries for this staff
            int totalUnpaid = 0;
            for (int i = pendingSalaries.Count - 1; i >= 0; i--)
            {
                var record = pendingSalaries[i];
                if (record.staffName == staffName && !record.isPaid)
                {
                    totalUnpaid += record.amount;
                    pendingSalaries.RemoveAt(i);
                }
            }
            
            int penalizedAmount = Mathf.RoundToInt(totalUnpaid * penaltyMultiplier);
            
            // Force payment
            if (EconomyManager.Instance != null)
            {
                bool canPay = EconomyManager.Instance.SpendMoney(penalizedAmount);
                
                if (!canPay)
                {
                    // Game over - not enough money
                    Debug.LogError($"[SalaryManager] Cannot pay forced salary ${penalizedAmount} - GAME OVER");
                    // TODO: Trigger game over
                    return;
                }
            }
            
            // Apply review penalty
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReviewScore(penalty);
            }
            
            // Fire the staff
            var staffManager = Staff.StaffManager.Instance;
            if (staffManager != null)
            {
                var hiredList = staffManager.GetHiredStaff();
                var staffToFire = hiredList.Find(s => s.Name == staffName);
                if (staffToFire != null)
                {
                    staffManager.FireStaff(staffToFire);
                }
            }
            
            Debug.LogWarning($"[SalaryManager] {staffName} quit due to unpaid salary! Total: ${totalUnpaid} × 1.1 = ${penalizedAmount}, Review: {penalty}");
            
            // Refresh payment panel to remove cards
            UI.PaymentListPanel.Instance?.RefreshDisplay();
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
