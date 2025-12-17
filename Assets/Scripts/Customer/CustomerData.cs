using UnityEngine;

namespace HairRemovalSim.Customer
{
    [System.Serializable]
    public class CustomerData
    {
        public string customerName;
        
        [Header("Appearance")]
        public SkinTone skinTone; // Enum: Pale, Fair, Medium, Dark, Black
        public HairinessLevel hairiness; // Enum: Low, Medium, High, Yeti

        [Header("Stats")]
        public WealthLevel wealth; // Enum: Poor, Average, Rich, Tycoon
        public float painTolerance; // 0.0 - 1.0
        public PainToleranceLevel painToleranceLevel; // For UI display
        public float patience; // 0.0 - 1.0
        public int baseBudget; // Customer's total budget

        [Header("Request")]
        public CustomerRequestPlan requestPlan; // One of 12 plans
        public TreatmentPlan selectedTreatmentPlan = TreatmentPlan.None; // UV mask-based system
        public System.Collections.Generic.List<HairRemovalSim.Core.BodyPart> requestedBodyParts = new System.Collections.Generic.List<HairRemovalSim.Core.BodyPart>(); 
        
        [Header("Treatment State")]
        public TreatmentBodyPart confirmedParts = TreatmentBodyPart.None; // Parts confirmed at reception
        public TreatmentMachine confirmedMachine = TreatmentMachine.Shaver; // Machine confirmed at reception
        public bool useAnesthesiaCream = false;
        public int reviewPenalty = 0;
        public int confirmedPrice = 0; // Price confirmed at reception

        public CustomerData()
        {
            // Random generation logic can go here or in a factory
        }
        
        /// <summary>
        /// Calculate total price based on requested body parts
        /// Uses baseBudget as price per body part
        /// </summary>
        public int GetTotalPrice()
        {
            return requestedBodyParts.Count * baseBudget;
        }
        
        /// <summary>
        /// Get display name for customer's requested plan
        /// </summary>
        public string GetPlanDisplayName()
        {
            return CustomerPlanHelper.GetPlanDisplayName(requestPlan);
        }
        
        /// <summary>
        /// Get required body parts for customer's plan
        /// </summary>
        public TreatmentBodyPart GetRequiredParts()
        {
            return CustomerPlanHelper.GetRequiredParts(requestPlan);
        }
    }

    public enum SkinTone { Pale, Fair, Medium, Dark, Black }
    public enum HairinessLevel { Low, Medium, High, Yeti }
    public enum WealthLevel { Poorest, Poor, Normal, Rich, Richest }  // 極貧, 貧乏, 普通, 富豪, 大富豪
    public enum BodyPart { Leg, Face, Chest, Arm }
    public enum PainToleranceLevel { Low, Medium, High }
}
