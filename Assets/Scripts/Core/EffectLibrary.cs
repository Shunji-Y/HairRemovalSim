using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// ScriptableObject that holds effect (particle/VFX) definitions.
    /// Create via Assets > Create > HairRemovalSim > Effect Library
    /// </summary>
    [CreateAssetMenu(fileName = "EffectLibrary", menuName = "HairRemovalSim/Effect Library")]
    public class EffectLibrary : ScriptableObject
    {
        [System.Serializable]
        public class EffectEntry
        {
            public string id;
            public GameObject prefab;
            [Tooltip("How many instances to pre-instantiate in the pool")]
            public int poolSize = 5;
            [Tooltip("Lifetime in seconds before auto-returning to pool. 0 = manual return only")]
            public float lifetime = 2f;
        }
        
        public EffectEntry[] effects;
        
        public EffectEntry GetEffect(string id)
        {
            if (effects == null) return null;
            
            foreach (var effect in effects)
            {
                if (effect.id == id) return effect;
            }
            return null;
        }
    }
}
