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

        private List<CustomerController> activeCustomers = new List<CustomerController>();

        private float timer;

        private void Start()
        {
            // Initialize timer so the first customer spawns immediately when the shop opens
            timer = spawnInterval;
        }

        private void Update()
        {
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

        private void SpawnCustomer()
        {
            if (customerPrefab == null || spawnPoint == null) return;

            GameObject obj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            CustomerController customer = obj.GetComponent<CustomerController>();
            
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
                
                customer.Initialize(data, exitPoint, receptionPoint, cashRegisterPoint);
                
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
