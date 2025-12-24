using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Base class for all item effects.
    /// Create sub-classes for each effect type.
    /// </summary>
    public abstract class EffectData : ScriptableObject
    {
        [Header("【効果情報】")]
        [Tooltip("Effect description for display")]
        [TextArea(2, 4)]
        public string description;
        
        /// <summary>
        /// Apply this effect to the given context.
        /// </summary>
        public abstract void Apply(EffectContext ctx);
        
        /// <summary>
        /// Check if this effect can be applied.
        /// </summary>
        public virtual bool CanApply(EffectContext ctx)
        {
            return true;
        }
        
        /// <summary>
        /// Get effect description for UI display.
        /// </summary>
        public virtual string GetDescription()
        {
            return description;
        }
    }
}
