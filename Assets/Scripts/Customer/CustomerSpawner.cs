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
        
        [Header("Spawn Intervals")]
        public float spawnInterval = 30f; // Seconds
        public int maxCustomers = 3;
        
        [Header("Object Pool")]
        public int poolSize = 3; // Pre-initialized customers

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
                // Generate random data
                CustomerData data = new CustomerData();
                data.customerName = "Guest " + Random.Range(100, 999);
                data.hairiness = (HairinessLevel)Random.Range(0, 4);
                data.wealth = (WealthLevel)Random.Range(0, 4);
                
                // Calculate budget based on wealth level
                switch (data.wealth)
                {
                    case WealthLevel.Poor:
                        data.baseBudget = Random.Range(20, 50);
                        break;
                    case WealthLevel.Average:
                        data.baseBudget = Random.Range(50, 100);
                        break;
                    case WealthLevel.Rich:
                        data.baseBudget = Random.Range(100, 200);
                        break;
                    case WealthLevel.Tycoon:
                        data.baseBudget = Random.Range(200, 500);
                        break;
                }
                
                customer.Initialize(data, exitPoint, receptionPoint, cashRegisterPoint, this);
                
                // Select a random treatment plan instead of random individual parts
                TreatmentPlan selectedPlan = (TreatmentPlan)Random.Range(0, System.Enum.GetValues(typeof(TreatmentPlan)).Length);
                
                // Clear previous requested parts
                data.requestedBodyParts.Clear();
                
                // Get body part names for this plan
                var requiredPartNames = selectedPlan.GetBodyPartNames();
                
                // Find matching BodyPart components by GameObject name
                var allBodyParts = customer.GetComponentsInChildren<Core.BodyPart>(true); // Include inactive
                
                foreach (var partName in requiredPartNames)
                {
                    // Find BodyPart whose GameObject name matches
                    var matchingPart = System.Array.Find(allBodyParts, bp => bp.gameObject.name == partName);
                    
                    if (matchingPart != null)
                    {
                        data.requestedBodyParts.Add(matchingPart);
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomerSpawner] Could not find BodyPart with GameObject name '{partName}' for plan {selectedPlan}");
                    }
                }
                
                // Fallback: if no parts found, assign all parts (FullBody)
                if (data.requestedBodyParts.Count == 0)
                {
                    Debug.LogWarning($"[CustomerSpawner] No body parts found for {selectedPlan}, assigning all available parts");
                    foreach (var part in allBodyParts)
                    {
                        data.requestedBodyParts.Add(part);
                    }
                }
                
                Debug.Log($"[CustomerSpawner] {data.customerName} selected plan: {selectedPlan.GetDisplayName()} ({data.requestedBodyParts.Count} parts)");
                
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
