using UnityEngine;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("List of customer prefabs to spawn randomly")]
        public List<GameObject> customerPrefabs = new List<GameObject>();
        public Transform spawnPoint;
        public Transform exitPoint;
        public Transform receptionPoint; // Pre-treatment reception
        public Transform cashRegisterPoint; // Post-treatment payment
        public UI.ReceptionManager receptionManager; // Reference to reception for queue registration
        public Core.BodyPartsDatabase bodyPartsDatabase; // UV-based body part system
        
        [Header("Spawn Intervals")]
        public float spawnInterval = 30f; // Seconds
        public int maxCustomers = 3;
        
        [Header("Object Pool")]
        public int poolSize = 3; // Pre-initialized customers
        
        [Header("Test/Debug Settings")]
        [Tooltip("Enable to use fixed settings instead of random")]
        public bool useTestSettings = false;
        [Tooltip("Fixed treatment plan for testing")]
        public CustomerRequestPlan testRequestPlan = CustomerRequestPlan.Beard;
        [Tooltip("Fixed hairiness level for testing")]
        public HairinessLevel testHairinessLevel = HairinessLevel.Medium;
        [Tooltip("Fixed wealth level for testing")]
        public WealthLevel testWealthLevel = WealthLevel.Rich;
        [Tooltip("Spawn customer immediately on play")]
        public bool spawnOnStart = false;
        
        [Header("Checkout Test Mode")]
        [Tooltip("Enable to spawn customer directly at register (skip reception/treatment)")]
        public bool checkoutTestMode = false;
        [Tooltip("Test review value for checkout test (randomized if 0)")]
        public int testReviewValue = 0;
        [Tooltip("Test confirmed price for checkout test (randomized if 0)")]
        public int testConfirmedPrice = 0;
        
        [Header("Attraction Rate Settings")]
        [Tooltip("Base attraction rate without any bonuses (%)")]
        [SerializeField] private float _baseAttractionRate = 30f;
        
        [Tooltip("Customer spawn interval at 100% attraction (seconds)")]
        [SerializeField] private float minSpawnInterval = 30f;
        
        [Tooltip("Customer spawn interval at 0% attraction (seconds)")]
        [SerializeField] private float maxSpawnInterval = 120f;
        
        /// <summary>
        /// Base attraction rate (read-only for UI display)
        /// </summary>
        public float BaseAttractionRate => _baseAttractionRate;

        private List<CustomerController> customerPool = new List<CustomerController>();
        private List<CustomerController> activeCustomers = new List<CustomerController>();

        private float timer = 0f;
        private bool poolInitialized = false;
        
        // ==========================================
        // Attraction Rate & VIP Coefficient
        // ==========================================
        
        /// <summary>
        /// Get current attraction rate (0-100%)
        /// Base rate + star bonus + ad boost
        /// </summary>
        public float GetCurrentAttractionRate()
        {
            float total = _baseAttractionRate;
            
            // Add star rating bonus from ShopManager
            total += ShopManager.Instance?.GetStarRatingBonus() ?? 0f;
            
            // Add advertising boost
            total += AdvertisingManager.Instance?.GetAttractionBoost() ?? 0f;
            
            return Mathf.Clamp(total, 0f, 100f);
        }
        
        /// <summary>
        /// Get current VIP coefficient (0-100)
        /// Review-based VIP + ad boost
        /// </summary>
        public float GetCurrentVipCoefficient()
        {
            float total = 0f;
            
            // Base VIP from past 3 days reviews
            total += ShopManager.Instance?.GetVipCoefficientFromReviews() ?? 50f;
            
            // Add advertising boost
            total += AdvertisingManager.Instance?.GetVipBoost() ?? 0f;
            
            return Mathf.Clamp(total, 0f, 100f);
        }
        
        /// <summary>
        /// Get current spawn interval based on attraction rate
        /// Higher attraction = shorter interval
        /// </summary>
        public float GetCurrentSpawnInterval()
        {
            float attraction = GetCurrentAttractionRate();
            // Lerp between max (0%) and min (100%)
            return Mathf.Lerp(maxSpawnInterval, minSpawnInterval, attraction / 100f);
        }
        
        /// <summary>
        /// Get customer type distribution based on star rating and VIP coefficient
        /// </summary>
        public float[] GetCustomerTypeDistribution()
        {
            int starRating = ShopManager.Instance?.StarRating ?? 1;
            float vipCoef = GetCurrentVipCoefficient();
            
            float[] distribution = new float[5];
            
            switch (starRating)
            {
                case 1:
                    distribution[0] = 100f;
                    break;
                case 2:
                    float poor2 = Mathf.Lerp(50f, 80f, vipCoef / 100f);
                    distribution[0] = 100f - poor2;
                    distribution[1] = poor2;
                    break;
                case 3:
                    float norm3 = Mathf.Lerp(0f, 50f, vipCoef / 100f);
                    float poor3 = Mathf.Lerp(50f, 30f, vipCoef / 100f);
                    distribution[0] = 100f - poor3 - norm3;
                    distribution[1] = poor3;
                    distribution[2] = norm3;
                    break;
                case 4:
                    float rich4 = Mathf.Lerp(0f, 30f, vipCoef / 100f);
                    float norm4 = Mathf.Lerp(30f, 40f, vipCoef / 100f);
                    float poor4 = Mathf.Lerp(35f, 20f, vipCoef / 100f);
                    distribution[0] = 100f - poor4 - norm4 - rich4;
                    distribution[1] = poor4;
                    distribution[2] = norm4;
                    distribution[3] = rich4;
                    break;
                case 5:
                    float richest5 = Mathf.Lerp(0f, 20f, vipCoef / 100f);
                    float rich5 = Mathf.Lerp(10f, 30f, vipCoef / 100f);
                    float norm5 = Mathf.Lerp(30f, 30f, vipCoef / 100f);
                    float poor5 = Mathf.Lerp(30f, 15f, vipCoef / 100f);
                    distribution[0] = 100f - poor5 - norm5 - rich5 - richest5;
                    distribution[1] = poor5;
                    distribution[2] = norm5;
                    distribution[3] = rich5;
                    distribution[4] = richest5;
                    break;
            }
            
            return distribution;
        }
        
        /// <summary>
        /// Get a random customer wealth level based on current distribution
        /// </summary>
        public WealthLevel GetRandomCustomerWealthLevel()
        {
            float[] distribution = GetCustomerTypeDistribution();
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            
            for (int i = 0; i < distribution.Length; i++)
            {
                cumulative += distribution[i];
                if (roll < cumulative)
                    return (WealthLevel)i;
            }
            
            return WealthLevel.Poorest;
        }

        private void Start()
        {
            // Initialize timer so the first customer spawns after opening
            timer = spawnInterval;
            
            // Initialize customer pool at scene start
            StartCoroutine(InitializePool());
        }
        
        private System.Collections.IEnumerator InitializePool()
        {
            if (customerPrefabs.Count == 0)
            {
                Debug.LogError("[CustomerSpawner] No customer prefabs assigned!");
                yield break;
            }
            
            Debug.Log($"[CustomerSpawner] Initializing customer pool with {poolSize} customers from {customerPrefabs.Count} prefab(s)...");
            
            for (int i = 0; i < poolSize; i++)
            {
                // Select prefab sequentially (0, 1, 2, 0, 1, 2, ...)
                GameObject prefab = customerPrefabs[i % customerPrefabs.Count];
                
                GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                obj.name = $"Customer_Pooled_{i}_{prefab.name}";
                CustomerController customer = obj.GetComponent<CustomerController>();
                
                if (customer != null)
                {
                    // IMPORTANT: Activate to trigger Start() and initialization
                    customer.gameObject.SetActive(true);
                    Debug.Log($"[CustomerSpawner] Pre-initializing customer {i + 1}/{poolSize} from {prefab.name} (activated)...");
                    
                    // Wait for initialization to complete
                    while (!customer.isInitialized)
                    {
                        yield return null;
                    }
                    
                    Debug.Log($"[CustomerSpawner] Customer {i + 1}/{poolSize} initialization complete, deactivating...");
                    
                    // Now deactivate and add to pool
                    customer.gameObject.SetActive(false);
                    customerPool.Add(customer);
                }
                
                // Already waited during initialization, no extra frame needed
            }
            
            poolInitialized = true;
            Debug.Log($"[CustomerSpawner] Customer pool initialization complete!");
            
            // Spawn immediately if test mode is enabled
            if (spawnOnStart && useTestSettings)
            {
                Debug.Log("[CustomerSpawner] spawnOnStart enabled - spawning test customer immediately!");
                SpawnCustomer();
            }
        }

        private void Update()
        {
            // Only spawn if pool is initialized
            if (!poolInitialized)
            {
                // Debug: Only log once to avoid spam
                if (Time.frameCount % 300 == 0) Debug.Log("[CustomerSpawner] Waiting for pool initialization...");
                return;
            }
            
            if (GameManager.Instance.CurrentState == GameManager.GameState.Day)
            {
                timer += Time.deltaTime;
                
                // Use dynamic spawn interval based on attraction rate
                float currentInterval = GetCurrentSpawnInterval();
                
                if (timer >= currentInterval)
                {
                    // Clean up nulls
                    activeCustomers.RemoveAll(c => c == null);

                    if (activeCustomers.Count < maxCustomers)
                    {
                        Debug.Log($"[CustomerSpawner] Spawning customer ({activeCustomers.Count}/{maxCustomers}), Attraction: {GetCurrentAttractionRate():F1}%, Interval: {currentInterval:F1}s");
                        SpawnCustomer();
                        timer = 0f;
                    }
                    else
                    {
                        //Debug.Log($"[CustomerSpawner] Max customers reached ({activeCustomers.Count}/{maxCustomers}), waiting...");
                    }
                }
            }
        }
        
        private CustomerController GetFromPool()
        {
            // Collect all available (inactive) customers
            var availableCustomers = new List<CustomerController>();
            int activeCount = 0;
            
            foreach (var customer in customerPool)
            {
                if (customer.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
                else
                {
                    availableCustomers.Add(customer);
                }
            }
            
            if (availableCustomers.Count > 0)
            {
                // Randomly select from available customers
                var selected = availableCustomers[Random.Range(0, availableCustomers.Count)];
                Debug.Log($"[CustomerSpawner] Randomly retrieved {selected.name} from pool ({availableCustomers.Count} available)");
                return selected;
            }
            
            Debug.LogWarning($"[CustomerSpawner] No available customers in pool! All {activeCount}/{customerPool.Count} are active. Consider increasing pool size.");
            return null;
        }
        
        public void ReturnToPool(CustomerController customer)
        {
            if (customer == null) return;
            
            Debug.Log($"[CustomerSpawner] Returning {customer.data?.customerName ?? "NULL"} to pool");
            
            // Disable NavMeshAgent first to prevent NavMesh warnings
            var agent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // Only call isStopped if agent is on NavMesh
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                agent.enabled = false;
            }
            
            customer.gameObject.SetActive(false);
            customer.transform.position = Vector3.zero;
            activeCustomers.Remove(customer);
        }
        
        /// <summary>
        /// Get current active customer count for close shop validation
        /// </summary>
        public int GetActiveCustomerCount()
        {
            // Clean up nulls first
            activeCustomers.RemoveAll(c => c == null);
            return activeCustomers.Count;
        }

        private void SpawnCustomer()
        {
            if (customerPrefabs.Count == 0 || spawnPoint == null) return;

            // Get customer from pool instead of Instantiate
            CustomerController customer = GetFromPool();
            if (customer == null) return;
            
            // Reset position and activate
            customer.transform.position = spawnPoint.position;
            customer.transform.rotation = spawnPoint.rotation;
            customer.gameObject.SetActive(true);
            
            // Properly warp NavMeshAgent to spawn point
            var agent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(spawnPoint.position);
                
                // Verify NavMesh placement
                if (!agent.isOnNavMesh)
                {
                    Debug.LogWarning($"[CustomerSpawner] Agent not on NavMesh after warp, trying SamplePosition");
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
                
                agent.isStopped = false;
            }
            
            if (customer != null)
            {
                // Assign BodyPartsDatabase
                customer.bodyPartsDatabase = bodyPartsDatabase;
                
                // Generate customer data (use test settings if enabled)
                CustomerData data = new CustomerData();
                data.customerName = useTestSettings ? "TestCustomer" : "Guest " + Random.Range(100, 999);
                data.hairiness = useTestSettings ? testHairinessLevel : (HairinessLevel)Random.Range(0, 4);
                data.wealth = useTestSettings ? testWealthLevel : GetRandomCustomerWealthLevel();
                
                // Generate pain tolerance level
                data.painTolerance = Random.Range(0.5f, 1f);
                if (data.painTolerance < 0.66f)
                    data.painToleranceLevel = PainToleranceLevel.Low;
                else if (data.painTolerance < 0.83f)
                    data.painToleranceLevel = PainToleranceLevel.Medium;
                else
                    data.painToleranceLevel = PainToleranceLevel.High;
                
                // Generate customer request plan (use test setting if enabled)
                if (useTestSettings)
                {
                    data.requestPlan = testRequestPlan;
                }
                else
                {
                    // Select plan based on wealth level
                    data.requestPlan = CustomerPlanHelper.GetRandomPlanForWealthLevel(data.wealth);
                }
                
                // Get required parts info for logging
                var requiredParts = CustomerPlanHelper.GetRequiredParts(data.requestPlan);
                var detailedParts = CustomerPlanHelper.GetDetailedTreatmentParts(requiredParts);
                int partCount = detailedParts.Length;
                
                // Price is now fixed per plan, not budget-based
                int planPrice = CustomerPlanHelper.GetPlanPrice(data.requestPlan);
                
                customer.Initialize(data, exitPoint, receptionPoint, cashRegisterPoint, this);
                
                // Checkout Test Mode: Skip reception/treatment, go directly to register
                if (checkoutTestMode)
                {
                    // Set test values
                    data.confirmedPrice = testConfirmedPrice > 0 ? testConfirmedPrice : planPrice;
                    data.confirmedParts = requiredParts;
                    data.reviewPenalty = testReviewValue != 0 ? -testReviewValue + customer.GetBaseReviewValue() : Random.Range(0, 40);
                    
                    Debug.Log($"[CustomerSpawner] CHECKOUT TEST: {data.customerName} -> Register, Price: ${data.confirmedPrice}, ReviewPenalty: {data.reviewPenalty}");
                    
                    // Register with CashRegister queue
                    var cashRegister = FindObjectOfType<UI.CashRegister>();
                    if (cashRegister != null)
                    {
                        cashRegister.RegisterCustomer(customer);
                    }
                    else
                    {
                        Debug.LogWarning("[CustomerSpawner] CashRegister not found! Customer going directly.");
                        customer.GoToCashRegister();
                    }
                    
                    activeCustomers.Add(customer);
                    return;
                }
                
                Debug.Log($"[CustomerSpawner] {data.customerName} requested plan: {data.GetPlanDisplayName()} ({partCount} parts), price: ${planPrice}, tolerance: {data.painToleranceLevel}");
                
                // Register with reception to get queue position
                if (receptionManager != null)
                {
                    Transform queuePos = receptionManager.RegisterCustomer(customer);
                    if (queuePos == null)
                    {
                        Debug.LogWarning($"[CustomerSpawner] Could not register {data.customerName} to queue, sending to reception");
                        customer.GoToReception(receptionPoint);
                    }
                }
                else
                {
                    Debug.LogError("[CustomerSpawner] ReceptionManager reference not set! Customer going to reception point");
                    customer.GoToReception(receptionPoint);
                }
                
                activeCustomers.Add(customer);
            }
        }
    }
}
