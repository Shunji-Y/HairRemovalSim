using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Disables pain judgment for the customer.
    /// Pain animation will not play.
    /// </summary>
    [CreateAssetMenu(fileName = "NoPainJudgment", menuName = "HairRemovalSim/Effects/Reception/No Pain Judgment")]
    public class NoPainJudgmentEffect : EffectData
    {
        public override void Apply(EffectContext ctx)
        {
            ctx.IgnorePainJudgment = true;
            Debug.Log("[NoPainJudgmentEffect] Pain judgment disabled");
        }
        
        public override string GetDescription()
        {
            return "痛み判定なし";
        }
    }
}
