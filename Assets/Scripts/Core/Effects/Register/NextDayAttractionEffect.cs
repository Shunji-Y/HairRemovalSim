using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Boosts attraction level for the next day only.
    /// Applied at day start, removed at day end.
    /// </summary>
    [CreateAssetMenu(fileName = "NextDayAttraction", menuName = "HairRemovalSim/Effects/Register/Next Day Attraction")]
    public class NextDayAttractionEffect : EffectData
    {
        [Header("【次の日の集客度アップ】")]
        [Tooltip("Next day attraction boost amount")]
        public float boostAmount = 1f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.NextDayAttractionBoost += boostAmount;
            Debug.Log($"[NextDayAttractionEffect] Next day attraction boost +{boostAmount}");
        }
        
        public override string GetDescription()
        {
            return $"次の日の集客度が{boostAmount}アップ";
        }
    }
}
