using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Global registry for all ItemData in the game.
    /// Singleton that provides lookup by itemId.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ItemDataRegistry : Singleton<ItemDataRegistry>
    {
        [Header("All Items")]
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();
        
        private Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();
        
        protected override void Awake()
        {
            base.Awake();
            BuildLookup();
        }
        
        private void BuildLookup()
        {
            itemLookup.Clear();
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    if (itemLookup.ContainsKey(item.itemId))
                    {
                        Debug.LogWarning($"[ItemDataRegistry] Duplicate itemId: {item.itemId}");
                        continue;
                    }
                    itemLookup[item.itemId] = item;
                }
            }
            Debug.Log($"[ItemDataRegistry] Registered {itemLookup.Count} items");
        }
        
        /// <summary>
        /// Get ItemData by itemId
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            itemLookup.TryGetValue(itemId, out var item);
            return item;
        }
        
        /// <summary>
        /// Check if item exists
        /// </summary>
        public bool HasItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemLookup.ContainsKey(itemId);
        }
        
        /// <summary>
        /// Get all items of a specific category
        /// </summary>
        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            List<ItemData> result = new List<ItemData>();
            foreach (var item in allItems)
            {
                if (item != null && item.category == category)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Get all items available in store
        /// </summary>
        public List<ItemData> GetStoreItems()
        {
            List<ItemData> result = new List<ItemData>();
            foreach (var item in allItems)
            {
                if (item != null && item.availableInStore)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Instantiate an item by itemId
        /// </summary>
        public GameObject InstantiateItem(string itemId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var itemData = GetItem(itemId);
            if (itemData == null || itemData.prefab == null)
            {
                Debug.LogWarning($"[ItemDataRegistry] Cannot instantiate: {itemId} (not found or no prefab)");
                return null;
            }
            
            GameObject obj = Instantiate(itemData.prefab, position, rotation, parent);
            obj.SetActive(true);
            return obj;
        }
        
        /// <summary>
        /// Instantiate an item from ItemData directly
        /// </summary>
        public GameObject InstantiateItem(ItemData itemData, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (itemData == null || itemData.prefab == null)
            {
                Debug.LogWarning("[ItemDataRegistry] Cannot instantiate: null itemData or no prefab");
                return null;
            }
            
            GameObject obj = Instantiate(itemData.prefab, position, rotation, parent);
            obj.SetActive(true);
            return obj;
        }
    }
}
