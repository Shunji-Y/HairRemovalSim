using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Customer;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Manages multiple CashRegisters in the shop.
    /// Routes customers to shortest queue and manages staff assignments.
    /// </summary>
    public class CashRegisterManager : MonoBehaviour
    {
        public static CashRegisterManager Instance { get; private set; }
        
        [Header("Debug")]
        [SerializeField] private List<CashRegister> registers = new List<CashRegister>();
        
        public int RegisterCount => registers.Count;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        /// <summary>
        /// Register a CashRegister with this manager
        /// </summary>
        public void RegisterCashRegister(CashRegister register)
        {
            if (register != null && !registers.Contains(register))
            {
                registers.Add(register);
                Debug.Log($"[CashRegisterManager] Registered {register.name}. Total: {registers.Count}");
            }
        }
        
        /// <summary>
        /// Unregister a CashRegister from this manager
        /// </summary>
        public void UnregisterCashRegister(CashRegister register)
        {
            if (registers.Remove(register))
            {
                Debug.Log($"[CashRegisterManager] Unregistered {register.name}. Total: {registers.Count}");
            }
        }
        
        /// <summary>
        /// Get the register with the shortest queue for customer routing
        /// Prioritizes registers that can immediately serve (IsAvailable)
        /// </summary>
        public CashRegister GetShortestQueueRegister()
        {
            if (registers.Count == 0) return null;
            if (registers.Count == 1) return registers[0];
            
            // First priority: Find an available register (not currently processing)
            // with the shortest queue
            CashRegister bestAvailable = null;
            int bestAvailableCount = int.MaxValue;
            
            // Second priority: Shortest queue overall (if no available)
            CashRegister shortestOverall = null;
            int shortestCount = int.MaxValue;
            
            foreach (var register in registers)
            {
                if (register == null) continue;
                
                int queueCount = register.QueueCount;
                
                // Track shortest queue overall
                if (queueCount < shortestCount)
                {
                    shortestCount = queueCount;
                    shortestOverall = register;
                }
                
                // Track best available (can immediately serve)
                if (register.IsAvailable && queueCount < bestAvailableCount)
                {
                    bestAvailableCount = queueCount;
                    bestAvailable = register;
                }
            }
            
            // Prefer available register, fallback to shortest queue
            return bestAvailable ?? shortestOverall ?? registers[0];
        }
        
        /// <summary>
        /// Get a register by index (for staff assignment)
        /// </summary>
        public CashRegister GetRegisterByIndex(int index)
        {
            if (index >= 0 && index < registers.Count)
            {
                return registers[index];
            }
            return null;
        }
        
        /// <summary>
        /// Get index of a register
        /// </summary>
        public int GetRegisterIndex(CashRegister register)
        {
            return registers.IndexOf(register);
        }
        
        /// <summary>
        /// Get a register that doesn't have staff assigned (for auto-assignment)
        /// </summary>
        public CashRegister GetUnstaffedRegister()
        {
            foreach (var register in registers)
            {
                if (register != null && !register.HasStaffAssigned)
                {
                    return register;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Get all registers
        /// </summary>
        public IReadOnlyList<CashRegister> GetAllRegisters()
        {
            return registers.AsReadOnly();
        }
        
        public CashRegister GetNearestRegister(Vector3 position)
        {
            if (registers.Count == 0) return null;
            if (registers.Count == 1) return registers[0];
            
            CashRegister nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var register in registers)
            {
                if (register == null) continue;
                
                float dist = Vector3.Distance(position, register.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = register;
                }
            }
            
            return nearest;
        }
        
        // Shared waiting list for all registers
        private List<CustomerController> waitingForPaymentList = new List<CustomerController>();
        
        private void Update()
        {
            // Check if any waiting customers can go to a register
            if (waitingForPaymentList.Count > 0)
            {
                TrySendWaitingCustomerToRegister();
            }
        }
        
        /// <summary>
        /// Register a customer to the shared payment queue
        /// </summary>
        public void RegisterCustomer(CustomerController customer)
        {
            if (customer == null || waitingForPaymentList.Contains(customer)) return;
            
            waitingForPaymentList.Add(customer);
            customer.StartWaiting();
            
            // Find a chair in ANY cashier area
            var chair = Core.ChairManager.Instance?.FindClosestEmptyChair(customer.transform.position, Environment.ChairCategory.Cashier);
            if (chair != null)
            {
                customer.GoToChair(chair);
                Debug.Log($"[CashRegisterManager] {customer.data.customerName} added to payment queue and sent to chair {chair.name}");
            }
            else
            {
                // No chair available - wait standing near a register?
                // For now, stand near the nearest register's waiting area if possible, or just stay put
                Debug.Log($"[CashRegisterManager] {customer.data.customerName} added to payment queue (no chair available)");
            }
        }
        
        /// <summary>
        /// Try to send a waiting customer to an available register
        /// </summary>
        private void TrySendWaitingCustomerToRegister()
        {
            // Find available registers (IsAvailable = true, i.e., currentCustomer == null)
            var availableRegisters = new List<CashRegister>();
            foreach (var register in registers)
            {
                if (register != null && register.IsAvailable)
                {
                    availableRegisters.Add(register);
                }
            }
            
            if (availableRegisters.Count == 0) return;
            
            // Pick appropriate register (e.g. closest or random)
            // Random for now to distribute load
            var targetRegister = availableRegisters[Random.Range(0, availableRegisters.Count)];
            
            // Get first waiting customer (FIFO)
            var customer = waitingForPaymentList[0];
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                waitingForPaymentList.RemoveAt(0);
                return;
            }
            
            // Assign customer to register
            // We use a modified version of RegisterCustomer or a new method on CashRegister
            // that accepts an assigned customer directly
            
            // For now, we'll use the existing RegisterCustomer but we need to ensure it processes immediately
            // because we know the register is available.
            // BUT existing RegisterCustomer adds to queue. We want to bypass queue or ensure queue works right.
            
            // Remove from shared list BEFORE sending to register to avoid duplicate assignment
            waitingForPaymentList.RemoveAt(0);
            
            // Use specific method to skip queue logic if possible, or use standard one
            // Standard one adds to queue, which is local to register.
            // If we use standard RegisterCustomer, the customer enters the local queue.
            // Since register.IsAvailable is true, isFirstCustomer will be true, and they go to counter.
            
            targetRegister.RegisterCustomer(customer);
            Debug.Log($"[CashRegisterManager] {customer.data?.customerName} assigned to register {targetRegister.name} from shared queue");
        }
        
        /// <summary>
        /// Remove customer from shared waiting list
        /// </summary>
        public void RemoveFromWaitingList(CustomerController customer)
        {
            if (waitingForPaymentList.Remove(customer))
            {
                Debug.Log($"[CashRegisterManager] {customer?.data?.customerName} removed from shared payment list");
            }
        }
    }
}
