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
                
                // Determine waypoint: if not at the back, use the position behind
                Transform waypoint = null;
                if (queueIndex + 1 < queuePositions.Length)
                {
                    waypoint = queuePositions[queueIndex + 1]; // Position behind
                }
                
                // Send customer to queue position via waypoint, facing cash register
                customer.GoToQueuePosition(queuePositions[queueIndex], transform, waypoint);
                
                return queuePositions[queueIndex];
            }
            else
            {
                Debug.LogWarning($"[CashRegister] {customer.data.customerName} registered but no queue position available (queue full)");
                return transform; // Fallback to register point
            }
        }

        public void OnInteract(InteractionController interactor)
        {
            // Block interaction if PaymentPanel is already open
            if (PaymentPanel.Instance != null && PaymentPanel.Instance.IsOpen)
            {
                Debug.Log("[CashRegister] PaymentPanel is already open, ignoring interaction");
                return;
            }
            
            // Process first customer in queue
            if (currentCustomer == null && customerQueue.Count > 0)
            {
                currentCustomer = customerQueue.Dequeue();
                UpdateQueuePositions();
            }
            
            if (currentCustomer != null)
            {
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
            if (customer == currentCustomer)
            {
                processedCustomers.Remove(currentCustomer);
                currentCustomer = null;
                ProcessNextCustomer();
            }
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
    }
}
