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
        public Transform receptionPoint;
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
                
                customer.Initialize(data, exitPoint, receptionPoint);
                
                // Find a free bed
                var beds = FindObjectsOfType<Environment.BedController>();
                Environment.BedController freeBed = null;
                foreach (var bed in beds)
                {
                    if (!bed.IsOccupied)
                    {
                        freeBed = bed;
                        break;
                    }
                }

                if (freeBed != null)
                {
                    customer.GoToBed(freeBed);
                }
                else
                {
                    // Fallback to reception if no bed
                    customer.GoToReception(receptionPoint);
                }
                
                activeCustomers.Add(customer);
            }
        }
    }
}
