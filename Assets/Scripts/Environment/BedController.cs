using UnityEngine;
using HairRemovalSim.Customer;

namespace HairRemovalSim.Environment
{
    public class BedController : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }
        public CustomerController CurrentCustomer { get; private set; }
        
        // Transform where the customer should lie down
        public Transform lieDownPoint;

        private void Awake()
        {
            // If no lie down point set, use own transform
            if (lieDownPoint == null)
            {
                lieDownPoint = transform;
            }
        }

        public void AssignCustomer(CustomerController customer)
        {
            CurrentCustomer = customer;
            IsOccupied = true;
        }

        public void ClearCustomer()
        {
            CurrentCustomer = null;
            IsOccupied = false;
        }
    }
}
