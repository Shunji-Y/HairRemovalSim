using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;
using HairRemovalSim.Core.Effects;
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
        public NavMeshObstacle obstacle; // For dynamic avoidance when stopped
        public BodyPartsDatabase bodyPartsDatabase; // UV-based body part system

        [Header("Status")]
        public float currentPain = 0f;
        public float maxPain = 100f;
        public bool IsCompleted = false; // Track if treatment is complete
        
        [Header("Review")]
        [SerializeField] private int baseReviewValue = 30; // Random 10-50 on spawn
        private int painMaxCount = 0; // How many times pain hit 100%
        
        [Header("Staff Processing")]
        [SerializeField] private float staffReviewCoefficient = 1f; // Applied when staff handles
        [SerializeField] private bool upsellSuccess = false; // True if staff upsell succeeded
        
        [Header("Additional Budget (Upsell)")]
        [SerializeField] private int additionalBudget = 0;
        public int AdditionalBudget => additionalBudget;
        
        [Header("Obstacle Settings")]
        [Tooltip("Radius of the NavMeshObstacle when stopped")]
        [SerializeField] private float obstacleRadius = 0.35f;
        [Tooltip("Height of the NavMeshObstacle")]
        [SerializeField] private float obstacleHeight = 2.0f;
        [Tooltip("Center offset of the NavMeshObstacle")]
        [SerializeField] private Vector3 obstacleCenter = new Vector3(0, 1f, 0);

        [Header("Wait Time Gauge")]
        [Tooltip("Default max wait time for each location (seconds)")]
        [SerializeField] private float defaultMaxWaitTime = 30f;
        [SerializeField] private WaitTimeGaugeUI waitTimeGauge;
        private float waitTimer = 0f;
        private float maxWaitTime = 30f;
        private bool isWaiting = false; // True if waiting timer should run
        private bool hasShownWaitWarning = false; // Track if 80% warning was shown
        
        [Header("Applied Effects")]
        private EffectContext appliedEffects; // Effects applied from reception items
        private Core.ItemData appliedReceptionItem; // Item applied at reception (for icon display)
        
        /// <summary>
        /// Get the reception item applied to this customer (for TreatmentPanel icon display)
        /// </summary>
        public Core.ItemData AppliedReceptionItem => appliedReceptionItem;

        private CustomerState currentState;
        public CustomerState CurrentState => currentState; // Public read-only access
        
        private Transform targetBed;
        public Environment.BedController assignedBed; // Track which bed this customer is using
        private Transform exitPoint;
        private Transform receptionPoint; // Pre-treatment reception
        private Transform cashRegisterPoint; // Post-treatment payment
        private CustomerSpawner spawner; // Reference to spawner for pool return
        
        [Header("Chair")]
        private Environment.Chair currentChair; // Chair this customer is currently occupying
        public Environment.Chair CurrentChair => currentChair;
        
        [Header("Initialization")]
        public bool isInitialized = false; // Track if BodyParts are initialized
        private bool isFailedTreatment = false; // Track if leaving due to failed treatment (skip payment)
        
        [Header("Payment Settings")]
        public float paymentDelay = 2.0f;
        private float paymentTimer = 0f;

        [Header("Visuals")]
        public Animator animator;
        private static readonly int SittingHash = Animator.StringToHash("Sitting");
        
        [Header("Clothing")]
        [Tooltip("Shirt/top clothing - hidden when ARM, CHEST, ABS, BACK, or ARMPIT is requested")]
        [SerializeField] private GameObject shirtObject;
        [Tooltip("Pants - hidden when LEG is requested")]
        [SerializeField] private GameObject pantsObject;
        [Tooltip("Boxer/underwear - shown when pants are hidden, hidden when pants are shown")]
        [SerializeField] private GameObject boxerObject;
        [Tooltip("Shoes - always hidden when lying down")]
        [SerializeField] private GameObject shoesObject;

        [SerializeField] private HairRemovalSim.Core.BodyPart bodyPart;

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
            
            // Auto-find BodyPart if not assigned in Inspector
            if (bodyPart == null)
            {
                bodyPart = GetComponentInChildren<HairRemovalSim.Core.BodyPart>();
            }
        }


        private void Start()
        {
            // Initialize BodyParts over multiple frames to avoid frame rate drop
            StartCoroutine(InitializeBodyPartsAsync());
        }

        private Coroutine movementCoroutine;

        private System.Collections.IEnumerator StandUpDelay()
        {
            // If currently sitting, wait 1 second before standing up (User request)
            if (animator != null && animator.GetBool(SittingHash))
            {
                yield return new WaitForSeconds(1.0f);
                SetSitting(false);
            }
        }
        
        private System.Collections.IEnumerator PrepareForMovement(bool skipReleaseChair = false)
        {
            // 1. Wait for stand up delay (User request)
            yield return StartCoroutine(StandUpDelay());

            // 2. Stand up animation
            SetSitting(false);
            
            // 3. Release logical chair occupancy (unless caller handles it)
            if (!skipReleaseChair)
            {
                ReleaseChair();
            }
            
            // 4. Disable obstacle and wait for NavMesh update
            if (obstacle != null && obstacle.enabled)
            {
                obstacle.enabled = false;
                yield return null; // Critical wait for NavMesh update (prevents warping)
            }
            
            // 5. Enable agent and ensure proper NavMesh placement
            if (agent != null)
            {
                agent.enabled = true;
                
                // Warp to current position to ensure NavMesh placement
                // (essential for customers coming from beds which may be off-NavMesh)
                agent.Warp(transform.position);
                
                // If not on NavMesh, try to find nearest valid point
                if (!agent.isOnNavMesh)
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
                
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
                else
                {
                    Debug.LogError($"[CustomerController] {data?.customerName} failed to place on NavMesh in PrepareForMovement!");
                }
            }
        }
        
        private System.Collections.IEnumerator InitializeBodyPartsAsync()
        {
            //   HairRemovalSim.Core.BodyPart[] bodyParts = GetComponentsInChildren<HairRemovalSim.Core.BodyPart>();
            yield return null;

            bodyPart.Initialize();
            int layer = LayerMask.NameToLayer("Customer");
            bodyPart.gameObject.layer = layer;
            //foreach (HairRemovalSim.Core.BodyPart part in bodyParts)
            //{
            //    part.Initialize();
            //    count++;
                
            //    // Wait one frame between each initialization to spread the load
            //    yield return null;
            //}
            
            isInitialized = true; // Mark as initialized
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
            
            // Initialize additional budget based on wealth level
            InitializeAdditionalBudget();
            
            // Configure NavMeshAgent for better avoidance
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Random priority helps agents resolve collisions (lower number = higher priority)
                // Only set priority if you want random variation, otherwise respect inspector?
                // For now, let's keep priority randomization but REMOVE radius/type override so Inspector settings work
                agent.avoidancePriority = Random.Range(30, 70);
                
                // REMOVED: agent.obstacleAvoidanceType = ... (Let user set in Inspector)
                // REMOVED: agent.radius = ... (Let user set in Inspector, user wants 0.1f)
            }
            
            // Configure NavMeshObstacle - MUST disable agent first to avoid conflict!
            if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                // Temporarily disable agent to avoid "both active" warning
                bool wasAgentEnabled = agent != null && agent.enabled;
                if (agent != null) agent.enabled = false;
                
                obstacle = gameObject.AddComponent<NavMeshObstacle>();
                obstacle.enabled = false; // Disable immediately!
                
                // Re-enable agent
                if (agent != null) agent.enabled = wasAgentEnabled;
            }
            
            if (obstacle != null)
            {
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.center = obstacleCenter;
                obstacle.radius = obstacleRadius; 
                obstacle.height = obstacleHeight;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false; // Carve immediately when enabled
                obstacle.enabled = false; // Improved performance: only enable when stopped
            }
            
            // Ensure agent is active by default
            SetAgentActive(true);
            
            // Mark as initialized (for pooled customers that don't call Start again)
            isInitialized = true;
        }
        
        /// <summary>
        /// Toggle between NavMeshAgent (moving) and NavMeshObstacle (stationary)
        /// </summary>
        private void SetAgentActive(bool isActive)
        {
            if (isActive)
            {
                // To move: disable obstacle first, then enable agent
                if (obstacle != null) obstacle.enabled = false;
                
                // Wait one frame? usually okay to do immediately if order is correct
                if (agent != null) 
                {
                    agent.enabled = true;
                    agent.isStopped = false;
                }
            }
            else
            {
                // To stop: disable agent first, then enable obstacle
                if (agent != null) 
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }
                
                // Enable obstacle only when few customers (≤10) to avoid PathInvalid issues
                int activeCount = CustomerSpawner.Instance?.ActiveCustomerCount ?? 0;
                if (obstacle != null && activeCount <= 10)
                {
                    obstacle.enabled = true;
                }
            }
        }

        /// <summary>
        /// Initialize additional budget based on customer's rank data
        /// </summary>
        private void InitializeAdditionalBudget()
        {
            if (data == null)
            {
                additionalBudget = 0;
                return;
            }
            
            // Use CustomerRankData budget range if available
            if (data.rankData != null)
            {
                additionalBudget = data.rankData.GetRandomBudget();
                Debug.Log($"[Customer] {data.customerName} budget from rank: {additionalBudget} (range: {data.rankData.budgetMin}-{data.rankData.budgetMax})");
            }
            else
            {
                // Fallback: Budget ranges per wealth level (legacy)
                (int min, int max) range = data.wealth switch
                {
                    WealthLevel.Poorest => (15, 23),
                    WealthLevel.Poor => (20, 35),
                    WealthLevel.Normal => (40, 60),
                    WealthLevel.Rich => (80, 120),
                    WealthLevel.Richest => (150, 250),
                    _ => (15, 23)
                };
                
                additionalBudget = Random.Range(range.min, range.max + 1);
                Debug.Log($"[Customer] {data.customerName} budget from fallback: {additionalBudget} (wealth: {data.wealth})");
            }
        }
        
        /// <summary>
        /// Consume additional budget (called on successful upsell at checkout only)
        /// </summary>
        public void ConsumeAdditionalBudget(int amount)
        {
            additionalBudget = Mathf.Max(0, additionalBudget - amount);
        }
        
        /// <summary>
        /// Get total budget (plan price + additional budget) for display
        /// </summary>
        public int GetTotalBudget()
        {
            int planPrice = data != null ? CustomerPlanHelper.GetPlanPrice(data.requestPlan) : 0;
            return planPrice + additionalBudget;
        }
        
        /// <summary>
        /// Calculate upsell success rate based on total spending vs total budget
        /// Formula: Clamp(2 - (totalSpending / totalBudget), 0, 1)
        /// where totalSpending = already spent (confirmedPrice) + new upsell price
        /// and totalBudget = plan price + additional budget (customer's fixed budget)
        /// </summary>
        public float CalculateUpsellSuccessRate(int itemPrice)
        {
            // Customer's fixed total budget (plan price + additional budget)
            int planPrice = data != null ? CustomerPlanHelper.GetPlanPrice(data.requestPlan) : 0;
            int totalBudget = planPrice + additionalBudget;
            
            // Already spent at reception (confirmedPrice includes reception upsells)
            int alreadySpent = data != null && data.confirmedPrice > 0 
                ? data.confirmedPrice 
                : planPrice;
            
            // Total spending if this upsell succeeds
            int totalSpending = alreadySpent + itemPrice;
            
            if (totalBudget <= 0) return 0f;
            if (itemPrice <= 0) return 1f;
            
            float rate = 2f - ((float)totalSpending / totalBudget);
            return Mathf.Clamp01(rate);
        }
        
        /// <summary>
        /// Calculate review penalty for failed upsell
        /// Formula: Min(5 / successRate, 50) - returns POSITIVE value (will be subtracted from review)
        /// </summary>
        public int CalculateUpsellFailurePenalty(float successRate)
        {
            if (successRate <= 0.01f) return 50; // Avoid division by near-zero
            float penalty = 3f / successRate;
            return Mathf.Min(Mathf.RoundToInt(penalty), 50);
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
            
            // Reset animator state for pool reuse
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetBool("IsLyingDown", false);
                animator.SetBool("IsLieDownFaceDown", false);
                animator.SetBool("TreatmentFinished", false);
                animator.Rebind(); // Force reset to initial state
            }
            
            // Reset pain state
            currentPain = 0f;
            isInPainStage3 = false;
            hasAppliedReviewPenalty = false;
            painHoldTimer = 0f;
            
            // Reset review state and generate new base value
            painMaxCount = 0;
            baseReviewValue = Random.Range(10, 51); // 10-50 random
            
            // Reset applied effects from reception items
            appliedEffects = null;
            appliedReceptionItem = null;
            
            // Reset HairTreatmentControllers for pool reuse
            var treatmentControllers = GetComponentsInChildren<HairTreatmentController>();
            foreach (var controller in treatmentControllers)
            {
                controller.ResetForReuse();
            }
            
            // Reset completed parts counter
            completedPartCount = 0;
            
            // Reset CustomerData treatment state for pool reuse
            if (data != null)
            {
                data.confirmedParts = TreatmentBodyPart.None;
                data.confirmedMachine = TreatmentMachine.Shaver;
                data.confirmedPrice = 0;
                data.useAnesthesiaCream = false;
                data.reviewPenalty = 0;
            }
            
            // Reset initialization flag so Bake will be called again
            isInitialized = false;
            isFailedTreatment = false;
            
            // Reset clothing to visible state
            SetClothingForStanding();
            
            // Reset wait timer
            waitTimer = 0f;
            isWaiting = false;
            maxWaitTime = defaultMaxWaitTime;
            
            // Reset wait time gauge UI
            if (waitTimeGauge != null)
            {
                waitTimeGauge.ResetGauge();
            }
            
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
            
            // Use confirmedParts from reception if available
            string[] targetPartNames;
            
            if (data != null && data.confirmedParts != TreatmentBodyPart.None)
            {
                // New system: use confirmed parts from reception
                targetPartNames = CustomerPlanHelper.GetDetailedTreatmentParts(data.confirmedParts);
            }
            else if (data != null && data.selectedTreatmentPlan != TreatmentPlan.None && bodyPartsDatabase != null)
            {
                // Fallback: old UV-based system
                var targetParts = data.selectedTreatmentPlan.GetBodyPartDefinitions(bodyPartsDatabase);
                if (targetParts == null || targetParts.Count == 0)
                {
                    return;
                }
                targetPartNames = new string[targetParts.Count];
                for (int i = 0; i < targetParts.Count; i++)
                {
                    targetPartNames[i] = targetParts[i].partName;
                }
            }
            else
            {
                return;
            }
            
            // Get renderer from the object that has BodyPart component
            var bodyPart = GetComponentInChildren<HairRemovalSim.Core.BodyPart>();
            if (bodyPart == null)
            {
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
                return;
            }
            
            
            // Store requested part names for completion tracking
            requestedPartNames.Clear();
            requestedPartNames.AddRange(targetPartNames);
            
            // Apply to each material - set Request flags for target parts
            Material[] materials = cachedRenderer.materials;
            cachedMaterials.Clear();
            
            foreach (var mat in materials)
            {
                cachedMaterials.Add(mat);
                
                // Reset all request/completed flags first
                ResetAllPartFlags(mat);
                
                // Set request flags for target parts
                foreach (var partName in targetPartNames)
                {
                    string propName = $"_Request_{partName}";
                    mat.SetFloat(propName, 1.0f);
                }
                
                // Start with highlight OFF - Q key will turn it on
                mat.SetFloat("_HighlightIntensity", 0f);
            }
            
            // Assign modified materials back to renderer
            cachedRenderer.materials = materials;
            
            // Reset completion counter
            completedPartCount = 0;
            
            // Setup Treatment Controllers with target part names
            var treatmentControllers = GetComponentsInChildren<Treatment.HairTreatmentController>();
            foreach (var controller in treatmentControllers)
            {
                controller.SetTargetBodyPartNames(targetPartNames, bodyPartsDatabase);
                controller.OnPartCompleted -= OnBodyPartCompleted;
                controller.OnPartCompleted += OnBodyPartCompleted;
            }
            
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
        /// Go directly to cash register (for checkout test mode)
        /// </summary>
        public void GoToCashRegister()
        {
            if (agent != null && cashRegisterPoint != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(cashRegisterPoint.position);
                currentState = CustomerState.WalkingToReception; // Use existing state for going to payment
                

            }
        }
        
        public bool IsSitting => animator != null && animator.GetBool(SittingHash);

        /// <summary>
        /// Set sitting animation state
        /// </summary>
        public void SetSitting(bool sitting)
        {
            if (animator != null)
            {
                animator.SetBool(SittingHash, sitting);
                Debug.Log($"[CustomerController] {data?.customerName} SetSitting({sitting})");
            }
        }
        
        // Track queue arrival coroutine to prevent duplicates
        private Coroutine queueArrivalCoroutine;
        
        /// <summary>
        /// Navigate to a queue position while waiting (sits after arrival)
        /// </summary>
        public void GoToQueuePosition(Transform queuePos, Transform faceTarget = null, bool shouldSit = true)
        {
            if (agent != null && queuePos != null)
            {
                // Stop previous queue arrival coroutine
                if (queueArrivalCoroutine != null)
                {
                    StopCoroutine(queueArrivalCoroutine);
                    queueArrivalCoroutine = null;
                }
                
                // Stand up if currently sitting
                SetSitting(false);
                
                agent.enabled = true;
                agent.isStopped = false;
                agent.updateRotation = true;
                currentState = CustomerState.Waiting;
                agent.SetDestination(queuePos.position);
                
                // Start coroutine to check arrival, rotate, and sit
                queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(queuePos, shouldSit));
            }
        }
        
        /// <summary>
        /// Navigate to counter point (reception/cashier) - stands, does not sit
        /// </summary>
        /// <param name="counterPos">Target position</param>
        /// <param name="onArrival">Optional callback when customer arrives</param>
        public void GoToCounterPoint(Transform counterPos, System.Action onArrival = null)
        {
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToCounterPointRoutine(counterPos, onArrival));
        }

        private System.Collections.IEnumerator GoToCounterPointRoutine(Transform counterPos, System.Action onArrival = null)
        {
            if (agent != null && counterPos != null)
            {
                yield return StartCoroutine(PrepareForMovement());

                // Stop previous queue arrival coroutine
                if (queueArrivalCoroutine != null)
                {
                    StopCoroutine(queueArrivalCoroutine);
                    queueArrivalCoroutine = null;
                }
                
                agent.updateRotation = true;
                currentState = CustomerState.Waiting;
                agent.SetDestination(counterPos.position);
                
                // Rotate to match point but don't sit, call onArrival when done
                queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(counterPos, false, onArrival));
            }
        }
        

        
        
        /// <summary>
        /// Wait for arrival at queue position, smoothly rotate, and optionally sit
        /// </summary>
        /// <param name="onArrival">Optional callback when customer arrives and is aligned</param>
        private System.Collections.IEnumerator WaitForQueueArrivalAndRotate(Transform queuePos, bool shouldSit = true, System.Action onArrival = null)
        {
            if (agent == null || queuePos == null) yield break;
            
            // Wait for path to be calculated
            yield return null;
            int waitFrames = 0;
            while (agent != null && agent.enabled && agent.pathPending)
            {
                waitFrames++;
                yield return null;
            }
            Debug.Log($"[CustomerController] Path calculation done in {waitFrames} frames. Status: {agent?.pathStatus}");
            
            if (agent != null && agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"[CustomerController] Path status is {agent.pathStatus} (Partial/Invalid) - may get stuck!");
                // Don't break immediately, let it try to move as far as possible? 
                // Or maybe this IS the problem. If invalid, remainingDistance might be 0.
            }

            // Wait until arrived
            float timeout = 30f;
            float elapsed = 0f;
            float arrivalDist = 0.5f; // Relaxed arrival distance to avoid getting stuck

            while (agent != null && agent.enabled && elapsed < timeout)
            {
                // Use BOTH NavMesh remainingDistance AND actual world distance
                // remainingDistance can be 0 when path is invalid or just after Warp
                float actualDist = Vector3.Distance(transform.position, queuePos.position);
                bool closeByNavMesh = !agent.pathPending && agent.remainingDistance <= arrivalDist;
                bool closeByWorld = actualDist <= arrivalDist + 0.5f; // Slightly relaxed for world check
                
                if (closeByNavMesh && closeByWorld)
                {
                    // Debug.Log($"[CustomerController] {data?.customerName} Arrived near chair (NavMesh: {agent.remainingDistance:F2}, World: {actualDist:F2})");
                    break;
                }
                
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
            
            if (elapsed >= timeout)
            {
                 // Check if we're actually close enough before forcing alignment
                 float actualDist = Vector3.Distance(transform.position, queuePos.position);
                 if (actualDist > 3.0f)
                 {
                     Debug.LogWarning($"[CustomerController] {data?.customerName} timed out but too far ({actualDist:F1}m). Retrying navigation.");
                     
                     // Release any occupied chair
                     ReleaseChair();
                     
                     // Reset path and retry
                     if (agent != null && agent.isOnNavMesh)
                     {
                         agent.ResetPath();
                         agent.SetDestination(queuePos.position);
                     }
                     
                     // Restart the arrival coroutine (recursive retry with shorter timeout)
                     queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(queuePos, shouldSit));
                     yield break;
                 }
                 Debug.LogWarning($"[CustomerController] {data?.customerName} timed out but close enough ({actualDist:F1}m). Proceeding.");
            }
            
            // Final distance check before alignment (prevents warp when NavMesh path is invalid)
            float finalDist = Vector3.Distance(transform.position, queuePos.position);
            if (finalDist > 3.0f)
            {
                Debug.LogWarning($"[CustomerController] {data?.customerName} too far to align ({finalDist:F1}m). Retrying.");
                
                // Release any occupied chair
                ReleaseChair();
                
                // Reset path and retry
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.SetDestination(queuePos.position);
                }
                
                // Restart the arrival coroutine
                queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(queuePos, shouldSit));
                yield break;
            }
            
            // Arrived - stop NavMesh agent
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            
            // DOUBLE CHECK: Verify chair is still ours before sitting
            if (shouldSit && currentChair != null)
            {
                if (currentChair.CurrentCustomer != this)
                {
                    // Someone else took our chair! Find alternative
                    Debug.LogWarning($"[CustomerController] {data?.customerName} chair was taken by someone else! Finding alternative.");
                    var alternative = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, currentChair.category);
                    if (alternative != null && alternative.Occupy(this))
                    {
                        currentChair = alternative;
                        // Move to new chair
                        agent.isStopped = false;
                        agent.SetDestination(alternative.SeatPosition.position);
                        queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(alternative.SeatPosition, true));
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomerController] {data?.customerName} no alternative chair, waiting standing");
                        currentChair = null;
                        StartWaiting();
                        yield break;
                    }
                }
            }
            
            // Smoothly align to the exact seat position/rotation
            yield return StartCoroutine(AlignToSeatRoutine(queuePos));
            
            // Sit down after arriving and rotating
            if (shouldSit)
            {
                SetSitting(true);
            }
            else
            {
                // If not sitting (queueing), we are already aligned by AlignToSeatRoutine
            }
            
            // Call arrival callback if provided
            onArrival?.Invoke();
        }
        
        /// <summary>
        /// Smoothly moves and rotates the customer to the exact target transform.
        /// Disables NavMeshAgent to avoid conflict during forced movement.
        /// </summary>
        private System.Collections.IEnumerator AlignToSeatRoutine(Transform target)
        {
            if (target == null) yield break;

            // Disable agent to allow manual transform manipulation
            if (agent != null)
            {
                agent.enabled = false; 
            }
            
            // Force stop walking animation immediately
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            
            // Goal position but keeping original Y (height)
            Vector3 targetPos = target.position;
            targetPos.y = startPos.y;
            
            float duration = 0.5f; // Slide time
            float time = 0f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                // SmoothStep
                t = t * t * (3f - 2f * t);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);
                yield return null;
            }
            
            // Ensure exact match (X/Z only)
            transform.position = targetPos;
            transform.rotation = target.rotation;
        }
            

        
        /// <summary>
        /// Navigate to waiting area while waiting for a bed to become available (sits)
        /// </summary>
        public void GoToWaitingArea(Transform waitPos)
        {
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToWaitingAreaRoutine(waitPos));
        }

        private System.Collections.IEnumerator GoToWaitingAreaRoutine(Transform waitPos)
        {
            if (agent != null && waitPos != null)
            {
                yield return StartCoroutine(PrepareForMovement());

                // Stop previous queue arrival coroutine
                if (queueArrivalCoroutine != null)
                {
                    StopCoroutine(queueArrivalCoroutine);
                    queueArrivalCoroutine = null;
                }
                
                agent.updateRotation = true;
                currentState = CustomerState.Waiting;
                agent.SetDestination(waitPos.position);
                
                // Rotate and sit
                queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(waitPos, true));
            }
        }
        
        /// <summary>
        /// Navigate to an area, then find and sit in a chair
        /// Uses delayed chair selection to avoid race conditions
        /// </summary>
        public void GoToAreaThenFindChair(Transform areaPoint, Environment.ChairCategory category)
        {
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToAreaThenFindChairRoutine(areaPoint, category));
        }
        
        private System.Collections.IEnumerator GoToAreaThenFindChairRoutine(Transform areaPoint, Environment.ChairCategory category)
        {
            if (areaPoint == null || agent == null) yield break;
            
            // Prepare for movement (stand up from current chair if any)
            yield return StartCoroutine(PrepareForMovement());
            
            // Start walking to area point
            agent.updateRotation = true;
            currentState = CustomerState.Waiting;
            agent.SetDestination(areaPoint.position);
            
            Debug.Log($"[CustomerController] {data?.customerName} walking to {category} area");
            
            // Wait until we're close to area (5 meters) then start looking for chair
            float areaArrivalDist = 5f;
            float timeout = 30f;
            float elapsed = 0f;
            Environment.Chair targetChair = null;
            
            while (agent != null && agent.enabled && elapsed < timeout)
            {
                float distToArea = Vector3.Distance(transform.position, areaPoint.position);
                
                // Once we're within range, look for a chair
                if (distToArea <= areaArrivalDist && targetChair == null)
                {
                    targetChair = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, category);
                    if (targetChair != null)
                    {
                        // Found a chair - redirect to it (but don't occupy yet)
                        agent.SetDestination(targetChair.SeatPosition.position);
                        Debug.Log($"[CustomerController] {data?.customerName} found chair {targetChair.name}, redirecting");
                    }
                }
                
                // Check if we're close to target chair
                if (targetChair != null)
                {
                    float distToChair = Vector3.Distance(transform.position, targetChair.SeatPosition.position);
                    if (distToChair <= 1.0f)
                    {
                        // We're at the chair - try to occupy
                        break;
                    }
                }
                
                // Check if agent stopped (arrived somewhere)
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    break;
                }
                
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
            
            // Now at destination - try to occupy chair if we found one
            if (targetChair != null)
            {
                // DOUBLE CHECK: is the chair still empty?
                if (!targetChair.IsOccupied && targetChair.Occupy(this))
                {
                    currentChair = targetChair;
                    Debug.Log($"[CustomerController] {data?.customerName} occupied chair {targetChair.name}");
                    
                    // Align and sit
                    agent.isStopped = true;
                    yield return StartCoroutine(AlignToSeatRoutine(targetChair.SeatPosition));
                    SetSitting(true);
                }
                else
                {
                    // Chair was taken - try to find another
                    Debug.LogWarning($"[CustomerController] {data?.customerName} chair {targetChair.name} was taken, finding alternative");
                    var alternative = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, category);
                    if (alternative != null && alternative.Occupy(this))
                    {
                        currentChair = alternative;
                        agent.SetDestination(alternative.SeatPosition.position);
                        
                        // Wait to arrive
                        yield return new WaitForSeconds(0.5f);
                        while (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance > 0.5f)
                        {
                            yield return new WaitForSeconds(0.2f);
                        }
                        
                        agent.isStopped = true;
                        yield return StartCoroutine(AlignToSeatRoutine(alternative.SeatPosition));
                        SetSitting(true);
                    }
                    else
                    {
                        // No chair available - just wait standing
                        Debug.LogWarning($"[CustomerController] {data?.customerName} no chair available, waiting standing");
                        StartWaiting();
                    }
                }
            }
            else
            {
                // Couldn't find chair - try to find one now
                var chair = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, category);
                if (chair != null && chair.Occupy(this))
                {
                    currentChair = chair;
                    agent.SetDestination(chair.SeatPosition.position);
                    
                    // Wait to arrive
                    yield return new WaitForSeconds(0.5f);
                    while (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance > 0.5f)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }
                    
                    agent.isStopped = true;
                    yield return StartCoroutine(AlignToSeatRoutine(chair.SeatPosition));
                    SetSitting(true);
                }
                else
                {
                    Debug.LogWarning($"[CustomerController] {data?.customerName} no chair available at area, waiting standing");
                    StartWaiting();
                }
            }
        }
        
        /// <summary>
        /// Navigate to a specific chair and sit
        /// </summary>
        public void GoToChair(Environment.Chair chair)
        {
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToChairRoutine(chair));
        }

        private System.Collections.IEnumerator GoToChairRoutine(Environment.Chair targetChair)
        {
            if (targetChair == null || agent == null) yield break;
            
            // Save reference to OLD chair before we do anything
            Environment.Chair oldChair = currentChair;
            
            // CRITICAL: Try to occupy the NEW chair FIRST before PrepareForMovement()
            // This prevents race condition where another customer takes the chair during wait
            Environment.Chair chairToUse = targetChair;
            if (!chairToUse.Occupy(this))
            {
                // Chair already taken - find alternative immediately
                var alternative = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, targetChair.category);
                if (alternative != null && alternative != targetChair && alternative.Occupy(this))
                {
                    chairToUse = alternative;
                    Debug.Log($"[CustomerController] {data?.customerName} chair {targetChair.name} taken, using {alternative.name}");
                }
                else
                {
                    // No chair available - wait in place
                    Debug.LogWarning($"[CustomerController] {data?.customerName} no chair available");
                    currentState = CustomerState.Waiting;
                    StartWaiting();
                    yield break;
                }
            }
            
            // Set currentChair IMMEDIATELY after Occupy succeeds
            // This prevents UpdateQueuePositions from re-assigning a chair
            currentChair = chairToUse;
            
            // Manually release OLD chair (don't rely on PrepareForMovement's ReleaseChair)
            if (oldChair != null && oldChair != chairToUse)
            {
                oldChair.Release();
                Debug.Log($"[CustomerController] {data?.customerName} released old chair {oldChair.name}");
            }
            
            // Now prepare for movement (stand up animation, enable agent)
            // skipReleaseChair=true because we already handled chair release above
            yield return StartCoroutine(PrepareForMovement(skipReleaseChair: true));

            // Stop previous queue arrival coroutine
            if (queueArrivalCoroutine != null)
            {
                StopCoroutine(queueArrivalCoroutine);
                queueArrivalCoroutine = null;
            }
            agent.updateRotation = true;
            currentState = CustomerState.Waiting;
            agent.SetDestination(chairToUse.SeatPosition.position);
            
            // Wait for path calculation
            yield return null;
            while (agent != null && agent.pathPending) yield return null;
            
            // If path is invalid, try ONE alternative immediately
            if (agent != null && agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"[CustomerController] {data?.customerName} path to {chairToUse.name} is {agent.pathStatus}. Trying simplified fallback...");
                chairToUse.Release(); // Release unreachable chair
                
                // Try to find another chair
                var alternative = Core.ChairManager.Instance?.FindClosestEmptyChair(transform.position, targetChair.category);
                if (alternative != null && alternative != targetChair && alternative.Occupy(this))
                {
                    Debug.Log($"[CustomerController] {data?.customerName} switching to {alternative.name}");
                    chairToUse = alternative;
                    currentChair = chairToUse;
                    agent.SetDestination(chairToUse.SeatPosition.position);
                }
                else
                {
                    Debug.LogError($"[CustomerController] {data?.customerName} No reachable alternative found. Waiting in place.");
                }
            }
            
            // Start waiting and arrival detection
            StartWaiting();
            queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(chairToUse.SeatPosition, true));
        }
        
        /// <summary>
        /// Navigate to a chair that is already reserved/occupied by this customer
        /// Used when chair is pre-reserved by ReceptionManager to avoid race conditions
        /// </summary>
        public void GoToReservedChair(Environment.Chair chair)
        {
            if (chair == null) return;
            
            // DON'T set currentChair here!
            // PrepareForMovement will release the OLD currentChair
            // We set the new chair AFTER PrepareForMovement completes
            
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToReservedChairRoutine(chair));
        }
        
        private System.Collections.IEnumerator GoToReservedChairRoutine(Environment.Chair chair)
        {
            if (chair == null || agent == null) yield break;
            
            // PrepareForMovement calls ReleaseChair which will release our pre-reserved chair
            // We need to restore it after PrepareForMovement completes
            yield return StartCoroutine(PrepareForMovement());
            
            // Restore the pre-reserved chair reference (PrepareForMovement cleared it)
            currentChair = chair;

            // Stop previous queue arrival coroutine
            if (queueArrivalCoroutine != null)
            {
                StopCoroutine(queueArrivalCoroutine);
                queueArrivalCoroutine = null;
            }
            
            // Chair is already occupied by us, just navigate
            agent.updateRotation = true;
            currentState = CustomerState.Waiting;
            agent.SetDestination(chair.SeatPosition.position);
            
            // Start waiting timer when going to chair (resets timer)
            StartWaiting();
            
            // Rotate and sit
            queueArrivalCoroutine = StartCoroutine(WaitForQueueArrivalAndRotate(chair.SeatPosition, true));
        }
        /// <summary>
        /// Release the currently occupied chair
        /// </summary>
        public void ReleaseChair()
        {
            if (currentChair != null)
            {
                currentChair.Release();
                currentChair = null;
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
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(LeaveShopRoutine());
        }

        private System.Collections.IEnumerator LeaveShopRoutine()
        {
            currentState = CustomerState.Leaving;
            
            yield return StartCoroutine(PrepareForMovement());
            
            // Stop any pending queue arrival coroutine
            if (queueArrivalCoroutine != null)
            {
                StopCoroutine(queueArrivalCoroutine);
                queueArrivalCoroutine = null;
            }
            
            if (agent == null)
            {
                ReturnToPool();
                yield break;
            }
            
            if (exitPoint == null)
            {
                ReturnToPool();
                yield break;
            }
            
            // Agent enabled by PrepareForMovement
            
            // Check if on NavMesh, if not try to warp
            if (!agent.isOnNavMesh)
            {
                // Try warp to current position first
                agent.Warp(transform.position);
                
                // If still not on NavMesh, find nearest point
                if (!agent.isOnNavMesh)
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        agent.Warp(hit.position);

                    }
                    else
                    {

                        ReturnToPool();
                        yield break;
                    }
                }
            }
            
            // Final check - ensure we're on NavMesh before calling NavMesh methods
            if (!agent.isOnNavMesh)
            {

                ReturnToPool();
                yield break;
            }
            
            // Now agent should be on NavMesh - set destination (same pattern as GoToBed)
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(exitPoint.position);
            
            // Wait for path calculation
            yield return null;
            
            // Wait until path is pending or computed
            while (agent != null && agent.pathPending) 
            {
                yield return null;
            }
            
            // Wait until arrived at exit
            while (agent != null && agent.remainingDistance > 0.5f)
            {
                yield return null;
            }
            
            Debug.Log($"[CustomerController] {data.customerName} has left the shop.");
            ReturnToPool();
            

        }
        
        /// <summary>
        /// Called when treatment fails (pain max 3x, wait timeout) - uses same departure flow as normal completion
        /// </summary>
        public void FailAndLeave()
        {
            Debug.Log($"[CustomerController] {data?.customerName ?? "Customer"} treatment failed, starting departure without payment");
            
            // Record angry customer
            if (DailyStatsManager.Instance != null)
                DailyStatsManager.Instance.RecordAngryCustomer();
            
            // Mark as failed (not completed, but departure should happen)
            isFailedTreatment = true;
            currentState = CustomerState.Completed; // Use same state for departure flow
            
            // Set TreatmentFinished = true (same as normal completion - wait for player to leave)
            if (animator != null && animator.enabled)
            {
                animator.SetBool("TreatmentFinished", true);
            }
            
            // Start the same departure flow as normal completion
            StartCoroutine(WaitForPlayerToLeave());
        }
        
        /// <summary>
        /// Safely return this customer to the pool
        /// </summary>
        private void ReturnToPool()
        {
            // Unregister from ReceptionManager first
            var receptionManager = UI.ReceptionManager.Instance;
            if (receptionManager != null)
            {
                receptionManager.UnregisterCustomer(this);
                receptionManager.RemoveFromWaitingList(this);
            }
            
            // Unregister from CashRegister
            var cashRegister = FindObjectOfType<UI.CashRegister>();
            if (cashRegister != null)
            {
                cashRegister.UnregisterCustomer(this);
            }
            
            // Reset staff processing state
            ResetStaffProcessing();
            
            if (spawner != null)
            {
                spawner.ReturnToPool(this);
            }
            else
            {
                // Fallback: just deactivate
                gameObject.SetActive(false);
            }
        }

        public void Pay(int amount)
        {
            EconomyManager.Instance.AddMoney(amount);
            Debug.Log($"{data.customerName} paid ${amount}.");
        }
        
        /// <summary>
        /// Calculate final payment based on completed requested parts only
        /// Uses confirmedPrice from reception
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
            
            // Calculate payment: confirmedPrice if all parts complete, proportional otherwise
            int totalParts = targetPartNames.Count;
            int payment = totalParts > 0 ? (data.confirmedPrice * completedCount) / totalParts : 0;
            Debug.Log($"[CustomerController] Payment calculation: {completedCount}/{totalParts} parts completed, ${payment} of ${data.confirmedPrice}");
            
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
            }
            
            // Spawn hair debris on the floor (for cleaning system)
            if (Environment.HairDebrisManager.Instance != null && assignedBed != null)
            {
                Environment.HairDebrisManager.Instance.OnHairRemoved(assignedBed.transform.position);
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
            // Show clothing when standing up after treatment
            SetClothingVisible(true);
            
            // Customer remains at their current position (on the bed)
            // No need to teleport - they will walk from current position
            
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
            
            // Wait for NavMesh to update (doors' NavMeshObstacle needs 1 frame to update)
            StartCoroutine(WalkToReceptionAfterNavMeshUpdate());
        }
        
        private System.Collections.IEnumerator WalkToReceptionAfterNavMeshUpdate()
        {
            // CRITICAL: Wait 2 frames for NavMeshObstacle to update after doors open
            yield return null;
            yield return null;
            
            // If failed treatment, go directly to exit (skip payment)
            if (isFailedTreatment)
            {
                isFailedTreatment = false; // Reset for pool reuse
                LeaveShop();
            }
            else
            {
                WalkToReception();
            }
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
            // Stop waiting timer - treatment is complete
            StopWaiting();
            
            // NOTE: Do NOT manually enable NavMeshAgent here!
            // Let GoToChair/GoToCounterPoint handle it via PrepareForMovement()
            // for consistent behavior with reception flow.
            
            // Stand up animation (reverse lie down)
            if (animator != null)
            {
                animator.SetBool("IsLyingDown", false);
                animator.SetBool("IsLieDownFaceDown", false);
            }
            
            // Set final destination and state
            currentState = CustomerState.Paying;
            
            // Register with cash register manager (shared queue)
            var registerManager = UI.CashRegisterManager.Instance;
            if (registerManager != null)
            {
                registerManager.RegisterCustomer(this);
            }
            else
            {
                Debug.LogError("[CustomerController] No CashRegisterManager found!");
                if (agent != null && agent.isOnNavMesh && cashRegisterPoint != null)
                {
                    agent.SetDestination(cashRegisterPoint.position);
                }
            }
        }
        
        private void ArriveAtReception()
        {
            currentState = CustomerState.Paying;
            paymentTimer = 0f;
            StartWaiting(); // Start waiting for payment
            Debug.Log($"{data.customerName} arrived at cash register. Ready for payment...");
        }
        
        private void GoToExit()
        {
            currentState = CustomerState.Leaving;
            if (exitPoint != null && agent != null && agent.isOnNavMesh)
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
        public float painReactionProbability = 0.3f; // Stage 1: 30% chance
        [Range(0f, 1f)]
        public float painReactionProbabilityStage2 = 0.5f; // Stage 2: 50% chance
        public float painDecayRate = 5f; // Pain decay per second
        public float painRecoveryThreshold = 60f; // Threshold to recover from Stage 3
        public float painHoldDuration = 1f; // How long to hold at 100% before decay starts
        
        private bool isInPainStage3 = false; // Currently in Stage 3 (treatment blocked)
        private bool hasAppliedReviewPenalty = false; // Only apply review penalty once per stage 3
        private float painHoldTimer = 0f; // Timer for holding at 100%
        
        /// <summary>
        /// Get current pain level (1-3, or 0 if no pain)
        /// </summary>
        public int PainLevel
        {
            get
            {
                if (currentPain >= 100f) return 3;
                if (currentPain > 50f) return 2;
                if (currentPain > 0f) return 1;
                return 0;
            }
        }
        
        /// <summary>
        /// Check if customer can receive treatment (not in pain stage 3 or recovered)
        /// </summary>
        public bool CanReceiveTreatment()
        {
            // If in stage 3, check if pain has dropped below recovery threshold
            if (isInPainStage3)
            {
                    if (currentPain <= painRecoveryThreshold)
                    {
                        isInPainStage3 = false;
                        hasAppliedReviewPenalty = false; // Reset penalty flag so it can be applied again next time
                        
                        // Clear animator pain cooldown state
                        if (animator != null && animator.enabled)
                        {
                            animator.SetBool("IsPainCooldown", false);
                        }
                        
                        Debug.Log($"[CustomerController] {data.customerName} recovered from pain! Can receive treatment again.");
                    }
                else
                {
                    return false; // Still in pain, can't receive treatment
                }
            }
            return true;
        }
        
        /// <summary>
        /// Update pain decay - called from Update()
        /// </summary>
        private void UpdatePainDecay()
        {
            if (currentState != CustomerState.InTreatment) return;
            if (currentPain <= 0f) return;
            
            // Hold at 100% for painHoldDuration before starting decay
            if (painHoldTimer > 0f)
            {
                painHoldTimer -= Time.deltaTime;
                return; // Don't decay during hold
            }
            
            currentPain -= painDecayRate * Time.deltaTime;
            currentPain = Mathf.Max(0f, currentPain);
        }
        
        /// <summary>
        /// Add pain to customer. Returns true if pain reaction (animation/sound) was triggered.
        /// Pain is scaled by customer's pain tolerance (lower tolerance = more pain)
        /// </summary>
        public bool AddPain(float amount)
        {
            // Check if pain judgment should be skipped (from effects)
            if (appliedEffects != null && appliedEffects.IgnorePainJudgment)
            {
                return false;
            }
            
            // Scale pain by tolerance (0 = no tolerance, 1 = full tolerance)
            float scaledAmount = amount * (1f - data.painTolerance);

            // Apply pain rate reduction from effects
            if (appliedEffects != null && appliedEffects.PainRateMultiplier < 1f)
            {
                scaledAmount *= appliedEffects.PainRateMultiplier;
            }

            
            currentPain += scaledAmount;
            currentPain = Mathf.Clamp(currentPain, 0f, maxPain);
            
            int painLevel = PainLevel;
            bool triggeredReaction = false;
            
            // Check if pain animation should be disabled (from effects)
            bool disableAnimation = appliedEffects != null && appliedEffects.DisablePainAnimation;
            
            // Stage 1: Small reaction with low probability (0-50%)
            if (painLevel == 1)
            {
                if (!disableAnimation && animator != null && amount > 0 && Random.value < painReactionProbability)
                {
                    animator.SetTrigger("Pain");
                    if (HairRemovalSim.Core.SoundManager.Instance != null)
                    {
                        HairRemovalSim.Core.SoundManager.Instance.PlaySFX("Pain");
                    }
                    triggeredReaction = true;
                }
            }
            // Stage 2: Larger reaction with higher probability (51-99%)
            else if (painLevel == 2)
            {
                if (!disableAnimation && animator != null && amount > 0 && Random.value < painReactionProbabilityStage2)
                {
                    animator.SetTrigger("PainStrong"); // Larger reaction
                    if (HairRemovalSim.Core.SoundManager.Instance != null)
                    {
                        HairRemovalSim.Core.SoundManager.Instance.PlaySFX("PainStrong");
                    }
                    triggeredReaction = true;
                }
            }
            // Stage 3: Extreme pain, block treatment (100%)
            else if (painLevel == 3 && !isInPainStage3)
            {
                // Check if customer can endure pain (from effects)
                if (appliedEffects != null && appliedEffects.PainEnduranceCount > 0)
                {
                    appliedEffects.PainEnduranceCount--;
                    currentPain = 0;// painRecoveryThreshold; // Reset to recovery threshold
                    Debug.Log($"[CustomerController] {data.customerName} endured 100% pain! Remaining endurance: {appliedEffects.PainEnduranceCount}");
                    return false; // No reaction, customer endured
                }
                
                isInPainStage3 = true;
                painHoldTimer = painHoldDuration; // Hold at 100% before decay starts
                painMaxCount++; // Track for review penalty
                
                // Set animator bool for pain cooldown state
                if (animator != null && animator.enabled)
                {
                    animator.SetBool("IsPainCooldown", true);
                }
                
                // Check if customer should leave due to too much pain (3 times max)
                if (painMaxCount >= 3)
                {
                    Debug.LogWarning($"[CustomerController] {data.customerName} has hit max pain 3 times! Leaving with -50 review.");
                    
                    // Add negative review
                    if (Core.ShopManager.Instance != null)
                    {
                        Core.ShopManager.Instance.AddReview(-50, painMaxCount);
                        Core.ShopManager.Instance.AddCustomerReview(1); // 1 star
                    }
                    
                    // Show angry leave popup
                    if (UI.PopupNotificationManager.Instance != null)
                    {
                        UI.PopupNotificationManager.Instance.ShowAngryLeave(50);
                    }
                    
                    // Show message box for treatment failure
                    UI.MessageBoxManager.Instance?.ShowDirectMessage(
                        LocalizationManager.Instance.Get("msg.treatment_fail") ?? "お客様が痛みに耐えられず帰ってしまった！", 
                        UI.MessageType.Complaint, 
                        false, 
                        "msg.treatment_fail");
                    
                    // Release bed first
                    if (assignedBed != null)
                    {
                        assignedBed.ClearCustomer();
                    }
                    
                    // Customer leaves without paying
                    FailAndLeave();
                    return true;
                }
                
                // Trigger writhing animation (unless disabled)
                if (!disableAnimation && animator != null)
                {
                    animator.SetTrigger("PainExtreme");
                }
                if (HairRemovalSim.Core.SoundManager.Instance != null)
                {
                    HairRemovalSim.Core.SoundManager.Instance.PlaySFX("PainExtreme");
                }
                
                // Apply review penalty (only once per stage 3 occurrence)
                if (!hasAppliedReviewPenalty)
                {
                    ApplyReviewPenalty();
                    hasAppliedReviewPenalty = true;
                }
                
                Debug.LogWarning($"[CustomerController] {data.customerName} is in extreme pain! Treatment blocked until pain drops to {painRecoveryThreshold}%. (Pain max count: {painMaxCount}/3)");
                triggeredReaction = true;
            }
            
            return triggeredReaction;
        }
        
        /// <summary>
        /// Reduce pain by specified amount (used by cooling gel)
        /// </summary>
        public void ReducePain(float amount)
        {
            currentPain -= amount;
            currentPain = Mathf.Max(0f, currentPain);
            Debug.Log($"[CustomerController] {data.customerName} pain reduced by {amount}. Current: {currentPain}");
        }
        
        #region Wait Time Gauge
        
        /// <summary>
        /// Start waiting timer at a new location (resets timer to 0)
        /// </summary>
        public void StartWaiting(float customMaxTime = 0f)
        {
            waitTimer = 0f;
            float baseMaxTime = customMaxTime > 0f ? customMaxTime : defaultMaxWaitTime;
            
            // Apply wait time boost from placement items
            float boostPercent = PlacementManager.Instance != null ? PlacementManager.Instance.GetWaitTimeBoost() : 0f;
            maxWaitTime = baseMaxTime * (1f + boostPercent);
            
            isWaiting = true;
            hasShownWaitWarning = false; // Reset warning flag
            Debug.Log($"[CustomerController] {data?.customerName} started waiting (max: {maxWaitTime:F1}s, boost: {boostPercent:P0})");
        }
        
        /// <summary>
        /// Resume waiting timer from current value (after ESC cancel)
        /// Does NOT reset waitTimer - continues from where it was
        /// </summary>
        public void ResumeWaiting()
        {
            isWaiting = true;
            Debug.Log($"[CustomerController] {data?.customerName} resumed waiting (current: {waitTimer:F1}s / {maxWaitTime}s)");
        }
        
        /// <summary>
        /// Pause waiting timer - gauge stays visible but stops incrementing
        /// Use when player interacts but hasn't confirmed yet
        /// </summary>
        public void PauseWaiting()
        {
            isWaiting = false;
            // Don't reset waitTimer - keep current progress visible
            Debug.Log($"[CustomerController] {data?.customerName} waiting paused (gauge visible)");
        }
        
        /// <summary>
        /// Stop waiting timer and hide gauge (processing confirmed)
        /// </summary>
        public void StopWaiting()
        {
            isWaiting = false;
            waitTimer = 0f;
            Debug.Log($"[CustomerController] {data?.customerName} stopped waiting (gauge hidden)");
        }
        
        /// <summary>
        /// Get current wait progress (0 to 1)
        /// </summary>
        public float GetWaitProgress()
        {
            if (maxWaitTime <= 0f) return 0f;
            return Mathf.Clamp01(waitTimer / maxWaitTime);
        }
        
        /// <summary>
        /// Check if currently waiting
        /// </summary>
        public bool IsWaiting => isWaiting;
        
        /// <summary>
        /// Reset wait timer to 0 (called when reception is complete)
        /// </summary>
        public void ResetWaitTimer()
        {
            waitTimer = 0f;
            isWaiting = false;
            Debug.Log($"[CustomerController] {data?.customerName} wait timer reset");
        }
        
        /// <summary>
        /// Resume wait timer (called when staff cancels reception processing)
        /// </summary>
        public void ResumeWaitTimer()
        {
            isWaiting = true;
            Debug.Log($"[CustomerController] {data?.customerName} wait timer resumed at {waitTimer:F1}s");
        }
        
        /// <summary>
        /// Called when wait time expires - customer leaves angry
        /// </summary>
        private void OnWaitTimeExpired()
        {
            isWaiting = false;
            waitTimer = 0f; // Reset timer to hide gauge
            
            // Add major review penalty
            if (data != null)
            {
                data.reviewPenalty += 30;
            }
            
            Debug.Log($"[CustomerController] {data?.customerName} waited too long and is leaving angry!");
            
            // Show message box for wait timeout
            UI.MessageBoxManager.Instance?.ShowDirectMessage(
                LocalizationManager.Instance.Get("msg.wait_timeout") ?? "お客様が待ちきれずに帰ってしまった！", 
                UI.MessageType.Complaint, 
                false, 
                "msg.wait_timeout");
            
            // Clear from ReceptionManager queue and waiting list
            if (UI.ReceptionManager.Instance != null)
            {
                UI.ReceptionManager.Instance.ClearCurrentCustomer(this);
                UI.ReceptionManager.Instance.UnregisterCustomer(this);
                UI.ReceptionManager.Instance.RemoveFromWaitingList(this);
            }
            
            // If in treatment (on bed), use FailAndLeave to properly get dressed and stand up
            if (currentState == CustomerState.InTreatment && assignedBed != null)
            {
                // Record angry customer and submit negative review
                if (Core.ShopManager.Instance != null)
                {
                    Core.ShopManager.Instance.AddReview(-50, 0);
                    Core.ShopManager.Instance.AddCustomerReview(1); // 1 star
                }
                
                // Show angry leave popup
                if (UI.PopupNotificationManager.Instance != null)
                {
                    UI.PopupNotificationManager.Instance.ShowAngryLeave(50);
                }
                
                FailAndLeave();
            }
            else
            {
                // Record angry customer and submit negative review (same as treatment timeout)
                if (Core.ShopManager.Instance != null)
                {
                    Core.ShopManager.Instance.AddReview(-50, 0);
                    Core.ShopManager.Instance.AddCustomerReview(1); // 1 star
                }
                
                // Show angry leave popup
                if (UI.PopupNotificationManager.Instance != null)
                {
                    UI.PopupNotificationManager.Instance.ShowAngryLeave(50);
                }
                
                // If at a bed (waiting for treatment), release it
                if (assignedBed != null)
                {
                    assignedBed.ClearCustomer();
                    assignedBed = null;
                }
                
                // Leave without paying
                LeaveShop();
            }
        }
        
        #endregion
        
        /// <summary>
        /// Apply reception item effects to this customer.
        /// Called when reception confirms with an extra item.
        /// </summary>
        public void ApplyReceptionEffects(ItemData item)
        {
            if (item == null) return;
            
            // Initialize effect context if needed
            if (appliedEffects == null)
            {
                appliedEffects = EffectContext.CreateForReception(this);
            }
            
            // Apply all effects from the item
            EffectHelper.ApplyEffects(item, appliedEffects);
            
            // Store item reference if it has effects (for TreatmentPanel icon display)
            if (item.effects != null && item.effects.Count > 0)
            {
                appliedReceptionItem = item;
            }
            
            // Add review bonus to persistent data
            if (item.reviewBonus != 0)
            {
                data.reviewBonus += item.reviewBonus;
                Debug.Log(data.reviewBonus);
                Debug.Log($"[CustomerController] Added review bonus {item.reviewBonus} to {data.customerName} (total bonus: {data.reviewBonus})");
            }
            
            Debug.Log($"[CustomerController] Applied reception effects from {item.name} to {data.customerName}");
        }
        
        /// <summary>
        /// Get the applied effect context (for checking effect status)
        /// </summary>
        public EffectContext GetAppliedEffects() => appliedEffects;
        
        /// <summary>
        /// Apply review penalty when customer reaches pain stage 3
        /// </summary>
        private void ApplyReviewPenalty()
        {
            // Penalty is tracked via painMaxCount and applied when treatment completes
            // Also apply immediate penalty for real-time tracking
            int penaltyPerPainEvent = 25; // Based on user report of -25
            data.reviewPenalty += penaltyPerPainEvent;
            
            Debug.Log($"[CustomerController] Pain max count for {data.customerName}: {painMaxCount}. Applied penalty: {penaltyPerPainEvent}, Total: {data.reviewPenalty}");
        }
        
        /// <summary>
        /// Get base review value for this customer (10-50)
        /// </summary>
        public int GetBaseReviewValue() => baseReviewValue;
        
        /// <summary>
        /// Get how many times this customer hit 100% pain
        /// </summary>
        public int GetPainMaxCount() => painMaxCount;

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
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(GoToBedRoutine(bed));
        }

        private System.Collections.IEnumerator GoToBedRoutine(Environment.BedController bed)
        {
            if (bed == null) yield break;
            
            yield return StartCoroutine(PrepareForMovement());
            
            // Stop any pending queue arrival coroutine
            if (queueArrivalCoroutine != null)
            {
                StopCoroutine(queueArrivalCoroutine);
                queueArrivalCoroutine = null;
            }
            
            // Try to assign customer to bed - skip if already assigned to this customer
            // (ReceptionManager may have pre-assigned the bed)
            if (bed.CurrentCustomer != this)
            {
                if (!bed.AssignCustomer(this))
                {
                    Debug.LogWarning($"[CustomerController] {data?.customerName} could not assign to bed {bed.name} - already occupied");
                    // Return to waiting area (customer will be re-queued)
                    currentState = CustomerState.Waiting;
                    yield break;
                }
            }
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
                agent.updateRotation = true;
                agent.SetDestination(destination);
            }
            
            // Stop waiting timer - no more wait timeout while heading to bed
            StopWaiting();
            
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
            
            // Check if staff is assigned to this bed
            if (assignedBed != null && Core.ShopManager.Instance != null && Staff.StaffManager.Instance != null)
            {
                int bedIndex = Core.ShopManager.Instance.Beds.IndexOf(assignedBed);
                if (bedIndex >= 0)
                {
                    // Check if any staff is assigned to this bed (Treatment role, specific bed index)
                    bool isStaffAssigned = Staff.StaffManager.Instance.IsPositionOccupied(Staff.StaffAssignment.Treatment, bedIndex);
                    
                    if (!isStaffAssigned)
                    {
                        // No staff assigned! Show warning.
                        // Format: "Customer waiting at bed {0}"
                        string msg = LocalizationManager.Instance.Get("msg.customer_waiting_at_bed", bedIndex + 1);
                        if (string.IsNullOrEmpty(msg) || msg.Contains("not found"))
                        {
                            msg = $"お客様がベッド{bedIndex + 1}で施術を待っています";
                        }
                        
                        UI.MessageBoxManager.Instance?.ShowDirectMessage(
                            msg, 
                            UI.MessageType.Warning,
                            false,
                            "msg.customer_waiting_at_bed");
                    }
                }
            }

            // Disable agent for manual positioning
            if (agent != null) agent.enabled = false;
            
            // Start waiting for treatment
            StartWaiting();

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
           // yield return new WaitForSeconds(0.1f);
            
            // THEN: Smoothly adjust position/rotation
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 targetPos;
            Quaternion targetRot;
            
            if (assignedBed != null && assignedBed.lieDownPoint != null)
            {
                targetPos = assignedBed.lieDownPoint.position;
                targetRot = assignedBed.lieDownPoint.rotation;

            }
            else
            {
                targetPos = targetBed.position;
                targetRot = targetBed.rotation;
            }
            
            //float duration = 0f;
            //float elapsed = 0f;
            
            //while (elapsed < duration)
            //{
            //    elapsed += Time.deltaTime;
            //    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                
            //    transform.position = Vector3.Lerp(startPos, targetPos, t);
            //    transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
            //    yield return null;
            //}
            
            // Snap to final position/rotation
            transform.position = targetPos;
            transform.rotation = targetRot;
            Debug.Log($"[CustomerController] Position/rotation adjusted: pos={targetPos}, rot=(0,0,0)");
            
            // Delay BakeMesh to allow animation to fully settle
            if (animator != null && animator.enabled)
            {
                Debug.Log("Baked");
                StartCoroutine(DelayedBakeMesh(1f));
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
            yield return null;
        }
        
        /// <summary>
        /// Called by Animation Event when lie-down animation completes
        /// Bakes mesh colliders for accurate hit detection
        /// </summary>
        public void OnLieDownComplete()
        {
            // Hide clothing when lying down for treatment
            
            Debug.Log($"[CustomerController] OnLieDownComplete called. bodyPart is null: {bodyPart == null}");
            
            if (bodyPart == null)
            {
                Debug.LogError("[CustomerController] bodyPart is null!");
                return;
            }
            
            try
            {
                Debug.Log($"[CustomerController] Calling BakeMeshForCollider on {bodyPart.name}");
                bodyPart.BakeMeshForCollider();
                Debug.Log($"[CustomerController] BakeMeshForCollider completed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CustomerController] Exception in BakeMeshForCollider: {e.Message}\n{e.StackTrace}");
            }
            
            Debug.Log($"[CustomerController] OnLieDownComplete: Baked mesh colliders for  ");
        }
        
        /// <summary>
        /// Delay BakeMesh to allow animation to settle
        /// </summary>
        private System.Collections.IEnumerator DelayedBakeMesh(float delay)
        {
            SetClothingVisible(false);

            yield return new WaitForSeconds(delay);
            
            // Fallback: Call BakeMesh if Animation Event didn't fire
            if (bodyPart != null)
            {
                OnLieDownComplete();
            }
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public string GetInteractionPrompt()
        {
            return currentState == CustomerState.Waiting 
                ? Core.LocalizationManager.Instance?.Get("prompt.call_customer") ?? "Call Customer"
                : "";
        }

        private void Update()
        {
            // Update pain decay during treatment
            UpdatePainDecay();
            
            // Update wait timer
            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                
                // Show warning at 80% wait time
                if (!hasShownWaitWarning && maxWaitTime > 0f && waitTimer >= maxWaitTime * 0.8f)
                {
                    hasShownWaitWarning = true;
                    UI.MessageBoxManager.Instance?.ShowDirectMessage(
                        LocalizationManager.Instance.Get("msg.wait_warning") ?? "お客様が待たされすぎてイライラしている！", 
                        UI.MessageType.Warning, 
                        false, 
                        "msg.wait_warning");
                }
                
                if (waitTimer >= maxWaitTime)
                {
                    OnWaitTimeExpired();
                    return; // Exit early since we're leaving
                }
            }
            
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
                    // Also verify with actual world distance to prevent false arrivals when path recalculates
                    if (agent.remainingDistance <= agent.stoppingDistance + bedArrivalDistance)
                    {
                        Vector3 bedPos = (assignedBed != null && assignedBed.arrivalPoint != null) 
                            ? assignedBed.arrivalPoint.position 
                            : (assignedBed != null ? assignedBed.transform.position : transform.position);
                        float actualDistance = Vector3.Distance(transform.position, bedPos);
                        
                        // Only arrive if we're actually close (prevents false arrival when path becomes invalid)
                        if (actualDistance <= bedArrivalDistance + 1.0f)
                        {
                            // Start door closing sequence, ArriveAtBed will be called when doors are closed
                            StartBedArrivalSequence();
                        }
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
                // Logic moved to LeaveShopRoutine coroutine to ensure proper exit behavior
            }
        }
        
        
        private int CalculatePayment()
        {
            // Return confirmed price from reception
            return data.confirmedPrice;
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
        
        /// <summary>
        /// Set clothing visibility based on confirmed body parts (called when lying down)
        /// </summary>
        public void SetClothingForTreatment()
        {
            // Get confirmed parts from customer data (set at reception)
            TreatmentBodyPart parts = data?.confirmedParts ?? TreatmentBodyPart.None;
            
            if (parts == TreatmentBodyPart.None)
            {
                Debug.Log("[CustomerController] No confirmed parts, only hiding shoes");
                if (shoesObject != null) shoesObject.SetActive(false);
                return;
            }
            
            // Check if upper body treatment is needed (Arms, Chest, Abs, Back, Armpits)
            bool needsShirtRemoval = (parts & (TreatmentBodyPart.Arms | TreatmentBodyPart.Chest | 
                TreatmentBodyPart.Abs | TreatmentBodyPart.Back | TreatmentBodyPart.Armpits)) != 0;
            
            // Check if lower body treatment is needed (Legs)
            bool needsPantsRemoval = (parts & TreatmentBodyPart.Legs | TreatmentBodyPart.Abs) != 0;
            
            // Apply clothing visibility
            if (shirtObject != null) shirtObject.SetActive(!needsShirtRemoval);
            if (shirtObject != pantsObject)
            {
                if (pantsObject != null) pantsObject.SetActive(!needsPantsRemoval);
                if (boxerObject != null) boxerObject.SetActive(needsPantsRemoval); // Show boxer when pants removed
            }
            else
            {
                if (boxerObject != null) boxerObject.SetActive(true); // Show boxer when pants removed

            }
            if (shoesObject != null) shoesObject.SetActive(false); // Always hide shoes when lying down
            
            Debug.Log($"[CustomerController] Clothing for treatment - Shirt: {!needsShirtRemoval}, Pants: {!needsPantsRemoval}, Boxer: {needsPantsRemoval}, Shoes: false");
        }
        
        /// <summary>
        /// Restore all clothing (called when standing up after treatment)
        /// </summary>
        public void SetClothingForStanding()
        {
            if (shirtObject != null) shirtObject.SetActive(true);
            if (pantsObject != null) pantsObject.SetActive(true);
            if (boxerObject != null) boxerObject.SetActive(false); // Hide boxer when pants are on
            if (shoesObject != null) shoesObject.SetActive(true);
            
            Debug.Log("[CustomerController] All clothing restored for standing");
        }
        
        /// <summary>
        /// Helper to set all clothing active/inactive
        /// </summary>
        private void SetAllClothingActive(bool active, bool showBoxerIfInactive)
        {
            if (shirtObject != null) shirtObject.SetActive(active);
            if (pantsObject != null) pantsObject.SetActive(active);
            if (boxerObject != null) boxerObject.SetActive(!active && showBoxerIfInactive);
            if (shoesObject != null) shoesObject.SetActive(active);
        }
        
        /// <summary>
        /// Legacy method - redirects to SetClothingForStanding/SetClothingForTreatment
        /// </summary>
        public void SetClothingVisible(bool visible)
        {
            if (visible)
                SetClothingForStanding();
            else
                SetClothingForTreatment();
        }
        
        // ========== STAFF PROCESSING METHODS ==========
        
        /// <summary>
        /// Set review coefficient from staff (called by StaffReceptionHandler)
        /// </summary>
        public void SetStaffReviewCoefficient(float coefficient)
        {
            staffReviewCoefficient = coefficient;
        }
        
        /// <summary>
        /// Get review coefficient for final calculation
        /// </summary>
        public float GetStaffReviewCoefficient()
        {
            return staffReviewCoefficient;
        }
        
        /// <summary>
        /// Set upsell success flag (called by StaffReceptionHandler)
        /// </summary>
        public void SetUpsellSuccess(bool success)
        {
            upsellSuccess = success;
        }
        
        /// <summary>
        /// Check if upsell was successful
        /// </summary>
        public bool GetUpsellSuccess()
        {
            return upsellSuccess;
        }
        
        /// <summary>
        /// Reset staff processing state (called when returning to pool)
        /// </summary>
        public void ResetStaffProcessing()
        {
            staffReviewCoefficient = 1f;
            upsellSuccess = false;
        }
        /// <summary>
        /// Get the current calculates review score based on personal factors
        /// (Base - Penalty + Bonus). Does not include environment factors like debris.
        /// </summary>
        public int GetCurrentReviewScore()
        {
            if (data == null) return 0;
            return GetBaseReviewValue() - data.reviewPenalty + data.reviewBonus;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (data == null || Camera.main == null) return;

            // Only show if visible and close enough
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (screenPos.z > 0 && screenPos.z < 10)
            {
                // Simple debug label
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
                style.fontStyle = FontStyle.Bold;
                
                // Background box
                float w = 250;
                float h = 60;
                Rect rect = new Rect(screenPos.x - w/2, Screen.height - screenPos.y - h/2, w, h);
                
                // Calculate score
                int currentScore = GetCurrentReviewScore();
                string text = $"Review: {currentScore}\n(Base:{GetBaseReviewValue()} - Pen:{data.reviewPenalty} + Bon:{data.reviewBonus})";
                
                // Draw shadow
                var shadowRect = rect;
                shadowRect.x += 1;
                shadowRect.y += 1;
                style.normal.textColor = Color.black;
                GUI.Label(shadowRect, text, style);
                
                // Draw text
                style.normal.textColor = currentScore >= 30 ? Color.green : (currentScore < 0 ? Color.red : Color.yellow);
                GUI.Label(rect, text, style);
            }
        }
#endif
    }
}
