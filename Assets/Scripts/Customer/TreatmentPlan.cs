using System.Collections.Generic;
using UnityEngine;

namespace HairRemovalSim.Customer
{
    /// <summary>
    /// Defines realistic treatment plans for the hair removal salon
    /// </summary>
    public enum TreatmentPlan
    {
        None = -1,          // No plan selected (default)
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
                case TreatmentPlan.None: return "None";
                case TreatmentPlan.UpperArms: return "Upper Arms";
                case TreatmentPlan.LowerArms: return "Lower Arms";
                case TreatmentPlan.FullArms: return "Full Arms";
                case TreatmentPlan.Calves: return "Calves";
                case TreatmentPlan.Thighs: return "Thighs";
                case TreatmentPlan.FullLegs: return "Full Legs";
                case TreatmentPlan.Chest: return "Chest";
                case TreatmentPlan.Abs: return "Abs";
                case TreatmentPlan.Beard: return "Beard";
                case TreatmentPlan.Back: return "Back";
                case TreatmentPlan.Armpits: return "Armpits";
                case TreatmentPlan.ArmsAndLegs: return "Arms & Legs";
                case TreatmentPlan.FullBody: return "Full Body";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Get list of body part names (as they appear on GameObjects) for a treatment plan
        /// </summary>
        public static List<string> GetBodyPartNames(this TreatmentPlan plan)
        {
            switch (plan)
            {
                case TreatmentPlan.None:
                    return new List<string>();
                    
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
        
        /// <summary>
        /// Get mask values for shader highlighting from BodyPartsDatabase
        /// Returns up to 12 mask values (0.0-1.0)
        /// </summary>
        public static float[] GetMaskValues(this TreatmentPlan plan, Core.BodyPartsDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("[TreatmentPlan] BodyPartsDatabase is null!");
                return new float[0];
            }
            
            var partNames = plan.GetBodyPartNames();
            var maskValues = new List<float>();
            
            foreach (var partName in partNames)
            {
                var partDef = database.GetPartByName(partName);
                if (partDef != null)
                {
                    maskValues.Add(partDef.maskValue);
                }
                else
                {
                    Debug.LogWarning($"[TreatmentPlan] Body part '{partName}' not found in database");
                }
            }
            
            return maskValues.ToArray();
        }
        
        /// <summary>
        /// Get BodyPartDefinition list for completion tracking (includes UV regions)
        /// </summary>
        public static List<Core.BodyPartDefinition> GetBodyPartDefinitions(this TreatmentPlan plan, Core.BodyPartsDatabase database)
        {
            var result = new List<Core.BodyPartDefinition>();
            if (database == null) return result;
            
            var partNames = plan.GetBodyPartNames();
            foreach (var partName in partNames)
            {
                var partDef = database.GetPartByName(partName);
                if (partDef != null)
                {
                    result.Add(partDef);
                }
            }
            
            return result;
        }
    }
}
