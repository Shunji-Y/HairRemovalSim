using UnityEngine;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Boosts customer review by a percentage.
    /// E.g., 5% boost means each customer's review is increased by 5%.
    /// </summary>
    [CreateAssetMenu(fileName = "ReviewPercentBoost", menuName = "HairRemovalSim/Effects/Placement/Review Percent Boost")]
    public class ReviewBoostEffect : EffectData
    {
        [Header("【レビュー値%アップ】")]
        [Tooltip("Review boost percentage (0.05 = 5% boost per customer)")]
        [Range(0f, 1f)]
        public float boostPercent = 0.05f;
        
        public override void Apply(EffectContext ctx)
        {
            ctx.ReviewPercentBoost += boostPercent;
            Debug.Log($"[ReviewBoostEffect] Review percent boost +{boostPercent:P0}");
        }
        
        public override string GetDescription()
        {
            return $"お客のレビュー+{boostPercent:P0}";
        }
    }
}
