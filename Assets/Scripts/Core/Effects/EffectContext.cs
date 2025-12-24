using UnityEngine;
using HairRemovalSim.Customer;

namespace HairRemovalSim.Core.Effects
{
    /// <summary>
    /// Context data passed to effects when applying.
    /// Contains references and modifiable values.
    /// </summary>
    public class EffectContext
    {
        // ==========================================
        // 【参照】References
        // ==========================================
        
        /// <summary>Customer receiving the effect</summary>
        public CustomerController Customer { get; set; }
        
        /// <summary>Item data being used</summary>
        public ItemData SourceItem { get; set; }
        
        // ==========================================
        // 【施術用】Treatment Effects
        // ==========================================
        
        /// <summary>Current pain level (0-100)</summary>
        public float CurrentPainLevel { get; set; }
        
        /// <summary>Pain reduction to apply (subtracted from current pain)</summary>
        public float PainReduction { get; set; }
        
        // ==========================================
        // 【受付用】Reception Effects
        // ==========================================
        
        /// <summary>Pain increase rate multiplier (1.0 = normal, 0.5 = 50% reduction)</summary>
        public float PainRateMultiplier { get; set; } = 1f;
        
        /// <summary>Number of times customer can endure 100% pain</summary>
        public int PainEnduranceCount { get; set; }
        
        /// <summary>If true, skip pain judgment</summary>
        public bool IgnorePainJudgment { get; set; }
        
        /// <summary>If true, disable customer pain animation</summary>
        public bool DisablePainAnimation { get; set; }
        
        // ==========================================
        // 【レジ横/設置用】Register/Placement Effects
        // ==========================================
        
        /// <summary>Attraction boost to apply (fixed points)</summary>
        public float AttractionBoost { get; set; }
        
        /// <summary>Attraction boost percentage (0.10 = 10% boost)</summary>
        public float AttractionPercentBoost { get; set; }
        
        /// <summary>Next day attraction boost</summary>
        public float NextDayAttractionBoost { get; set; }
        
        /// <summary>Review value boost (fixed)</summary>
        public int ReviewBoost { get; set; }
        
        /// <summary>Review percentage boost (0.05 = 5% boost per customer)</summary>
        public float ReviewPercentBoost { get; set; }
        
        // ==========================================
        // Factory Methods
        // ==========================================
        
        /// <summary>Create context for treatment item use</summary>
        public static EffectContext CreateForTreatment(CustomerController customer, float currentPain)
        {
            return new EffectContext
            {
                Customer = customer,
                CurrentPainLevel = currentPain
            };
        }
        
        /// <summary>Create context for reception item use</summary>
        public static EffectContext CreateForReception(CustomerController customer)
        {
            return new EffectContext
            {
                Customer = customer
            };
        }
        
        /// <summary>Create context for register/placement item use</summary>
        public static EffectContext CreateForRegister()
        {
            return new EffectContext();
        }
    }
}
