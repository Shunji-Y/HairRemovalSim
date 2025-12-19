using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Manages hired staff - hiring, firing, salary payments, spawning
    /// </summary>
    public class StaffManager : MonoBehaviour
    {
        public static StaffManager Instance { get; private set; }
        
        [Header("Staff Pool")]
        [Tooltip("Available staff profiles that can be hired")]
        public List<StaffProfileData> availableProfiles = new List<StaffProfileData>();
        
        [Header("Staff Prefab")]
        [Tooltip("Prefab for spawning staff (capsule placeholder)")]
        public GameObject staffPrefab;
        
        [Header("Spawn Settings")]
        [Tooltip("Where staff spawn at start of day")]
        public Transform staffSpawnPoint;
        
        [Header("References")]
        [Tooltip("Reference to beds for treatment assignment")]
        public BedController[] beds;
        
        [Header("Debug")]
        [SerializeField] private List<HiredStaffData> hiredStaff = new List<HiredStaffData>();
        
        // Events
        public System.Action<HiredStaffData> OnStaffHired;
        public System.Action<HiredStaffData> OnStaffFired;
        public System.Action<int> OnSalariesPaid; // Total paid
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            // Auto-find beds if not assigned
            if (beds == null || beds.Length == 0)
            {
                beds = FindObjectsOfType<BedController>();
                Debug.Log($"[StaffManager] Auto-found {beds?.Length ?? 0} beds in scene");
            }
            
            // Subscribe to day change event via GameEvents
            Core.GameEvents.OnDayChanged += OnDayChanged;
        }
        
        private void OnDestroy()
        {
            Core.GameEvents.OnDayChanged -= OnDayChanged;
        }
        
        /// <summary>
        /// Called when day changes - activate new staff
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            // Activate staff hired yesterday
            ActivateNewStaff(newDay);
            
            Debug.Log($"[StaffManager] Day {newDay}: {HiredStaffCount} hired staff");
        }
        
        /// <summary>
        /// Hire a new staff member
        /// </summary>
        public bool HireStaff(StaffProfileData profile, StaffAssignment assignment = StaffAssignment.None, int bedIndex = -1)
        {
            if (profile == null)
            {
                Debug.LogError("[StaffManager] Cannot hire null profile");
                return false;
            }
            
            int currentDay = Core.GameManager.Instance?.DayCount ?? 1;
            var hiredData = new HiredStaffData(profile, currentDay);
            hiredData.assignment = assignment;
            hiredData.assignedBedIndex = bedIndex;
            
            hiredStaff.Add(hiredData);
            
            Debug.Log($"[StaffManager] Hired {profile.staffName} ({profile.Rank}). Assignment: {hiredData.GetAssignmentDisplayText()}. Starts tomorrow.");
            
            OnStaffHired?.Invoke(hiredData);
            return true;
        }
        
        /// <summary>
        /// Fire a staff member
        /// </summary>
        public bool FireStaff(HiredStaffData staffData)
        {
            if (staffData == null || !hiredStaff.Contains(staffData))
            {
                Debug.LogWarning("[StaffManager] Staff not found in hired list");
                return false;
            }
            
            // Destroy spawned controller if exists
            if (staffData.controller != null)
            {
                Destroy(staffData.controller.gameObject);
            }
            
            hiredStaff.Remove(staffData);
            
            Debug.Log($"[StaffManager] Fired {staffData.Name}");
            
            OnStaffFired?.Invoke(staffData);
            return true;
        }
        
        /// <summary>
        /// Change staff assignment
        /// </summary>
        public bool SetStaffAssignment(HiredStaffData staffData, StaffAssignment assignment, int bedIndex = -1)
        {
            if (staffData == null) return false;
            
            // Check if position is already occupied by another staff
            if (IsPositionOccupied(assignment, bedIndex, staffData))
            {
                Debug.LogWarning($"[StaffManager] Cannot assign {staffData.Name}: position already occupied");
                return false;
            }
            
            staffData.assignment = assignment;
            staffData.assignedBedIndex = assignment == StaffAssignment.Treatment ? bedIndex : -1;
            
            // Update controller if active
            if (staffData.controller != null)
            {
                staffData.controller.UpdateAssignment();
            }
            
            Debug.Log($"[StaffManager] {staffData.Name} assigned to {staffData.GetAssignmentDisplayText()}");
            return true;
        }
        
        /// <summary>
        /// Check if a position is already occupied by another staff
        /// </summary>
        public bool IsPositionOccupied(StaffAssignment assignment, int bedIndex, HiredStaffData excludeStaff = null)
        {
            foreach (var staff in hiredStaff)
            {
                if (staff == excludeStaff) continue;
                if (staff.assignment != assignment) continue;
                
                // For treatment, check bed index
                if (assignment == StaffAssignment.Treatment)
                {
                    if (staff.assignedBedIndex == bedIndex)
                        return true;
                }
                else if (assignment != StaffAssignment.None)
                {
                    // Only one staff per non-treatment position
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get first available bed index for treatment assignment
        /// Returns -1 if no beds available
        /// </summary>
        public int GetFirstAvailableBedIndex()
        {
            Debug.Log($"[StaffManager] GetFirstAvailableBedIndex: beds={beds?.Length ?? -1}");
            
            if (beds == null)
            {
                Debug.LogWarning("[StaffManager] beds array is null!");
                return -1;
            }
            
            if (beds.Length == 0)
            {
                Debug.LogWarning("[StaffManager] beds array is empty!");
                return -1;
            }
            
            for (int i = 0; i < beds.Length; i++)
            {
                bool occupied = IsPositionOccupied(StaffAssignment.Treatment, i);
                Debug.Log($"[StaffManager] Bed {i}: occupied={occupied}");
                if (!occupied)
                {
                    return i;
                }
            }
            
            Debug.LogWarning("[StaffManager] All beds occupied!");
            return -1;
        }
        
        /// <summary>
        /// Activate staff that were hired on previous days
        /// </summary>
        private void ActivateNewStaff(int currentDay)
        {
            foreach (var staff in hiredStaff)
            {
                if (staff.ShouldStartWorking(currentDay))
                {
                    staff.isActive = true;
                    SpawnStaffController(staff);
                    Debug.Log($"[StaffManager] {staff.Name} started working on day {currentDay}");
                }
            }
        }
        
        /// <summary>
        /// Spawn staff controller in 3D world
        /// </summary>
        public void SpawnStaffController(HiredStaffData staffData)
        {
            if (staffData == null || staffPrefab == null)
            {
                Debug.LogWarning("[StaffManager] Cannot spawn - missing prefab or data");
                return;
            }
            
            // Destroy existing controller if any
            if (staffData.controller != null)
            {
                Destroy(staffData.controller.gameObject);
            }
            
            Vector3 spawnPos = staffSpawnPoint != null ? staffSpawnPoint.position : Vector3.zero;
            GameObject obj = Instantiate(staffPrefab, spawnPos, Quaternion.identity);
            obj.name = $"Staff_{staffData.Name}";
            
            StaffController controller = obj.GetComponent<StaffController>();
            if (controller == null)
            {
                controller = obj.AddComponent<StaffController>();
            }
            
            controller.Initialize(staffData);
            staffData.controller = controller;
            
            Debug.Log($"[StaffManager] Spawned {staffData.Name} at {spawnPos}");
        }
        
        /// <summary>
        /// Pay daily salaries and deduct from economy
        /// </summary>
        private int PayDailySalaries()
        {
            int totalSalary = 0;
            
            foreach (var staff in hiredStaff)
            {
                if (staff.isActive)
                {
                    totalSalary += staff.DailySalary;
                }
            }
            
            if (totalSalary > 0 && Core.EconomyManager.Instance != null)
            {
                Core.EconomyManager.Instance.SpendMoney(totalSalary);
                OnSalariesPaid?.Invoke(totalSalary);
            }
            
            return totalSalary;
        }
        
        /// <summary>
        /// Get all hired staff
        /// </summary>
        public List<HiredStaffData> GetHiredStaff() => new List<HiredStaffData>(hiredStaff);
        
        /// <summary>
        /// Get count of hired staff
        /// </summary>
        public int HiredStaffCount => hiredStaff.Count;
        
        /// <summary>
        /// Get total daily salary cost
        /// </summary>
        public int GetTotalDailySalaryCost()
        {
            int total = 0;
            foreach (var staff in hiredStaff)
            {
                if (staff.isActive)
                {
                    total += staff.DailySalary;
                }
            }
            return total;
        }
        
        /// <summary>
        /// Force spawn all active staff (for testing)
        /// </summary>
        public void DebugSpawnAllStaff()
        {
            foreach (var staff in hiredStaff)
            {
                if (staff.isActive && staff.controller == null)
                {
                    SpawnStaffController(staff);
                }
            }
        }
        
        /// <summary>
        /// Force hire and immediately activate (for testing)
        /// </summary>
        public HiredStaffData DebugHireAndActivate(StaffProfileData profile, StaffAssignment assignment = StaffAssignment.None)
        {
            if (profile == null) return null;
            
            int currentDay = Core.GameManager.Instance?.DayCount ?? 1;
            var hiredData = new HiredStaffData(profile, currentDay - 1); // Hire "yesterday" so active today
            hiredData.assignment = assignment;
            hiredData.isActive = true;
            
            hiredStaff.Add(hiredData);
            SpawnStaffController(hiredData);
            
            Debug.Log($"[StaffManager] DEBUG: Hired and activated {profile.staffName}");
            
            return hiredData;
        }
        
        /// <summary>
        /// Refresh bed references from ShopManager
        /// Called when beds are added during shop upgrades
        /// </summary>
        public void RefreshBedAssignments()
        {
            var shopManager = Core.ShopManager.Instance;
            if (shopManager == null || shopManager.Beds == null) return;
            
            beds = new BedController[shopManager.Beds.Count];
            for (int i = 0; i < shopManager.Beds.Count; i++)
            {
                beds[i] = shopManager.Beds[i];
            }
            
            Debug.Log($"[StaffManager] Refreshed bed assignments. Total beds: {beds.Length}");
        }
    }
}
