using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Pain reduction effect for treatment items (冷却ジェル等).
    /// Immediately reduces pain gauge by a fixed amount.
    /// </summary>
    [CreateAssetMenu(fileName = "PainReduction", menuName = "HairRemovalSim/Effects/Treatment/Pain Reduction")]
    public class PainReductionEffect : EffectData
    {
        [Header("【痛みゲージ減少】")]
        [Tooltip("Amount to reduce pain gauge (fixed points)")]
        public float reductionAmount = 20f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.PainReduction += reductionAmount;
            Debug.Log($"[PainReductionEffect] Pain reduction +{reductionAmount}");
        }
        
        public override string GetDescription()
        {
            return $"痛みゲージ{reductionAmount}減少";
        }
    }
}
