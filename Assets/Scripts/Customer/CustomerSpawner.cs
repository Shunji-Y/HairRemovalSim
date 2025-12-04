using UnityEngine;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject customerPrefab;
        public Transform spawnPoint;
        public Transform exitPoint;
        public Transform receptionPoint; // Pre-treatment reception
        public Transform cashRegisterPoint; // Post-treatment payment
        public float spawnInterval = 30f; // Seconds
        public int maxCustomers = 3;
        
        [Header("Object Pool")]
        public int poolSize = 3; // Pre-initialized customers

        private List<CustomerController> customerPool = new List<CustomerController>();
        private List<CustomerController> activeCustomers = new List<CustomerController>();

        private float timer;
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
            if (!poolInitialized) return;
            
            if (GameManager.Instance.CurrentState == GameManager.GameState.Day)
            {
                timer += Time.deltaTime;
                if (timer >= spawnInterval)
                {
                    // Clean up nulls
                    activeCustomers.RemoveAll(c => c == null);

                    if (activeCustomers.Count < maxCustomers)
                    {
                        SpawnCustomer();
                        timer = 0f;
                    }
                }
            }
        }
        
        private CustomerController GetFromPool()
        {
            foreach (var customer in customerPool)
            {
                if (!customer.gameObject.activeInHierarchy)
                {
                    return customer;
                }
            }
            
            Debug.LogWarning("[CustomerSpawner] No available customers in pool! Consider increasing pool size.");
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
                
                // Generate random requested body parts (1-3 parts) from actual BodyPart components
                var allBodyParts = new System.Collections.Generic.List<Core.BodyPart>(customer.GetComponentsInChildren<Core.BodyPart>());
                
                if (allBodyParts.Count > 0)
                {
                    int partCount = Random.Range(1, Mathf.Min(4, allBodyParts.Count + 1)); // 1 to min(3, totalParts)
                    
                    // Shuffle and take first N
                    for (int i = 0; i < partCount; i++)
                    {
                        if (allBodyParts.Count > 0)
                        {
                            int randomIndex = Random.Range(0, allBodyParts.Count);
                            data.requestedBodyParts.Add(allBodyParts[randomIndex]);
                            allBodyParts.RemoveAt(randomIndex); // Avoid duplicates
                        }
                    }
                    
                    Debug.Log($"[CustomerSpawner] {data.customerName} requesting {data.requestedBodyParts.Count} parts: {string.Join(", ", data.requestedBodyParts.ConvertAll(bp => bp.partName))}");
                }
                else
                {
                    Debug.LogWarning($"[CustomerSpawner] {data.customerName} has no BodyPart components! Cannot assign requested parts.");
                }
                
                // Send customer to reception first (not directly to bed)
                if (receptionPoint != null)
                {
                    customer.GoToReception(receptionPoint);
                    Debug.Log($"[CustomerSpawner] {data.customerName} heading to reception");
                }
                else
                {
                    Debug.LogWarning("[CustomerSpawner] No reception point set!");
                }
                
                activeCustomers.Add(customer);
            }
        }
    }
}
