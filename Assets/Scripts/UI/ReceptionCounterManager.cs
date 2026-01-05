using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Customer;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Manages multiple ReceptionManagers (reception counters) in the shop.
    /// Routes customers to shortest queue and manages staff assignments.
    /// </summary>
    public class ReceptionCounterManager : MonoBehaviour
    {
        public static ReceptionCounterManager Instance { get; private set; }
        
        [Header("Debug")]
        [SerializeField] private List<ReceptionManager> receptions = new List<ReceptionManager>();
        
        public int ReceptionCount => receptions.Count;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        /// <summary>
        /// Register a ReceptionManager with this manager
        /// </summary>
        public void RegisterReception(ReceptionManager reception)
        {
            if (reception != null && !receptions.Contains(reception))
            {
                receptions.Add(reception);
                Debug.Log($"[ReceptionCounterManager] Registered {reception.name}. Total: {receptions.Count}");
            }
        }
        
        /// <summary>
        /// Unregister a ReceptionManager from this manager
        /// </summary>
        public void UnregisterReception(ReceptionManager reception)
        {
            if (receptions.Remove(reception))
            {
                Debug.Log($"[ReceptionCounterManager] Unregistered {reception.name}. Total: {receptions.Count}");
            }
        }
        
        /// <summary>
        /// Get the reception with the shortest queue for customer routing
        /// Prioritizes receptions that can immediately serve (IsAvailable)
        /// </summary>
        public ReceptionManager GetShortestQueueReception()
        {
            if (receptions.Count == 0) return null;
            if (receptions.Count == 1) return receptions[0];
            
            // First priority: Find an available reception (not currently processing)
            // with the shortest queue
            ReceptionManager bestAvailable = null;
            int bestAvailableCount = int.MaxValue;
            
            // Second priority: Shortest queue overall (if no available)
            ReceptionManager shortestOverall = null;
            int shortestCount = int.MaxValue;
            
            foreach (var reception in receptions)
            {
                if (reception == null) continue;
                
                int queueCount = reception.QueueCount;
                
                // Track shortest queue overall
                if (queueCount < shortestCount)
                {
                    shortestCount = queueCount;
                    shortestOverall = reception;
                }
                
                // Track best available (can immediately serve)
                if (reception.IsAvailable && queueCount < bestAvailableCount)
                {
                    bestAvailableCount = queueCount;
                    bestAvailable = reception;
                }
            }
            
            // Prefer available reception, fallback to shortest queue
            return bestAvailable ?? shortestOverall ?? receptions[0];
        }
        
        /// <summary>
        /// Get a reception by index (for staff assignment)
        /// </summary>
        public ReceptionManager GetReceptionByIndex(int index)
        {
            if (index >= 0 && index < receptions.Count)
            {
                return receptions[index];
            }
            return null;
        }
        
        /// <summary>
        /// Get index of a reception
        /// </summary>
        public int GetReceptionIndex(ReceptionManager reception)
        {
            return receptions.IndexOf(reception);
        }
        
        /// <summary>
        /// Get a reception that doesn't have staff assigned (for auto-assignment)
        /// </summary>
        public ReceptionManager GetUnstaffedReception()
        {
            foreach (var reception in receptions)
            {
                if (reception != null && !reception.HasStaffAssigned)
                {
                    return reception;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Get all receptions
        /// </summary>
        public IReadOnlyList<ReceptionManager> GetAllReceptions()
        {
            return receptions.AsReadOnly();
        }
        
        /// <summary>
        /// Find nearest reception to a position (for player interaction)
        /// </summary>
        public ReceptionManager GetNearestReception(Vector3 position)
        {
            if (receptions.Count == 0) return null;
            if (receptions.Count == 1) return receptions[0];
            
            ReceptionManager nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var reception in receptions)
            {
                if (reception == null) continue;
                
                float dist = Vector3.Distance(position, reception.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = reception;
                }
            }
            
            return nearest;
        }
        
        // Shared waiting list for all receptions
        private List<CustomerController> waitingForBedList = new List<CustomerController>();
        
        // Beds reference
        public IReadOnlyList<Environment.BedController> beds => Core.ShopManager.Instance?.Beds;

        private void Update()
        {
            // Check if any waiting customers can go to a bed
            if (waitingForBedList.Count > 0)
            {
                TrySendWaitingCustomerToBed();
            }
        }

        /// <summary>
        /// Try to send a waiting customer to an available bed
        /// </summary>
        private void TrySendWaitingCustomerToBed()
        {
            // Collect all available beds
            var availableBeds = new List<Environment.BedController>();
            if (beds != null)
            {
                foreach (var bed in beds)
                {
                    if (bed != null && !bed.IsOccupied)
                    {
                        availableBeds.Add(bed);
                    }
                }
            }
            
            if (availableBeds.Count == 0) return;
            
            var availableBed = availableBeds[Random.Range(0, availableBeds.Count)];
            
            // Get first waiting customer (FIFO)
            var customer = waitingForBedList[0];
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                waitingForBedList.RemoveAt(0);
                return;
            }
            
            // CRITICAL: Reserve bed IMMEDIATELY to prevent next frame from sending another customer
            if (!availableBed.AssignCustomer(customer))
            {
                Debug.LogWarning($"[ReceptionCounterManager] Could not reserve bed for {customer.data?.customerName}");
                return;
            }
            
            // Now send to bed (bed is already reserved)
            waitingForBedList.RemoveAt(0);
            customer.GoToBed(availableBed);
            
            Debug.Log($"[ReceptionCounterManager] {customer.data?.customerName} sent to bed from shared waiting area");
        }

        /// <summary>
        /// Add customer to waiting list for a bed (shared list)
        /// </summary>
        public void AddToWaitingList(CustomerController customer)
        {
            if (customer == null || waitingForBedList.Contains(customer)) return;
            
            waitingForBedList.Add(customer);
            customer.StartWaiting();
            
            // Find a chair in ANY waiting area
            var chair = Core.ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Waiting);
            if (chair != null)
            {
                customer.GoToChair(chair);
                Debug.Log($"[ReceptionCounterManager] {customer.data.customerName} added to waiting list and sent to chair {chair.name}");
            }
            else
            {
                // No chair available - wait standing (maybe near nearest reception?)
                Debug.Log($"[ReceptionCounterManager] {customer.data.customerName} added to waiting list (no chair available)");
            }
        }
        
        /// <summary>
        /// Remove customer from waiting list
        /// </summary>
        public void RemoveFromWaitingList(CustomerController customer)
        {
            if (waitingForBedList.Remove(customer))
            {
                Debug.Log($"[ReceptionCounterManager] {customer?.data?.customerName} removed from shared waiting list");
            }
        }
        
        /// <summary>
        /// Get shared waiting list count
        /// </summary>
        public int WaitingForBedCount => waitingForBedList.Count;
    }
}
