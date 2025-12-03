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

        private CustomerController currentCustomerAtReception;

        private void OnTriggerEnter(Collider other)
        {
            CustomerController customer = other.GetComponent<CustomerController>();
            if (customer != null)
            {
                currentCustomerAtReception = customer;
                Debug.Log("Customer arrived at reception.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            CustomerController customer = other.GetComponent<CustomerController>();
            if (customer != null && currentCustomerAtReception == customer)
            {
                currentCustomerAtReception = null;
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

        public void OnHoverEnter() { /* Highlight reception desk */ }
        public void OnHoverExit() { /* Unhighlight */ }
        
        public string GetInteractionPrompt()
        {
            return currentCustomerAtReception != null ? "Speak to Customer" : "Reception Desk";
        }

        private void OpenReceptionUI()
        {
            Debug.Log($"Hello {currentCustomerAtReception.data.customerName}! Select a plan.");
            // TODO: Show UI with buttons for plans
            // For prototype, auto-assign Bed 0
            AssignBed(0);
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
