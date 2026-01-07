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
            
            // Validate NavMesh accessibility (STRICT CHECK)
            UnityEngine.AI.NavMeshHit hit;
            // Check within 0.5f radius (very close)
            if (UnityEngine.AI.NavMesh.SamplePosition(SeatPosition.position, out hit, 0.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // Found point on NavMesh, but is it close enough efficiently?
                float dist = Vector3.Distance(SeatPosition.position, hit.position);
                // Debug.Log($"[Chair] {name} SeatPoint NavMesh OK. Dist: {dist:F3}");
            }
            else
            {
                // Not found nearby - try slighly larger radius to tell user how far off they are
                if (UnityEngine.AI.NavMesh.SamplePosition(SeatPosition.position, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    float dist = Vector3.Distance(SeatPosition.position, hit.position);
                    Debug.LogError($"[Chair] {name} SeatPoint is OFF NavMesh! Closest mesh is {dist:F2}m away (limit 0.5m). Move SeatPoint closer to blue area.");
                }
                else
                {
                    Debug.LogError($"[Chair] {name} SeatPoint is COMPLETELY OFF NavMesh! No mesh found within 2m.");
                }
            }
            
            // Check for self-obstruction
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null && obstacle.enabled && obstacle.carving)
            {
                // Check if SeatPoint is inside the obstacle bounds roughly
                float distToCenter = Vector3.Distance(SeatPosition.position, obstacle.transform.position + obstacle.center);
                if (distToCenter < obstacle.radius)
                {
                    Debug.LogError($"[Chair] {name} SeatPoint is INSIDE its own NavMeshObstacle! (DistToCenter: {distToCenter:F2} < Radius: {obstacle.radius}). Customers cannot reach this.");
                }
            }
            
            // Ensure registration (in case OnEnable ran before Manager was ready)
            ChairManager.Instance?.RegisterChair(this);
            
            // Check reachability from a reference point (e.g. ChairManager's position or (0,0,0))
            // This detects isolated NavMesh islands
            if (ChairManager.Instance != null)
            {
                UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                Vector3 startPos = ChairManager.Instance.transform.position;
                // If Manager is at 0,0,0 (common manager location), it might not be on NavMesh.
                // Better to use the hit position we found earlier or just skip if Manager is purely logic.
                
                // Let's assume ChairManager is placed somewhere valid or we try to find a valid point near it.
                UnityEngine.AI.NavMeshHit startHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(startPos, out startHit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    if (UnityEngine.AI.NavMesh.CalculatePath(startHit.position, SeatPosition.position, UnityEngine.AI.NavMesh.AllAreas, path))
                    {
                        if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                        {
                           // Debug.LogError($"[Chair] {name} is NOT REACHABLE from Manager! PathStatus: {path.status}. It might be on an isolated NavMesh island.");
                        }
                    }
                }
            }
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
            int chairId = GetInstanceID();
            int chairIndex = ChairManager.Instance?.GetChairIndex(this) ?? -1;
            string prevOccupant = currentCustomer?.data?.customerName ?? "NULL";
            
            Debug.Log($"[Chair] Occupy attempt: {customer.data?.customerName} -> {name} [#{chairIndex}] (ID:{chairId}), prevOccupant={prevOccupant}, SeatPos={SeatPosition?.position}");
            
            // Check if chair is already occupied by a VALID customer
            // Unity's null check uses overloaded operator to detect destroyed objects
            if (currentCustomer != null)
            {
                // Double-check the object is still valid and active
                try
                {
                    if (currentCustomer.gameObject != null && currentCustomer.gameObject.activeInHierarchy)
                    {
                        Debug.LogWarning($"[Chair] {name} (ID:{chairId}) REJECT: already occupied by {currentCustomer.data?.customerName} - rejecting {customer.data?.customerName}");
                        return false;
                    }
                    else
                    {
                        // currentCustomer exists but is inactive/destroyed, clear it
                        Debug.LogWarning($"[Chair] {name} (ID:{chairId}) had stale reference (inactive/destroyed), clearing");
                        currentCustomer = null;
                    }
                }
                catch (System.Exception)
                {
                    // Reference is completely broken
                    Debug.LogWarning($"[Chair] {name} (ID:{chairId}) had broken reference, clearing");
                    currentCustomer = null;
                }
            }
            
            currentCustomer = customer;
            Debug.Log($"[Chair] SUCCESS: {customer.data?.customerName} occupied {name} (ID:{chairId})");
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
