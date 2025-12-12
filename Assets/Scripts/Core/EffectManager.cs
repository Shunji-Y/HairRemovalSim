using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages visual effects with object pooling.
    /// </summary>
    public class EffectManager : Singleton<EffectManager>
    {
        [Header("Effect Library")]
        [SerializeField] private EffectLibrary effectLibrary;
        
        private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, EffectLibrary.EffectEntry> effectLookup = new Dictionary<string, EffectLibrary.EffectEntry>();
        private Transform poolParent;
        
        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }
        
        private void InitializePools()
        {
            if (effectLibrary == null)
            {
                Debug.LogWarning("[EffectManager] No EffectLibrary assigned!");
                return;
            }
            
            // Create parent for pooled objects
            poolParent = new GameObject("EffectPool").transform;
            poolParent.SetParent(transform);
            
            // Build lookup and pre-instantiate pools
            foreach (var entry in effectLibrary.effects)
            {
                if (string.IsNullOrEmpty(entry.id) || entry.prefab == null) continue;
                
                effectLookup[entry.id] = entry;
                effectPools[entry.id] = new Queue<GameObject>();
                
                // Pre-instantiate pool
                for (int i = 0; i < entry.poolSize; i++)
                {
                    GameObject obj = Instantiate(entry.prefab, poolParent);
                    obj.SetActive(false);
                    effectPools[entry.id].Enqueue(obj);
                }
                
                Debug.Log($"[EffectManager] Pooled {entry.poolSize} instances of '{entry.id}'");
            }
        }
        
        /// <summary>
        /// Spawn an effect at a position
        /// </summary>
        public GameObject PlayEffect(string effectId, Vector3 position)
        {
            return PlayEffect(effectId, position, Quaternion.identity);
        }
        
        /// <summary>
        /// Spawn an effect at a position with rotation
        /// </summary>
        public GameObject PlayEffect(string effectId, Vector3 position, Quaternion rotation)
        {
            if (!effectLookup.TryGetValue(effectId, out var entry))
            {
                Debug.LogWarning($"[EffectManager] Effect not found: {effectId}");
                return null;
            }
            
            GameObject obj = GetFromPool(effectId, entry);
            if (obj == null) return null;
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            // Auto-return to pool after lifetime
            if (entry.lifetime > 0)
            {
                StartCoroutine(ReturnToPoolAfterDelay(effectId, obj, entry.lifetime));
            }
            
            return obj;
        }
        
        /// <summary>
        /// Spawn an effect attached to a transform
        /// </summary>
        public GameObject PlayEffectAttached(string effectId, Transform parent, Vector3 localOffset = default)
        {
            if (!effectLookup.TryGetValue(effectId, out var entry))
            {
                Debug.LogWarning($"[EffectManager] Effect not found: {effectId}");
                return null;
            }
            
            GameObject obj = GetFromPool(effectId, entry);
            if (obj == null) return null;
            
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localOffset;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            
            // Auto-return to pool after lifetime
            if (entry.lifetime > 0)
            {
                StartCoroutine(ReturnToPoolAfterDelay(effectId, obj, entry.lifetime));
            }
            
            return obj;
        }
        
        /// <summary>
        /// Manually return an effect to its pool
        /// </summary>
        public void ReturnToPool(string effectId, GameObject obj)
        {
            if (obj == null) return;
            
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            
            if (effectPools.ContainsKey(effectId))
            {
                effectPools[effectId].Enqueue(obj);
            }
        }
        
        // Dictionary to track active loop effects
        private Dictionary<string, GameObject> activeLoopEffects = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// Play a looping effect (stays active until manually stopped)
        /// </summary>
        public GameObject PlayLoopEffect(string effectId, Transform parent, Vector3 localOffset = default)
        {
            // If already playing this loop effect, return existing one
            if (activeLoopEffects.TryGetValue(effectId, out var existing) && existing != null && existing.activeInHierarchy)
            {
                return existing;
            }
            
            if (!effectLookup.TryGetValue(effectId, out var entry))
            {
                Debug.LogWarning($"[EffectManager] Loop Effect not found: {effectId}");
                return null;
            }
            
            GameObject obj = GetFromPool(effectId, entry);
            if (obj == null) return null;
            
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localOffset;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            
            // Track this loop effect (don't auto-return)
            activeLoopEffects[effectId] = obj;
            
            return obj;
        }
        
        /// <summary>
        /// Stop a looping effect and return it to pool
        /// </summary>
        public void StopLoopEffect(string effectId)
        {
            if (activeLoopEffects.TryGetValue(effectId, out var obj) && obj != null)
            {
                ReturnToPool(effectId, obj);
                activeLoopEffects.Remove(effectId);
            }
        }
        
        private GameObject GetFromPool(string effectId, EffectLibrary.EffectEntry entry)
        {
            if (effectPools[effectId].Count > 0)
            {
                return effectPools[effectId].Dequeue();
            }
            
            // Pool empty, instantiate new one
            GameObject obj = Instantiate(entry.prefab, poolParent);
            return obj;
        }
        
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(string effectId, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(effectId, obj);
        }
    }
}
