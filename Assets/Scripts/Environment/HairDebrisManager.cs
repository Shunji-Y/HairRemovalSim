using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Manages hair debris spawning, tracking, and cleaning progress.
    /// Debris spawns during treatment and must be cleaned after hours.
    /// Uses object pool for performance.
    /// </summary>
    public class HairDebrisManager : MonoBehaviour
    {
        public static HairDebrisManager Instance { get; private set; }
        
        [Header("Spawn Settings")]
        [Tooltip("Prefab for hair debris decal")]
        [SerializeField] private GameObject debrisPrefab;
        
        [Tooltip("Chance to spawn debris when hair is removed (0-1)")]
        [SerializeField] private float spawnChance = 0.3f;
        
        [Tooltip("Minimum spawn radius (inner circle of donut)")]
        [SerializeField] private float minSpawnRadius = 0.5f;
        
        [Tooltip("Maximum spawn radius (outer circle of donut)")]
        [SerializeField] private float maxSpawnRadius = 1.5f;
        
        [Tooltip("Height offset for decal projection")]
        [SerializeField] private float heightOffset = 0.1f;
        
        [Header("Object Pool")]
        [Tooltip("Initial pool size")]
        [SerializeField] private int poolSize = 20;
        
        [Header("Daily Limit")]
        [Tooltip("Maximum debris that can spawn per day")]
        [SerializeField] private int maxSpawnPerDay = 10;
        
        [Header("Floor Detection")]
        [Tooltip("Layer mask for floor only (to avoid spawning on curtains etc)")]
        [SerializeField] private LayerMask floorLayerMask = ~0; // Default: all layers
        
        // Object pool
        private Queue<HairDebrisDecal> debrisPool = new Queue<HairDebrisDecal>();
        private Transform poolParent;
        
        // Tracking
        private List<HairDebrisDecal> activeDebris = new List<HairDebrisDecal>();
        private int totalSpawnedEver = 0;
        private int cleanedEver = 0;
        private int spawnedToday = 0; // Reset each day
        private bool iconsVisible = true;
        
        // Events
        public event System.Action<float> OnCleaningProgressChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializePool();
        }
        
        private void InitializePool()
        {
            if (debrisPrefab == null) return;
            
            // Create parent for pooled objects
            poolParent = new GameObject("DebrisPool").transform;
            poolParent.SetParent(transform);
            
            // Pre-spawn pool
            for (int i = 0; i < poolSize; i++)
            {
                var obj = Instantiate(debrisPrefab, poolParent);
                obj.SetActive(false);
                var debris = obj.GetComponent<HairDebrisDecal>();
                if (debris != null)
                {
                    debrisPool.Enqueue(debris);
                }
            }
            
            Debug.Log($"[HairDebrisManager] Pool initialized with {poolSize} debris objects");
        }
        
        private HairDebrisDecal GetFromPool()
        {
            HairDebrisDecal debris;
            
            if (debrisPool.Count > 0)
            {
                debris = debrisPool.Dequeue();
            }
            else
            {
                // Expand pool if empty
                var obj = Instantiate(debrisPrefab, poolParent);
                debris = obj.GetComponent<HairDebrisDecal>();
            }
            
            return debris;
        }
        
        private void ReturnToPool(HairDebrisDecal debris)
        {
            if (debris == null) return;
            
            debris.gameObject.SetActive(false);
            debris.transform.SetParent(poolParent);
            debrisPool.Enqueue(debris);
        }
        
        /// <summary>
        /// Called when hair is removed during treatment
        /// </summary>
        public void OnHairRemoved(Vector3 bedPosition)
        {
            if (debrisPrefab == null) return;
            
            // Check daily limit
            if (spawnedToday >= maxSpawnPerDay) return;
            
            // Random chance to spawn
            if (Random.value > spawnChance) return;
            
            // Random position in donut shape (between minRadius and maxRadius)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            Vector3 spawnPos = bedPosition + new Vector3(x, heightOffset, z);
            
            // Raycast to find floor only (use layer mask to ignore curtains etc)
            if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, floorLayerMask))
            {
                spawnPos = hit.point + Vector3.up * heightOffset;
            }
            else
            {
                // No floor found at this position, skip spawning
                return;
            }
            
            // Get debris from pool
            var debrisComponent = GetFromPool();
            if (debrisComponent == null) return;
            
            // Reset and position
            debrisComponent.transform.position = spawnPos;
            debrisComponent.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            debrisComponent.transform.SetParent(null);
            debrisComponent.ResetState();
            debrisComponent.SetIconVisible(iconsVisible); // Hide icon during business hours
            debrisComponent.gameObject.SetActive(true);
            
            activeDebris.Add(debrisComponent);
            totalSpawnedEver++;
            spawnedToday++;
            
            Debug.Log($"[HairDebrisManager] Spawned debris at {spawnPos}, today: {spawnedToday}/{maxSpawnPerDay}, total: {activeDebris.Count}");
        }
        
        /// <summary>
        /// Force spawn debris (for debug - bypasses spawn chance)
        /// </summary>
        public void ForceSpawnDebris(Vector3 bedPosition)
        {
            if (debrisPrefab == null) return;
            
            // Random position in donut shape
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            Vector3 spawnPos = bedPosition + new Vector3(x, heightOffset, z);
            
            // Raycast to find floor only
            if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, floorLayerMask))
            {
                spawnPos = hit.point + Vector3.up * heightOffset;
            }
            else
            {
                // No floor found, skip
                return;
            }
            
            // Get debris from pool
            var debrisComponent = GetFromPool();
            if (debrisComponent == null) return;
            
            // Reset and position
            debrisComponent.transform.position = spawnPos;
            debrisComponent.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            debrisComponent.transform.SetParent(null);
            debrisComponent.ResetState();
            debrisComponent.SetIconVisible(iconsVisible);
            debrisComponent.gameObject.SetActive(true);
            
            activeDebris.Add(debrisComponent);
            totalSpawnedEver++;
            spawnedToday++;
        }
        
        /// <summary>
        /// Set visibility of all debris icons (hidden during business hours)
        /// </summary>
        public void SetIconsVisible(bool visible)
        {
            iconsVisible = visible;
            
            foreach (var debris in activeDebris)
            {
                if (debris != null)
                {
                    debris.SetIconVisible(visible);
                }
            }
            
            Debug.Log($"[HairDebrisManager] Icons visibility set to {visible}");
        }
        
        /// <summary>
        /// Called when a debris is cleaned
        /// </summary>
        public void OnDebrisCleaned(HairDebrisDecal debris)
        {
            if (activeDebris.Contains(debris))
            {
                activeDebris.Remove(debris);
                cleanedEver++;
                
                // Return to pool
                ReturnToPool(debris);
                
                // Notify UI
                OnCleaningProgressChanged?.Invoke(GetCleaningProgress());
                
                Debug.Log($"[HairDebrisManager] Debris cleaned, progress: {GetCleaningProgress():P0}");
            }
        }
        
        /// <summary>
        /// Get current cleaning progress (0-1)
        /// </summary>
        public float GetCleaningProgress()
        {
            if (totalSpawnedEver == 0) return 1f;
            return (float)cleanedEver / totalSpawnedEver;
        }
        
        /// <summary>
        /// Get number of remaining debris
        /// </summary>
        public int GetRemainingCount()
        {
            return activeDebris.Count;
        }
        
        /// <summary>
        /// Get total spawned ever (cumulative)
        /// </summary>
        public int GetTotalSpawnedEver()
        {
            return totalSpawnedEver;
        }
        
        /// <summary>
        /// Get spawned today count
        /// </summary>
        public int GetSpawnedToday()
        {
            return spawnedToday;
        }
        
        /// <summary>
        /// Calculate review penalty based on remaining debris count.
        /// Each debris = -1 point.
        /// </summary>
        public int GetDebrisPenalty()
        {
            return -activeDebris.Count;
        }
        
        /// <summary>
        /// Clean all remaining debris (called by Roomba)
        /// </summary>
        public void CleanAllDebris()
        {
            foreach (var debris in activeDebris.ToArray())
            {
                if (debris != null && !debris.IsCleaned)
                {
                    debris.Clean();
                }
            }
            
            Debug.Log("[HairDebrisManager] All debris auto-cleaned by Roomba");
        }
        
        /// <summary>
        /// Called at start of new day. 
        /// Debris is cumulative - not cleared, only counts are carried over.
        /// </summary>
        public void OnNewDayStart()
        {
            // Reset daily spawn counter
            spawnedToday = 0;
            
            // Hide icons during business hours
            SetIconsVisible(false);
            
            Debug.Log($"[HairDebrisManager] New day started. Remaining debris: {activeDebris.Count}, Total ever: {totalSpawnedEver}, Cleaned: {cleanedEver}");
        }
        
        /// <summary>
        /// Force reset all debris (e.g., for debug or game reset)
        /// </summary>
        public void ForceResetAll()
        {
            // Return all active debris to pool
            foreach (var debris in activeDebris)
            {
                if (debris != null)
                {
                    ReturnToPool(debris);
                }
            }
            
            activeDebris.Clear();
            totalSpawnedEver = 0;
            cleanedEver = 0;
            spawnedToday = 0;
            
            OnCleaningProgressChanged?.Invoke(1f);
            
            Debug.Log("[HairDebrisManager] Force reset all debris");
        }
    }
}
