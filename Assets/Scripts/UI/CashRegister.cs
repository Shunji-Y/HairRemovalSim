using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;
using HairRemovalSim.Player;

namespace HairRemovalSim.UI
{
    public class CashRegister : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        public float detectionRadius = 2.0f;
        
        [Header("Staff")]
        [Tooltip("Position where staff stands when assigned to cashier")]
        public Transform staffPoint;
        
        [Tooltip("Position where restock staff stands to refill items")]
        public Transform restockPoint;
        
        [Tooltip("Position where customer stands when being served")]
        public Transform cashierPoint;
        
        [Header("Camera")]
        [Tooltip("Camera position when interacting with this register")]
        public Transform cameraPosition;
        
        private System.Collections.Generic.Queue<CustomerController> customerQueue = new System.Collections.Generic.Queue<CustomerController>();
        private CustomerController currentCustomer = null;
        private CustomerController customerAtRegister;
        private System.Collections.Generic.HashSet<CustomerController> processedCustomers = new System.Collections.Generic.HashSet<CustomerController>();

        /// <summary>
        /// Register a customer to the payment queue and return their assigned position
        /// </summary>
        public Transform RegisterCustomer(CustomerController customer)
        {
            if (processedCustomers.Contains(customer))
            {
                Debug.LogWarning($"[CashRegister] {customer.data.customerName} already registered");
                return null;
            }
            
            // Enqueue FIRST to prevent race condition
            customerQueue.Enqueue(customer);
            processedCustomers.Add(customer);
            
            // Check if this is the first customer AFTER enqueueing
            // AND no one is currently being processed
            bool isFirstCustomer = customerQueue.Count == 1 && currentCustomer == null;
            
            // Start waiting timer for cashier queue
            customer.StartWaiting();
            
            // Callback to show notification when customer arrives
            System.Action onArrival = () => 
            {
                // Don't show if panel is already open or staff is handling
                if (PaymentPanel.Instance?.IsOpen == true) return;
                if (HasStaffAssigned) return;
                
                MessageBoxManager.Instance?.ShowMessage("msg.wait_cashier", MessageType.Warning,true,"wait_cashier");
            };
            
            // First customer goes directly to cashier counter
            if (isFirstCustomer && cashierPoint != null)
            {
                Debug.Log($"[CashRegister] {customer.data.customerName} going directly to cashier counter");
                customer.GoToCounterPoint(cashierPoint, onArrival);
                return cashierPoint;
            }
            
            // Others find an empty chair (prioritize Cashier category)
            var chair = Core.ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Cashier);
            if (chair != null)
            {
                Debug.Log($"[CashRegister] {customer.data.customerName} going to chair {chair.name}");
                customer.GoToChair(chair);
                // Note: Chair arrival doesn't need notification
                return chair.SeatPosition;
            }
            
            // No chair available - go to counter position
            Debug.LogWarning($"[CashRegister] {customer.data.customerName} no chair available, standing at register");
            customer.GoToCounterPoint(transform, onArrival);
            return transform;
        }
        
        /// <summary>
        /// Unregister a customer (called when customer returns to pool)
        /// </summary>
        public void UnregisterCustomer(CustomerController customer)
        {
            if (customer == null) return;
            
            processedCustomers.Remove(customer);
            
            if (customer == currentCustomer)
            {
                currentCustomer = null;
            }
            
            // Note: We don't remove from queue here as CleanupQueue will handle it
            Debug.Log($"[CashRegister] Unregistered customer {customer.data?.customerName}");
        }

        public void OnInteract(InteractionController interactor)
        {
            // Block interaction if PaymentPanel is already open
            if (PaymentPanel.Instance != null && PaymentPanel.Instance.IsOpen)
            {
                Debug.Log("[CashRegister] PaymentPanel is already open, ignoring interaction");
                return;
            }
            
            // Block interaction if staff is assigned
            if (HasStaffAssigned)
            {
                Debug.Log("[CashRegister] Staff is handling cashier, player cannot interact");
                return;
            }
            
            Debug.Log($"[CashRegister] OnInteract - currentCustomer: {currentCustomer?.data?.customerName ?? "NULL"}, queue: {customerQueue.Count}");

       

            // Clear invalid currentCustomer (destroyed or inactive)
            try
            {
                if (currentCustomer != null && (currentCustomer.gameObject == null || !currentCustomer.gameObject.activeInHierarchy))
                {
                    Debug.LogWarning("[CashRegister] currentCustomer is invalid, clearing");
                    currentCustomer = null;
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("[CashRegister] currentCustomer reference is broken, clearing");
                currentCustomer = null;
            }
            
            // PEEK first customer in queue (don't dequeue yet - wait for Payment button)
            if (currentCustomer == null && customerQueue.Count > 0)
            {
                currentCustomer = customerQueue.Peek(); // PEEK, not Dequeue!
                Debug.Log($"[CashRegister] Peeked {currentCustomer.data?.customerName}, still in queue: {customerQueue.Count}");
            }
            
            // Open payment panel if we have a customer
            if (currentCustomer != null)
            {
                var dis = Vector3.Distance(currentCustomer.transform.position, transform.position);

                if (dis>detectionRadius)
                {
                    return;
                }
                MessageBoxManager.Instance.DismissMessage("wait_cashier");
                TutorialManager.Instance.TriggerEvent("CashierFirst");
                ProcessPayment();


            }
            else
            {
                Debug.Log("[CashRegister] No customers waiting in queue");
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
        
        public string GetInteractionPrompt() => customerAtRegister != null 
            ? Core.LocalizationManager.Instance?.Get("prompt.collect_payment") ?? "Collect Payment"
            : Core.LocalizationManager.Instance?.Get("prompt.cash_register") ?? "Cash Register";

        private void ProcessPayment()
        {
            if (currentCustomer == null) return;
            
            // Move camera to fixed position
            MoveCameraToPosition();
            
            // Get station index for this register
            int stationIndex = CashRegisterManager.Instance?.GetRegisterIndex(this) ?? 0;
            
            // Open PaymentPanel with station index
            if (PaymentPanel.Instance != null)
            {
                PaymentPanel.Instance.Show(currentCustomer, stationIndex);
            }
            else
            {
                Debug.LogWarning("[CashRegister] PaymentPanel not found! Processing direct payment.");
                
                // Fallback: direct payment
                int finalAmount = currentCustomer.CalculateFinalPayment();
                EconomyManager.Instance.AddMoney(finalAmount);
                Debug.Log($"[CashRegister] {currentCustomer.data.customerName} paid ${finalAmount}");
                
                currentCustomer.LeaveShop();
                processedCustomers.Remove(currentCustomer);
                currentCustomer = null;
                ProcessNextCustomer();
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
        /// Called by PaymentPanel when payment is processed (success or customer left)
        /// </summary>
        public void OnPaymentProcessed(CustomerController customer)
        {
            Debug.Log($"[CashRegister] OnPaymentProcessed called for {customer?.data?.customerName ?? "NULL"}");
            
            // NOW dequeue the customer (they were only Peeked before)
            if (customerQueue.Count > 0 && customerQueue.Peek() == customer)
            {
                customerQueue.Dequeue();
                Debug.Log($"[CashRegister] Dequeued {customer?.data?.customerName} after payment, remaining: {customerQueue.Count}");
            }
            
            // Clear current customer
            if (customer != null)
            {
                processedCustomers.Remove(customer);
            }
            currentCustomer = null;
            
            // Clean up queue - remove any invalid customers
            CleanupQueue();
            
            // NOW update queue positions - remaining customers move forward
            UpdateQueuePositions();
            
            ProcessNextCustomer();
        }
        
        /// <summary>
        /// Remove invalid (destroyed/inactive) customers from queue
        /// </summary>
        private void CleanupQueue()
        {
            var validCustomers = new System.Collections.Generic.Queue<CustomerController>();
            int originalCount = customerQueue.Count;
            
            while (customerQueue.Count > 0)
            {
                var customer = customerQueue.Dequeue();
                
                // Use Unity's null coalescing - returns true if object is destroyed
                bool isValid = false;
                try
                {
                    // Unity overrides == operator, so this properly checks for destroyed objects
                    isValid = customer != null && customer.gameObject != null && customer.gameObject.activeInHierarchy;
                }
                catch (System.Exception)
                {
                    isValid = false;
                }
                
                if (isValid)
                {
                    validCustomers.Enqueue(customer);
                }
                else
                {
                    Debug.LogWarning($"[CashRegister] Removed invalid customer from queue");
                }
            }
            
            customerQueue = validCustomers;
           // Debug.Log($"[CashRegister] CleanupQueue: kept {validCustomers.Count} of {originalCount} customers");
        }
        
        private void UpdateQueuePositions()
        {
            int index = 0;
            foreach (var customer in customerQueue)
            {
                if (index == 0)
                {
                    // First customer goes to cashier counter
                    if (cashierPoint != null)
                    {
                        // Release the chair before moving to counter so it's available for others
                        customer.ReleaseChair();
                        customer.GoToCounterPoint(cashierPoint);
                        Debug.Log($"[CashRegister] {customer.data.customerName} moving to cashier counter");
                    }
                }
                else
                {
                    // If customer doesn't have a chair, find one
                    if (customer.CurrentChair == null)
                    {
                        var chair = Core.ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Cashier);
                        if (chair != null)
                        {
                            customer.GoToChair(chair);
                            Debug.Log($"[CashRegister] {customer.data.customerName} moving to chair {chair.name}");
                        }
                    }
                }
                index++;
            }
        }
        
        private void ProcessNextCustomer()
        {
            if (customerQueue.Count > 0 && currentCustomer == null)
            {
                Debug.Log($"[CashRegister] {customerQueue.Count} customer(s) waiting in queue");
            }
        }
        

        
        /// <summary>
        /// Dequeue customer for staff processing
        /// Customer is already at cashier counter
        /// </summary>
        public CustomerController DequeueCustomerForStaff()
        {
            // Clean up invalid customers first
            CleanupQueue();
            
            if (customerQueue.Count == 0) return null;
            
            // CRITICAL: Don't dequeue if already processing someone
            if (currentCustomer != null)
            {
                Debug.Log($"[CashRegister] Cannot dequeue - already processing {currentCustomer.data?.customerName}");
                return null;
            }
            
            var customer = customerQueue.Dequeue();
            
            // CRITICAL: Set currentCustomer to prevent UpdateQueuePositions from sending next customer
            currentCustomer = customer;
            
            processedCustomers.Remove(customer);
            
            // Customer is already at counter, no need to move them
            
            // NOTE: Do NOT update remaining customers' positions here.
            // Queue should only advance after staff finishes processing the current customer.
            
            Debug.Log($"[CashRegister] Staff dequeued {customer?.data?.customerName}, remaining: {customerQueue.Count}");
            
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
        /// Return customer to front of queue (called when staff cancels checkout)
        /// </summary>
        public void ReturnCustomerToQueue(CustomerController customer)
        {
            if (customer == null) return;
            
            // Create new queue with this customer at front
            var newQueue = new System.Collections.Generic.Queue<CustomerController>();
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
            
            Debug.Log($"[CashRegister] {customer.data?.customerName} returned to front of queue");
        }
        
        /// <summary>
        /// Get current queue count
        /// </summary>
        public int QueueCount => customerQueue.Count;
        
        /// <summary>
        /// Staff assigned to this register
        /// </summary>
        public Staff.StaffController AssignedStaff { get; set; }
        
        /// <summary>
        /// Check if staff is assigned to this register
        /// </summary>
        public bool HasStaffAssigned => AssignedStaff != null;
        
        /// <summary>
        /// Check if this register can immediately serve a new customer
        /// </summary>
        public bool IsAvailable => currentCustomer == null;
        
        private void Awake()
        {
            // Register with CashRegisterManager (supports multiple registers)
        }
        
        private void Start()
        {
            // Register with manager after it's initialized
            if (CashRegisterManager.Instance != null)
            {
                CashRegisterManager.Instance.RegisterCashRegister(this);
            }
            else
            {
                Debug.LogWarning($"[CashRegister] {name} could not find CashRegisterManager!");
            }
        }
        
        private void OnDestroy()
        {
            if (CashRegisterManager.Instance != null)
            {
                CashRegisterManager.Instance.UnregisterCashRegister(this);
            }
        }
    }
}
