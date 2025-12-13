using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Customer
{
    /// <summary>
    /// Treatment machine types
    /// </summary>
    public enum TreatmentMachine
    {
        Shaver,
        Laser
        // Wax, Gel to be added later
    }
    
    /// <summary>
    /// Price entry for a body part
    /// </summary>
    [System.Serializable]
    public class BodyPartPrice
    {
        public TreatmentBodyPart bodyPart;
        public int shaverPrice = 15;
        public int laserPrice = 40;
    }
    
    /// <summary>
    /// ScriptableObject containing treatment price table
    /// </summary>
    [CreateAssetMenu(fileName = "TreatmentPriceTable", menuName = "HairRemovalSim/Treatment Price Table")]
    public class TreatmentPriceTable : ScriptableObject
    {
        [Header("Body Part Prices")]
        public List<BodyPartPrice> bodyPartPrices = new List<BodyPartPrice>()
        {
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Armpits, shaverPrice = 15, laserPrice = 40 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Arms, shaverPrice = 15, laserPrice = 80 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Chest, shaverPrice = 15, laserPrice = 90 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Abs, shaverPrice = 15, laserPrice = 90 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Beard, shaverPrice = 15, laserPrice = 100 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Legs, shaverPrice = 15, laserPrice = 120 },
            new BodyPartPrice { bodyPart = TreatmentBodyPart.Back, shaverPrice = 15, laserPrice = 130 }
        };
        
        [Header("Additional Costs")]
        public int anesthesiaCreamCost = 10;
        
        [Header("Review Penalties (Adjustable)")]
        [Tooltip("Penalty when selected parts don't match customer's plan")]
        public int partMismatchPenalty = -20;
        
        [Tooltip("Penalty when price slightly exceeds budget")]
        public int budgetExcessPenalty = -15;
        
        [Tooltip("Penalty when using wrong tool during treatment")]
        public int toolMismatchPenalty = -10;
        
        [Tooltip("Penalty when price exceeds 50% of budget")]
        public int severeBudgetExcessPenalty = -50;
        
        [Tooltip("If total penalty reaches this, customer leaves")]
        public int leaveThreshold = -50;

        /// <summary>
        /// Calculate total price for selected body parts
        /// </summary>
        public int CalculatePrice(TreatmentBodyPart selectedParts, TreatmentMachine machine, bool useAnesthesia)
        {
            int total = 0;
            
            foreach (var priceEntry in bodyPartPrices)
            {
                // Check if this body part is selected
                if ((selectedParts & priceEntry.bodyPart) != 0)
                {
                    total += machine == TreatmentMachine.Laser ? priceEntry.laserPrice : priceEntry.shaverPrice;
                }
            }
            
            if (useAnesthesia)
            {
                total += anesthesiaCreamCost;
            }
            
            return total;
        }
        
        /// <summary>
        /// Calculate review penalty based on selection vs customer request
        /// </summary>
        public int CalculatePenalty(TreatmentBodyPart selectedParts, CustomerRequestPlan plan, int price, int budget)
        {
            int penalty = 0;
            
            // Check part mismatch (missing or extra parts)
            TreatmentBodyPart missing = CustomerPlanHelper.GetMissingParts(selectedParts, plan);
            TreatmentBodyPart extra = CustomerPlanHelper.GetExtraParts(selectedParts, plan);
            
            if (missing != TreatmentBodyPart.None || extra != TreatmentBodyPart.None)
            {
                penalty += partMismatchPenalty;
            }
            
            // Check budget excess
            if (price > budget)
            {
                float excessRatio = (float)(price - budget) / budget;
                
                if (excessRatio >= 0.5f)
                {
                    // Severe excess (50%+)
                    penalty += severeBudgetExcessPenalty;
                }
                else
                {
                    penalty += budgetExcessPenalty;
                }
            }
            
            return penalty;
        }
        
        /// <summary>
        /// Check if customer should leave based on penalty
        /// </summary>
        public bool ShouldCustomerLeave(int totalPenalty)
        {
            return totalPenalty <= leaveThreshold;
        }
    }
}
