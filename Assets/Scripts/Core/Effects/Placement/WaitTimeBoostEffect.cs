using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Boosts customer wait time by a percentage.
    /// E.g., 20% boost means customers can wait 20% longer before getting angry.
    /// </summary>
    [CreateAssetMenu(fileName = "WaitTimeBoost", menuName = "HairRemovalSim/Effects/Placement/Wait Time Boost")]
    public class WaitTimeBoostEffect : EffectData
    {
        [Header("【待ち時間%アップ】")]
        [Tooltip("Wait time boost percentage (0.20 = +20% wait time)")]
        [Range(0f, 1f)]
        public float boostPercent = 0.20f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.WaitTimePercentBoost += boostPercent;
            Debug.Log($"[WaitTimeBoostEffect] Wait time percent boost +{boostPercent:P0}");
        }
        
        public override string GetDescription()
        {
            return $"お客の待ち時間+{boostPercent:P0}";
        }
    }
}
