using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Allows customer to endure 100% pain multiple times.
    /// Also allows laser use during 60% cooldown.
    /// </summary>
    [CreateAssetMenu(fileName = "PainEndurance", menuName = "HairRemovalSim/Effects/Reception/Pain Endurance")]
    public class PainEnduranceEffect : EffectData
    {
        [Header("【痛み耐久】")]
        [Tooltip("Number of times customer can endure 100% pain")]
        public int enduranceCount = 3;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.PainEnduranceCount += enduranceCount;
            Debug.Log($"[PainEnduranceEffect] Pain endurance +{enduranceCount}");
        }
        
        public override string GetDescription()
        {
            return $"痛み100%で{enduranceCount}回まで耐える";
        }
    }
}
