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
        
        [Header("Attraction Level Settings (Python-based)")]
        [Tooltip("Current base attraction level (starts at 50, increases with good reviews)")]
        [SerializeField] private float _currentAttractionLevel = 50f;
        
        [Tooltip("Facility boost from items like air purifier (0 to 0.3)")]
        [SerializeField] private float _facilityBoost = 0f;
        
        [Tooltip("Business hours in seconds (10:00-19:00 = 9 hours = 540 real seconds at 60x speed)")]
        [SerializeField] private float businessHoursSeconds = 600f;
        
        /// <summary>
        /// Current base attraction level (read-only for UI display)
        /// </summary>
        public float CurrentAttractionLevel => _currentAttractionLevel;
        
        /// <summary>
        /// Current facility boost multiplier (read-only)
        /// </summary>
        public float FacilityBoost => _facilityBoost;

        private List<CustomerController> customerPool = new List<CustomerController>();
        private List<CustomerController> activeCustomers = new List<CustomerController>();

        private float timer = 0f;
        private bool poolInitialized = false;
        
        // ==========================================
        // Attraction Level System (Python-based)
        // ==========================================
        
        /// <summary>
        /// Get attraction cap for current grade (100, 200, 300, 400, 500, 600)
        /// </summary>
        public int GetAttractionCap()
        {
            return ShopManager.Instance?.GetCurrentAttractionCap() ?? 100;
        }
        
        /// <summary>
        /// Get effective attraction level including ad boost and percentage boost
        /// </summary>
        public float GetEffectiveAttraction()
        {
            float baseAndAd = _currentAttractionLevel;
            
            // Add advertising boost (now in points, not percentage)
            baseAndAd += AdvertisingManager.Instance?.GetAttractionBoost() ?? 0f;
            
            // Apply percentage boost from placement items (e.g., 10% boost)
            float total = baseAndAd * (1f + _attractionPercentBoost);
            
            // Clamp to cap
            int cap = GetAttractionCap();
            return Mathf.Clamp(total, 0f, cap);
        }
        
        /// <summary>
        /// Get attraction ratio (0 to 1) for spawn interval calculation
        /// </summary>
        public float GetAttractionRatio()
        {
            int cap = GetAttractionCap();
            if (cap <= 0) return 0f;
            return GetEffectiveAttraction() / cap;
        }
        
        /// <summary>
        /// Get max customers for current grade with facility boost
        /// </summary>
        public int GetMaxCustomers()
        {
            int baseMax = ShopManager.Instance?.GetCurrentMaxCustomers() ?? 18;
            return Mathf.RoundToInt(baseMax * (1f + _facilityBoost));
        }
        
        /// <summary>
        /// Update attraction level based on average review (call at day end)
        /// Formula: (avgReview / 5) × grade = points added
        /// </summary>
        public void UpdateAttractionLevel(float avgReviewPerCustomer)
        {
            int grade = ShopManager.Instance?.ShopGrade ?? 1;
            float change = (avgReviewPerCustomer / 5f) * grade;
            int cap = GetAttractionCap();
            _currentAttractionLevel = Mathf.Clamp(_currentAttractionLevel + change, 10f, cap);
            Debug.Log($"[CustomerSpawner] Attraction updated: +{change:F1} (G{grade}) → {_currentAttractionLevel:F0}/{cap}");
        }
        
        /// <summary>
        /// Add facility boost from purchased items (air purifier, etc.)
        /// </summary>
        public void AddFacilityBoost(float boost)
        {
            _facilityBoost = Mathf.Clamp(_facilityBoost + boost, 0f, 0.3f);
            Debug.Log($"[CustomerSpawner] Facility boost updated: {_facilityBoost:P0}");
        }
        
        // ==========================================
        // Checkout Item Effects
        // ==========================================
        
        private float _nextDayAttractionBoost = 0f;
        private float _attractionPercentBoost = 0f;
        
        /// <summary>
        /// Add permanent attraction boost from checkout items (e.g. membership stamp)
        /// </summary>
        public void AddAttractionBoost(float boost)
        {
            int cap = GetAttractionCap();
            _currentAttractionLevel = Mathf.Clamp(_currentAttractionLevel + boost, 10f, cap);
            Debug.Log($"[CustomerSpawner] Attraction boost +{boost:F1} → {_currentAttractionLevel:F0}/{cap}");
        }
        
        /// <summary>
        /// Add percentage attraction boost from placement items (0.10 = 10%)
        /// Applied as multiplier on (base + ad boost)
        /// </summary>
        public void AddAttractionPercentBoost(float percentBoost)
        {
            _attractionPercentBoost += percentBoost;
            Debug.Log($"[CustomerSpawner] Attraction percent boost +{percentBoost:P0} (total: {_attractionPercentBoost:P0})");
        }
        
        /// <summary>
        /// Get current attraction percent boost
        /// </summary>
        public float AttractionPercentBoost => _attractionPercentBoost;
        
        /// <summary>
        /// Add next-day attraction boost from checkout items (e.g. coupon)
        /// Applied at day start, cleared at day end
        /// </summary>
        public void AddNextDayAttractionBoost(float boost)
        {
            _nextDayAttractionBoost += boost;
            Debug.Log($"[CustomerSpawner] Next day attraction boost +{boost:F1} (total: {_nextDayAttractionBoost:F1})");
        }
        
        /// <summary>
        /// Apply pending next-day boosts (call at day start)
        /// </summary>
        public void ApplyNextDayBoosts()
        {
            if (_nextDayAttractionBoost > 0f)
            {
                AddAttractionBoost(_nextDayAttractionBoost);
                Debug.Log($"[CustomerSpawner] Applied next day boost: +{_nextDayAttractionBoost:F1}");
                _nextDayAttractionBoost = 0f;
            }
        }
        
        /// <summary>
        /// Legacy: Get current attraction rate as percentage for UI compatibility
        /// </summary>
        public float GetCurrentAttractionRate()
        {
            return GetAttractionRatio() * 100f;
        }
        
        /// <summary>
        /// Legacy: Base attraction rate for AdvertisingPanel compatibility
        /// Returns base attraction as percentage of cap
        /// </summary>
        
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
        /// Get expected customers for today based on attraction level
        /// Formula: maxCustomers × (effectiveAttraction / attractionCap)
        /// </summary>
        public int GetExpectedCustomers()
        {
            int maxCust = GetMaxCustomers();
            float ratio = GetAttractionRatio();
            int expected = Mathf.Max(1, Mathf.RoundToInt(maxCust * ratio));
            return expected;
        }
        
        /// <summary>
        /// Get current spawn interval based on expected customers
        /// Formula: businessHours / expectedCustomers
        /// Recalculates immediately when ads affect attraction
        /// </summary>
        public float GetCurrentSpawnInterval()
        {
            int expected = GetExpectedCustomers();
            float interval = businessHoursSeconds / expected;
            // Clamp to reasonable range (min 5 seconds, max 300 seconds)
            return Mathf.Clamp(interval, 5f, 300f);
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
        
        // ==========================================
        // Daily Review Tracking for Attraction Update
        // ==========================================
        
        private float _dailyReviewTotal = 0f;
        private int _dailyCustomerCount = 0;
        
        /// <summary>
        /// Record a customer's review for daily attraction update
        /// Call this from CheckoutPanel when customer completes checkout
        /// </summary>
        public void RecordDailyReview(float reviewValue)
        {
            _dailyReviewTotal += reviewValue;
            _dailyCustomerCount++;
        }
        
        private void OnEnable()
        {
            // Subscribe to shop closed event for daily attraction update
            GameEvents.OnShopClosed += OnDayEnd;
            GameEvents.OnShopOpened += OnDayStart;
        }
        
        private void OnDisable()
        {
            GameEvents.OnShopClosed -= OnDayEnd;
            GameEvents.OnShopOpened -= OnDayStart;
        }
        
        public float GetDailyAverageReview()
        {
            float avgReview = _dailyReviewTotal / _dailyCustomerCount;
            return avgReview;
        }

        private void OnDayEnd()
        {
            // Calculate average review and update attraction level
            if (_dailyCustomerCount > 0)
            {
                float avgReview = _dailyReviewTotal / _dailyCustomerCount;
                Debug.Log($"[CustomerSpawner] Day End - Customers: {_dailyCustomerCount}, TotalReview: {_dailyReviewTotal:F0}, AvgReview: {avgReview:F1}");
                UpdateAttractionLevel(avgReview);
            }
            else
            {
                Debug.Log("[CustomerSpawner] No customers today, attraction unchanged");
            }
        }
        
        private void OnDayStart()
        {
            // Reset daily tracking
            _dailyReviewTotal = 0f;
            _dailyCustomerCount = 0;
            
            // First customer spawns within 0-20 seconds of opening
            timer = GetCurrentSpawnInterval() - Random.Range(0f, 20f);
            timer = Mathf.Max(0f, timer); // Ensure non-negative
            
            Debug.Log($"[CustomerSpawner] Day started. Attraction: {_currentAttractionLevel:F0}/{GetAttractionCap()}, Expected: {GetExpectedCustomers()}, Interval: {GetCurrentSpawnInterval():F1}s");
        }

        private void Start()
        {
            // Initialize maxCustomers from current grade
            if (ShopManager.Instance != null)
            {
                maxCustomers = ShopManager.Instance.GetCurrentMaxSimultaneous();
                Debug.Log($"[CustomerSpawner] Initialized maxCustomers to {maxCustomers} for grade {ShopManager.Instance.ShopGrade}");
            }
            
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
            
            // Pool is empty - create new customer instance (same as Staff pool behavior)
            Debug.Log($"[CustomerSpawner] Pool empty ({activeCount}/{customerPool.Count} active), creating new customer instance");
            return CreateNewCustomerInstance();
        }
        
        /// <summary>
        /// Create a new customer instance when pool is exhausted
        /// </summary>
        private CustomerController CreateNewCustomerInstance()
        {
            if (customerPrefabs.Count == 0)
            {
                Debug.LogError("[CustomerSpawner] No customer prefabs assigned!");
                return null;
            }
            
            // Randomly select a prefab
            GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            obj.name = $"Customer_Dynamic_{customerPool.Count}_{prefab.name}";
            
            CustomerController customer = obj.GetComponent<CustomerController>();
            if (customer != null)
            {
                // Add to pool for future reuse
                customerPool.Add(customer);
                Debug.Log($"[CustomerSpawner] Created new customer instance: {obj.name} (pool size now: {customerPool.Count})");
            }
            
            return customer;
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
                data.painTolerance = Random.Range(0f, 0.5f);
                if (data.painTolerance < 0.166f)
                    data.painToleranceLevel = PainToleranceLevel.Low;
                else if (data.painTolerance < 0.333f)
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
                    
                    // Record for daily stats
                    if (Core.DailyStatsManager.Instance != null)
                    {
                        Core.DailyStatsManager.Instance.RecordCustomerSpawned();
                    }
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
                
                // Record for daily stats
                if (Core.DailyStatsManager.Instance != null)
                {
                    Core.DailyStatsManager.Instance.RecordCustomerSpawned();
                }
            }
        }
    }
}
