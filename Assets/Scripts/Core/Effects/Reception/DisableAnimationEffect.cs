using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Disables customer pain animation during laser treatment.
    /// </summary>
    [CreateAssetMenu(fileName = "DisableAnimation", menuName = "HairRemovalSim/Effects/Reception/Disable Animation")]
    public class DisableAnimationEffect : EffectData
    {
        public override void Apply(EffectContext ctx)
        {
            ctx.DisablePainAnimation = true;
            Debug.Log("[DisableAnimationEffect] Pain animation disabled");
        }
        
        public override string GetDescription()
        {
            return "レーザー照射時のアニメーション無効";
        }
    }
}
