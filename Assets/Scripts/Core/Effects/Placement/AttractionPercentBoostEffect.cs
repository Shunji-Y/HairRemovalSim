using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Boosts attraction by a percentage of (base + ad boost).
    /// E.g., 10% boost means attraction is increased by 10%.
    /// </summary>
    [CreateAssetMenu(fileName = "AttractionPercentBoost", menuName = "HairRemovalSim/Effects/Placement/Attraction Percent Boost")]
    public class AttractionPercentBoostEffect : EffectData
    {
        [Header("【集客度%アップ】")]
        [Tooltip("Attraction boost percentage (0.10 = 10% boost)")]
        [Range(0f, 1f)]
        public float boostPercent = 0.10f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.AttractionPercentBoost += boostPercent;
            Debug.Log($"[AttractionPercentBoostEffect] Attraction percent boost +{boostPercent:P0}");
        }
        
        public override string GetDescription()
        {
            return $"集客度+{boostPercent:P0}";
        }
    }
}
