using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Roomba effect - automatically cleans all hair debris at the start of a new day.
    /// Applied as a placement item effect.
    /// </summary>
    [CreateAssetMenu(fileName = "AutoClean", menuName = "HairRemovalSim/Effects/Placement/Auto Clean (Roomba)")]
    public class AutoCleanEffect : EffectData
    {
        [Header("【自動掃除】")]
        [Tooltip("If true, all debris is cleaned when new day starts")]
        public bool enableAutoClean = true;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.AutoCleanEnabled = true;
            Debug.Log("[AutoCleanEffect] Roomba auto-clean enabled");
        }
        
        public override string GetDescription()
        {
            return "翌日開始時に床の毛を自動掃除";
        }
    }
}
