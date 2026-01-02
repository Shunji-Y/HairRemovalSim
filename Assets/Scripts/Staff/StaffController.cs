using UnityEngine;
using UnityEngine.AI;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Staff controller for 3D movement and behavior
    /// Uses capsule placeholder until proper model is available
    /// </summary>
    public class StaffController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private StaffState currentState = StaffState.Idle;
        
        [Header("References")]
        private HiredStaffData staffData;
        private NavMeshAgent agent;
        private Animator animator;
        
        // Animator parameter hashes for performance
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int InReceRegiHash = Animator.StringToHash("InReceRegi");
        private static readonly int InTreatmentHash = Animator.StringToHash("InTreatment");
        private static readonly int RestockFromWarehouseHash = Animator.StringToHash("RestockFromWarehouse");
        private static readonly int RestockToAnywhereHash = Animator.StringToHash("RestockToAnywhere");
        private static readonly int BowHash = Animator.StringToHash("Bow");
        
        [Header("Station Points")]
        [Tooltip("Where to stand when assigned to reception")]
        public Transform receptionStationPoint;
        [Tooltip("Where to stand when assigned to cashier")]
        public Transform cashierStationPoint;
        [Tooltip("Exit point to leave at end of day")]
        public Transform exitPoint;
        
        [Header("Wander Settings")]
        public float wanderRadius = 5f;
        public float wanderInterval = 3f;
        private float wanderTimer;
        
        // Events
        public System.Action<StaffState> OnStateChanged;
        
        public enum StaffState
        {
            Idle,
            WalkingToStation,
            AtStation,
            Working,
            WanderingBeforeOpen,
            Leaving
        }
        
        public StaffState CurrentState => currentState;
        public HiredStaffData StaffData => staffData;
        
        // Door handling for treatment assignment
        private bool hasOpenedDoorForEntry = false;
        private BedController targetBedForDoor = null;
        [SerializeField] private float doorOpenDistance = 2f;
        
        // Target station for rotation alignment
        private Transform targetStationPoint = null;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            
            // Get animator (may be on child object)
            animator = GetComponentInChildren<Animator>();
            
            // Add work handlers if not present
            if (GetComponent<StaffReceptionHandler>() == null)
                gameObject.AddComponent<StaffReceptionHandler>();
            if (GetComponent<StaffCashierHandler>() == null)
                gameObject.AddComponent<StaffCashierHandler>();
            if (GetComponent<StaffTreatmentHandler>() == null)
                gameObject.AddComponent<StaffTreatmentHandler>();
            if (GetComponent<StaffRestockHandler>() == null)
                gameObject.AddComponent<StaffRestockHandler>();
        }
        
        private void Start()
        {
            // Find station points if not assigned
            FindStationPoints();
            
            // Subscribe to shop events
            Core.GameEvents.OnShopOpened += OnShopOpened;
            Core.GameEvents.OnShopClosed += OnShopClosed;
        }
        
        private void OnDestroy()
        {
            Core.GameEvents.OnShopOpened -= OnShopOpened;
            Core.GameEvents.OnShopClosed -= OnShopClosed;
        }
        
        private void OnShopOpened()
        {
            if (staffData == null || !staffData.isActive) return;
            
            // Go to assigned station when shop opens
            GoToAssignedStation();
            Debug.Log($"[StaffController] {staffData.Name} heading to station on shop open");
        }
        
        private void OnShopClosed()
        {
            if (staffData == null) return;
            
            // Staff continues working overtime - don't leave immediately
            // They will process remaining customers
            Debug.Log($"[StaffController] {staffData.Name} working overtime to serve remaining customers");
        }
        
        private void Update()
        {
            // Update animator speed parameter
            UpdateAnimator();
            
            switch (currentState)
            {
                case StaffState.WanderingBeforeOpen:
                    UpdateWandering();
                    break;
                    
                case StaffState.WalkingToStation:
                    CheckArrivalAtStation();
                    CheckDoorDistanceForEntry();
                    break;
                    
                case StaffState.AtStation:
                    // Wait for work (handled by assignment handlers)
                    break;
            }
        }
        
        /// <summary>
        /// Update animator parameters based on current state
        /// </summary>
        private void UpdateAnimator()
        {
            if (animator == null) return;
            
            // Update speed based on agent velocity
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat(SpeedHash, speed);
        }
        
        #region Animation Control Methods
        /// <summary>
        /// Set InReceRegi animator parameter (for reception/cashier processing)
        /// </summary>
        public void SetAnimInReceRegi(bool value)
        {
            if (animator != null)
                animator.SetBool(InReceRegiHash, value);
        }
        
        /// <summary>
        /// Set InTreatment animator parameter (for treatment processing)
        /// </summary>
        public void SetAnimInTreatment(bool value)
        {
            if (animator != null)
                animator.SetBool(InTreatmentHash, value);
        }
        
        /// <summary>
        /// Set RestockFromWarehouse animator parameter
        /// </summary>
        public void SetAnimRestockFromWarehouse(bool value)
        {
            if (animator != null)
                animator.SetBool(RestockFromWarehouseHash, value);
        }
        
        /// <summary>
        /// Set RestockToAnywhere animator parameter
        /// </summary>
        public void SetAnimRestockToAnywhere(bool value)
        {
            if (animator != null)
                animator.SetBool(RestockToAnywhereHash, value);
        }
        
        /// <summary>
        /// Clear all work animation states
        /// </summary>
        public void ClearAllWorkAnimations()
        {
            SetAnimInReceRegi(false);
            SetAnimInTreatment(false);
            SetAnimRestockFromWarehouse(false);
            SetAnimRestockToAnywhere(false);
        }
        
        /// <summary>
        /// Trigger bow animation
        /// </summary>
        public void TriggerBow()
        {
            if (animator != null)
                animator.SetTrigger(BowHash);
        }
        #endregion
        
        /// <summary>
        /// Initialize with hired staff data
        /// </summary>
        public void Initialize(HiredStaffData data)
        {
            staffData = data;
            
            // Set display name
            gameObject.name = $"Staff_{data.Name}";
            
            // Place on NavMesh
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }
            
            // Check current game state
            var gameState = Core.GameManager.Instance?.CurrentState;
            
            if (gameState == Core.GameManager.GameState.Preparation)
            {
                // Before shop opens - wander around
                SetState(StaffState.WanderingBeforeOpen);
            }
            else
            {
                // Shop is open - go to station
                GoToAssignedStation();
            }
            
            Debug.Log($"[StaffController] {data.Name} initialized. Assignment: {data.GetAssignmentDisplayText()}");
        }
        
        /// <summary>
        /// Called when assignment changes
        /// </summary>
        public void UpdateAssignment()
        {
            if (staffData == null) return;
            
            Debug.Log($"[StaffController] {staffData.Name} assignment updated to {staffData.GetAssignmentDisplayText()}");
            
            var previousAssignment = staffData.previousAssignment;
            
            // If was doing treatment, cancel it so player can take over
            var treatmentHandler = GetComponent<StaffTreatmentHandler>();
            if (treatmentHandler != null && treatmentHandler.IsProcessing)
            {
                treatmentHandler.CancelTreatment();
            }
            
            // If was doing reception, cancel it and return customer to queue
            if (previousAssignment == StaffAssignment.Reception)
            {
                var receptionHandler = GetComponent<StaffReceptionHandler>();
                if (receptionHandler != null && receptionHandler.IsProcessing)
                {
                    receptionHandler.CancelReception();
                }
            }
            
            // If was doing cashier, cancel it
            if (previousAssignment == StaffAssignment.Cashier)
            {
                var cashierHandler = GetComponent<StaffCashierHandler>();
                if (cashierHandler != null && cashierHandler.IsProcessing)
                {
                    cashierHandler.CancelCheckout();
                }
            }
            
            // If leaving a bed, handle door exit
            if (previousAssignment == StaffAssignment.Treatment && staffData.assignment != StaffAssignment.Treatment)
            {
                StartCoroutine(ExitBedWithDoor());
            }
            else
            {
                GoToAssignedStation();
            }
        }
        
        /// <summary>
        /// Exit bed area with door handling (open, exit, close)
        /// </summary>
        private System.Collections.IEnumerator ExitBedWithDoor()
        {
            // Get the bed we're leaving from using previous bed index
            BedController bed = null;
            if (staffData != null && staffData.previousBedIndex >= 0)
            {
                var beds = StaffManager.Instance?.beds;
                if (beds != null && staffData.previousBedIndex < beds.Count)
                {
                    bed = beds[staffData.previousBedIndex];
                }
            }
            
            if (bed == null)
            {
                GoToAssignedStation();
                yield break;
            }
            
            // Open the door (use right door as exit)
            var exitDoor = bed.rightDoor ?? bed.leftDoor;
            if (exitDoor != null && !exitDoor.IsOpen)
            {
                exitDoor.Open();
                yield return new WaitForSeconds(exitDoor.animationDuration + 0.1f);
            }
            
            // Move to station
            GoToAssignedStation();
            
            // Wait until we've moved away from bed
            yield return new WaitForSeconds(1.5f);
            
            // Close the door
            if (exitDoor != null && exitDoor.IsOpen)
            {
                exitDoor.Close();
            }
        }
        
        /// <summary>
        /// Navigate to assigned station
        /// </summary>
        public void GoToAssignedStation()
        {
            if (staffData == null) return;
            
            Transform destination = GetStationPoint(staffData.assignment);
            
            if (destination != null)
            {
                targetStationPoint = destination; // Save for rotation alignment
                SetDestination(destination.position);
                SetState(StaffState.WalkingToStation);
                
                // Track door handling for treatment assignment
                if (staffData.assignment == StaffAssignment.Treatment)
                {
                    targetBedForDoor = GetAssignedBed();
                    hasOpenedDoorForEntry = false;
                }
                else
                {
                    targetBedForDoor = null;
                    hasOpenedDoorForEntry = false;
                }
            }
            else
            {
                targetStationPoint = null;
                SetState(StaffState.Idle);
            }
        }
        
        /// <summary>
        /// Get station point for assignment type
        /// </summary>
        private Transform GetStationPoint(StaffAssignment assignment)
        {
            switch (assignment)
            {
                case StaffAssignment.Reception:
                    return receptionStationPoint;
                case StaffAssignment.Cashier:
                    return cashierStationPoint;
                case StaffAssignment.Treatment:
                    // Get bed's staff point
                    if (staffData.assignedBedIndex >= 0)
                    {
                        var beds = StaffManager.Instance?.beds;
                        if (beds != null && staffData.assignedBedIndex < beds.Count)
                        {
                            var bed = beds[staffData.assignedBedIndex];
                            // Use staffPoint if available, otherwise arrivalPoint or transform
                            return bed?.staffPoint ?? bed?.arrivalPoint ?? bed?.transform;
                        }
                    }
                    return null;
                case StaffAssignment.Restock:
                    // Get warehouse staff point
                    var warehouse = Core.WarehouseManager.Instance;
                    if (warehouse != null)
                    {
                        return warehouse.staffPoint ?? warehouse.transform;
                    }
                    return null;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Find station points in scene
        /// </summary>
        private void FindStationPoints()
        {
            // Try to find reception point
            if (receptionStationPoint == null)
            {
                var receptionManager = UI.ReceptionManager.Instance;
                if (receptionManager != null)
                {
                    // Use staffPoint if available, otherwise fall back to transform
                    receptionStationPoint = receptionManager.staffPoint != null 
                        ? receptionManager.staffPoint 
                        : receptionManager.transform;
                }
            }
            
            // Try to find cashier point
            if (cashierStationPoint == null)
            {
                var cashRegister = FindObjectOfType<UI.CashRegister>();
                if (cashRegister != null)
                {
                    // Use staffPoint if available, otherwise fall back to transform
                    cashierStationPoint = cashRegister.staffPoint != null 
                        ? cashRegister.staffPoint 
                        : cashRegister.transform;
                }
            }
            
            // Try to find exit point
            if (exitPoint == null)
            {
                var spawner = FindObjectOfType<Customer.CustomerSpawner>();
                if (spawner != null)
                {
                    exitPoint = spawner.exitPoint;
                }
            }
        }
        
        /// <summary>
        /// Check if arrived at station
        /// </summary>
        private void CheckArrivalAtStation()
        {
            if (agent == null || !agent.enabled) return;
            
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Stop agent and disable rotation override
                agent.isStopped = true;
                agent.updateRotation = false;
                
                SetState(StaffState.AtStation);
                
                // Smooth rotation to station point
                if (targetStationPoint != null)
                {
                    StartCoroutine(SmoothRotateToTarget(targetStationPoint.rotation));
                }
                
                // Close doors after arriving at treatment station
                if (targetBedForDoor != null && hasOpenedDoorForEntry)
                {
                    targetBedForDoor.CloseDoors();
                    hasOpenedDoorForEntry = false;
                    targetBedForDoor = null;
                }
            }
        }
        
        /// <summary>
        /// Smoothly rotate to target rotation
        /// </summary>
        private System.Collections.IEnumerator SmoothRotateToTarget(Quaternion targetRotation)
        {
            float rotationSpeed = 180f; // degrees per second
            
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }
            
            transform.rotation = targetRotation;
        }
        
        /// <summary>
        /// Check if staff is close enough to door to open it
        /// </summary>
        private void CheckDoorDistanceForEntry()
        {
            if (targetBedForDoor == null || hasOpenedDoorForEntry) return;
            
            // Check distance to bed's staff point
            if (targetBedForDoor.staffPoint != null)
            {
                float distanceToBed = Vector3.Distance(transform.position, targetBedForDoor.staffPoint.position);
                
                if (distanceToBed <= doorOpenDistance)
                {
                    // Open the doors
                    targetBedForDoor.OpenDoors();
                    hasOpenedDoorForEntry = true;
                    Debug.Log($"[StaffController] Opening doors to enter bed area");
                }
            }
        }
        
        /// <summary>
        /// Wander around before shop opens
        /// </summary>
        private void UpdateWandering()
        {
            wanderTimer -= Time.deltaTime;
            
            if (wanderTimer <= 0)
            {
                WanderToRandomPoint();
                wanderTimer = wanderInterval + Random.Range(-1f, 1f);
            }
        }
        
        private void WanderToRandomPoint()
        {
            if (agent == null || !agent.enabled) return;
            
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        
        /// <summary>
        /// Set NavMesh destination
        /// </summary>
        private void SetDestination(Vector3 position)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true; // Re-enable rotation when moving
                agent.SetDestination(position);
            }
        }
        
        /// <summary>
        /// Leave the shop (end of day)
        /// </summary>
        public void LeaveShop()
        {
            if (exitPoint != null)
            {
                SetDestination(exitPoint.position);
                SetState(StaffState.Leaving);
            }
        }
        
        /// <summary>
        /// Set state and fire event
        /// </summary>
        private void SetState(StaffState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(newState);
            }
        }
        
        /// <summary>
        /// Get staff display info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"{staffData?.Name ?? "Unknown"} | {currentState} | {staffData?.GetAssignmentDisplayText() ?? "N/A"}";
        }
        
        /// <summary>
        /// Get assigned bed for treatment
        /// </summary>
        public BedController GetAssignedBed()
        {
            if (staffData == null || staffData.assignment != StaffAssignment.Treatment)
                return null;
            
            if (staffData.assignedBedIndex < 0)
                return null;
            
            var beds = StaffManager.Instance?.beds;
            if (beds != null && staffData.assignedBedIndex < beds.Count)
            {
                return beds[staffData.assignedBedIndex];
            }
            return null;
        }
    }
}
