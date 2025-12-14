using UnityEngine;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject customerPrefab;
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

        private List<CustomerController> customerPool = new List<CustomerController>();
        private List<CustomerController> activeCustomers = new List<CustomerController>();

        private float timer = 0f;
        private bool poolInitialized = false;

        private void Start()
        {
            // Initialize timer so the first customer spawns after opening
            timer = spawnInterval;
            
            // Initialize customer pool at scene start
            StartCoroutine(InitializePool());
        }
        
        private System.Collections.IEnumerator InitializePool()
        {
            Debug.Log($"[CustomerSpawner] Initializing customer pool with {poolSize} customers...");
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(customerPrefab, Vector3.zero, Quaternion.identity);
                obj.name = $"Customer_Pooled_{i}";
                CustomerController customer = obj.GetComponent<CustomerController>();
                
                if (customer != null)
                {
                    // IMPORTANT: Activate to trigger Start() and initialization
                    customer.gameObject.SetActive(true);
                    Debug.Log($"[CustomerSpawner] Pre-initializing customer {i + 1}/{poolSize} (activated)...");
                    
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
                if (timer >= spawnInterval)
                {
                    // Clean up nulls
                    activeCustomers.RemoveAll(c => c == null);

                    if (activeCustomers.Count < maxCustomers)
                    {
                        Debug.Log($"[CustomerSpawner] Attempting to spawn customer ({activeCustomers.Count}/{maxCustomers})");
                        SpawnCustomer();
                        timer = 0f;
                    }
                    else
                    {
                        Debug.Log($"[CustomerSpawner] Max customers reached ({activeCustomers.Count}/{maxCustomers}), waiting...");
                    }
                }
            }
        }
        
        private CustomerController GetFromPool()
        {
            int activeCount = 0;
            foreach (var customer in customerPool)
            {
                if (customer.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
                else
                {
                    Debug.Log($"[CustomerSpawner] Retrieved {customer.name} from pool");
                    return customer;
                }
            }
            
            Debug.LogWarning($"[CustomerSpawner] No available customers in pool! All {activeCount}/{customerPool.Count} are active. Consider increasing pool size.");
            return null;
        }
        
        public void ReturnToPool(CustomerController customer)
        {
            if (customer == null) return;
            
            Debug.Log($"[CustomerSpawner] Returning {customer.data.customerName} to pool");
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
            if (customerPrefab == null || spawnPoint == null) return;

            // Get customer from pool instead of Instantiate
            CustomerController customer = GetFromPool();
            if (customer == null) return;
            
            // Reset position and activate
            customer.transform.position = spawnPoint.position;
            customer.transform.rotation = spawnPoint.rotation;
            customer.gameObject.SetActive(true);
            
            if (customer != null)
            {
                // Assign BodyPartsDatabase
                customer.bodyPartsDatabase = bodyPartsDatabase;
                
                // Generate customer data (use test settings if enabled)
                CustomerData data = new CustomerData();
                data.customerName = useTestSettings ? "TestCustomer" : "Guest " + Random.Range(100, 999);
                data.hairiness = useTestSettings ? testHairinessLevel : (HairinessLevel)Random.Range(0, 4);
                data.wealth = useTestSettings ? testWealthLevel : (WealthLevel)Random.Range(0, 4);
                
                // Generate pain tolerance level
                data.painTolerance = Random.Range(0f, 1f);
                if (data.painTolerance < 0.33f)
                    data.painToleranceLevel = PainToleranceLevel.Low;
                else if (data.painTolerance < 0.66f)
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
                    int requestPlanIndex = Random.Range(0, 12); // 0-11 for 12 plans
                    data.requestPlan = (CustomerRequestPlan)requestPlanIndex;
                }
                
                // Adjust budget based on plan complexity (number of parts)
                var requiredParts = CustomerPlanHelper.GetRequiredParts(data.requestPlan);
                var detailedParts = CustomerPlanHelper.GetDetailedTreatmentParts(requiredParts);
                int partCount = detailedParts.Length;
                
                // Budget per part based on wealth level
                int budgetPerPart;
                switch (data.wealth)
                {
                    case WealthLevel.Poor:
                        budgetPerPart = Random.Range(15, 21); // $15-20 per part
                        break;
                    case WealthLevel.Average:
                        budgetPerPart = Random.Range(20, 31); // $20-30 per part
                        break;
                    case WealthLevel.Rich:
                        budgetPerPart = Random.Range(30, 46); // $30-45 per part
                        break;
                    case WealthLevel.Tycoon:
                        budgetPerPart = Random.Range(30, 61); // $30-60 per part
                        break;
                    default:
                        budgetPerPart = 20;
                        break;
                }
                
                // Final budget = budget per part × parts (minimum parts = 1)
                data.baseBudget = budgetPerPart * Mathf.Max(1, partCount);
                
                customer.Initialize(data, exitPoint, receptionPoint, cashRegisterPoint, this);
                
                // Checkout Test Mode: Skip reception/treatment, go directly to register
                if (checkoutTestMode)
                {
                    // Set test values
                    data.confirmedPrice = testConfirmedPrice > 0 ? testConfirmedPrice : Random.Range(30, 150);
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
                
                Debug.Log($"[CustomerSpawner] {data.customerName} requested plan: {data.GetPlanDisplayName()} ({partCount} parts), budget: ${data.baseBudget} (${budgetPerPart}/part), tolerance: {data.painToleranceLevel}");
                
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
