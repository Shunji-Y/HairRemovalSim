using System.Collections.Generic;

namespace HairRemovalSim.Customer
{
    /// <summary>
    /// Defines realistic treatment plans for the hair removal salon
    /// </summary>
    public enum TreatmentPlan
    {
        UpperArms,          // 0: Left + Right Upper Arms
        LowerArms,          // 1: Left + Right Lower Arms
        FullArms,           // 2: All 4 arm parts
        Calves,             // 3: Left + Right Calves
        Thighs,             // 4: Left + Right Thighs
        FullLegs,           // 5: All 4 leg parts
        Chest,              // 6: Chest only
        Abs,                // 7: Abs only
        Beard,              // 8: Beard only
        Back,               // 9: Back only
        Armpits,            // 10: Left + Right Armpits
        ArmsAndLegs,        // 11: Full arms + Full legs
        FullBody            // 12: All body parts
    }

    /// <summary>
    /// Helper class for treatment plan operations
    /// </summary>
    public static class TreatmentPlanExtensions
    {
        /// <summary>
        /// Get display name for a treatment plan
        /// </summary>
        public static string GetDisplayName(this TreatmentPlan plan)
        {
            switch (plan)
            {
                case TreatmentPlan.UpperArms: return "上腕脱毛";
                case TreatmentPlan.LowerArms: return "下腕脱毛";
                case TreatmentPlan.FullArms: return "腕全体脱毛";
                case TreatmentPlan.Calves: return "ふくらはぎ脱毛";
                case TreatmentPlan.Thighs: return "太もも脱毛";
                case TreatmentPlan.FullLegs: return "脚全体脱毛";
                case TreatmentPlan.Chest: return "胸脱毛";
                case TreatmentPlan.Abs: return "お腹脱毛";
                case TreatmentPlan.Beard: return "ひげ脱毛";
                case TreatmentPlan.Back: return "背中脱毛";
                case TreatmentPlan.Armpits: return "わき脱毛";
                case TreatmentPlan.ArmsAndLegs: return "腕・脚脱毛";
                case TreatmentPlan.FullBody: return "全身脱毛";
                default: return "不明";
            }
        }

        /// <summary>
        /// Get list of body part names (as they appear on GameObjects) for a treatment plan
        /// </summary>
        public static List<string> GetBodyPartNames(this TreatmentPlan plan)
        {
            switch (plan)
            {
                case TreatmentPlan.UpperArms:
                    return new List<string> { "LeftUpperArm", "RightUpperArm" };
                
                case TreatmentPlan.LowerArms:
                    return new List<string> { "LeftLowerArm", "RightLowerArm" };
                
                case TreatmentPlan.FullArms:
                    return new List<string> { "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm" };
                
                case TreatmentPlan.Calves:
                    return new List<string> { "RightCalf", "LeftCalf" };
                
                case TreatmentPlan.Thighs:
                    return new List<string> { "RightThigh", "LeftThigh" };
                
                case TreatmentPlan.FullLegs:
                    return new List<string> { "RightCalf", "LeftCalf", "RightThigh", "LeftThigh" };
                
                case TreatmentPlan.Chest:
                    return new List<string> { "Chest" };
                
                case TreatmentPlan.Abs:
                    return new List<string> { "Abs" };
                
                case TreatmentPlan.Beard:
                    return new List<string> { "Beard" };
                
                case TreatmentPlan.Back:
                    return new List<string> { "Back" };
                
                case TreatmentPlan.Armpits:
                    return new List<string> { "RightArmpit", "LeftArmpit" };
                
                case TreatmentPlan.ArmsAndLegs:
                    return new List<string> 
                    { 
                        "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm",
                        "RightCalf", "LeftCalf", "RightThigh", "LeftThigh" 
                    };
                
                case TreatmentPlan.FullBody:
                    return new List<string> 
                    { 
                        "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm",
                        "RightCalf", "LeftCalf", "RightThigh", "LeftThigh",
                        "Chest", "Abs", "Beard", "Back", "RightArmpit", "LeftArmpit"
                    };
                
                default:
                    return new List<string>();
            }
        }
    }
}
