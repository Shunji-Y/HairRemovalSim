using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Helper class for applying effects from ItemData.
    /// </summary>
    public static class EffectHelper
    {
        /// <summary>
        /// Apply all effects from an item to the context.
        /// </summary>
        public static void ApplyEffects(ItemData item, EffectContext ctx)
        {
            if (item == null || item.effects == null || item.effects.Count == 0)
                return;
            
            foreach (var effect in item.effects)
            {
                if (effect != null && effect.CanApply(ctx))
                {
                    effect.Apply(ctx);
                }
            }
        }
        
        /// <summary>
        /// Get combined effect description for UI display.
        /// </summary>
        public static string GetEffectDescription(ItemData item)
        {
            if (item == null || item.effects == null || item.effects.Count == 0)
                return "";
            
            var sb = new System.Text.StringBuilder();
            foreach (var effect in item.effects)
            {
                if (effect != null)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append(effect.GetDescription());
                }
            }
            
            return sb.ToString();
        }
    }
}
