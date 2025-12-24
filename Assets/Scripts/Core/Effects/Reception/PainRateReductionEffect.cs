using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Reduces pain increase rate for reception items (麻酔クリーム等).
    /// Applied at reception, affects entire treatment session.
    /// </summary>
    [CreateAssetMenu(fileName = "PainRateReduction", menuName = "HairRemovalSim/Effects/Reception/Pain Rate Reduction")]
    public class PainRateReductionEffect : EffectData
    {
        [Header("【痛み上昇度減少】")]
        [Tooltip("Multiplier for pain increase (0.5 = 50% reduction)")]
        [Range(0f, 1f)]
        public float rateMultiplier = 0.5f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.PainRateMultiplier *= rateMultiplier;
            Debug.Log($"[PainRateReductionEffect] Pain rate multiplier = {ctx.PainRateMultiplier}");
        }
        
        public override string GetDescription()
        {
            float reductionPercent = (1f - rateMultiplier) * 100f;
            return $"痛み上昇度{reductionPercent}%減少";
        }
    }
}
