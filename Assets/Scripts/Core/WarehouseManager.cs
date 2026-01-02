using UnityEngine;
using System;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages warehouse inventory with expandable slot system.
    /// Handles item storage, stacking, and slot management.
    /// </summary>
    public class WarehouseManager : Singleton<WarehouseManager>
    {
        [System.Serializable]
        public class WarehouseSlot
        {
            public string itemId;
            public int quantity;
            
            public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
            
            public void Clear()
            {
                itemId = null;
                quantity = 0;
            }
        }
        
        [Header("Warehouse Settings")]
        [Tooltip("Number of columns (fixed)")]
        [SerializeField] private int columns = 5;
        
        [Tooltip("Initial number of rows")]
        [SerializeField] private int initialRows = 3;
        
        [Tooltip("Current warehouse level (affects row count)")]
        [SerializeField] private int warehouseLevel = 1;
        
        [Header("Staff")]
        [Tooltip("Position where staff stands when idle at warehouse")]
        public Transform staffPoint;
        
        [Tooltip("Position where staff picks up items from warehouse")]
        public Transform pickupPoint;
        
        [Header("New Item Indicator")]
        [Tooltip("TMP text to show 'New!!' when items are delivered")]
        [SerializeField] private TMPro.TextMeshProUGUI newIndicatorText;
        
        // Flag for new items delivered
        private bool hasNewItems = false;
        
        // Slot data
        private WarehouseSlot[] slots;
        
        // Events
        public event Action<int> OnSlotChanged; // slot index
        public event Action OnWarehouseUpdated;
        
        public int Columns => columns;
        public int CurrentRows => initialRows + (warehouseLevel - 1);
        public int MaxSlots => columns * CurrentRows;
        public int Level => warehouseLevel;
        public bool HasNewItems => hasNewItems;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeSlots();
        }
        
        private void InitializeSlots()
        {
            // Allocate max possible slots (for level 10 = initial + 9 rows)
            int maxPossibleSlots = columns * (initialRows + 9);
            slots = new WarehouseSlot[maxPossibleSlots];
            
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new WarehouseSlot();
            }
            
            Debug.Log($"[WarehouseManager] Initialized with {MaxSlots} slots (Level {warehouseLevel})");
        }
        
        /// <summary>
        /// Upgrade warehouse to add one more row
        /// </summary>
        public void UpgradeWarehouse()
        {
            warehouseLevel++;
            Debug.Log($"[WarehouseManager] Upgraded to Level {warehouseLevel}, now has {MaxSlots} slots");
            OnWarehouseUpdated?.Invoke();
        }
        
        /// <summary>
        /// Get slot data at index
        /// </summary>
        public WarehouseSlot GetSlot(int index)
        {
            if (index < 0 || index >= MaxSlots) return null;
            return slots[index];
        }
        
        /// <summary>
        /// Check if items can be added (enough empty slots/stack space)
        /// </summary>
        public bool CanAddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return false;
            
            int remaining = quantity;
            int maxStack = itemData.maxWarehouseStack;
            
            // First try to fill existing stacks
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (slots[i].itemId == itemId)
                {
                    int space = maxStack - slots[i].quantity;
                    remaining -= space;
                }
            }
            
            if (remaining <= 0) return true;
            
            // Then check empty slots
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    remaining -= maxStack;
                }
            }
            
            return remaining <= 0;
        }
        
        /// <summary>
        /// Add items to warehouse. Returns quantity actually added.
        /// </summary>
        public int AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return 0;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null) return 0;
            
            int remaining = quantity;
            int maxStack = itemData.maxWarehouseStack;
            
            // First fill existing stacks
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (slots[i].itemId == itemId && slots[i].quantity < maxStack)
                {
                    int space = maxStack - slots[i].quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    slots[i].quantity += toAdd;
                    remaining -= toAdd;
                    OnSlotChanged?.Invoke(i);
                }
            }
            
            // Then use empty slots
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int toAdd = Mathf.Min(maxStack, remaining);
                    slots[i].itemId = itemId;
                    slots[i].quantity = toAdd;
                    remaining -= toAdd;
                    OnSlotChanged?.Invoke(i);
                }
            }
            
            int added = quantity - remaining;
            if (added > 0)
            {
                Debug.Log($"[WarehouseManager] Added {added}x {itemData.name}");
                OnWarehouseUpdated?.Invoke();
            }
            
            return added;
        }
        
        /// <summary>
        /// Remove items from a specific slot
        /// </summary>
        public int RemoveFromSlot(int slotIndex, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return 0;
            if (slots[slotIndex].IsEmpty) return 0;
            
            int toRemove = Mathf.Min(quantity, slots[slotIndex].quantity);
            slots[slotIndex].quantity -= toRemove;
            
            if (slots[slotIndex].quantity <= 0)
            {
                slots[slotIndex].Clear();
            }
            
            OnSlotChanged?.Invoke(slotIndex);
            OnWarehouseUpdated?.Invoke();
            
            return toRemove;
        }
        
        /// <summary>
        /// Remove items by itemId. Returns quantity actually removed.
        /// </summary>
        public int RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return 0;
            
            int remaining = quantity;
            
            // Find slots with this item and remove
            for (int i = 0; i < MaxSlots && remaining > 0; i++)
            {
                if (slots[i].itemId == itemId && slots[i].quantity > 0)
                {
                    int toRemove = Mathf.Min(remaining, slots[i].quantity);
                    slots[i].quantity -= toRemove;
                    remaining -= toRemove;
                    
                    if (slots[i].quantity <= 0)
                    {
                        slots[i].Clear();
                    }
                    
                    OnSlotChanged?.Invoke(i);
                }
            }
            
            int removed = quantity - remaining;
            if (removed > 0)
            {
                OnWarehouseUpdated?.Invoke();
            }
            
            return removed;
        }
        
        /// <summary>
        /// Set slot data directly (for sync from external UI)
        /// </summary>
        public void SetSlot(int slotIndex, string itemId, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            
            slots[slotIndex].itemId = itemId;
            slots[slotIndex].quantity = quantity;
            
            OnSlotChanged?.Invoke(slotIndex);
            OnWarehouseUpdated?.Invoke();
        }
        
        /// <summary>
        /// Clear a specific slot
        /// </summary>
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            
            slots[slotIndex].Clear();
            
            OnSlotChanged?.Invoke(slotIndex);
            OnWarehouseUpdated?.Invoke();
        }
        
        /// <summary>
        /// Move items from one slot to another (or to shelf)
        /// Returns quantity actually moved
        /// </summary>
        public int MoveToSlot(int fromSlot, int toSlot, int quantity)
        {
            if (fromSlot < 0 || fromSlot >= MaxSlots) return 0;
            if (toSlot < 0 || toSlot >= MaxSlots) return 0;
            if (fromSlot == toSlot) return 0;
            
            var source = slots[fromSlot];
            var target = slots[toSlot];
            
            if (source.IsEmpty) return 0;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(source.itemId);
            if (itemData == null) return 0;
            
            int maxStack = itemData.maxWarehouseStack;
            int toMove = Mathf.Min(quantity, source.quantity);
            
            // Target is empty
            if (target.IsEmpty)
            {
                int canMove = Mathf.Min(toMove, maxStack);
                target.itemId = source.itemId;
                target.quantity = canMove;
                source.quantity -= canMove;
                
                if (source.quantity <= 0) source.Clear();
                
                OnSlotChanged?.Invoke(fromSlot);
                OnSlotChanged?.Invoke(toSlot);
                return canMove;
            }
            
            // Target has same item
            if (target.itemId == source.itemId)
            {
                int space = maxStack - target.quantity;
                int canMove = Mathf.Min(toMove, space);
                target.quantity += canMove;
                source.quantity -= canMove;
                
                if (source.quantity <= 0) source.Clear();
                
                OnSlotChanged?.Invoke(fromSlot);
                OnSlotChanged?.Invoke(toSlot);
                return canMove;
            }
            
            // Different items - no move
            return 0;
        }
        
        /// <summary>
        /// Get count of empty slots
        /// </summary>
        public int GetEmptySlotCount()
        {
            int count = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (slots[i].IsEmpty) count++;
            }
            return count;
        }
        
        /// <summary>
        /// Get total quantity of a specific item
        /// </summary>
        public int GetTotalItemCount(string itemId)
        {
            int total = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (slots[i].itemId == itemId)
                {
                    total += slots[i].quantity;
                }
            }
            return total;
        }
        
        /// <summary>
        /// Get all items as dictionary (itemId -> total quantity)
        /// </summary>
        public Dictionary<string, int> GetAllItems()
        {
            var result = new Dictionary<string, int>();
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    if (result.ContainsKey(slots[i].itemId))
                        result[slots[i].itemId] += slots[i].quantity;
                    else
                        result[slots[i].itemId] = slots[i].quantity;
                }
            }
            return result;
        }
        
        /// <summary>
        /// Show "New!!" indicator when items are delivered
        /// </summary>
        public void ShowNewIndicator()
        {
            hasNewItems = true;
            if (newIndicatorText != null)
            {
                newIndicatorText.gameObject.SetActive(true);
            }
            Debug.Log("[WarehouseManager] New items delivered! Showing indicator.");
        }
        
        /// <summary>
        /// Hide "New!!" indicator when player interacts with warehouse
        /// </summary>
        public void HideNewIndicator()
        {
            hasNewItems = false;
            if (newIndicatorText != null)
            {
                newIndicatorText.gameObject.SetActive(false);
            }
            Debug.Log("[WarehouseManager] Indicator hidden after interaction.");
        }
    }
}
