using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;
using HairRemovalSim.Customer;
using HairRemovalSim.Environment;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.UI
{
    public class ReceptionManager : MonoBehaviour, IInteractable
    {
        public static ReceptionManager Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void OnEnable()
        {
            // Reset queues when shop OPENS (new day), not when it closes
            // This allows remaining customers to be processed after closing time
            GameEvents.OnShopOpened += ResetQueues;
        }
        
        private void OnDisable()
        {
            GameEvents.OnShopOpened -= ResetQueues;
        }
        
        /// <summary>
        /// Reset all queues and state when shop closes (new day)
        /// </summary>
        private void ResetQueues()
        {
            Debug.Log($"[ReceptionManager] Resetting queues. Queue: {customerQueue.Count}, Processed: {processedCustomers.Count}, Waiting: {waitingForBedList.Count}");
            
            customerQueue.Clear();
            processedCustomers.Clear();
            waitingForBedList.Clear();
            currentCustomer = null;
            currentCustomerAtReception = null;
            
            Debug.Log("[ReceptionManager] All queues reset for new day");
        }
        
        [Header("Settings")]
        // Beds are now referenced from ShopManager.Instance.Beds
        public IReadOnlyList<BedController> beds => ShopManager.Instance?.Beds;
        public float detectionRadius = 2.0f; // Distance to detect customers
        
        [Header("Staff")]
        [Tooltip("Position where staff stands when assigned to reception")]
        public Transform staffPoint;
        
        [Tooltip("Position where restock staff stands to refill items")]
        public Transform restockPoint;
        
        [Tooltip("Position where customer stands when being served")]
        public Transform receptionPoint;
        
        private System.Collections.Generic.Queue<CustomerController> customerQueue = new System.Collections.Generic.Queue<CustomerController>();
        private System.Collections.Generic.List<CustomerController> waitingForBedList = new System.Collections.Generic.List<CustomerController>();
        private CustomerController currentCustomer = null;
        private CustomerController currentCustomerAtReception;
        private System.Collections.Generic.HashSet<CustomerController> processedCustomers = new System.Collections.Generic.HashSet<CustomerController>();

        /// <summary>
        /// Register a customer to the queue and return their assigned position
        /// </summary>
        public Transform RegisterCustomer(CustomerController customer)
        {
            if (processedCustomers.Contains(customer))
            {
                Debug.LogWarning($"[ReceptionManager] {customer.data.customerName} already registered");
                return null;
            }
            
            // Check if this is the first customer (queue empty AND no one being processed)
            bool isFirstCustomer = customerQueue.Count == 0 && currentCustomer == null;
            
            customerQueue.Enqueue(customer);
            processedCustomers.Add(customer);
            
            // Start waiting timer for reception queue
            customer.StartWaiting();
            
            // First customer goes directly to reception counter (if receptionPoint exists)
            if (isFirstCustomer && receptionPoint != null)
            {
                Debug.Log($"[ReceptionManager] {customer.data.customerName} going directly to reception counter");
                customer.GoToCounterPoint(receptionPoint);
                return receptionPoint;
            }
            
            // Others find an empty chair (prioritize Reception category)
            var chair = ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Reception);
            if (chair != null)
            {
                Debug.Log($"[ReceptionManager] {customer.data.customerName} going to chair {chair.name}");
                customer.GoToChair(chair);
                return chair.SeatPosition;
            }
            else
            {
                Debug.LogWarning($"[ReceptionManager] {customer.data.customerName} no chair available, standing at reception");
                customer.GoToCounterPoint(transform);
                return transform;
            }
        }

        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            // Block interaction if staff is assigned
            if (HasStaffAssigned)
            {
                Debug.Log("[ReceptionManager] Staff is handling reception, player cannot interact");
                return;
            }
            
            // Process first customer in queue (if not already processing one)
            if (currentCustomer == null && customerQueue.Count > 0)
            {
                currentCustomer = customerQueue.Dequeue();
                // Don't update queue positions yet - wait until customer confirms and moves to bed
            }
            
            if (currentCustomer != null)
            {
                // Always pause waiting when opening UI (handles re-interact after cancel)
                currentCustomer.PauseWaiting();
                OpenReceptionUI();
            }
            else
            {
                Debug.Log("[ReceptionManager] No customers waiting in queue");
            }
        }

        public void OnHoverEnter()
        {
            var outline = GetComponent<Effects.OutlineHighlighter>();
            if (outline != null) outline.enabled = true;
        }
        
        public void OnHoverExit()
        {
            var outline = GetComponent<Effects.OutlineHighlighter>();
            if (outline != null) outline.enabled = false;
        }
        
        public string GetInteractionPrompt()
        {
            return currentCustomerAtReception != null ? "Speak to Customer" : "Reception Desk";
        }

        private void OpenReceptionUI()
        {
            if (currentCustomer == null) return;
            
            var partNames = currentCustomer.data.requestedBodyParts.ConvertAll(bp => bp.partName);
            Debug.Log($"[ReceptionManager] Hello {currentCustomer.data.customerName}! Requested plan: {currentCustomer.data.GetPlanDisplayName()}");
            
            // Show reception panel
            if (ReceptionPanel.Instance != null)
            {
                ReceptionPanel.Instance.Show(currentCustomer, OnReceptionConfirmed);
            }
            else
            {
                Debug.LogWarning("[ReceptionManager] ReceptionPanel not found! Auto-assigning to bed.");
                AssignToAvailableBed();
            }
        }
        
        /// <summary>
        /// Called when player confirms in reception panel
        /// </summary>
        private void OnReceptionConfirmed(CustomerController customer, Customer.TreatmentBodyPart selectedParts, Customer.TreatmentMachine machine, bool useAnesthesia, int price)
        {
            Debug.Log($"[ReceptionManager] Confirmed: {customer.data.customerName} - Parts: {selectedParts}, Machine: {machine}, Anesthesia: {useAnesthesia}, Price: ${price}");
            
            // Assign to bed
            AssignToAvailableBed();
        }

        private void AssignToAvailableBed()
        {
            if (currentCustomer == null) return;
            
            // Collect all available beds
            var availableBeds = new System.Collections.Generic.List<BedController>();
            foreach (var bed in beds)
            {
                if (bed != null && !bed.IsOccupied)
                {
                    availableBeds.Add(bed);
                }
            }
            
            // Pick a random available bed
            if (availableBeds.Count > 0)
            {
                var randomBed = availableBeds[UnityEngine.Random.Range(0, availableBeds.Count)];
                currentCustomer.GoToBed(randomBed);
                Debug.Log($"[ReceptionManager] {currentCustomer.data.customerName} sent to {randomBed.name} (random)");
                
                // Remove from processed set so they can be processed again if needed
                processedCustomers.Remove(currentCustomer);
                currentCustomer = null;
                
                // NOW advance the queue since customer is heading to bed
                UpdateQueuePositions();
                ProcessNextCustomer();
                return;
            }
            
            // No available beds - send customer to waiting area
            Debug.Log($"[ReceptionManager] No available beds! {currentCustomer.data.customerName} sent to waiting area.");
            AddToWaitingList(currentCustomer);
            
            // Clear current customer and advance queue
            processedCustomers.Remove(currentCustomer);
            currentCustomer = null;
            UpdateQueuePositions();
            ProcessNextCustomer();
        }
        
        private void UpdateQueuePositions()
        {
            int index = 0;
            foreach (var customer in customerQueue)
            {
                if (index == 0)
                {
                    // First customer goes to reception counter
                    if (receptionPoint != null)
                    {
                        customer.GoToCounterPoint(receptionPoint);
                        Debug.Log($"[ReceptionManager] {customer.data.customerName} moving to reception counter");
                    }
                }
                else
                {
                    // If customer doesn't have a chair, find one
                    if (customer.CurrentChair == null)
                    {
                        var chair = ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Reception);
                        if (chair != null)
                        {
                            customer.GoToChair(chair);
                            Debug.Log($"[ReceptionManager] {customer.data.customerName} moving to chair {chair.name}");
                        }
                    }
                    // If already has chair, stay there
                }
                index++;
            }
        }
        
        private void ProcessNextCustomer()
        {
            if (customerQueue.Count > 0 && currentCustomer == null)
            {
                Debug.Log($"[ReceptionManager] {customerQueue.Count} customer(s) waiting in queue");
            }
        }

        public void AssignBed(int bedIndex)
        {
            if (beds != null && bedIndex >= 0 && bedIndex < beds.Count)
            {
                currentCustomerAtReception.GoToBed(beds[bedIndex]);
                currentCustomerAtReception = null; // Clear reception
                Debug.Log("Customer sent to bed.");
            }
        }
        
        /// <summary>
        /// Clear current customer (called when customer leaves at reception)
        /// </summary>
        public void ClearCurrentCustomer(CustomerController customer)
        {
            if (currentCustomer == customer)
            {
                processedCustomers.Remove(customer);
                currentCustomer = null;
                Debug.Log($"[ReceptionManager] Cleared current customer who left at reception");
                
                // Update queue positions after customer leaves
                UpdateQueuePositions();
            }
        }
        
        /// <summary>
        /// Unregister customer completely from queue and processed list
        /// Called when customer returns to pool
        /// </summary>
        public void UnregisterCustomer(CustomerController customer)
        {
            if (customer == null) return;
            
            // Remove from processed set
            processedCustomers.Remove(customer);
            
            // Clear if current customer
            if (currentCustomer == customer)
            {
                currentCustomer = null;
            }
            
            // Note: We can't easily remove from Queue, but the Queue will be cleaned during iteration
            // The processedCustomers HashSet is the main check for "already registered"
            
            Debug.Log($"[ReceptionManager] Unregistered {customer.data?.customerName ?? "unknown"} from reception");
        }
        
        /// <summary>
        /// Check if staff is assigned to reception
        /// </summary>
        public bool HasStaffAssigned
        {
            get
            {
                var staffManager = Staff.StaffManager.Instance;
                if (staffManager == null) return false;
                
                foreach (var staff in staffManager.GetHiredStaff())
                {
                    if (staff.isActive && staff.assignment == Staff.StaffAssignment.Reception)
                        return true;
                }
                return false;
            }
        }
        
        /// <summary>
        /// Dequeue customer for staff processing (called by StaffReceptionHandler)
        /// Customer is already at reception counter
        /// </summary>
        public CustomerController DequeueCustomerForStaff()
        {
            if (customerQueue.Count == 0) return null;
            
            var customer = customerQueue.Dequeue();
            
            // Remove from processed set - customer is now being served
            processedCustomers.Remove(customer);
            
            // NOTE: Do NOT update remaining customers' positions here.
            // Queue should only advance after staff finishes processing the current customer.
            
            Debug.Log($"[ReceptionManager] Staff dequeued {customer?.data?.customerName}, remaining: {customerQueue.Count}");
            
            return customer;
        }
        
        /// <summary>
        /// Advance the queue - move next customer to counter and others forward
        /// </summary>
        public void AdvanceQueue()
        {
            UpdateQueuePositions();
        }
        
        /// <summary>
        /// Return customer to front of queue (called when staff cancels reception)
        /// </summary>
        public void ReturnCustomerToQueue(CustomerController customer)
        {
            if (customer == null) return;
            
            // Create new queue with this customer at front
            var newQueue = new Queue<CustomerController>();
            newQueue.Enqueue(customer);
            
            // Add remaining customers
            while (customerQueue.Count > 0)
            {
                newQueue.Enqueue(customerQueue.Dequeue());
            }
            
            customerQueue = newQueue;
            
            // Mark as not processed so player can interact
            processedCustomers.Remove(customer);
            
            UpdateQueuePositions();
            
            Debug.Log($"[ReceptionManager] {customer.data?.customerName} returned to front of queue");
        }
        
        /// <summary>
        /// Get current queue count
        /// </summary>
        public int QueueCount => customerQueue.Count;
        
        // ========== BED WAITING AREA ==========
        
        private void Update()
        {
            // Check if any waiting customers can go to a bed
            if (waitingForBedList.Count > 0)
            {
                TrySendWaitingCustomerToBed();
            }
        }
        
        /// <summary>
        /// Add customer to waiting list for a bed
        /// </summary>
        public void AddToWaitingList(CustomerController customer)
        {
            if (customer == null || waitingForBedList.Contains(customer)) return;
            
            waitingForBedList.Add(customer);
            
            // Find a Waiting category chair for the customer
            var chair = ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Waiting);
            if (chair != null)
            {
                customer.GoToChair(chair);
                customer.StartWaiting();
                Debug.Log($"[ReceptionManager] {customer.data?.customerName} sent to waiting chair {chair.name}");
            }
            else
            {
                // Fallback: try any empty chair
                chair = ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Reception);
                if (chair != null)
                {
                    customer.GoToChair(chair);
                }
                else
                {
                    customer.GoToCounterPoint(transform);
                }
                customer.StartWaiting();
                Debug.Log($"[ReceptionManager] {customer.data?.customerName} sent to fallback waiting area");
            }
        }
        
        /// <summary>
        /// Try to send a waiting customer to an available bed
        /// </summary>
        private void TrySendWaitingCustomerToBed()
        {
            // Collect all available beds and pick random
            var availableBeds = new System.Collections.Generic.List<Environment.BedController>();
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
            
            var availableBed = availableBeds[UnityEngine.Random.Range(0, availableBeds.Count)];
            
            // Get first waiting customer
            var customer = waitingForBedList[0];
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                waitingForBedList.RemoveAt(0);
                return;
            }
            
            // Send to bed
            waitingForBedList.RemoveAt(0);
            customer.GoToBed(availableBed);
            
            Debug.Log($"[ReceptionManager] {customer.data?.customerName} sent to bed from waiting area");
            
            // Update waiting positions
            UpdateWaitingPositions();
        }
        
        /// <summary>
        /// Update waiting customer positions after one leaves
        /// Customers stay on their chairs, no repositioning needed
        /// </summary>
        private void UpdateWaitingPositions()
        {
            // With ChairManager, customers stay on their chairs until called to bed
            // No repositioning needed
        }
        
        /// <summary>
        /// Remove customer from waiting list (e.g., when they leave)
        /// </summary>
        public void RemoveFromWaitingList(CustomerController customer)
        {
            if (waitingForBedList.Remove(customer))
            {
                UpdateWaitingPositions();
                Debug.Log($"[ReceptionManager] {customer?.data?.customerName} removed from waiting list");
            }
        }
        
        /// <summary>
        /// Get waiting list count
        /// </summary>
        public int WaitingForBedCount => waitingForBedList.Count;
        
        /// <summary>
        /// Refresh bed references from ShopManager
        /// Called when beds are added during shop upgrades
        /// </summary>
        public void RefreshBedReferences()
        {
            // Beds are now auto-referenced from ShopManager.Instance.Beds
            Debug.Log($"[ReceptionManager] Beds auto-referenced from ShopManager. Total beds: {beds?.Count ?? 0}");
        }
    }
}
