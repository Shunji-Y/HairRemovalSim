using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;
using HairRemovalSim.Player;
using HairRemovalSim.Treatment;

namespace HairRemovalSim.Customer
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerController : MonoBehaviour, IInteractable
    {
        public enum CustomerState { Entering, Waiting, WalkingToBed, InTreatment, Completed, WalkingToReception, Paying, Leaving }
        
        [Header("Data")]
        public CustomerData data;
        
        [Header("References")]
        public NavMeshAgent agent;
        public BodyPartsDatabase bodyPartsDatabase; // UV-based body part system

        [Header("Status")]
        public float currentPain = 0f;
        public float maxPain = 100f;
        public bool IsCompleted = false; // Track if treatment is complete

        private CustomerState currentState;
        public CustomerState CurrentState => currentState; // Public read-only access
        
        private Transform targetBed;
        public Environment.BedController assignedBed; // Track which bed this customer is using
        private Transform exitPoint;
        private Transform receptionPoint; // Pre-treatment reception
        private Transform cashRegisterPoint; // Post-treatment payment
        private CustomerSpawner spawner; // Reference to spawner for pool return
        
        [Header("Initialization")]
        public bool isInitialized = false; // Track if BodyParts are initialized
        
        [Header("Payment Settings")]
        public float paymentDelay = 2.0f;
        private float paymentTimer = 0f;

        [Header("Visuals")]
        public Animator animator;
        
        [Header("Rotation")]
        public bool isSupine = true; // true = face-up, false = face-down
        private bool isRotating = false;
        private Quaternion targetRotation;
        public float rotationDuration = 1.0f;
        private float rotationElapsed = 0f;
        
        // Cached material instances for consistent shader property updates
        private List<Material> cachedMaterials = new List<Material>();

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }


        private void Start()
        {
            // Initialize BodyParts over multiple frames to avoid frame rate drop
            StartCoroutine(InitializeBodyPartsAsync());
        }
        
        private System.Collections.IEnumerator InitializeBodyPartsAsync()
        {
            HairRemovalSim.Core.BodyPart[] bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            Debug.Log($"[CustomerController] Initializing {bodyParts.Length} body parts over {bodyParts.Length} frames...");
            
            int count = 0;
            foreach (HairRemovalSim.Core.BodyPart part in bodyParts)
            {
                part.Initialize();
                count++;
                
                // Wait one frame between each initialization to spread the load
                yield return null;
            }
            
            isInitialized = true; // Mark as initialized
            Debug.Log($"[CustomerController] Finished initializing {count} body parts for {(data != null ? data.customerName : "pooled customer")}");
        }
        



        public void Initialize(CustomerData newData, Transform exit, Transform reception, Transform cashRegister, CustomerSpawner spawnerRef)
        {
            data = newData;
            exitPoint = exit;
            receptionPoint = reception;
            cashRegisterPoint = cashRegister;
            spawner = spawnerRef;
            
            // Reset all states for pool reuse
            ResetCustomerState();
            
            // Reset all body parts for reuse
            ResetAllBodyParts();
        }
        
        /// <summary>
        /// Reset customer state variables for pool reuse
        /// </summary>
        private void ResetCustomerState()
        {
            // Reset completion flags
            IsCompleted = false;
            
            // Reset rotation state to default (supine = face-up)
            isSupine = true;
            isRotating = false;
            rotationElapsed = 0f;
            
            // Reset timers
            paymentTimer = 0f;
            
            // Reset state to initial
            currentState = CustomerState.Waiting;
            
            // Reset HairTreatmentControllers for pool reuse
            var treatmentControllers = GetComponentsInChildren<HairTreatmentController>();
            foreach (var controller in treatmentControllers)
            {
                controller.ResetForReuse();
            }
            
            // Reset completed parts counter
            completedPartCount = 0;
            
            Debug.Log($"[CustomerController] Reset state for {data.customerName}");
        }
        
        /// <summary>
        /// Reset all body parts to 0% completion for pool reuse
        /// </summary>
        private void ResetAllBodyParts()
        {
            var bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            foreach (var part in bodyParts)
            {
                part.Reset();
            }
            Debug.Log($"[CustomerController] Reset {bodyParts.Length} body parts for {data.customerName}");
        }
        
        /// <summary>
        /// Highlight requested body parts using UV mask system (called when arriving at bed)
        /// </summary>
        // Highlight state tracking
        private int completedPartCount = 0;
        private SkinnedMeshRenderer cachedRenderer;
        private List<string> requestedPartNames = new List<string>();

        private void SetupRequestedPartsForHighlighting()
        {
            Debug.Log($"[Highlight] === SETUP === data={data}, plan={data?.selectedTreatmentPlan}");
            
            // Validation
            if (data == null || data.selectedTreatmentPlan == TreatmentPlan.None)
            {
                Debug.LogWarning("[Highlight] No treatment plan selected");
                return;
            }
            
            if (bodyPartsDatabase == null)
            {
                Debug.LogError("[Highlight] BodyPartsDatabase is not assigned!");
                return;
            }
            
            // Get renderer from the object that has BodyPart component
            var bodyPart = GetComponentInChildren<HairRemovalSim.Core.BodyPart>();
            if (bodyPart == null)
            {
                Debug.LogError("[Highlight] No BodyPart component found!");
                return;
            }
            
            cachedRenderer = bodyPart.GetComponent<SkinnedMeshRenderer>();
            if (cachedRenderer == null)
            {
                cachedRenderer = bodyPart.GetComponentInParent<SkinnedMeshRenderer>();
                if (cachedRenderer == null)
                {
                    cachedRenderer = bodyPart.GetComponentInChildren<SkinnedMeshRenderer>();
                }
            }
            
            if (cachedRenderer == null)
            {
                Debug.LogError("[Highlight] No SkinnedMeshRenderer found on BodyPart object!");
                return;
            }
            
            Debug.Log($"[Highlight] Renderer found: {cachedRenderer.name}");
            
            // Get target parts from treatment plan
            var targetParts = data.selectedTreatmentPlan.GetBodyPartDefinitions(bodyPartsDatabase);
            
            if (targetParts == null || targetParts.Count == 0)
            {
                Debug.LogWarning("[Highlight] No target parts found for treatment plan");
                return;
            }
            
            // Store requested part names for completion tracking
            requestedPartNames.Clear();
            foreach (var part in targetParts)
            {
                requestedPartNames.Add(part.partName);
            }
            
            // Apply to each material - set Request flags for target parts
            Material[] materials = cachedRenderer.materials;
            cachedMaterials.Clear();
            
            foreach (var mat in materials)
            {
                cachedMaterials.Add(mat);
                
                // Reset all request/completed flags first
                ResetAllPartFlags(mat);
                
                // Set request flags for target parts
                foreach (var part in targetParts)
                {
                    string propName = $"_Request_{part.partName}";
                    mat.SetFloat(propName, 1.0f);
                    Debug.Log($"[Highlight] Set {propName} = 1.0 on {mat.name}");
                }
                
                // Start with highlight OFF - Q key will turn it on
                mat.SetFloat("_HighlightIntensity", 0f);
            }
            
            // Assign modified materials back to renderer
            cachedRenderer.materials = materials;
            
            // Reset completion counter
            completedPartCount = 0;
            
            // Setup Treatment Controllers
            var treatmentControllers = GetComponentsInChildren<Treatment.HairTreatmentController>();
            foreach (var controller in treatmentControllers)
            {
                controller.SetTargetBodyParts(targetParts);
                controller.OnPartCompleted -= OnBodyPartCompleted;
                controller.OnPartCompleted += OnBodyPartCompleted;
            }
            
            Debug.Log($"[Highlight] === COMPLETE === Setup {targetParts.Count} parts for highlighting");
        }
        
        /// <summary>
        /// Reset all per-part flags to 0
        /// </summary>
        private void ResetAllPartFlags(Material mat)
        {
            string[] partNames = { "Beard", "Chest", "Abs", "Back", "LeftArmpit", "RightArmpit",
                                   "LeftUpperArm", "LeftLowerArm", "RightUpperArm", "RightLowerArm",
                                   "LeftThigh", "LeftCalf", "RightThigh", "RightCalf" };
            
            foreach (var name in partNames)
            {
                mat.SetFloat($"_Request_{name}", 0f);
                mat.SetFloat($"_Completed_{name}", 0f);
            }
        }
        
        /// <summary>
        /// Called when an individual body part treatment is completed
        /// Adds mask value to _CompletedPartMasks to stop highlighting that part
        /// </summary>
        private void OnBodyPartCompleted(string partName)
        {
            Debug.Log($"[Highlight] OnBodyPartCompleted: {partName}");
            
            // Set the completed flag for this part on all materials
            string propName = $"_Completed_{partName}";
            
            foreach (var mat in cachedMaterials)
            {
                if (mat != null && mat.HasProperty(propName))
                {
                    mat.SetFloat(propName, 1.0f);
                    Debug.Log($"[Highlight] Set {propName} = 1.0 on {mat.name}");
                }
            }
            
            completedPartCount++;
            StartCoroutine(FlashCompletedPart(partName));
        }
        
        /// <summary>
        /// Flash effect coroutine for completed body part
        /// </summary>
        private System.Collections.IEnumerator FlashCompletedPart(string partName)
        {
            float flashDuration = 0.5f;
            float flashIntensity = 2f;
            float elapsed = 0f;
            
            string flashPropName = $"_Flash_{partName}";
            
            // Flash in
            while (elapsed < flashDuration)
            {
                float t = elapsed / flashDuration;
                float intensity = Mathf.Lerp(0f, flashIntensity, t);
                UpdatePartFlashIntensity(flashPropName, intensity);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Flash out
            elapsed = 0f;
            while (elapsed < flashDuration)
            {
                float t = elapsed / flashDuration;
                float intensity = Mathf.Lerp(flashIntensity, 0f, t);
                UpdatePartFlashIntensity(flashPropName, intensity);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            UpdatePartFlashIntensity(flashPropName, 0f);
        }
        
        private void UpdatePartFlashIntensity(string propName, float intensity)
        {
            foreach (var mat in cachedMaterials)
            {
                if (mat != null && mat.HasProperty(propName))
                {
                    mat.SetFloat(propName, intensity);
                }
            }
        }
        
        /// <summary>
        /// Reset all body part colors to white (FFFFFF, intensity 0)
        /// </summary>
        private void ResetBodyPartColors()
        {
            var bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            foreach (var part in bodyParts)
            {
                part.ResetColor();
            }
            
            // Also reset shader properties on cached materials
            foreach (var mat in cachedMaterials)
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_HighlightIntensity")) mat.SetFloat("_HighlightIntensity", 0f);
                    if (mat.HasProperty("_FlashIntensity")) mat.SetFloat("_FlashIntensity", 0f);
                }
            }
        }

        public void GoToReception(Transform receptionPoint)
        {
            if (agent != null)
            {
                agent.enabled = true;
                agent.SetDestination(receptionPoint.position);
                currentState = CustomerState.Waiting;
            }
        }
        
        /// <summary>
        /// Navigate to a queue position while waiting, optionally via a waypoint
        /// </summary>
        public void GoToQueuePosition(Transform queuePos, Transform faceTarget = null, Transform waypoint = null)
        {
            if (agent != null && queuePos != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.updateRotation = true;
                currentState = CustomerState.Waiting;
                
                // If waypoint specified, go there first, then to queue position
                if (waypoint != null)
                {
                    StartCoroutine(MoveViaWaypoint(waypoint, queuePos));
                    Debug.Log($"[CustomerController] {data.customerName} moving to queue via waypoint");
                }
                else
                {
                    agent.SetDestination(queuePos.position);
                    Debug.Log($"[CustomerController] {data.customerName} moving directly to queue position");
                }
            }
        }
        
        private System.Collections.IEnumerator MoveViaWaypoint(Transform waypoint, Transform finalDestination)
        {
            if (agent == null) yield break;
            
            // First, go to waypoint
            agent.SetDestination(waypoint.position);
            Debug.Log($"[CustomerController] {data.customerName} STEP 1: Setting destination to waypoint {waypoint.name}");
            
            // Wait until we reach the waypoint using simple distance check
            float waypointReachedDistance = 1.0f;
            while (true)
            {
                if (agent == null || !agent.enabled) yield break;
                
                float distance = Vector3.Distance(transform.position, waypoint.position);
                
                if (distance < waypointReachedDistance)
                {
                    Debug.Log($"[CustomerController] {data.customerName} STEP 2: Reached waypoint! Distance: {distance:F2}m. Now going to final position {finalDestination.name}");
                    break;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // Small delay before changing destination
            yield return new WaitForSeconds(0.1f);
            
            // Then go to final destination
            if (agent != null && agent.enabled && finalDestination != null)
            {
                agent.SetDestination(finalDestination.position);
                Debug.Log($"[CustomerController] {data.customerName} STEP 3: Final destination set to {finalDestination.name}");
            }
        }

        public void StartTreatment()
        {
            currentState = CustomerState.InTreatment;
            agent.isStopped = true;
            // Trigger animation: Lie down
        }

        public void LeaveShop()
        {
            currentState = CustomerState.Leaving;
            agent.isStopped = false;
            agent.updateRotation = true; // Re-enable rotation for walking
            if (exitPoint != null)
            {
                agent.SetDestination(exitPoint.position);
            }
            else
            {
                Destroy(gameObject); // Fallback
            }
        }

        public void Pay(int amount)
        {
            EconomyManager.Instance.AddMoney(amount);
            Debug.Log($"{data.customerName} paid ${amount}.");
        }
        
        /// <summary>
        /// Calculate final payment based on completed requested parts only
        /// Uses baseBudget as price per body part
        /// </summary>
        public int CalculateFinalPayment()
        {
            int completedCount = 0;
            
            // Get HairTreatmentController to check actual completion
            var treatmentController = GetComponentInChildren<Treatment.HairTreatmentController>();
            if (treatmentController == null)
            {
                Debug.LogWarning("[CustomerController] No HairTreatmentController found for payment calculation");
                return 0;
            }
            
            // Get target part names from treatment plan
            var targetPartNames = treatmentController.GetTargetPartNames();
            
            foreach (var partName in targetPartNames)
            {
                if (treatmentController.IsPartCompleted(partName))
                {
                    completedCount++;
                    Debug.Log($"[CustomerController] Part {partName} is completed");
                }
            }
            
            // Calculate payment: baseBudget × completed parts
            int payment = completedCount * data.baseBudget;
            Debug.Log($"[CustomerController] Payment calculation: {completedCount}/{targetPartNames.Count} parts completed × ${data.baseBudget}/part = ${payment}");
            
            return payment;
        }
        
        public void CompleteTreatment()
        {
            if (IsCompleted) return; // Already completed

            Debug.Log($"Customer {data.customerName} treatment completed.");
            
            // Force mark all requested parts as completed to hide any remaining hair (98% threshold issue)
            ForceCompleteAllRequestedParts();
            
            // Mark as completed
            IsCompleted = true; 
            currentState = CustomerState.Completed;
            
            // Don't release bed yet - keep reference for door control
            
            // Ensure all decals are hidden
            var treatmentControllers = GetComponentsInChildren<Treatment.HairTreatmentController>();
            foreach (var controller in treatmentControllers)
            {
                controller.HideDecal();
            }
            
            // Set TreatmentFinished = true (customer is waiting for player to leave)
            if (animator != null && animator.enabled)
            {
                animator.SetBool("TreatmentFinished", true);
                Debug.Log("[CustomerController] TreatmentFinished = true, waiting for player to leave");
            }
            
            // Start checking if player leaves the bed area
            StartCoroutine(WaitForPlayerToLeave());
        }
        
        [Header("Curtain Settings")]
        [Tooltip("Distance player needs to be from bed before doors close")]
        public float playerLeaveDistance = 3f;
        
        /// <summary>
        /// Wait for player to leave the bed area, then start the departure sequence
        /// </summary>
        private System.Collections.IEnumerator WaitForPlayerToLeave()
        {
            Transform playerTransform = Camera.main?.transform;
            if (playerTransform == null || assignedBed == null)
            {
                // No player reference, just proceed after delay
                yield return new WaitForSeconds(2f);
                StartDepartureSequence();
                yield break;
            }
            
            // Wait until player is far enough from bed
            while (true)
            {
                float distance = Vector3.Distance(playerTransform.position, assignedBed.transform.position);
                if (distance > playerLeaveDistance)
                {
                    break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            Debug.Log("[CustomerController] Player left bed area, closing doors");
            StartDepartureSequence();
        }
        
        /// <summary>
        /// Start departure: close doors, stand up, open doors, walk to reception
        /// </summary>
        private void StartDepartureSequence()
        {
            if (assignedBed != null)
            {
                // Subscribe to door closed event
                assignedBed.OnDoorsClosed += OnDoorsClosedForDeparture;
                assignedBed.CloseDoors();
            }
            else
            {
                // No doors, proceed immediately
                StandUpSequence();
            }
        }
        
        private void OnDoorsClosedForDeparture()
        {
            if (assignedBed != null)
            {
                assignedBed.OnDoorsClosed -= OnDoorsClosedForDeparture;
            }
            StandUpSequence();
        }
        
        /// <summary>
        /// Customer stands up, then doors open, then walk to reception
        /// </summary>
        private void StandUpSequence()
        {
            // Set TreatmentFinished = false (stand up animation)
            if (animator != null && animator.enabled)
            {
                animator.SetBool("TreatmentFinished", false);
                animator.SetBool("IsLyingDown", false);
                animator.SetBool("IsLieDownFaceDown", false);
                Debug.Log("[CustomerController] Standing up");
            }
            
            // Wait a moment then open doors
            StartCoroutine(OpenDoorsAfterStandUp());
        }
        
        private System.Collections.IEnumerator OpenDoorsAfterStandUp()
        {
            // Wait for stand up animation
            yield return new WaitForSeconds(1.5f);
            
            if (assignedBed != null)
            {
                // Subscribe to door opened event
                assignedBed.OnDoorsOpened += OnDoorsOpenedForDeparture;
                assignedBed.OpenDoors();
            }
            else
            {
                // No doors, proceed immediately
                WalkToReception();
            }
        }
        
        private void OnDoorsOpenedForDeparture()
        {
            if (assignedBed != null)
            {
                assignedBed.OnDoorsOpened -= OnDoorsOpenedForDeparture;
                
                // Now release the bed
                assignedBed.ClearCustomer();
                assignedBed = null;
                Debug.Log("Bed released after departure.");
            }
            WalkToReception();
        }
        
        private void WalkToReception()
        {
            StandUpAndWalkToReception();
        }
        
        /// <summary>
        /// Force mark all requested parts as completed (sets _Completed_{PartName} = 1)
        /// This ensures all hair is hidden even if only 98% was removed
        /// Also marks parts as completed in HairTreatmentController for payment calculation
        /// </summary>
        private void ForceCompleteAllRequestedParts()
        {
            if (cachedMaterials == null || cachedMaterials.Count == 0)
            {
                Debug.LogWarning("[CompleteTreatment] No cached materials to update");
                return;
            }
            
            // Get treatment controller to mark parts as completed
            var treatmentController = GetComponentInChildren<Treatment.HairTreatmentController>();
            
            foreach (var partName in requestedPartNames)
            {
                // Update shader property
                string propName = $"_Completed_{partName}";
                foreach (var mat in cachedMaterials)
                {
                    if (mat != null && mat.HasProperty(propName))
                    {
                        mat.SetFloat(propName, 1.0f);
                    }
                }
                
                // Mark as completed in treatment controller for payment calculation
                if (treatmentController != null)
                {
                    treatmentController.ForcePartComplete(partName);
                }
                
                Debug.Log($"[CompleteTreatment] Forced complete: {partName}");
            }
        }
        
        private void StandUpAndWalkToReception()
        {
            // Re-enable NavMeshAgent
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            
            // Stand up (reverse lie down animation if applicable)
            if (animator != null)
            {
                animator.SetBool("IsLyingDown", false);
                animator.SetBool("IsLieDownFaceDown", false); // Reset to face-up
                // TreatmentFinished already set to false in TreatmentFinishedSequence
            }
            
            // Set final destination and state
            currentState = CustomerState.Paying;
            
            // Register with cash register to get queue position
            var cashRegister = FindObjectOfType<UI.CashRegister>();
            if (cashRegister != null)
            {
                Transform queuePos = cashRegister.RegisterCustomer(this);
                if (queuePos == null)
                {
                    Debug.LogWarning($"[CustomerController] Could not register to payment queue, going to register point");
                    if (agent != null && cashRegisterPoint != null)
                    {
                        agent.SetDestination(cashRegisterPoint.position);
                    }
                }
            }
            else
            {
                Debug.LogError("[CustomerController] CashRegister not found!");
                if (agent != null && cashRegisterPoint != null)
                {
                    agent.SetDestination(cashRegisterPoint.position);
                }
            }
        }
        
        private void ArriveAtReception()
        {
            currentState = CustomerState.Paying;
            paymentTimer = 0f;
            Debug.Log($"{data.customerName} arrived at cash register. Ready for payment...");
        }
        
        private void GoToExit()
        {
            currentState = CustomerState.Leaving;
            if (exitPoint != null)
            {
                agent.SetDestination(exitPoint.position);
                Debug.Log($"{data.customerName} heading to exit...");
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void RotateCustomer()
        {
            if (currentState != CustomerState.InTreatment) return;
            
            // Toggle face-down state
            isSupine = !isSupine;
            
            // If animator is available, use animation-based flip
            if (animator != null && animator.enabled)
            {
                animator.SetBool("IsLieDownFaceDown", !isSupine);
                Debug.Log($"[Animator] Set IsLieDownFaceDown = {!isSupine} ({(isSupine ? "Supine (face-up)" : "Prone (face-down)")})");
            }
            else
            {
                // Fallback: Transform rotation for non-animated customers
                if (!isRotating)
                {
                    isRotating = true;
                    targetRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                    Debug.Log($"[Transform] Rotating customer to {(isSupine ? "Supine (face-up)" : "Prone (face-down)")}");
                }
            }
        }
        [Header("Pain Settings")]
        [Range(0f, 1f)]
        public float painReactionProbability = 0.3f; // 30% chance to trigger pain reaction
        
        /// <summary>
        /// Add pain to customer. Returns true if pain reaction (animation/sound) was triggered.
        /// </summary>
        public bool AddPain(float amount)
        {
            currentPain += amount;
            currentPain = Mathf.Clamp(currentPain, 0, maxPain);
            Debug.Log($"Customer Pain: {currentPain}/{maxPain}");
            
            bool triggeredReaction = false;
            
            // Trigger pain animation with probability
            if (animator != null && amount > 0 && Random.value < painReactionProbability)
            {
                animator.SetTrigger("Pain");
                triggeredReaction = true;
            }
            
            if (currentPain >= maxPain)
            {
                Debug.LogWarning("Customer is in too much pain!");
                // TODO: Trigger angry leaving or complaint
            }
            
            return triggeredReaction;
        }

        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            if (currentState == CustomerState.Waiting)
            {
                Debug.Log("Player called customer.");
                
                // Find an empty bed
                var beds = FindObjectsOfType<Environment.BedController>();
                if (beds.Length == 0)
                {
                    Debug.LogError("No BedController found in scene! Please create beds using AssetGenerator.");
                    return;
                }

                foreach (var bed in beds)
                {
                    if (!bed.IsOccupied)
                    {
                        GoToBed(bed);
                        return;
                    }
                }
                
                Debug.LogWarning("All beds are occupied!");
            }
        }
        
        public bool IsReadyForTreatment => currentState == CustomerState.InTreatment;
        
        [Header("Door Detection")]
        [Tooltip("Distance to detect and open closed doors")]
        public float doorDetectionDistance = 1.5f;
        
        [Tooltip("Distance to consider arrived at bed (should be larger when using bed center)")]
        public float bedArrivalDistance = 1.5f;
        
        private bool waitingForDoorToOpen = false;
        
        /// <summary>
        /// Check if we're near closed doors and open them (stops while door opens)
        /// </summary>
        private void CheckAndOpenNearbyDoors()
        {
            if (assignedBed == null || waitingForDoorToOpen) return;
            
            // Check left door
            if (assignedBed.leftDoor != null && !assignedBed.leftDoor.IsOpen && !assignedBed.leftDoor.IsAnimating)
            {
                float dist = Vector3.Distance(transform.position, assignedBed.leftDoor.transform.position);
                if (dist < doorDetectionDistance)
                {
                    StopAndOpenDoor(assignedBed.leftDoor);
                    return;
                }
            }
            
            // Check right door
            if (assignedBed.rightDoor != null && !assignedBed.rightDoor.IsOpen && !assignedBed.rightDoor.IsAnimating)
            {
                float dist = Vector3.Distance(transform.position, assignedBed.rightDoor.transform.position);
                if (dist < doorDetectionDistance)
                {
                    StopAndOpenDoor(assignedBed.rightDoor);
                    return;
                }
            }
        }
        
        private void StopAndOpenDoor(Environment.CurtainDoor door)
        {
            waitingForDoorToOpen = true;
            
            // Stop walking
            if (agent != null)
            {
                agent.isStopped = true;
                animator?.SetFloat("Speed", 0f);
            }
            
            Debug.Log($"[CustomerController] Stopping to open door: {door.name}");
            
            // Subscribe to door open event
            door.OnDoorOpened += OnDoorOpenedWhileWalking;
            door.Open();
        }
        
        private void OnDoorOpenedWhileWalking()
        {
            // Unsubscribe from both doors just in case
            if (assignedBed?.leftDoor != null)
                assignedBed.leftDoor.OnDoorOpened -= OnDoorOpenedWhileWalking;
            if (assignedBed?.rightDoor != null)
                assignedBed.rightDoor.OnDoorOpened -= OnDoorOpenedWhileWalking;
            
            waitingForDoorToOpen = false;
            
            // Resume walking
            if (agent != null && currentState == CustomerState.WalkingToBed)
            {
                agent.isStopped = false;
                Debug.Log("[CustomerController] Door opened, resuming walk to bed");
            }
        }

        public void GoToBed(Environment.BedController bed)
        {
            if (bed == null) return;
            
            bed.AssignCustomer(this);
            assignedBed = bed; // Store reference

            if (bed.lieDownPoint != null)
            {
                targetBed = bed.lieDownPoint;
            }
            else
            {
                targetBed = bed.transform;
            }

            // Determine destination: use arrivalPoint (bed center) if available
            Vector3 destination = bed.arrivalPoint != null 
                ? bed.arrivalPoint.position 
                : bed.transform.position;

            // Start walking immediately - doors will be handled when customer gets close
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(destination);
            }
            
            currentState = CustomerState.WalkingToBed;
            Debug.Log("[CustomerController] Walking to bed");
        }
        
        private bool waitingForDoors = false;
        
        /// <summary>
        /// Start the bed arrival sequence: close doors, wait, then lie down
        /// </summary>
        private void StartBedArrivalSequence()
        {
            if (waitingForDoors) return; // Already waiting
            waitingForDoors = true;
            
            // Stop walking and wait for animation to settle before continuing
            StartCoroutine(StopWalkingAndArrive());
        }
        
        /// <summary>
        /// Stop walking animation and wait before arriving at bed
        /// </summary>
        private System.Collections.IEnumerator StopWalkingAndArrive()
        {
            // Stop agent and walking animation
            if (agent != null)
            {
                agent.isStopped = true;
            }
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
            
            // Wait for animation to fully stop (1 frame + small delay)
            yield return null;
            yield return new WaitForSeconds(0.1f);
            
            // Now proceed with door closing and arrival
            if (assignedBed != null)
            {
                if (assignedBed.AreAllDoorsClosed && !assignedBed.AreDoorsAnimating)
                {
                    Debug.Log("[CustomerController] Doors already closed, proceeding to lie down");
                    waitingForDoors = false;
                    ArriveAtBed();
                }
                else
                {
                    assignedBed.OnDoorsClosed += OnDoorsClosedForArrival;
                    assignedBed.CloseDoors();
                    Debug.Log("[CustomerController] Closing curtain doors before lying down");
                }
            }
            else
            {
                waitingForDoors = false;
                ArriveAtBed();
            }
        }
        
        private void OnDoorsClosedForArrival()
        {
            if (assignedBed != null)
            {
                assignedBed.OnDoorsClosed -= OnDoorsClosedForArrival;
            }
            waitingForDoors = false;
            ArriveAtBed();
        }

        private void ArriveAtBed()
        {
            Debug.Log($"[CustomerController] ArriveAtBed() CALLED! targetBed={targetBed}");
            if (targetBed == null) return;

            // Disable agent for manual positioning
            if (agent != null) agent.enabled = false;

            // Smoothly move to lieDownPoint over 0.3 seconds
            StartCoroutine(SmoothMoveToLieDownPosition());
        }
        
        /// <summary>
        /// Smoothly move to lieDownPoint position and rotation over 0.3 seconds
        /// </summary>
        private System.Collections.IEnumerator SmoothMoveToLieDownPosition()
        {
            // FIRST: Start lie down animation
            if (animator != null && animator.enabled)
            {
                animator.SetFloat("Speed", 0f); // Stop walk animation
                animator.SetBool("IsLieDownFaceDown", false); // Start face-up
                animator.SetBool("IsLyingDown", true);
                Debug.Log("[CustomerController] Started LieDown animation");
            }
            
            // Wait a bit for animation to start transitioning
            yield return new WaitForSeconds(0.1f);
            
            // THEN: Smoothly adjust position/rotation
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 targetPos;
            Quaternion targetRot = Quaternion.identity; // (0, 0, 0)
            
            if (assignedBed != null && assignedBed.lieDownPoint != null)
            {
                targetPos = assignedBed.lieDownPoint.position;
            }
            else
            {
                targetPos = targetBed.position;
            }
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }
            
            // Snap to final position/rotation
            transform.position = targetPos;
            transform.rotation = targetRot;
            Debug.Log($"[CustomerController] Position/rotation adjusted: pos={targetPos}, rot=(0,0,0)");
            
            // Delay BakeMesh to allow animation to fully settle
            if (animator != null && animator.enabled)
            {
                StartCoroutine(DelayedBakeMesh(0.5f));
            }
            else
            {
                // Fallback: Rotate to lie down (adjust as needed based on model)
                transform.Rotate(-90, 0, 0); // Lie on back (usually -90 or 90 depending on pivot)
              //  transform.position += Vector3.up * 0.5f; // Adjust height for lying
            }
            
            currentState = CustomerState.InTreatment;
            Debug.Log("Customer arrived at bed and is ready for treatment.");
            
            // NOTE: BakeMesh is now called via Animation Event (OnLieDownComplete)
            
            // Setup requested parts for Q-key highlighting (but don't highlight automatically)
            SetupRequestedPartsForHighlighting();
            
            // Start Treatment Session
            TreatmentManager.Instance.StartSession(this);
        }
        
        /// <summary>
        /// Called by Animation Event when lie-down animation completes
        /// Bakes mesh colliders for accurate hit detection
        /// </summary>
        public void OnLieDownComplete()
        {
            var bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            foreach (var part in bodyParts)
            {
                part.BakeMeshForCollider();
            }
            
            Debug.Log($"[CustomerController] OnLieDownComplete: Baked mesh colliders for {bodyParts.Length} body parts");
        }
        
        /// <summary>
        /// Delay BakeMesh to allow animation to settle
        /// </summary>
        private System.Collections.IEnumerator DelayedBakeMesh(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnLieDownComplete();
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public string GetInteractionPrompt()
        {
            return currentState == CustomerState.Waiting ? "Call Customer" : "";
        }

        private void Update()
        {
            // Sync animation speed with NavMeshAgent velocity
            if (animator != null && agent != null && agent.enabled)
            {
                float speed = agent.velocity.magnitude;
                animator.SetFloat("Speed", speed);
            }
            
            // Handle smooth rotation
            if (isRotating)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime / rotationDuration);
                
                // Check if rotation is complete
                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
                {
                    transform.rotation = targetRotation;
                    isRotating = false;
                }
            }
            
            // State-specific behavior
            if (currentState == CustomerState.WalkingToBed)
            {
                if (agent != null && agent.isOnNavMesh && !agent.pathPending && !waitingForDoorToOpen)
                {
                    // Check if we're near a closed door and need to open it
                    CheckAndOpenNearbyDoors();
                    
                    // Don't check arrival if we just started opening a door
                    if (waitingForDoorToOpen) return;
                    
                    // Check if arrived at bed (using bedArrivalDistance for center-based detection)
                    if (agent.remainingDistance <= agent.stoppingDistance + bedArrivalDistance)
                    {
                        // Start door closing sequence, ArriveAtBed will be called when doors are closed
                        StartBedArrivalSequence();
                    }
                }
            }
            else if (currentState == CustomerState.WalkingToReception)
            {
                if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    ArriveAtReception();
                }
            }
            else if (currentState == CustomerState.Waiting)
            {
                // Stop moving/rotating when arrived at queue position
                if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    agent.isStopped = true;
                    agent.updateRotation = false;
                }
            }
            else if (currentState == CustomerState.Paying)
            {
                // Stop moving/rotating when arrived at cash register queue
                if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    agent.isStopped = true;
                    agent.updateRotation = false;
                }
                // Auto-payment disabled - payment now happens via CashRegister interaction
                /* OLD AUTO-PAYMENT CODE:
                paymentTimer += Time.deltaTime;
                if (paymentTimer >= paymentDelay)
                {
                    // Calculate and process payment
                    int payment = CalculatePayment();
                    Pay(payment);
                    GoToExit();
                }
                */
                // Customer waits at cash register until player interacts
            }
            else if (currentState == CustomerState.Leaving)
            {
                if (agent != null && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    Debug.Log($"{data.customerName} has left the shop.");
                    
                    // Return to pool instead of Destroy
                    if (spawner != null)
                    {
                        spawner.ReturnToPool(this);
                    }
                    else
                    {
                        Debug.LogWarning("[CustomerController] No spawner reference! Destroying instead.");
                        Destroy(gameObject);
                    }
                }
            }
        }
        
        
        private int CalculatePayment()
        {
            // Simple calculation based on budget
            // TODO: Factor in satisfaction, treatment quality, etc.
            return data.baseBudget;
        }
        private void OnDestroy()
        {
            Debug.Log($"CustomerController on {gameObject.name} was destroyed.");
            
            // Safety: Release bed if still assigned
            if (assignedBed != null)
            {
                assignedBed.ClearCustomer();
                assignedBed = null;
            }
        }
    }
}
