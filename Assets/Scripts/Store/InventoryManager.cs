using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// Manages player inventory (warehouse).
    /// Tracks purchased items and quantities.
    /// </summary>
    public class InventoryManager : Core.Singleton<InventoryManager>
    {
        private Dictionary<StoreItemData, int> inventory = new Dictionary<StoreItemData, int>();
        
        /// <summary>
        /// Add items to inventory
        /// </summary>
        public void AddItem(StoreItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            
            if (inventory.ContainsKey(item))
            {
                inventory[item] += quantity;
            }
            else
            {
                inventory[item] = quantity;
            }
            
            Debug.Log($"[InventoryManager] Added {quantity}x {item.itemName}. Total: {inventory[item]}");
        }
        
        /// <summary>
        /// Remove items from inventory
        /// </summary>
        public bool RemoveItem(StoreItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            
            if (!inventory.ContainsKey(item) || inventory[item] < quantity)
            {
                return false;
            }
            
            inventory[item] -= quantity;
            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
            }
            
            Debug.Log($"[InventoryManager] Removed {quantity}x {item.itemName}");
            return true;
        }
        
        /// <summary>
        /// Get quantity of item in inventory
        /// </summary>
        public int GetItemCount(StoreItemData item)
        {
            if (item == null) return 0;
            return inventory.ContainsKey(item) ? inventory[item] : 0;
        }
        
        /// <summary>
        /// Check if player has at least specified quantity
        /// </summary>
        public bool HasItem(StoreItemData item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }
    }
}
