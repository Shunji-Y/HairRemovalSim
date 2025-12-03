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
            // Initialize BodyParts
            HairRemovalSim.Core.BodyPart[] bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            foreach (HairRemovalSim.Core.BodyPart part in bodyParts)
            {
                part.Initialize();
            }
        }
        


        public void Initialize(CustomerData newData, Transform exit, Transform reception, Transform cashRegister)
        {
            data = newData;
            exitPoint = exit;
            receptionPoint = reception;
            cashRegisterPoint = cashRegister;
        }

        public void GoToReception(Transform receptionPoint)
        {
            agent.SetDestination(receptionPoint.position);
            currentState = CustomerState.Waiting;
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
            
            // Calculate payment: completed parts only
            int payment = completedCount * data.pricePerBodyPart;
            Debug.Log($"[CustomerController] Payment calculation: {completedCount}/{data.requestedBodyParts.Count} parts completed = ${payment}");
            
            return payment;
        }
        
        public void CompleteTreatment()
        {
            Debug.Log($"{data.customerName} treatment complete. Standing up...");
            currentState = CustomerState.Completed;
            
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
                agent.isStopped = false;
            }
            
            // Reset rotation (stand up)
            if (animator != null)
            {
                animator.SetBool("IsLyingDown", false);
            }
            else
            {
                // Fallback: reset rotation
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
            
            // Walk to cash register (not reception!)
            if (cashRegisterPoint != null)
            {
                agent.SetDestination(cashRegisterPoint.position);
                currentState = CustomerState.WalkingToReception;
                Debug.Log($"{data.customerName} walking to cash register...");
            }
            else
            {
                Debug.LogWarning("No cash register point set. Customer leaving directly.");
                LeaveShop();
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
                    Destroy(gameObject);
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
