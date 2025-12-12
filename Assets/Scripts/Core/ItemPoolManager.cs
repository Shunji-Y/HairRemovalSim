using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Object pool manager for items (tools, consumables, etc.)
    /// Singleton that manages instantiation and recycling of item prefabs.
    /// </summary>
    public class ItemPoolManager : Singleton<ItemPoolManager>
    {
        [System.Serializable]
        public class ItemPoolEntry
        {
            public string itemId;
            public GameObject prefab;
            public int initialPoolSize = 3;
        }
        
        [Header("Item Pool Settings")]
        [SerializeField] private List<ItemPoolEntry> itemEntries = new List<ItemPoolEntry>();
        [SerializeField] private Transform poolParent;
        
        private Dictionary<string, ItemPoolEntry> itemLookup = new Dictionary<string, ItemPoolEntry>();
        private Dictionary<string, Queue<GameObject>> itemPools = new Dictionary<string, Queue<GameObject>>();
        
        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }
        
        private void InitializePools()
        {
            // Create pool parent if not assigned
            if (poolParent == null)
            {
                GameObject parent = new GameObject("ItemPool");
                parent.transform.SetParent(transform);
                poolParent = parent.transform;
            }
            
            // Build lookup and create pools
            foreach (var entry in itemEntries)
            {
                if (string.IsNullOrEmpty(entry.itemId) || entry.prefab == null)
                {
                    Debug.LogWarning("[ItemPoolManager] Invalid item entry - skipping");
                    continue;
                }
                
                itemLookup[entry.itemId] = entry;
                itemPools[entry.itemId] = new Queue<GameObject>();
                
                // Pre-instantiate pool
                for (int i = 0; i < entry.initialPoolSize; i++)
                {
                    GameObject obj = Instantiate(entry.prefab, poolParent);
                    obj.SetActive(false);
                    itemPools[entry.itemId].Enqueue(obj);
                }
            }
            
            Debug.Log($"[ItemPoolManager] Initialized {itemLookup.Count} item pools");
        }
        
        /// <summary>
        /// Get an item from the pool
        /// </summary>
        public GameObject GetItem(string itemId)
        {
            if (!itemLookup.TryGetValue(itemId, out var entry))
            {
                Debug.LogWarning($"[ItemPoolManager] Item not found: {itemId}");
                return null;
            }
            
            GameObject obj;
            
            if (itemPools[itemId].Count > 0)
            {
                obj = itemPools[itemId].Dequeue();
            }
            else
            {
                // Pool empty - create new instance
                obj = Instantiate(entry.prefab, poolParent);
            }
            
            obj.SetActive(true);
            obj.transform.SetParent(null);
            return obj;
        }
        
        /// <summary>
        /// Get an item and place it at specific position/rotation
        /// </summary>
        public GameObject GetItem(string itemId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject obj = GetItem(itemId);
            if (obj == null) return null;
            
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        
        /// <summary>
        /// Return an item to the pool
        /// </summary>
        public void ReturnItem(string itemId, GameObject obj)
        {
            if (obj == null) return;
            
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            
            if (itemPools.ContainsKey(itemId))
            {
                itemPools[itemId].Enqueue(obj);
            }
            else
            {
                // Unknown item ID - just destroy it
                Destroy(obj);
            }
        }
        
        /// <summary>
        /// Check if an item exists in the pool registry
        /// </summary>
        public bool HasItem(string itemId)
        {
            return itemLookup.ContainsKey(itemId);
        }
        
        /// <summary>
        /// Get the prefab for an item (for UI display, etc.)
        /// </summary>
        public GameObject GetPrefab(string itemId)
        {
            if (itemLookup.TryGetValue(itemId, out var entry))
            {
                return entry.prefab;
            }
            return null;
        }
    }
}
