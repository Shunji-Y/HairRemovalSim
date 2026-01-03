using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;
using System.Collections;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Treatment shelf with configurable rows, columns, and dimensions.
    /// Each slot can hold 1-4 items based on ShelfItem.maxStackOnShelf.
    /// Items are placed as actual GameObjects that can be interacted with.
    /// </summary>
    public class TreatmentShelf : MonoBehaviour
    {
        [System.Serializable]
        public class ShelfSlot
        {
            public string itemId;
            public int quantity;
            [System.NonSerialized]
            public List<GameObject> instances = new List<GameObject>();
        }
        
        [Header("Shelf Layout")]
        [Tooltip("Number of rows (vertical tiers)")]
        [Range(1, 5)]
        public int rowCount = 3;
        
        [Tooltip("Number of columns per row")]
        [Range(1, 6)]
        public int columnCount = 3;
        
        [Header("Slot Dimensions")]
        [Tooltip("Width of each slot (X axis - horizontal)")]
        public float slotWidth = 0.15f;
        
        [Tooltip("Depth of each slot (Z axis - front to back)")]
        public float slotDepth = 0.15f;
        
        [Tooltip("Horizontal gap between columns")]
        public float columnGap = 0.02f;
        
        [Header("Row Configuration")]
        [Tooltip("Height of each row/tier (Y spacing)")]
        public float rowHeight = 0.15f;
        
        [Tooltip("Y position of bottom row from shelf origin")]
        public float bottomRowY = 0.05f;
        
        [Header("Position Offset")]
        [Tooltip("X offset for first column (negative = left)")]
        public float originOffsetX = -0.17f;
        
        [Tooltip("Z offset (depth position)")]
        public float originOffsetZ = 0f;
        
        [Header("Highlight Colors")]
        [ColorUsage(true, true)]
        [Tooltip("Color for highlighting items on hover")]
        public Color highlightColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        [ColorUsage(true, true)]
        [Tooltip("Color for empty slot indicator")]
        public Color emptySlotColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        
        [Header("Initial Items")]
        [Tooltip("Items to spawn on shelf at game start")]
        [SerializeField] private List<InitialSlotItem> initialItems = new List<InitialSlotItem>();
        
        [System.Serializable]
        public class InitialSlotItem
        {
            [Tooltip("Which row (0-indexed)")]
            public int row;
            [Tooltip("Which column (0-indexed)")]
            public int col;
            [Tooltip("ItemData to place")]
            public ItemData itemData;
            [Tooltip("Quantity to place")]
            [Range(1, 4)]
            public int quantity = 1;
        }
        
        // Dynamic slot array
        private ShelfSlot[,] slots;
        
        public int RowCount => rowCount;
        public int ColumnCount => columnCount;
        
        private void Awake()
        {
            InitializeSlots();
            CreateSlotColliders();
        }
        
        private void Start()
        {
            SpawnInitialItems();
        }

        private void SpawnInitialItems()
        {
            
            foreach (var item in initialItems)
            {
                if (item.itemData == null) 
                {
                    continue;
                }
                if (item.row < 0 || item.row >= rowCount) 
                {
                    continue;
                }
                if (item.col < 0 || item.col >= columnCount) 
                {
                    continue;
                }

                int qty = item.quantity > 0 ? item.quantity : 1; // Fallback to 1 if quantity is 0
                bool success = PlaceItem(item.row, item.col, item.itemData.itemId, qty);
            }
        }
        
        private void InitializeSlots()
        {
            slots = new ShelfSlot[rowCount, columnCount];
            
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    slots[row, col] = new ShelfSlot();
                }
            }
        }
        
        /// <summary>
        /// Create slot colliders for all slots (always active)
        /// </summary>
        private void CreateSlotColliders()
        {
            // Create container
            var slotContainer = new GameObject("SlotColliders");
            slotContainer.transform.SetParent(transform);
            slotContainer.transform.localPosition = Vector3.zero;
            slotContainer.transform.localRotation = Quaternion.identity;
            
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    // Create slot object
                    GameObject slotObj = new GameObject($"Slot_{row}_{col}");
                    slotObj.layer = LayerMask.NameToLayer("Interactable");
                    slotObj.transform.SetParent(slotContainer.transform);
                    
                    // Set local position
                    float x = originOffsetX + col * (slotWidth + columnGap);
                    float y = bottomRowY + row * rowHeight;
                    float z = originOffsetZ;
                    slotObj.transform.localPosition = new Vector3(x, y, z);
                    slotObj.transform.localRotation = Quaternion.identity;
                    
                    // Add box collider
                    BoxCollider collider = slotObj.AddComponent<BoxCollider>();
                    collider.size = new Vector3(slotWidth, 0.05f, slotDepth);
                    collider.isTrigger = false;
                    
                    // Add unified interactable
                    ShelfSlotInteractable interactable = slotObj.AddComponent<ShelfSlotInteractable>();
                    interactable.Initialize(this, row, col);
                }
            }
        }
        
        /// <summary>
        /// Get world position for a slot's center
        /// </summary>
        public Vector3 GetSlotPosition(int row, int col)
        {
            float x = originOffsetX + col * (slotWidth + columnGap);
            float y = bottomRowY + row * rowHeight;
            float z = originOffsetZ;
            
            return transform.TransformPoint(new Vector3(x, y, z));
        }
        
        /// <summary>
        /// Get world position for a sub-slot (when item stacks > 1)
        /// subIndex: 0=top-left, 1=top-right, 2=bottom-left, 3=bottom-right
        /// </summary>
        public Vector3 GetSubSlotPosition(int row, int col, int subIndex)
        {
            Vector3 slotCenter = GetSlotPosition(row, col);
            
            // Sub-slot offsets (local space)
            float halfX = slotWidth * 0.25f;
            float halfZ = slotDepth * 0.25f;
            
            Vector3 offset = subIndex switch
            {
                0 => new Vector3(-halfX, 0, halfZ),  // top-left
                1 => new Vector3(halfX, 0, halfZ),   // top-right
                2 => new Vector3(-halfX, 0, -halfZ), // bottom-left
                3 => new Vector3(halfX, 0, -halfZ),  // bottom-right
                _ => Vector3.zero
            };
            
            return slotCenter + transform.TransformDirection(offset);
        }
        
        /// <summary>
        /// Place an item in a slot
        /// </summary>
        public bool PlaceItem(int row, int col, string itemId, int quantity = 1)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return false;
            
            var slot = slots[row, col];
            
            // Check if slot is occupied by different item
            if (!string.IsNullOrEmpty(slot.itemId) && slot.itemId != itemId)
            {
                return false;
            }
            
            // Get item data from registry
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null)
            {
                return false;
            }
            
            // Slot [0,0] is shaver-only, and shaver can only be at [0,0]
            if (row == 0 && col == 0 && itemData.toolType != TreatmentToolType.Shaver)
            {
                return false;
            }
            if (itemData.toolType == TreatmentToolType.Shaver && (row != 0 || col != 0))
            {
                return false;
            }
            
            // Check max stack
            int newQuantity = slot.quantity + quantity;
            if (newQuantity > itemData.maxStackOnShelf)
            {
                return false;
            }
            
            // Update slot data
            slot.itemId = itemId;
            slot.quantity = newQuantity;
            
            // Spawn item instances
            RefreshSlotInstances(row, col);
            
            return true;
        }
        
        /// <summary>
        /// Add quantity to an existing slot (for UI warehouse transfer)
        /// </summary>
        public bool AddToSlot(int row, int col, int quantity)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return false;
            
            var slot = slots[row, col];
            if (string.IsNullOrEmpty(slot.itemId)) return false;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(slot.itemId);
            if (itemData == null) return false;
            
            int newQuantity = slot.quantity + quantity;
            if (newQuantity > itemData.maxStackOnShelf) return false;
            
            slot.quantity = newQuantity;
            RefreshSlotInstances(row, col);
            return true;
        }
        
        /// <summary>
        /// Remove quantity from a slot (for UI warehouse transfer)
        /// </summary>
        public int RemoveFromSlot(int row, int col, int quantity)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return 0;
            
            var slot = slots[row, col];
            if (string.IsNullOrEmpty(slot.itemId)) return 0;
            
            int toRemove = Mathf.Min(quantity, slot.quantity);
            slot.quantity -= toRemove;
            
            if (slot.quantity <= 0)
            {
                slot.itemId = null;
                slot.quantity = 0;
            }
            
            RefreshSlotInstances(row, col);
            return toRemove;
        }
        
        /// <summary>
        /// Place an existing item GameObject directly on shelf (no pool involved)
        /// </summary>
        public bool PlaceItemDirect(int row, int col, string itemId, GameObject itemObject)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return false;
            if (itemObject == null) return false;
            
            var slot = slots[row, col];
            
            // Determine if slot is truly empty
            bool isSlotEmpty = string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0;
            
            // Check if slot is occupied by different item
            if (!isSlotEmpty && slot.itemId != itemId)
            {
                Debug.LogWarning($"[TreatmentShelf] Slot [{row},{col}] already has {slot.itemId}, cannot place {itemId}");
                return false;
            }
            
            // Get item data from registry
            var itemData = ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[TreatmentShelf] Unknown item: {itemId}");
                return false;
            }
            
            // Slot [0,0] is shaver-only, and shaver can only be at [0,0]
            if (row == 0 && col == 0 && itemData.toolType != TreatmentToolType.Shaver)
            {
                Debug.Log($"[TreatmentShelf] Slot [0,0] is shaver-only. Cannot place {itemData.toolType}");
                return false;
            }
            if (itemData.toolType == TreatmentToolType.Shaver && (row != 0 || col != 0))
            {
                Debug.Log($"[TreatmentShelf] Shaver can only be placed at [0,0], not [{row},{col}]");
                return false;
            }
            
            // Check max stack (only if not empty - empty slots start at 0)
            int currentQty = isSlotEmpty ? 0 : slot.quantity;
            int newQuantity = currentQty + 1;
            if (newQuantity > itemData.maxStackOnShelf)
            {
                Debug.LogWarning($"[TreatmentShelf] Cannot place more {itemId} (current: {currentQty}, max: {itemData.maxStackOnShelf})");
                return false;
            }
            
            // Update slot data (reset itemId if was empty)
            slot.itemId = itemId;
            slot.quantity = newQuantity;
            
            // Position the item on shelf
            int subIndex = -1;
            Vector3 position;
            if (itemData.maxStackOnShelf == 1)
            {
                position = GetSlotPosition(row, col);
            }
            else
            {
                subIndex = slot.quantity - 1;
                position = GetSubSlotPosition(row, col, subIndex);
            }
            
            itemObject.transform.position = position + itemData.shelfPosition;
            itemObject.transform.rotation = transform.rotation * Quaternion.Euler(itemData.shelfRotation);
            
            // Only apply custom scale if set
            if (itemData.shelfScale != Vector3.one)
            {
                itemObject.transform.localScale = itemData.shelfScale;
            }
            
            // Add to instances list
            slot.instances.Add(itemObject);
            
            // Add ShelfSlotInteractable for pickup
            var interactable = itemObject.GetComponent<ShelfSlotInteractable>();
            if (interactable == null)
            {
                interactable = itemObject.AddComponent<ShelfSlotInteractable>();
            }
            interactable.Initialize(this, row, col);
            
            Debug.Log($"[TreatmentShelf] Placed {itemId} directly at [{row},{col}]");
            return true;
        }
        
        /// <summary>
        /// Remove an item from a slot
        /// </summary>
        public bool RemoveItem(int row, int col, int quantity = 1)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return false;
            
            var slot = slots[row, col];
            
            if (string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0)
            {
                return false;
            }
            
            slot.quantity = Mathf.Max(0, slot.quantity - quantity);
            
            if (slot.quantity <= 0)
            {
                slot.itemId = null;
            }
            
            RefreshSlotInstances(row, col);
            
            return true;
        }
        
        /// <summary>
        /// Remove an item from a slot without returning it to pool.
        /// Used when the item is being picked up directly.
        /// </summary>
        public bool RemoveItemDirect(int row, int col, GameObject itemToRemove)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return false;
            
            var slot = slots[row, col];
            
            if (string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0)
            {
                return false;
            }
            
            // Remove from instances list
            slot.instances.Remove(itemToRemove);
            
            slot.quantity = Mathf.Max(0, slot.quantity - 1);
            
            if (slot.quantity <= 0)
            {
                slot.itemId = null;
            }
            
            return true;
        }
        
        /// <summary>
        /// Refresh visual instances for a slot based on current data
        /// </summary>
        private void RefreshSlotInstances(int row, int col)
        {
            var slot = slots[row, col];
            
            // Clear existing instances
            foreach (var instance in slot.instances)
            {
                if (instance != null)
                {
                    // If itemId is valid, try to return to pool, otherwise just destroy
                    if (!string.IsNullOrEmpty(slot.itemId) && ItemPoolManager.Instance != null)
                    {
                        ItemPoolManager.Instance.ReturnItem(slot.itemId, instance);
                    }
                    else
                    {
                        Destroy(instance);
                    }
                }
            }
            slot.instances.Clear();
            
            // Spawn new instances
            if (string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0) return;
            
            var itemData = ItemDataRegistry.Instance?.GetItem(slot.itemId);
            if (itemData == null) return;
            if (ItemDataRegistry.Instance == null) return;
            
            for (int i = 0; i < slot.quantity; i++)
            {
                Vector3 position;
                int subIndex = -1;
                
                if (itemData.maxStackOnShelf == 1)
                {
                    // Single item - center of slot
                    position = GetSlotPosition(row, col);
                }
                else
                {
                    // Multiple items - use sub-slots
                    subIndex = i;
                    position = GetSubSlotPosition(row, col, i);
                }
                
                Quaternion rotation = transform.rotation * Quaternion.Euler(itemData.shelfRotation);
                
                // Instantiate via ItemDataRegistry (uses prefab from ItemData)
                GameObject obj = ItemDataRegistry.Instance.InstantiateItem(slot.itemId, position + itemData.shelfPosition, rotation, null);
                if (obj != null)
                {
                    // Only apply custom scale if set (not default 1,1,1)
                    if (itemData.shelfScale != Vector3.one)
                    {
                        obj.transform.localScale = itemData.shelfScale;
                    }
                    // Otherwise keep prefab's original scale
                    
                    slot.instances.Add(obj);
                    
                    // Add interactable component for pickup/swap
                    var interactable = obj.GetComponent<ShelfSlotInteractable>();
                    if (interactable == null)
                    {
                        interactable = obj.AddComponent<ShelfSlotInteractable>();
                    }
                    interactable.Initialize(this, row, col);
                }
            }
        }
        
        /// <summary>
        /// Get slot data for saving
        /// </summary>
        public ShelfSlot GetSlotData(int row, int col)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return null;
            return slots[row, col];
        }
        
        /// <summary>
        /// Load slot data (for save/load)
        /// </summary>
        public void LoadSlotData(int row, int col, string itemId, int quantity)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= columnCount) return;
            
            slots[row, col].itemId = itemId;
            slots[row, col].quantity = quantity;
            RefreshSlotInstances(row, col);
        }
        
        /// <summary>
        /// Clear all items from shelf
        /// </summary>
        public void ClearAll()
        {
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    var slot = slots[row, col];
                    foreach (var instance in slot.instances)
                    {
                        if (instance != null && ItemPoolManager.Instance != null)
                        {
                            ItemPoolManager.Instance.ReturnItem(slot.itemId, instance);
                        }
                    }
                    slot.instances.Clear();
                    slot.itemId = null;
                    slot.quantity = 0;
                }
            }
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Set Gizmos matrix to match object's transform (handles rotation/scale)
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            // Draw slot positions for debugging (in local space now)
            Gizmos.color = Color.cyan;
            Vector3 slotSize = new Vector3(slotWidth, 0.02f, slotDepth);
            
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    // Calculate local position directly
                    float x = originOffsetX + col * (slotWidth + columnGap);
                    float y = bottomRowY + row * rowHeight;
                    float z = originOffsetZ;
                    Vector3 localPos = new Vector3(x, y, z);
                    
                    Gizmos.DrawWireCube(localPos, slotSize * 0.9f);
                    
                    // Draw sub-slots
                    Gizmos.color = Color.yellow;
                    float halfX = slotWidth * 0.25f;
                    float halfZ = slotDepth * 0.25f;
                    
                    Vector3[] subOffsets = new Vector3[]
                    {
                        new Vector3(-halfX, 0, halfZ),  // top-left
                        new Vector3(halfX, 0, halfZ),   // top-right
                        new Vector3(-halfX, 0, -halfZ), // bottom-left
                        new Vector3(halfX, 0, -halfZ),  // bottom-right
                    };
                    
                    for (int sub = 0; sub < 4; sub++)
                    {
                        Gizmos.DrawWireSphere(localPos + subOffsets[sub], 0.01f);
                    }
                    Gizmos.color = Color.cyan;
                }
            }
            
            // Restore matrix
            Gizmos.matrix = oldMatrix;
        }
        #endif
        
        // ========== STAFF TREATMENT METHODS ==========
        
        /// <summary>
        /// Check if shelf has any items
        /// </summary>
        public bool HasItems()
        {
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    var slot = slots[row, col];
                    if (!string.IsNullOrEmpty(slot.itemId) && slot.quantity > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        /// <summary>
        /// Consume a random item from the shelf (for staff treatment)
        /// Returns item ID and data, or null if no items
        /// </summary>
        public (string itemId, Core.ItemData itemData)? ConsumeRandomItem()
        {
            // Collect all slots with items (skip [0,0] which is reserved for shaver)
            var slotsWithItems = new List<(int row, int col, string itemId)>();
            
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    // Skip shaver slot [0,0] - never consume shaver
                    if (row == 0 && col == 0) continue;
                    
                    var slot = slots[row, col];
                    if (!string.IsNullOrEmpty(slot.itemId) && slot.quantity > 0)
                    {
                        slotsWithItems.Add((row, col, slot.itemId));
                    }
                }
            }
            
            if (slotsWithItems.Count == 0) return null;
            
            // Pick random slot
            var selected = slotsWithItems[Random.Range(0, slotsWithItems.Count)];
            
            // Get item data before removing
            var itemData = Core.ItemDataRegistry.Instance?.GetItem(selected.itemId);
            if (itemData == null) return null;
            
            // Consume one
            RemoveItem(selected.row, selected.col, 1);
            
            Debug.Log($"[TreatmentShelf] Staff consumed {selected.itemId} from [{selected.row},{selected.col}]");
            
            return (selected.itemId, itemData);
        }
        
        /// <summary>
        /// Check if all slots are full
        /// </summary>
        public bool IsFull()
        {
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    var slot = slots[row, col];
                    if (string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0)
                    {
                        return false;
                    }
                    
                    // Check if slot has room for more (if same item could stack)
                    var itemData = Core.ItemDataRegistry.Instance?.GetItem(slot.itemId);
                    if (itemData != null && slot.quantity < itemData.maxStackOnShelf)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Add items to shelf (for staff restocking)
        /// Returns number of items actually added
        /// </summary>
        public int AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return 0;
            
            var itemData = Core.ItemDataRegistry.Instance?.GetItem(itemId);
            if (itemData == null || !itemData.CanPlaceOnShelf) return 0;
            
            int remaining = quantity;
            int maxStack = itemData.maxStackOnShelf;
            
            // First pass: try to add to existing stacks
            for (int row = 0; row < rowCount && remaining > 0; row++)
            {
                for (int col = 0; col < columnCount && remaining > 0; col++)
                {
                    var slot = slots[row, col];
                    if (slot.itemId == itemId && slot.quantity < maxStack)
                    {
                        int space = maxStack - slot.quantity;
                        int toAdd = Mathf.Min(space, remaining);
                        slot.quantity += toAdd;
                        remaining -= toAdd;
                    }
                }
            }
            
            // Second pass: use empty slots
            for (int row = 0; row < rowCount && remaining > 0; row++)
            {
                for (int col = 0; col < columnCount && remaining > 0; col++)
                {
                    var slot = slots[row, col];
                    if (string.IsNullOrEmpty(slot.itemId) || slot.quantity <= 0)
                    {
                        int toAdd = Mathf.Min(maxStack, remaining);
                        slot.itemId = itemId;
                        slot.quantity = toAdd;
                        remaining -= toAdd;
                    }
                }
            }
            
            int added = quantity - remaining;
            if (added > 0)
            {
                Debug.Log($"[TreatmentShelf] Staff added {added}x {itemId}");
            }
            return added;
        }
    }
}
