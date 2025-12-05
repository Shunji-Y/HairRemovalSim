using UnityEngine;
using UnityEngine.AI;
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

        [Header("Status")]
        public float currentPain = 0f;
        public float maxPain = 100f;
        public bool IsCompleted = false; // Track if treatment is complete

        private CustomerState currentState;
        public CustomerState CurrentState => currentState; // Public read-only access
        
        private Transform targetBed;
        private Environment.BedController assignedBed; // Track which bed this customer is using
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
        /// Highlight requested body parts with orange glow (called when arriving at bed)
        /// </summary>
        private void HighlightRequestedParts()
        {
            foreach (var part in data.requestedBodyParts)
            {
                var renderer = part.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterials != null)
                {
                    // Set HDR orange color (FFBE4A) with emission intensity 2.0
                    Color glowColor = new Color(1.0f, 0.745f, 0.29f, 1.0f) * 2.0f;
                    
                    // Handle multiple materials - create instances for all
                    Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        newMaterials[i] = new Material(renderer.sharedMaterials[i]);
                        newMaterials[i].SetColor("_BodyColor", glowColor);
                    }
                    renderer.materials = newMaterials;
                    
                    Debug.Log($"[CustomerController] Highlighted {part.partName} with orange glow (intensity 2.0) on {newMaterials.Length} material(s)");
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
                var renderer = part.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    // Set to white with no emission
                    Color whiteColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // No intensity multiplier = 0
                    renderer.material.SetColor("_BodyColor", whiteColor);
                    
                    Debug.Log($"[CustomerController] Reset {part.partName} color to white");
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
            
            // Count requested parts that are completed
            foreach (var requestedPart in data.requestedBodyParts)
            {
                if (requestedPart != null && requestedPart.CompletionPercentage >= 100f)
                {
                    completedCount++;
                }
            }
            
            // Calculate payment: baseBudget × completed parts
            int payment = completedCount * data.baseBudget;
            Debug.Log($"[CustomerController] Payment calculation: {completedCount}/{data.requestedBodyParts.Count} parts completed × ${data.baseBudget}/part = ${payment}");
            
            return payment;
        }
        
        public void CompleteTreatment()
        {
            if (IsCompleted) return; // Already completed

            Debug.Log($"Customer {data.customerName} treatment completed.");
            
            // Note: Body part colors are now reset individually when each part reaches 100%
            
            // Mark as completed and set state to start walking to reception after delay
            IsCompleted = true; 
            currentState = CustomerState.WalkingToReception; // Transition to walking to reception
            
            // Start a timer before moving to reception to allow for visual feedback/animation
            paymentTimer = paymentDelay; 
            
            // Release bed
            if (assignedBed != null)
            {
                assignedBed.ClearCustomer();
                assignedBed = null;
                Debug.Log("Bed released.");
            }
            
            StandUpAndWalkToReception();
        }
        
        private void StandUpAndWalkToReception()
        {
            // Re-enable NavMeshAgent
            if (agent != null)
            {
                agent.enabled = true;
            }
            
            // Stand up (reverse lie down animation if applicable)
            if (animator != null)
            {
                animator.SetBool("IsLyingDown", false);
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
            if (currentState != CustomerState.InTreatment || isRotating) return;
            
            isSupine = !isSupine;
            isRotating = true;
            
            // Calculate target rotation (180 degrees around LOCAL Y-axis)
            targetRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            
            Debug.Log($"Rotating customer to {(isSupine ? "Supine (face-up)" : "Prone (face-down)")}");
        }

        public void AddPain(float amount)
        {
            currentPain += amount;
            currentPain = Mathf.Clamp(currentPain, 0, maxPain);
            Debug.Log($"Customer Pain: {currentPain}/{maxPain}");
            
            if (currentPain >= maxPain)
            {
                Debug.LogWarning("Customer is in too much pain!");
                // TODO: Trigger angry leaving or complaint
            }
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

            //Debug.Log($"Customer moving to bed {bed.name}...");
            
            // Enable agent and set destination
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.SetDestination(bed.transform.position);
            }
            
            currentState = CustomerState.WalkingToBed;
        }

        private void ArriveAtBed()
        {

            if (targetBed == null) return;

            // Disable agent for manual positioning
            if (agent != null) agent.enabled = false;

            // Snap to bed position
            // Adjust position based on bed height (approx 1.0f for now)
            transform.position = targetBed.position; 
            transform.rotation = targetBed.rotation;
            
            if (animator != null)
            {
                animator.SetBool("IsLyingDown", true);
                // Ensure we don't rotate via transform if animation handles it, 
                // BUT usually lie down animations are in local space, so we might still need to orient the root.
                // For now, let's assume the animation keeps the root rotation or we rotate the root to match bed.
                // If the animation is "standing to lying", the root stays. 
                // If it's a static "lying" pose, we might need to rotate the root if the pose is upright in model space.
                // Let's assume a standard "Lie Down" animation state.
            }
            else
            {
                // Fallback: Rotate to lie down (adjust as needed based on model)
                transform.Rotate(-90, 0, 0); // Lie on back (usually -90 or 90 depending on pivot)
              //  transform.position += Vector3.up * 0.5f; // Adjust height for lying
            }
            
            currentState = CustomerState.InTreatment;
            Debug.Log("Customer arrived at bed and is ready for treatment.");
            
            // Highlight requested body parts with orange glow
            HighlightRequestedParts();
            
            // Start Treatment Session
            TreatmentManager.Instance.StartSession(this);
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public string GetInteractionPrompt()
        {
            return currentState == CustomerState.Waiting ? "Call Customer" : "";
        }

        private void Update()
        {
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
                if (agent != null && !agent.pathPending)
                {
                    
                    // Debug.Log($"Dist: {agent.remainingDistance}, Stop: {agent.stoppingDistance}");
                    if (agent.remainingDistance <= agent.stoppingDistance + 1.5f)
                    {
                        ArriveAtBed();
                    }
                }
            }
            else if (currentState == CustomerState.WalkingToReception)
            {
                if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    ArriveAtReception();
                }
            }
            else if (currentState == CustomerState.Paying)
            {
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
