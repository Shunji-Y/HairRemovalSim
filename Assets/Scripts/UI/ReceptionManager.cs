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

        private CustomerController currentCustomerAtReception;

        private void Update()
        {
            // Find closest customer in Waiting state within radius
            CustomerController closestCustomer = null;
            float closestDistance = detectionRadius;

            var allCustomers = FindObjectsOfType<CustomerController>();
            foreach (var customer in allCustomers)
            {
                // Only detect customers in Waiting state (at reception)
                if (customer.CurrentState != CustomerController.CustomerState.Waiting) continue;

                float distance = Vector3.Distance(transform.position, customer.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCustomer = customer;
                }
            }

            // Update current customer
            if (closestCustomer != currentCustomerAtReception)
            {
                if (currentCustomerAtReception != null)
                {
                    // Previous customer left
                    Debug.Log($"[ReceptionManager] {currentCustomerAtReception.data.customerName} left reception area");
                }

                currentCustomerAtReception = closestCustomer;

                if (currentCustomerAtReception != null)
                {
                    Debug.Log($"[ReceptionManager] {currentCustomerAtReception.data.customerName} arrived at reception");
                }
            }
        }

        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            if (currentCustomerAtReception != null)
            {
                OpenReceptionUI();
            }
            else
            {
                Debug.Log("No customer at reception.");
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
            var partNames = currentCustomerAtReception.data.requestedBodyParts.ConvertAll(bp => bp.partName);
            Debug.Log($"[ReceptionManager] Hello {currentCustomerAtReception.data.customerName}! Requested parts: {string.Join(", ", partNames)}");
            
            // Auto-assign to first available bed
            AssignToAvailableBed();
        }

        private void AssignToAvailableBed()
        {
            // Find first available bed
            foreach (var bed in beds)
            {
                if (bed != null && !bed.IsOccupied)
                {
                    currentCustomerAtReception.GoToBed(bed);
                    Debug.Log($"[ReceptionManager] {currentCustomerAtReception.data.customerName} sent to {bed.name}");
                    currentCustomerAtReception = null;
                    return;
                }
            }
            
            Debug.LogWarning("[ReceptionManager] No available beds! Customer waiting...");
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
