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
        
        private CustomerController customerAtRegister;

        private void Update()
        {
            // Find closest customer in Paying state within radius
            CustomerController closestCustomer = null;
            float closestDistance = detectionRadius;

            var allCustomers = FindObjectsOfType<CustomerController>();
            foreach (var customer in allCustomers)
            {
                // Only detect customers in Paying state (waiting for payment)
                if (customer.CurrentState != CustomerController.CustomerState.Paying) continue;

                float distance = Vector3.Distance(transform.position, customer.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCustomer = customer;
                }
            }

            // Update current customer
            if (closestCustomer != customerAtRegister)
            {
                customerAtRegister = closestCustomer;

                if (customerAtRegister != null)
                {
                    Debug.Log($"[CashRegister] {customerAtRegister.data.customerName} arrived at register");
                }
            }
        }

        public void OnInteract(InteractionController interactor)
        {
            if (customerAtRegister != null)
            {
                ProcessPayment();
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
            // Calculate payment based on completed requested parts only
            int finalAmount = customerAtRegister.CalculateFinalPayment();
            
            EconomyManager.Instance.AddMoney(finalAmount);
            Debug.Log($"[CashRegister] {customerAtRegister.data.customerName} paid ${finalAmount}");
            
            customerAtRegister.LeaveShop();
            customerAtRegister = null;
        }
    }
}
