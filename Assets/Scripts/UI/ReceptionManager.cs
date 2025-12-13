using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.UI
{
    public class ReceptionManager : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        public Environment.BedController[] beds; // Available beds
        public float detectionRadius = 2.0f; // Distance to detect customers
        
        [Header("Queue Management")]
        public Transform[] queuePositions; // 待機位置の配列

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
            
            customerQueue.Enqueue(customer);
            processedCustomers.Add(customer);
            
            int queueIndex = customerQueue.Count - 1; // 0-based index
            
            if (queueIndex < queuePositions.Length)
            {
                Debug.Log($"[ReceptionManager] {customer.data.customerName} registered to queue position {queueIndex + 1}");
                
                // Send customer to queue position, facing reception desk
                customer.GoToQueuePosition(queuePositions[queueIndex], transform);
                
                return queuePositions[queueIndex];
            }
            else
            {
                Debug.LogWarning($"[ReceptionManager] {customer.data.customerName} registered but no queue position available (queue full)");
                return transform; // Fallback to reception point
            }
        }

        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            // Process first customer in queue
            if (currentCustomer == null && customerQueue.Count > 0)
            {
                currentCustomer = customerQueue.Dequeue();
                // Don't update queue positions yet - wait until customer confirms and moves to bed
            }
            
            if (currentCustomer != null)
            {
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
            
            // Find first available bed
            foreach (var bed in beds)
            {
                if (bed != null && !bed.IsOccupied)
                {
                    currentCustomer.GoToBed(bed);
                    Debug.Log($"[ReceptionManager] {currentCustomer.data.customerName} sent to {bed.name}");
                    
                    // Remove from processed set so they can be processed again if needed
                    processedCustomers.Remove(currentCustomer);
                    currentCustomer = null;
                    
                    // NOW advance the queue since customer is heading to bed
                    UpdateQueuePositions();
                    ProcessNextCustomer();
                    return;
                }
            }
            
            Debug.LogWarning("[ReceptionManager] No available beds! Customer waiting...");
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
                    Debug.Log($"[ReceptionManager] {customer.data.customerName} moving to queue position {index + 1}");
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
            if (bedIndex >= 0 && bedIndex < beds.Length)
            {
                currentCustomerAtReception.GoToBed(beds[bedIndex]);
                currentCustomerAtReception = null; // Clear reception
                Debug.Log("Customer sent to bed.");
            }
        }
    }
}
