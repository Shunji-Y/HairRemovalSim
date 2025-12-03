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
        public float patience; // 0.0 - 1.0
        public int baseBudget;

        [Header("Request")]
        public BodyPart requestedBodyPart; // Enum: Leg, Face, Chest, Arm
        


        public CustomerData()
        {
            // Random generation logic can go here or in a factory
        }
    }

    public enum SkinTone { Pale, Fair, Medium, Dark, Black }
    public enum HairinessLevel { Low, Medium, High, Yeti }
    public enum WealthLevel { Poor, Average, Rich, Tycoon }
    public enum BodyPart { Leg, Face, Chest, Arm }
}
