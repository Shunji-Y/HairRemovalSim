using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;
using HairRemovalSim.Player;

namespace HairRemovalSim.UI
{
    public class CashRegister : MonoBehaviour, IInteractable
    {
        private CustomerController customerAtRegister;

        private void OnTriggerEnter(Collider other)
        {
            CustomerController customer = other.GetComponent<CustomerController>();
            if (customer != null)
            {
                customerAtRegister = customer;
                Debug.Log("Customer at register.");
            }
        }

        public void OnInteract(InteractionController interactor)
        {
            if (customerAtRegister != null)
            {
                ProcessPayment();
            }
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public string GetInteractionPrompt() => "Collect Payment";

        private void ProcessPayment()
        {
            // Calculate payment based on satisfaction
            // Base fee + Tips
            int baseFee = 5000; // Placeholder
            float satisfactionMultiplier = 1.0f; // TODO: Get from customer stats
            
            int finalAmount = Mathf.RoundToInt(baseFee * satisfactionMultiplier);
            
            EconomyManager.Instance.AddMoney(finalAmount);
            
            customerAtRegister.LeaveShop();
            customerAtRegister = null;
        }
    }
}
