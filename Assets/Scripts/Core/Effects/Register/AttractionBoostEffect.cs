using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Boosts attraction level for register/placement items.
    /// Applied permanently or for the current day.
    /// </summary>
    [CreateAssetMenu(fileName = "AttractionBoost", menuName = "HairRemovalSim/Effects/Register/Attraction Boost")]
    public class AttractionBoostEffect : EffectData
    {
        [Header("【集客度アップ】")]
        [Tooltip("Attraction boost amount")]
        public float boostAmount = 0.1f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.AttractionBoost += boostAmount;
            Debug.Log($"[AttractionBoostEffect] Attraction boost +{boostAmount}");
        }
        
        public override string GetDescription()
        {
            return $"基本集客度が{boostAmount}アップ";
        }
    }
}
