using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// Manages player inventory (warehouse).
    /// Tracks purchased items and pending orders.
    /// Now uses unified ItemData.
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        // Use itemId as key for serialization compatibility
        private Dictionary<string, int> inventory = new Dictionary<string, int>();
        private Dictionary<string, int> pendingOrders = new Dictionary<string, int>();
        
        /// <summary>
        /// Add items to pending orders (delivered next day)
        /// </summary>
        public void AddPendingOrder(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            
            string itemId = item.itemId;
            if (pendingOrders.ContainsKey(itemId))
            {
                pendingOrders[itemId] += quantity;
            }
            else
            {
                pendingOrders[itemId] = quantity;
            }
            
            Debug.Log($"[InventoryManager] Order placed: {quantity}x {item.displayName} (delivered next day)");
        }
        
        /// <summary>
        /// Process pending orders - call this when a new day starts
        /// </summary>
        public void ProcessPendingOrders()
        {
            if (pendingOrders.Count == 0)
            {
                Debug.Log("[InventoryManager] No pending orders to process");
                return;
            }
            
            foreach (var order in pendingOrders)
            {
                AddItemById(order.Key, order.Value);
            }
            
            Debug.Log($"[InventoryManager] Processed {pendingOrders.Count} orders - items delivered!");
            pendingOrders.Clear();
        }
        
        /// <summary>
        /// Add items directly to inventory by ItemData
        /// </summary>
        public void AddItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            AddItemById(item.itemId, quantity);
            Debug.Log($"[InventoryManager] Added {quantity}x {item.displayName}. Total: {GetItemCount(item)}");
        }
        
        /// <summary>
        /// Add items directly to inventory by itemId
        /// </summary>
        public void AddItemById(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;
            
            if (inventory.ContainsKey(itemId))
            {
                inventory[itemId] += quantity;
            }
            else
            {
                inventory[itemId] = quantity;
            }
        }
        
        /// <summary>
        /// Remove items from inventory
        /// </summary>
        public bool RemoveItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            return RemoveItemById(item.itemId, quantity);
        }
        
        /// <summary>
        /// Remove items from inventory by itemId
        /// </summary>
        public bool RemoveItemById(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            
            if (!inventory.ContainsKey(itemId) || inventory[itemId] < quantity)
            {
                return false;
            }
            
            inventory[itemId] -= quantity;
            if (inventory[itemId] <= 0)
            {
                inventory.Remove(itemId);
            }
            
            return true;
        }
        
        /// <summary>
        /// Get quantity of item in inventory
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            return GetItemCountById(item.itemId);
        }
        
        /// <summary>
        /// Get quantity by itemId
        /// </summary>
        public int GetItemCountById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            return inventory.ContainsKey(itemId) ? inventory[itemId] : 0;
        }
        
        /// <summary>
        /// Check if player has at least specified quantity
        /// </summary>
        public bool HasItem(ItemData item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }
        
        /// <summary>
        /// Get pending order count for an item
        /// </summary>
        public int GetPendingCount(ItemData item)
        {
            if (item == null) return 0;
            return pendingOrders.ContainsKey(item.itemId) ? pendingOrders[item.itemId] : 0;
        }
        
        /// <summary>
        /// Get all inventory items
        /// </summary>
        public Dictionary<string, int> GetAllItems()
        {
            return new Dictionary<string, int>(inventory);
        }
    }
}
