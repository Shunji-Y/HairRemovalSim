using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;
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
        
        [Header("Queue Management")]
        public Transform[] queuePositions; // 待機位置の配列
        
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
            
            customerQueue.Enqueue(customer);
            processedCustomers.Add(customer);
            
            int queueIndex = customerQueue.Count - 1; // 0-based index
            
            if (queueIndex < queuePositions.Length)
            {
                Debug.Log($"[CashRegister] {customer.data.customerName} registered to queue position {queueIndex + 1}");
                
                // Send customer to queue position, facing cash register
                customer.GoToQueuePosition(queuePositions[queueIndex], transform);
                
                return queuePositions[queueIndex];
            }
            else
            {
                Debug.LogWarning($"[CashRegister] {customer.data.customerName} registered but no queue position available (queue full)");
                return transform; // Fallback to register point
            }
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
        
        public string GetInteractionPrompt() => customerAtRegister != null ? "Collect Payment" : "Cash Register";

        private void ProcessPayment()
        {
            if (currentCustomer == null) return;
            
            // Open PaymentPanel instead of direct payment
            if (PaymentPanel.Instance != null)
            {
                PaymentPanel.Instance.Show(currentCustomer);
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
            if (queuePositions == null || queuePositions.Length == 0) return;
            
            int index = 0;
            foreach (var customer in customerQueue)
            {
                if (index < queuePositions.Length)
                {
                    customer.GoToQueuePosition(queuePositions[index], transform);
                    Debug.Log($"[CashRegister] {customer.data.customerName} moving to queue position {index + 1}");
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
        /// Check if staff is assigned to cashier
        /// </summary>
        public bool HasStaffAssigned
        {
            get
            {
                var staffManager = Staff.StaffManager.Instance;
                if (staffManager == null) return false;
                
                foreach (var staff in staffManager.GetHiredStaff())
                {
                    if (staff.isActive && staff.assignment == Staff.StaffAssignment.Cashier)
                        return true;
                }
                return false;
            }
        }
        
        /// <summary>
        /// Dequeue customer for staff processing
        /// </summary>
        public CustomerController DequeueCustomerForStaff()
        {
            // Clean up invalid customers first
            CleanupQueue();
            
            if (customerQueue.Count == 0) return null;
            
            var customer = customerQueue.Dequeue();
            processedCustomers.Remove(customer);
            
            // Update remaining customers' positions
            UpdateQueuePositions();
            
            Debug.Log($"[CashRegister] Staff dequeued {customer?.data?.customerName}, remaining: {customerQueue.Count}");
            
            return customer;
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
        /// Singleton instance
        /// </summary>
        public static CashRegister Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[CashRegister] Multiple instances found!");
            }
        }
    }
}
