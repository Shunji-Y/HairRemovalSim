using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core.Effects;
using HairRemovalSim.Customer;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages placement items - fixed slot system for decorative/functional items.
    /// Items are purchased from ToolShop, placed immediately, and provide permanent effects.
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }
        
        [System.Serializable]
        public class PlacementSlot
        {
            public string slotId;
            public Transform spawnPoint;
            [HideInInspector] public ItemData placedItem;
            [HideInInspector] public GameObject spawnedObject;
        }
        
        [Header("Placement Slots")]
        [Tooltip("Available placement slots in the scene")]
        public List<PlacementSlot> slots = new List<PlacementSlot>();
        
        // Currently owned placement items (item IDs)
        private HashSet<string> ownedItems = new HashSet<string>();
        
        // Applied effects tracking
        private Dictionary<string, EffectContext> appliedEffects = new Dictionary<string, EffectContext>();
        
        // Accumulated wait time boost from all placement items
        private float totalWaitTimeBoost = 0f;
        
        // Roomba auto-clean enabled
        private bool autoCleanEnabled = false;
        
        /// <summary>
        /// Get the total wait time percentage boost from all placement items
        /// </summary>
        public float GetWaitTimeBoost() => totalWaitTimeBoost;
        
        /// <summary>
        /// Check if Roomba auto-clean is enabled
        /// </summary>
        public bool IsAutoCleanEnabled() => autoCleanEnabled;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Check if a placement item is already owned
        /// </summary>
        public bool IsOwned(string itemId)
        {
            return ownedItems.Contains(itemId);
        }
        
        /// <summary>
        /// Purchase and place an item
        /// </summary>
        public bool PlaceItem(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.placementSlotId))
            {
                Debug.LogWarning("[PlacementManager] Invalid item or no slot ID");
                return false;
            }
            
            if (IsOwned(item.itemId))
            {
                Debug.LogWarning($"[PlacementManager] Item {item.itemId} already owned");
                return false;
            }
            
            // Find the slot
            var slot = slots.Find(s => s.slotId == item.placementSlotId);
            if (slot == null)
            {
                Debug.LogWarning($"[PlacementManager] Slot {item.placementSlotId} not found");
                return false;
            }
            
            // Check cost
            if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendMoney(item.price))
            {
                Debug.Log($"[PlacementManager] Not enough money for {item.name}");
                return false;
            }
            
            // Spawn prefab
            if (item.placementPrefab != null && slot.spawnPoint != null)
            {
                slot.spawnedObject = Instantiate(item.placementPrefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
                slot.spawnedObject.transform.SetParent(slot.spawnPoint);
            }
            
            slot.placedItem = item;
            ownedItems.Add(item.itemId);
            
            // Apply effects
            ApplyItemEffects(item);
            
            Debug.Log($"[PlacementManager] Placed {item.name} at slot {slot.slotId}");
            return true;
        }
        
        /// <summary>
        /// Sell and remove an item (50% refund)
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            if (!IsOwned(itemId))
            {
                Debug.LogWarning($"[PlacementManager] Item {itemId} not owned");
                return false;
            }
            
            // Find the slot with this item
            var slot = slots.Find(s => s.placedItem != null && s.placedItem.itemId == itemId);
            if (slot == null)
            {
                ownedItems.Remove(itemId);
                return true;
            }
            
            // Refund 50%
            int refund = slot.placedItem.price / 2;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(refund);
            }
            
            // Remove effects
            RemoveItemEffects(slot.placedItem);
            
            // Destroy spawned object
            if (slot.spawnedObject != null)
            {
                Destroy(slot.spawnedObject);
                slot.spawnedObject = null;
            }
            
            string itemName = slot.placedItem.name;
            slot.placedItem = null;
            ownedItems.Remove(itemId);
            
            Debug.Log($"[PlacementManager] Sold {itemName}, refunded ${refund}");
            return true;
        }
        
        /// <summary>
        /// Apply permanent effects from a placement item
        /// </summary>
        private void ApplyItemEffects(ItemData item)
        {
            if (item == null || item.effects == null || item.effects.Count == 0) return;
            
            var ctx = EffectContext.CreateForRegister();
            EffectHelper.ApplyEffects(item, ctx);
            
            // Apply to CustomerSpawner
            var spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                if (ctx.AttractionBoost > 0f)
                {
                    spawner.AddAttractionBoost(ctx.AttractionBoost);
                }
                if (ctx.AttractionPercentBoost > 0f)
                {
                    spawner.AddAttractionPercentBoost(ctx.AttractionPercentBoost);
                }
            }
            
            // Apply review percent boost to ShopManager
            if (ShopManager.Instance != null && ctx.ReviewPercentBoost > 0f)
            {
                ShopManager.Instance.AddReviewPercentBoost(ctx.ReviewPercentBoost);
            }
            
            // Apply wait time boost
            if (ctx.WaitTimePercentBoost > 0f)
            {
                totalWaitTimeBoost += ctx.WaitTimePercentBoost;
            }
            
            // Apply auto-clean (Roomba)
            if (ctx.AutoCleanEnabled)
            {
                autoCleanEnabled = true;
            }
            
            // Store for later removal
            appliedEffects[item.itemId] = ctx;
            
            Debug.Log($"[PlacementManager] Applied effects: Attraction+{ctx.AttractionBoost}, Attraction%+{ctx.AttractionPercentBoost:P0}, Review%+{ctx.ReviewPercentBoost:P0}, WaitTime%+{ctx.WaitTimePercentBoost:P0}, AutoClean={ctx.AutoCleanEnabled}");
        }
        
        /// <summary>
        /// Remove effects when item is sold
        /// </summary>
        private void RemoveItemEffects(ItemData item)
        {
            if (item == null) return;
            
            if (!appliedEffects.TryGetValue(item.itemId, out var ctx)) return;
            
            // Remove from CustomerSpawner
            var spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                if (ctx.AttractionBoost > 0f)
                {
                    spawner.AddAttractionBoost(-ctx.AttractionBoost);
                }
                if (ctx.AttractionPercentBoost > 0f)
                {
                    spawner.AddAttractionPercentBoost(-ctx.AttractionPercentBoost);
                }
            }
            
            // Remove review percent boost from ShopManager
            if (ShopManager.Instance != null && ctx.ReviewPercentBoost > 0f)
            {
                ShopManager.Instance.AddReviewPercentBoost(-ctx.ReviewPercentBoost);
            }
            
            // Remove wait time boost
            if (ctx.WaitTimePercentBoost > 0f)
            {
                totalWaitTimeBoost -= ctx.WaitTimePercentBoost;
            }
            
            // Remove auto-clean (Roomba) - need to recheck all items
            if (ctx.AutoCleanEnabled)
            {
                autoCleanEnabled = false;
                // Check if any other item provides auto-clean
                foreach (var other in appliedEffects.Values)
                {
                    if (other.AutoCleanEnabled)
                    {
                        autoCleanEnabled = true;
                        break;
                    }
                }
            }
            
            appliedEffects.Remove(item.itemId);
            
            Debug.Log($"[PlacementManager] Removed effects from {item.name}");
        }
        
        /// <summary>
        /// Get all placement items for UI display
        /// </summary>
        public List<ItemData> GetAllPlacementItems()
        {
            var registry = ItemDataRegistry.Instance;
            if (registry == null) return new List<ItemData>();
            
            return registry.GetItemsByCategory(ItemCategory.PlacementItem);
        }
    }
}
