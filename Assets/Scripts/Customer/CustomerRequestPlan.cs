namespace HairRemovalSim.Customer
{
    /// <summary>
    /// 12 treatment plans that customers can request
    /// </summary>
    public enum CustomerRequestPlan
    {
        Beard,              // ひげ
        Arms,               // 腕
        Legs,               // 脚
        ArmsAndLegs,        // 腕、脚
        Chest,              // 胸
        Stomach,            // お腹
        Back,               // 背中
        UpperBodyNoArms,    // 上半身(腕なし) = Chest + Stomach + Back
        UpperBody,          // 上半身 = Chest + Stomach + Back + Arms
        Underarms,          // わき
        FullBodyNoBeard,    // 全身脱毛(ひげなし)
        FullBodyWithBeard   // 全身脱毛(ひげあり)
    }
    
    /// <summary>
    /// 7 body parts that player can select to cover customer's plan
    /// </summary>
    [System.Flags]
    public enum TreatmentBodyPart
    {
        None = 0,
        Arms = 1 << 0,      // 腕
        Armpits = 1 << 1,   // わき
        Legs = 1 << 2,      // 脚
        Chest = 1 << 3,     // 胸
        Abs = 1 << 4,       // お腹
        Beard = 1 << 5,     // ひげ
        Back = 1 << 6       // 背中
    }
    
    /// <summary>
    /// Helper class to map customer plans to required body parts
    /// </summary>
    public static class CustomerPlanHelper
    {
        /// <summary>
        /// Get the body parts required for a customer's plan
        /// </summary>
        public static TreatmentBodyPart GetRequiredParts(CustomerRequestPlan plan)
        {
            switch (plan)
            {
                case CustomerRequestPlan.Beard:
                    return TreatmentBodyPart.Beard;
                case CustomerRequestPlan.Arms:
                    return TreatmentBodyPart.Arms;
                case CustomerRequestPlan.Legs:
                    return TreatmentBodyPart.Legs;
                case CustomerRequestPlan.ArmsAndLegs:
                    return TreatmentBodyPart.Arms | TreatmentBodyPart.Legs;
                case CustomerRequestPlan.Chest:
                    return TreatmentBodyPart.Chest;
                case CustomerRequestPlan.Stomach:
                    return TreatmentBodyPart.Abs;
                case CustomerRequestPlan.Back:
                    return TreatmentBodyPart.Back;
                case CustomerRequestPlan.UpperBodyNoArms:
                    return TreatmentBodyPart.Chest | TreatmentBodyPart.Abs | TreatmentBodyPart.Back;
                case CustomerRequestPlan.UpperBody:
                    return TreatmentBodyPart.Chest | TreatmentBodyPart.Abs | TreatmentBodyPart.Back | TreatmentBodyPart.Arms;
                case CustomerRequestPlan.Underarms:
                    return TreatmentBodyPart.Armpits;
                case CustomerRequestPlan.FullBodyNoBeard:
                    return TreatmentBodyPart.Arms | TreatmentBodyPart.Armpits | TreatmentBodyPart.Legs | 
                           TreatmentBodyPart.Chest | TreatmentBodyPart.Abs | TreatmentBodyPart.Back;
                case CustomerRequestPlan.FullBodyWithBeard:
                    return TreatmentBodyPart.Arms | TreatmentBodyPart.Armpits | TreatmentBodyPart.Legs | 
                           TreatmentBodyPart.Chest | TreatmentBodyPart.Abs | TreatmentBodyPart.Back | TreatmentBodyPart.Beard;
                default:
                    return TreatmentBodyPart.None;
            }
        }
        
        /// <summary>
        /// Get display name for a plan
        /// </summary>
        public static string GetPlanDisplayName(CustomerRequestPlan plan)
        {
            switch (plan)
            {
                case CustomerRequestPlan.Beard: return "Beard";
                case CustomerRequestPlan.Arms: return "Arms";
                case CustomerRequestPlan.Legs: return "Legs";
                case CustomerRequestPlan.ArmsAndLegs: return "Arms & Legs";
                case CustomerRequestPlan.Chest: return "Chest";
                case CustomerRequestPlan.Stomach: return "Stomach";
                case CustomerRequestPlan.Back: return "Back";
                case CustomerRequestPlan.UpperBodyNoArms: return "Upper Body (No Arms)";
                case CustomerRequestPlan.UpperBody: return "Upper Body";
                case CustomerRequestPlan.Underarms: return "Underarms";
                case CustomerRequestPlan.FullBodyNoBeard: return "Full Body (No Beard)";
                case CustomerRequestPlan.FullBodyWithBeard: return "Full Body";
                default: return "Unknown";
            }
        }
        
        /// <summary>
        /// Check if player's selected parts cover the customer's requested plan
        /// </summary>
        public static bool DoesSelectionCoverPlan(TreatmentBodyPart selectedParts, CustomerRequestPlan plan)
        {
            TreatmentBodyPart required = GetRequiredParts(plan);
            return (selectedParts & required) == required;
        }
        
        /// <summary>
        /// Get parts that are selected but not required (extra parts)
        /// </summary>
        public static TreatmentBodyPart GetExtraParts(TreatmentBodyPart selectedParts, CustomerRequestPlan plan)
        {
            TreatmentBodyPart required = GetRequiredParts(plan);
            return selectedParts & ~required;
        }
        
        /// <summary>
        /// Get parts that are required but not selected (missing parts)
        /// </summary>
        public static TreatmentBodyPart GetMissingParts(TreatmentBodyPart selectedParts, CustomerRequestPlan plan)
        {
            TreatmentBodyPart required = GetRequiredParts(plan);
            return required & ~selectedParts;
        }
        
        /// <summary>
        /// Get display string of required parts for a plan (comma-separated)
        /// Example: "Chest, Abs, Back"
        /// </summary>
        public static string GetRequiredPartsDisplay(CustomerRequestPlan plan)
        {
            TreatmentBodyPart required = GetRequiredParts(plan);
            var parts = new System.Collections.Generic.List<string>();
            
            if ((required & TreatmentBodyPart.Arms) != 0) parts.Add("Arms");
            if ((required & TreatmentBodyPart.Armpits) != 0) parts.Add("Armpits");
            if ((required & TreatmentBodyPart.Legs) != 0) parts.Add("Legs");
            if ((required & TreatmentBodyPart.Chest) != 0) parts.Add("Chest");
            if ((required & TreatmentBodyPart.Abs) != 0) parts.Add("Abs");
            if ((required & TreatmentBodyPart.Beard) != 0) parts.Add("Beard");
            if ((required & TreatmentBodyPart.Back) != 0) parts.Add("Back");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }
        
        /// <summary>
        /// Map 7 reception parts to 14 detailed treatment parts
        /// </summary>
        public static string[] GetDetailedTreatmentParts(TreatmentBodyPart selectedParts)
        {
            var parts = new System.Collections.Generic.List<string>();
            
            if ((selectedParts & TreatmentBodyPart.Arms) != 0)
            {
                parts.Add("LeftUpperArm");
                parts.Add("LeftLowerArm");
                parts.Add("RightUpperArm");
                parts.Add("RightLowerArm");
            }
            
            if ((selectedParts & TreatmentBodyPart.Armpits) != 0)
            {
                parts.Add("LeftArmpit");
                parts.Add("RightArmpit");
            }
            
            if ((selectedParts & TreatmentBodyPart.Legs) != 0)
            {
                parts.Add("LeftThigh");
                parts.Add("LeftCalf");
                parts.Add("RightThigh");
                parts.Add("RightCalf");
            }
            
            if ((selectedParts & TreatmentBodyPart.Chest) != 0)
            {
                parts.Add("Chest");
            }
            
            if ((selectedParts & TreatmentBodyPart.Abs) != 0)
            {
                parts.Add("Abs");
            }
            
            if ((selectedParts & TreatmentBodyPart.Beard) != 0)
            {
                parts.Add("Beard");
            }
            
            if ((selectedParts & TreatmentBodyPart.Back) != 0)
            {
                parts.Add("Back");
            }
            
            return parts.ToArray();
        }
    }
}
