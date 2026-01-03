using UnityEngine;
using UnityEditor;
using HairRemovalSim.Core;
using HairRemovalSim.Staff;
using System.Collections.Generic;
using System.IO;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Editor script to generate 30 CustomerRankData ScriptableObjects from spreadsheet data
    /// </summary>
    public class CustomerRankDataGenerator : EditorWindow
    {
        [MenuItem("HairRemovalSim/Generate Customer Rank Data")]
        public static void GenerateCustomerRankData()
        {
            string folderPath = "Assets/ScriptableObjects/Customer";
            
            // Create folder if needed
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                {
                    AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                }
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Customer");
            }
            
            // Define all 30 ranks based on spreadsheet
            var rankDataList = new List<RankDefinition>
            {
                // Poorest (1-6) - Grade 1
                new RankDefinition { star = 1, tier = CustomerTier.Poorest, sub = 1, grade = 1, price = 30, budgetMin = 15, budgetMax = 23, staff = null, plan = "脇" },
                new RankDefinition { star = 2, tier = CustomerTier.Poorest, sub = 2, grade = 1, price = 30, budgetMin = 23, budgetMax = 30, staff = null, plan = "" },
                new RankDefinition { star = 3, tier = CustomerTier.Poorest, sub = 3, grade = 1, price = 40, budgetMin = 30, budgetMax = 37, staff = null, plan = "脇、胸" },
                new RankDefinition { star = 4, tier = CustomerTier.Poorest, sub = 4, grade = 2, price = 40, budgetMin = 37, budgetMax = 45, staff = StaffRank.College, plan = "" },
                new RankDefinition { star = 5, tier = CustomerTier.Poorest, sub = 5, grade = 2, price = 50, budgetMin = 45, budgetMax = 52, staff = null, plan = "脇、胸、腹" },
                new RankDefinition { star = 6, tier = CustomerTier.Poorest, sub = 6, grade = 2, price = 50, budgetMin = 52, budgetMax = 60, staff = null, plan = "" },
                
                // Poor (1-6) - Grade 2
                new RankDefinition { star = 7, tier = CustomerTier.Poor, sub = 1, grade = 2, price = 80, budgetMin = 60, budgetMax = 70, staff = null, plan = "脇、胸、腹、背中" },
                new RankDefinition { star = 8, tier = CustomerTier.Poor, sub = 2, grade = 2, price = 80, budgetMin = 70, budgetMax = 80, staff = StaffRank.NewGrad, plan = "胸、腹、背中" },
                new RankDefinition { star = 9, tier = CustomerTier.Poor, sub = 3, grade = 3, price = 80, budgetMin = 80, budgetMax = 90, staff = null, plan = "腹、背中" },
                new RankDefinition { star = 10, tier = CustomerTier.Poor, sub = 4, grade = 3, price = 70, budgetMin = 90, budgetMax = 100, staff = null, plan = "腹、背中、ひげ" },
                new RankDefinition { star = 11, tier = CustomerTier.Poor, sub = 5, grade = 3, price = 70, budgetMin = 100, budgetMax = 110, staff = null, plan = "背中、ひげ" },
                new RankDefinition { star = 12, tier = CustomerTier.Poor, sub = 6, grade = 3, price = 70, budgetMin = 110, budgetMax = 120, staff = null, plan = "ひげ" },
                
                // Normal (1-6) - Grade 4
                new RankDefinition { star = 13, tier = CustomerTier.Normal, sub = 1, grade = 4, price = 100, budgetMin = 120, budgetMax = 130, staff = null, plan = "ひげ、腕" },
                new RankDefinition { star = 14, tier = CustomerTier.Normal, sub = 2, grade = 4, price = 100, budgetMin = 130, budgetMax = 140, staff = StaffRank.MidCareer, plan = "腕" },
                new RankDefinition { star = 15, tier = CustomerTier.Normal, sub = 3, grade = 4, price = 120, budgetMin = 140, budgetMax = 150, staff = null, plan = "ひげ、腕、脚" },
                new RankDefinition { star = 16, tier = CustomerTier.Normal, sub = 4, grade = 4, price = 120, budgetMin = 150, budgetMax = 160, staff = null, plan = "脚" },
                new RankDefinition { star = 17, tier = CustomerTier.Normal, sub = 5, grade = 4, price = 135, budgetMin = 160, budgetMax = 180, staff = null, plan = "腕、脚、ひげ（高品質）" },
                new RankDefinition { star = 18, tier = CustomerTier.Normal, sub = 6, grade = 4, price = 135, budgetMin = 180, budgetMax = 210, staff = null, plan = "腕、脚、ひげ（高品質）" },
                
                // Rich (1-6) - Grade 5
                new RankDefinition { star = 19, tier = CustomerTier.Rich, sub = 1, grade = 5, price = 240, budgetMin = 210, budgetMax = 240, staff = StaffRank.Veteran, plan = "ひげ（高品質）、腕&脚" },
                new RankDefinition { star = 20, tier = CustomerTier.Rich, sub = 2, grade = 5, price = 240, budgetMin = 240, budgetMax = 270, staff = null, plan = "腕&脚" },
                new RankDefinition { star = 21, tier = CustomerTier.Rich, sub = 3, grade = 5, price = 200, budgetMin = 270, budgetMax = 300, staff = null, plan = "ひげ（高品質）、胸&腹&背中" },
                new RankDefinition { star = 22, tier = CustomerTier.Rich, sub = 4, grade = 5, price = 200, budgetMin = 300, budgetMax = 330, staff = null, plan = "胸&腹&背中" },
                new RankDefinition { star = 23, tier = CustomerTier.Rich, sub = 5, grade = 5, price = 320, budgetMin = 330, budgetMax = 350, staff = null, plan = "胸&腹&背中、上半身" },
                new RankDefinition { star = 24, tier = CustomerTier.Rich, sub = 6, grade = 5, price = 320, budgetMin = 350, budgetMax = 410, staff = null, plan = "ひげ（高品質）、上半身" },
                
                // Richest (1-6) - Grade 6
                new RankDefinition { star = 25, tier = CustomerTier.Richest, sub = 1, grade = 6, price = 470, budgetMin = 410, budgetMax = 470, staff = StaffRank.Professional, plan = "ひげ（高品質）、上半身、全身ひげなし" },
                new RankDefinition { star = 26, tier = CustomerTier.Richest, sub = 2, grade = 6, price = 470, budgetMin = 470, budgetMax = 520, staff = null, plan = "上半身、全身ひげなし" },
                new RankDefinition { star = 27, tier = CustomerTier.Richest, sub = 3, grade = 6, price = 470, budgetMin = 520, budgetMax = 580, staff = null, plan = "全身ひげなし" },
                new RankDefinition { star = 28, tier = CustomerTier.Richest, sub = 4, grade = 6, price = 560, budgetMin = 580, budgetMax = 630, staff = null, plan = "上半身、全身ひげなし、全身ひげあり" },
                new RankDefinition { star = 29, tier = CustomerTier.Richest, sub = 5, grade = 6, price = 560, budgetMin = 630, budgetMax = 690, staff = null, plan = "全身ひげなし、全身ひげあり" },
                new RankDefinition { star = 30, tier = CustomerTier.Richest, sub = 6, grade = 6, price = 560, budgetMin = 690, budgetMax = 750, staff = null, plan = "全身ひげあり" },
            };
            
            int created = 0;
            foreach (var def in rankDataList)
            {
                string tierName = def.tier.ToString();
                string assetName = $"CustomerRank_{tierName}{def.sub}";
                string assetPath = $"{folderPath}/{assetName}.asset";
                
                // Create or update asset
                CustomerRankData data = AssetDatabase.LoadAssetAtPath<CustomerRankData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CustomerRankData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }
                
                // Set values
                data.rankName = $"{tierName}{def.sub}";
                data.tier = def.tier;
                data.subLevel = def.sub;
                data.requiredStarLevel = def.star;
                data.requiredGrade = def.grade;
                data.planPrice = def.price;
                data.budgetMin = def.budgetMin;
                data.budgetMax = def.budgetMax;
                data.requiredStaffRank = def.staff;
                data.planDescriptionKey = def.plan;
                
                EditorUtility.SetDirty(data);
                created++;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[CustomerRankDataGenerator] Created/updated {created} CustomerRankData assets in {folderPath}");
            EditorUtility.DisplayDialog("Complete", $"Created {created} CustomerRankData assets!", "OK");
        }
        
        private class RankDefinition
        {
            public int star;
            public CustomerTier tier;
            public int sub;
            public int grade;
            public int price;
            public int budgetMin;
            public int budgetMax;
            public StaffRank? staff;
            public string plan;
        }
    }
}
