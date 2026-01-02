using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    public enum ChairCategory
    {
        Reception,  // Chairs for customers waiting for reception
        Cashier,    // Chairs for customers waiting for payment
        Waiting     // Chairs for customers waiting for bed
    }
    
    /// <summary>
    /// Chair component - manages seat position and occupancy
    /// </summary>
    public class Chair : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Category of this chair for priority matching")]
        public ChairCategory category = ChairCategory.Waiting;
        
        [Tooltip("Where customer sits (uses transform if not set)")]
        public Transform seatPoint;
        
        [Header("Status")]
        [SerializeField] private CustomerController currentCustomer;
        
        /// <summary>
        /// Whether this chair is currently occupied
        /// </summary>
        public bool IsOccupied => currentCustomer != null;
        
        /// <summary>
        /// The customer currently sitting on this chair
        /// </summary>
        public CustomerController CurrentCustomer => currentCustomer;
        
        /// <summary>
        /// Get the seat position (uses seatPoint if set, otherwise transform)
        /// </summary>
        public Transform SeatPosition => seatPoint != null ? seatPoint : transform;
        
        private void Start()
        {
            if (seatPoint == null)
            {
                Debug.LogWarning($"[Chair] {name} has no SeatPoint assigned! Using transform position which might be incorrect (ground level).");
            }
            
            // Validate NavMesh accessibility
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(SeatPosition.position, out hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                 Debug.LogWarning($"[Chair] {name} seems to be off NavMesh! Customers may not be able to reach it.");
            }
            
            // Ensure registration (in case OnEnable ran before Manager was ready)
            ChairManager.Instance?.RegisterChair(this);
        }
        
        private void OnEnable()
        {
            // Register with ChairManager
            if (ChairManager.Instance != null)
            {
                ChairManager.Instance.RegisterChair(this);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from ChairManager
            if (ChairManager.Instance != null)
            {
                ChairManager.Instance.UnregisterChair(this);
            }
        }
        
        /// <summary>
        /// Occupy this chair with a customer
        /// </summary>
        public bool Occupy(CustomerController customer)
        {
            if (currentCustomer != null)
            {
                Debug.LogWarning($"[Chair] {name} already occupied by {currentCustomer.data?.customerName}");
                return false;
            }
            
            currentCustomer = customer;
            Debug.Log($"[Chair] {customer.data?.customerName} occupied {name} ({category})");
            return true;
        }
        
        /// <summary>
        /// Release this chair
        /// </summary>
        public void Release()
        {
            if (currentCustomer != null)
            {
                Debug.Log($"[Chair] {currentCustomer.data?.customerName} released {name}");
                currentCustomer = null;
            }
        }
        
        /// <summary>
        /// Force release (for cleanup)
        /// </summary>
        public void ForceRelease()
        {
            currentCustomer = null;
        }
    }
}
