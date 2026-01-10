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
        
        /// <summary>
        /// Staff assigned to this reception
        /// </summary>
        public Staff.StaffController AssignedStaff { get; set; }
        
        private void Awake()
        {
            // Keep Instance for backward compatibility, but also register with manager
            if (Instance == null)
                Instance = this;
        }
        
        private void Start()
        {
            // Register with manager after it's initialized
            if (ReceptionCounterManager.Instance != null)
            {
                ReceptionCounterManager.Instance.RegisterReception(this);
            }
            else
            {
                Debug.LogWarning($"[ReceptionManager] {name} could not find ReceptionCounterManager!");
            }
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
        
        private void OnDestroy()
        {
            if (ReceptionCounterManager.Instance != null)
            {
                ReceptionCounterManager.Instance.UnregisterReception(this);
            }
        }
        
        /// <summary>
        /// Reset all queues and state when shop closes (new day)
        /// </summary>
        private void ResetQueues()
        {
            Debug.Log($"[ReceptionManager] Resetting queues. Queue: {customerQueue.Count}, Processed: {processedCustomers.Count}");
            
            customerQueue.Clear();
            processedCustomers.Clear();
            // Waiting list is now managed by ReceptionCounterManager
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
        
        [Header("Camera")]
        [Tooltip("Camera position when interacting with this reception")]
        public Transform cameraPosition;
        
        
        
        private System.Collections.Generic.Queue<CustomerController> customerQueue = new System.Collections.Generic.Queue<CustomerController>();
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
            
            // Enqueue FIRST to prevent race condition
            customerQueue.Enqueue(customer);
            processedCustomers.Add(customer);
            
            // Check if this is the first customer AFTER enqueueing
            // (queue count == 1 means this customer is alone in queue)
            bool isFirstCustomer = customerQueue.Count == 1 && currentCustomer == null;
            
            // Start waiting timer for reception queue
            customer.StartWaiting();
            
            // Callback to show notification when customer arrives
            System.Action onArrival = () => 
            {
                // Don't show if panel is already open or staff is handling
                if (ReceptionPanel.Instance?.IsOpen == true) return;
                if (HasStaffAssigned) return;
                
                MessageBoxManager.Instance?.ShowMessage("msg.wait_reception", MessageType.Warning,true,"wait_reception");
            };
            
            // First customer goes directly to reception counter (if receptionPoint exists)
            if (isFirstCustomer && receptionPoint != null)
            {
                Debug.Log($"[ReceptionManager] {customer.data.customerName} going directly to reception counter");
                customer.GoToCounterPoint(receptionPoint, onArrival);
                return receptionPoint;
            }
            
            // Others find an empty chair (prioritize Reception category)
            var chair = ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Reception);
            if (chair != null)
            {
                Debug.Log($"[ReceptionManager] {customer.data.customerName} going to chair {chair.name}");
                customer.GoToChair(chair);
                // Note: Chair arrival doesn't need notification (they're waiting, not ready for service)
                return chair.SeatPosition;
            }
            
            // No chair available - go to counter position
            Debug.LogWarning($"[ReceptionManager] {customer.data.customerName} no chair available, standing at reception");
            customer.GoToCounterPoint(transform, onArrival);
            return transform;
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
                var dis = Vector3.Distance(currentCustomer.transform.position, transform.position);

                if (dis > detectionRadius)
                {
                    return;
                }
                currentCustomer.PauseWaiting();

                MessageBoxManager.Instance.DismissMessage("wait_reception");
                TutorialManager.Instance.TriggerEvent("ReceptionFirst");

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
            
            // Move camera to fixed position
            MoveCameraToPosition();
            
            var partNames = currentCustomer.data.requestedBodyParts.ConvertAll(bp => bp.partName);
            Debug.Log($"[ReceptionManager] Hello {currentCustomer.data.customerName}! Requested plan: {currentCustomer.data.GetPlanDisplayName()}");
            
            // Get station index for this reception
            int stationIndex = ReceptionCounterManager.Instance?.GetReceptionIndex(this) ?? 0;
            
            // Show reception panel with station index
            if (ReceptionPanel.Instance != null)
            {
                ReceptionPanel.Instance.Show(currentCustomer, OnReceptionConfirmed, stationIndex);
            }
            else
            {
                Debug.LogWarning("[ReceptionManager] ReceptionPanel not found! Auto-assigning to bed.");
                AssignToAvailableBed();
            }
        }
        
        /// <summary>
        /// Move camera to the designated position for this station
        /// </summary>
        private void MoveCameraToPosition()
        {
            if (cameraPosition == null) return;
            
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetCameraOverride(cameraPosition.position, cameraPosition.rotation);
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
            
            // ALWAYS add to waiting list to respect FIFO order
            // TrySendWaitingCustomerToBed will handle sending in correct order
            Debug.Log($"[ReceptionManager] {currentCustomer.data.customerName} added to bed waiting list");
            AddToWaitingList(currentCustomer);
            
            // Remove from processed set so they can be processed again if needed
            processedCustomers.Remove(currentCustomer);
            currentCustomer = null;
            
            // NOW advance the queue since customer is in waiting list
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
                    // First customer goes to reception counter ONLY if no one is being processed
                    if (currentCustomer == null && receptionPoint != null)
                    {
                        customer.GoToCounterPoint(receptionPoint);
                        Debug.Log($"[ReceptionManager] {customer.data.customerName} moving to reception counter");
                    }
                    // If someone is being processed, first in queue stays where they are (chair or waiting)
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
            if (currentCustomerAtReception == null) return;
            
            // ALWAYS add to waiting list to respect FIFO order
            // TrySendWaitingCustomerToBed will handle sending in correct order
            AddToWaitingList(currentCustomerAtReception);
            Debug.Log($"[ReceptionManager] {currentCustomerAtReception.data?.customerName} added to bed waiting list via AssignBed");
            currentCustomerAtReception = null;
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
        /// Check if staff is assigned to this specific reception
        /// </summary>
        public bool HasStaffAssigned => AssignedStaff != null;
        
        /// <summary>
        /// Check if this reception can immediately serve a new customer
        /// (no one currently being processed and queue is empty or first can go to counter)
        /// </summary>
        public bool IsAvailable => currentCustomer == null;
        
        /// <summary>
        /// Dequeue customer for staff processing (called by StaffReceptionHandler)
        /// Customer is already at reception counter
        /// </summary>
        public CustomerController DequeueCustomerForStaff()
        {
            if (customerQueue.Count == 0) return null;
            
            // CRITICAL: Don't dequeue if already processing someone
            if (currentCustomer != null)
            {
                Debug.Log($"[ReceptionManager] Cannot dequeue - already processing {currentCustomer.data?.customerName}");
                return null;
            }
            
            var customer = customerQueue.Dequeue();
            
            // CRITICAL: Set currentCustomer to prevent UpdateQueuePositions from sending next customer
            currentCustomer = customer;
            
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
            // CRITICAL: Clear current customer so next can be processed
            currentCustomer = null;
            
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
        
        /// <summary>
        /// Add customer to waiting list for a bed
        /// </summary>
        public void AddToWaitingList(CustomerController customer)
        {
            if (ReceptionCounterManager.Instance != null)
            {
                ReceptionCounterManager.Instance.AddToWaitingList(customer);
            }
            else
            {
                Debug.LogError("[ReceptionManager] ReceptionCounterManager missing! Cannot add to waiting list.");
            }
        }

        public void RemoveFromWaitingList(CustomerController customer)
        {
            if (ReceptionCounterManager.Instance != null)
            {
                ReceptionCounterManager.Instance.RemoveFromWaitingList(customer);
            }
        }
        
        public int WaitingForBedCount => ReceptionCounterManager.Instance?.WaitingForBedCount ?? 0;
        /// </summary>
        // WaitingForBedCount is already defined above
        // public int WaitingForBedCount => waitingForBedList.Count; // REMOVED
        
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
