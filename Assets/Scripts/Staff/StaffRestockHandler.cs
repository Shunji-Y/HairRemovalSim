using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using HairRemovalSim.Core;
using HairRemovalSim.UI;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Handles automatic restocking when staff is assigned to Restock.
    /// Checks slots every 10 seconds, gathers items from warehouse,
    /// moves to restock spots, waits 1 second per item, then restocks.
    /// </summary>
    public class StaffRestockHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StaffController staffController;
        [SerializeField] private NavMeshAgent agent;
        
        [Header("Timing")]
        [SerializeField] private float checkInterval = 10f;
        [SerializeField] private float restockTimePerItem = 1f;
        
        [Header("State")]
        [SerializeField] private RestockState currentState = RestockState.Idle;
        
        // Carried items: itemId -> quantity
        private Dictionary<string, int> carriedItems = new Dictionary<string, int>();
        private int totalCarriedCount = 0;
        
        // Restock targets
        private Queue<RestockTarget> restockQueue = new Queue<RestockTarget>();
        private RestockTarget currentTarget;
        
        private float checkTimer = 0f;
        private Coroutine restockCoroutine;
        
        public enum RestockState
        {
            Idle,           // Waiting at warehouse
            Gathering,      // Picking up items from warehouse
            MovingToSpot,   // Walking to restock point
            Restocking,     // Placing items (waiting)
            Returning       // Going back to warehouse
        }
        
        public enum RestockLocation
        {
            Reception,
            Checkout,
            TreatmentShelf
        }
        
        private class RestockTarget
        {
            public RestockLocation location;
            public Transform restockPoint;
            public List<(string itemId, int quantity)> itemsToPlace = new List<(string, int)>();
            
            public int TotalItemCount
            {
                get
                {
                    int count = 0;
                    foreach (var item in itemsToPlace)
                        count += item.quantity;
                    return count;
                }
            }
        }
        
        public RestockState CurrentState => currentState;
        public bool IsCarryingItems => totalCarriedCount > 0;
        
        private void Start()
        {
            if (staffController == null)
                staffController = GetComponent<StaffController>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
        }
        
        private void Update()
        {
            // Only run if assigned to Restock
            if (staffController == null || staffController.StaffData == null) return;
            if (staffController.StaffData.assignment != StaffAssignment.Restock) return;
            
            // Check timer when idle
            if (currentState == RestockState.Idle)
            {
                checkTimer += Time.deltaTime;
                if (checkTimer >= checkInterval)
                {
                    checkTimer = 0f;
                    CheckAndStartRestock();
                }
            }
            
            // Check arrival when moving
            if (currentState == RestockState.MovingToSpot && agent != null)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    ArriveAtRestockSpot();
                }
            }
            
            if (currentState == RestockState.Returning && agent != null)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    // Match rotation to warehouse staff point (smooth)
                    var warehousePoint = WarehouseManager.Instance?.staffPoint;
                    if (warehousePoint != null)
                    {
                        agent.updateRotation = false;
                        StartCoroutine(SmoothRotateToTarget(warehousePoint.rotation));
                    }
                    
                    currentState = RestockState.Idle;
                }
            }
        }
        
        /// <summary>
        /// Smoothly rotate to target rotation
        /// </summary>
        private IEnumerator SmoothRotateToTarget(Quaternion targetRotation)
        {
            float rotationSpeed = 180f; // degrees per second
            
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }
            
            transform.rotation = targetRotation;
        }
        
        /// <summary>
        /// Check for empty slots and start restocking if needed
        /// </summary>
        private void CheckAndStartRestock()
        {
            if (WarehouseManager.Instance == null) return;
            
            var targets = DetectEmptySlots();
            if (targets.Count == 0) return;
            
            // Calculate how many items we can carry
            int capacity = staffController.StaffData?.profile?.rankData?.itemSlotCount ?? 5;
            
            // Plan items to gather (don't remove from warehouse yet)
            int plannedCount = PlanItemsToGather(targets, capacity);
            Debug.Log($"[StaffRestockHandler] Planned to gather {plannedCount} items");
            if (plannedCount == 0) return;
            
            // Start by moving to warehouse pickup point
            Debug.Log("[StaffRestockHandler] Starting MoveToPickupPointAndGather coroutine");
            StartCoroutine(MoveToPickupPointAndGather(targets, capacity));
        }
        
        /// <summary>
        /// Move to warehouse pickup point, gather items with delay, then start restocking
        /// </summary>
        private IEnumerator MoveToPickupPointAndGather(List<RestockTarget> targets, int capacity)
        {
            // Use pickupPoint if available, otherwise fall back to staffPoint
            var warehouse = WarehouseManager.Instance;
            var pickupPoint = warehouse?.pickupPoint ?? warehouse?.staffPoint;
            if (pickupPoint == null || agent == null)
            {
                Debug.LogWarning("[StaffRestockHandler] No pickup point or staff point found on WarehouseManager");
                currentState = RestockState.Idle;
                yield break;
            }
            
            Debug.Log($"[StaffRestockHandler] Moving to pickup point at {pickupPoint.position}");
            
            // Move to pickup point
            currentState = RestockState.Gathering;
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(pickupPoint.position);
            
            Debug.Log($"[StaffRestockHandler] Agent destination set, remaining distance: {agent.remainingDistance}");
            
            // Wait until arrived at pickup point
            while (agent != null && agent.enabled)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    Debug.Log("[StaffRestockHandler] Arrived at pickup point");
                    break;
                }
                yield return new WaitForSeconds(0.1f);
            }
            
            // Arrived at pickup - stop and rotate
            if (agent != null)
            {
                agent.isStopped = true;
                agent.updateRotation = false;
            }
            
            // Smooth rotation to pickup point
            yield return StartCoroutine(SmoothRotateToTarget(pickupPoint.rotation));
            
            // Set animation state - gathering from warehouse
            staffController?.SetAnimRestockFromWarehouse(true);
            
            // Actually gather items from warehouse
            int gatheredCount = GatherItemsFromWarehouse(targets, capacity);
            Debug.Log($"[StaffRestockHandler] Gathered {gatheredCount} items from warehouse");
            if (gatheredCount == 0)
            {
                staffController?.SetAnimRestockFromWarehouse(false);
                currentState = RestockState.Idle;
                yield break;
            }
            
            // Build restock queue now that items are gathered
            restockQueue.Clear();
            foreach (var target in targets)
            {
                if (target.itemsToPlace.Count > 0)
                {
                    restockQueue.Enqueue(target);
                    Debug.Log($"[StaffRestockHandler] Queued {target.location} with {target.itemsToPlace.Count} item types");
                }
            }
            
            if (restockQueue.Count == 0)
            {
                Debug.LogWarning("[StaffRestockHandler] No targets in queue after gathering");
                staffController?.SetAnimRestockFromWarehouse(false);
                currentState = RestockState.Idle;
                yield break;
            }
            
            // Wait time based on items gathered (1 second per item)
            float gatherTime = gatheredCount * 1f;
            Debug.Log($"[StaffRestockHandler] Waiting {gatherTime}s for gathering");
            yield return new WaitForSeconds(gatherTime);
            
            // Done gathering, start moving to restock targets
            staffController?.SetAnimRestockFromWarehouse(false);
            Debug.Log("[StaffRestockHandler] Gathering complete, moving to next target");
            MoveToNextTarget();
        }
        
        /// <summary>
        /// Plan items to gather without removing from warehouse (for validation)
        /// </summary>
        private int PlanItemsToGather(List<RestockTarget> targets, int capacity)
        {
            int planned = 0;
            var warehouse = WarehouseManager.Instance;
            if (warehouse == null) return 0;
            
            var warehouseItems = warehouse.GetAllItems();
            
            foreach (var target in targets)
            {
                if (planned >= capacity) break;
                
                foreach (var kvp in warehouseItems)
                {
                    if (planned >= capacity) break;
                    
                    string itemId = kvp.Key;
                    int available = kvp.Value;
                    if (available <= 0) continue;
                    
                    var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                    if (itemData == null) continue;
                    
                    bool canPlace = false;
                    switch (target.location)
                    {
                        case RestockLocation.Reception:
                            canPlace = itemData.CanPlaceAtReception;
                            break;
                        case RestockLocation.Checkout:
                            canPlace = itemData.CanUseAtCheckout;
                            break;
                        case RestockLocation.TreatmentShelf:
                            canPlace = itemData.CanPlaceOnShelf;
                            break;
                    }
                    
                    if (canPlace)
                    {
                        int maxStack = itemData.maxStackOnShelf;
                        int toTake = Mathf.Min(available, maxStack, capacity - planned);
                        planned += toTake;
                    }
                }
            }
            
            return planned;
        }
        
        /// <summary>
        /// Detect empty slots in reception, checkout, and treatment shelves
        /// Returns list of targets with empty slots
        /// </summary>
        private List<RestockTarget> DetectEmptySlots()
        {
            var targets = new List<RestockTarget>();
            
            // Reception slots
            var receptionTarget = CheckReceptionSlots();
            if (receptionTarget != null)
                targets.Add(receptionTarget);
            
            // Checkout slots
            var checkoutTarget = CheckCheckoutSlots();
            if (checkoutTarget != null)
                targets.Add(checkoutTarget);
            
            // Treatment shelf slots (all beds)
            var shelfTargets = CheckTreatmentShelfSlots();
            targets.AddRange(shelfTargets);
            
            return targets;
        }
        
        private RestockTarget CheckReceptionSlots()
        {
            if (ReceptionPanel.Instance == null) return null;
            
            var slots = ReceptionPanel.Instance.GetExtraItemSlots();
            if (slots == null || slots.Length == 0) return null;
            
            var emptySlots = new List<int>();
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsEmpty)
                    emptySlots.Add(slot.SlotIndex);
            }
            
            if (emptySlots.Count == 0) return null;
            
            // Get restock point from ReceptionManager
            Transform restockPoint = ReceptionManager.Instance?.restockPoint;
            
            return new RestockTarget
            {
                location = RestockLocation.Reception,
                restockPoint = restockPoint
            };
        }
        
        private RestockTarget CheckCheckoutSlots()
        {
            if (PaymentPanel.Instance == null) return null;
            
            var slots = PaymentPanel.Instance.GetCheckoutItemSlots();
            if (slots == null || slots.Length == 0) return null;
            
            var emptySlots = new List<int>();
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsEmpty)
                    emptySlots.Add(slot.SlotIndex);
            }
            
            if (emptySlots.Count == 0) return null;
            
            // Get restock point from CashRegister (use first register)
            Transform restockPoint = CashRegisterManager.Instance?.GetRegisterByIndex(0)?.restockPoint;
            
            return new RestockTarget
            {
                location = RestockLocation.Checkout,
                restockPoint = restockPoint
            };
        }
        
        private List<RestockTarget> CheckTreatmentShelfSlots()
        {
            var targets = new List<RestockTarget>();
            
            // Get all beds from StaffManager
            var beds = StaffManager.Instance?.beds;
            if (beds == null || beds.Count == 0)
            {
                Debug.Log("[StaffRestockHandler] No beds found in StaffManager");
                return targets;
            }
            
            Debug.Log($"[StaffRestockHandler] Checking {beds.Count} beds for shelf restocking");
            
            foreach (var bed in beds)
            {
                if (bed == null) continue;
                
                // Check installed shelves directly (more reliable than GetComponentsInChildren)
                var shelves = bed.installedShelves;
                if (shelves == null || shelves.Length == 0)
                {
                    Debug.Log($"[StaffRestockHandler] Bed {bed.name} has no installed shelves");
                    continue;
                }
                
                foreach (var shelf in shelves)
                {
                    if (shelf == null) continue;
                    
                    bool isFull = shelf.IsFull();
                    Debug.Log($"[StaffRestockHandler] Shelf on {bed.name}: IsFull={isFull}");
                    
                    if (!isFull)
                    {
                        targets.Add(new RestockTarget
                        {
                            location = RestockLocation.TreatmentShelf,
                            restockPoint = bed.restockPoint ?? bed.transform
                        });
                        break; // One target per bed
                    }
                }
            }
            
            Debug.Log($"[StaffRestockHandler] Found {targets.Count} shelf targets to restock");
            return targets;
        }
        
        /// <summary>
        /// Gather items from warehouse based on detected targets
        /// </summary>
        private int GatherItemsFromWarehouse(List<RestockTarget> targets, int capacity)
        {
            int gathered = 0;
            carriedItems.Clear();
            
            var warehouse = WarehouseManager.Instance;
            if (warehouse == null) return 0;
            
            // Get all items in warehouse
            var warehouseItems = warehouse.GetAllItems();
            Debug.Log($"[StaffRestockHandler] Warehouse has {warehouseItems.Count} item types, checking against {targets.Count} targets");
            
            foreach (var target in targets)
            {
                if (gathered >= capacity) break;
                
                // Find items suitable for this location
                foreach (var kvp in warehouseItems)
                {
                    if (gathered >= capacity) break;
                    
                    string itemId = kvp.Key;
                    int available = kvp.Value;
                    if (available <= 0) continue;
                    
                    var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                    if (itemData == null)
                    {
                        Debug.Log($"[StaffRestockHandler] Item {itemId} not found in registry");
                        continue;
                    }
                    
                    bool canPlace = false;
                    switch (target.location)
                    {
                        case RestockLocation.Reception:
                            canPlace = itemData.CanPlaceAtReception;
                            break;
                        case RestockLocation.Checkout:
                            canPlace = itemData.CanUseAtCheckout;
                            break;
                        case RestockLocation.TreatmentShelf:
                            canPlace = itemData.CanPlaceOnShelf;
                            break;
                    }
                    
                    Debug.Log($"[StaffRestockHandler] Item {itemId} for {target.location}: canPlace={canPlace} (CanPlaceOnShelf={itemData.CanPlaceOnShelf})");
                    
                    if (!canPlace) continue;
                    
                    // Determine how much to take (limited only by capacity, not stack size)
                    // Stack size limit is applied when placing items, not when carrying
                    int toTake = Mathf.Min(available, capacity - gathered);
                    
                    if (toTake > 0)
                    {
                        // Remove from warehouse
                        int removed = warehouse.RemoveItem(itemId, toTake);
                        if (removed > 0)
                        {
                            // Add to carried items
                            if (!carriedItems.ContainsKey(itemId))
                                carriedItems[itemId] = 0;
                            carriedItems[itemId] += removed;
                            gathered += removed;
                            
                            // Add to target's items to place
                            target.itemsToPlace.Add((itemId, removed));
                            
                            Debug.Log($"[StaffRestockHandler] Took {removed}x {itemId} for {target.location}");
                        }
                    }
                }
            }
            
            totalCarriedCount = gathered;
            return gathered;
        }
        
        private void MoveToNextTarget()
        {
            // Clear warehouse gathering animation when moving to target
            staffController?.SetAnimRestockFromWarehouse(false);
            
            if (restockQueue.Count == 0)
            {
                // All done, return to warehouse
                ReturnToWarehouse();
                return;
            }
            
            currentTarget = restockQueue.Dequeue();
            
            if (currentTarget.restockPoint != null && agent != null)
            {
                currentState = RestockState.MovingToSpot;
                
                // Ensure agent is ready to move
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(currentTarget.restockPoint.position);
                
                Debug.Log($"[StaffRestockHandler] Moving to {currentTarget.location}");
            }
            else
            {
                // No restock point, skip
                Debug.LogWarning($"[StaffRestockHandler] No restock point for {currentTarget.location}, skipping");
                MoveToNextTarget();
            }
        }
        
        private void ArriveAtRestockSpot()
        {
            if (currentTarget == null)
            {
                MoveToNextTarget();
                return;
            }
            
            // Stop and disable agent rotation
            if (agent != null)
            {
                agent.isStopped = true;
                agent.updateRotation = false;
            }
            
            // Start restocking coroutine (includes rotation)
            if (restockCoroutine != null)
                StopCoroutine(restockCoroutine);
            
            restockCoroutine = StartCoroutine(RestockAtCurrentSpot());
        }
        
        private IEnumerator RestockAtCurrentSpot()
        {
            currentState = RestockState.Restocking;
            
            // Smooth rotation to restock point
            if (currentTarget.restockPoint != null)
            {
                yield return StartCoroutine(SmoothRotateToTarget(currentTarget.restockPoint.rotation));
            }
            
            // Set animation state - restocking
            staffController?.SetAnimRestockToAnywhere(true);
            
            int totalItems = currentTarget.TotalItemCount;
            float waitTime = totalItems * restockTimePerItem;
            
            yield return new WaitForSeconds(waitTime);
            
            // Clear animation state
            staffController?.SetAnimRestockToAnywhere(false);
            
            // Actually place items
            PlaceItemsAtLocation(currentTarget);
            
            // Move to next target
            MoveToNextTarget();
        }
        
        private void PlaceItemsAtLocation(RestockTarget target)
        {
            foreach (var (itemId, quantity) in target.itemsToPlace)
            {
                // Remove from carried items
                if (carriedItems.ContainsKey(itemId))
                {
                    carriedItems[itemId] -= quantity;
                    if (carriedItems[itemId] <= 0)
                        carriedItems.Remove(itemId);
                }
                totalCarriedCount -= quantity;
                
                // Place at location
                switch (target.location)
                {
                    case RestockLocation.Reception:
                        PlaceAtReception(itemId, quantity);
                        break;
                    case RestockLocation.Checkout:
                        PlaceAtCheckout(itemId, quantity);
                        break;
                    case RestockLocation.TreatmentShelf:
                        PlaceAtShelf(itemId, quantity, target.restockPoint);
                        break;
                }
            }
        }
        
        private void PlaceAtReception(string itemId, int quantity)
        {
            if (ReceptionPanel.Instance == null) return;
            
            var slots = ReceptionPanel.Instance.GetExtraItemSlots();
            if (slots == null) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            int maxStack = itemData?.maxStackOnShelf ?? 99;
            
            int remaining = quantity;
            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot == null) continue;
                
                // Empty slot or same item
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(remaining, maxStack);
                    slot.SetItem(itemId, toAdd);
                    remaining -= toAdd;
                    Debug.Log($"[StaffRestockHandler] Placed {toAdd}x {itemId} at reception (new slot)");
                }
                else if (slot.ItemId == itemId)
                {
                    int space = maxStack - slot.Quantity;
                    if (space > 0)
                    {
                        int toAdd = Mathf.Min(remaining, space);
                        slot.AddQuantity(toAdd);
                        remaining -= toAdd;
                        Debug.Log($"[StaffRestockHandler] Added {toAdd}x {itemId} to reception slot");
                    }
                }
            }
            
            if (remaining > 0)
            {
                Debug.LogWarning($"[StaffRestockHandler] Could not place {remaining}x {itemId} at reception - all slots full");
            }
        }
        
        private void PlaceAtCheckout(string itemId, int quantity)
        {
            if (PaymentPanel.Instance == null) return;
            
            var slots = PaymentPanel.Instance.GetCheckoutItemSlots();
            if (slots == null) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            int maxStack = itemData?.maxStackOnShelf ?? 99;
            
            int remaining = quantity;
            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot == null) continue;
                
                // Empty slot or same item
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(remaining, maxStack);
                    slot.SetItemFromStock(itemId, toAdd);
                    remaining -= toAdd;
                    Debug.Log($"[StaffRestockHandler] Placed {toAdd}x {itemId} at checkout (new slot)");
                }
                else if (slot.ItemId == itemId)
                {
                    int space = maxStack - slot.Quantity;
                    if (space > 0)
                    {
                        int toAdd = Mathf.Min(remaining, space);
                        // Add to existing - need to get current qty first
                        int newQty = slot.Quantity + toAdd;
                        slot.SetItemFromStock(itemId, newQty);
                        remaining -= toAdd;
                        Debug.Log($"[StaffRestockHandler] Added {toAdd}x {itemId} to checkout slot");
                    }
                }
            }
            
            if (remaining > 0)
            {
                Debug.LogWarning($"[StaffRestockHandler] Could not place {remaining}x {itemId} at checkout - all slots full");
            }
        }
        
        private void PlaceAtShelf(string itemId, int quantity, Transform bedTransform)
        {
            if (bedTransform == null) return;
            
            var bed = bedTransform.GetComponentInParent<BedController>();
            if (bed == null) bed = bedTransform.GetComponent<BedController>();
            if (bed == null)
            {
                Debug.LogWarning("[StaffRestockHandler] Could not find BedController");
                return;
            }
            
            // Use installedShelves directly
            var shelves = bed.installedShelves;
            if (shelves == null || shelves.Length == 0)
            {
                Debug.LogWarning($"[StaffRestockHandler] Bed {bed.name} has no installed shelves");
                return;
            }
            
            int remaining = quantity;
            
            foreach (var shelf in shelves)
            {
                if (shelf == null || remaining <= 0) continue;
                
                // Find empty or same-item slots and use PlaceItem (which spawns visuals)
                int rowCount = shelf.rowCount;
                int colCount = shelf.columnCount;
                
                for (int row = 0; row < rowCount && remaining > 0; row++)
                {
                    for (int col = 0; col < colCount && remaining > 0; col++)
                    {
                        var slotData = shelf.GetSlotData(row, col);
                        
                        // Empty slot - place items
                        if (slotData == null || string.IsNullOrEmpty(slotData.itemId) || slotData.quantity <= 0)
                        {
                            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                            int maxStack = itemData?.maxStackOnShelf ?? 1;
                            int toPlace = Mathf.Min(remaining, maxStack);
                            
                            if (shelf.PlaceItem(row, col, itemId, toPlace))
                            {
                                Debug.Log($"[StaffRestockHandler] Placed {toPlace}x {itemId} at shelf [{row},{col}]");
                                remaining -= toPlace;
                            }
                        }
                        // Same item - add to existing stack
                        else if (slotData.itemId == itemId)
                        {
                            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
                            int maxStack = itemData?.maxStackOnShelf ?? 1;
                            int space = maxStack - slotData.quantity;
                            
                            if (space > 0)
                            {
                                int toAdd = Mathf.Min(remaining, space);
                                if (shelf.AddToSlot(row, col, toAdd))
                                {
                                    Debug.Log($"[StaffRestockHandler] Added {toAdd}x {itemId} to shelf [{row},{col}]");
                                    remaining -= toAdd;
                                }
                            }
                        }
                    }
                }
            }
            
            if (remaining > 0)
            {
                Debug.LogWarning($"[StaffRestockHandler] Could not place {remaining}x {itemId} - shelves may be full");
            }
        }
        
        private void ReturnToWarehouse()
        {
            // Clear all restock animations
            staffController?.SetAnimRestockFromWarehouse(false);
            staffController?.SetAnimRestockToAnywhere(false);
            
            // Return any remaining items to warehouse
            if (carriedItems.Count > 0)
            {
                var warehouse = WarehouseManager.Instance;
                if (warehouse != null)
                {
                    foreach (var kvp in carriedItems)
                    {
                        int added = warehouse.AddItem(kvp.Key, kvp.Value);
                        if (added < kvp.Value)
                        {
                            Debug.LogWarning($"[StaffRestockHandler] Could not return {kvp.Value - added}x {kvp.Key} to warehouse (full)");
                        }
                    }
                }
                carriedItems.Clear();
                totalCarriedCount = 0;
            }
            
            // Move back to warehouse
            var warehousePoint = WarehouseManager.Instance?.staffPoint;
            if (warehousePoint != null && agent != null)
            {
                currentState = RestockState.Returning;
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(warehousePoint.position);
            }
            else
            {
                currentState = RestockState.Idle;
            }
        }
        
        private void OnDisable()
        {
            if (restockCoroutine != null)
            {
                StopCoroutine(restockCoroutine);
                restockCoroutine = null;
            }
            
            // Return items if stopped mid-restock
            if (carriedItems.Count > 0)
            {
                ReturnToWarehouse();
            }
        }
    }
}
