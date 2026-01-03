using UnityEngine;
using UnityEditor;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Editor script to update ItemData requiredStarLevel based on spreadsheet
    /// </summary>
    public class ItemDataStarLevelUpdater : EditorWindow
    {
        [MenuItem("HairRemovalSim/Update Item Star Levels")]
        public static void UpdateItemStarLevels()
        {
            // Item ID -> requiredStarLevel mapping from spreadsheet
            var starLevelMap = new Dictionary<string, int>
            {
                // ツール - Treatment Tools
                { "SmoothRayFace", 1 },
                { "RustyShaver", 1 },
                { "SmoothRayBody", 3 },
                { "TheShaver", 6 },
                { "SmoothRayBodyPro", 7 },
                { "SmoothRayFacePro", 9 },
                { "PremiumShaver", 11 },
                { "SmoothRayBodyProMax", 13 },
                { "SmoothRayFaceProMax", 16 },
                { "SmoothRayBodyUltra", 19 },
                { "SmoothRayFaceUltra", 21 },
                { "ContinuousLaser", 23 },
                { "NuSmoothRay", 24 },
                { "νSmoothRay", 24 },
                { "SmoothRayOmega", 27 },
                { "SmoothRayω", 27 },
                
                // 受付アイテム - Reception Items
                { "NambingCreamC", 1 },
                { "NambingCreamB", 7 },
                { "ServiceTicket", 4 },
                { "StressBall", 10 },
                { "VIPServiceTicket", 14 },
                { "LaughingGas", 17 },
                { "NambingCreamA", 20 },
                { "NambingCreamaA", 20 },
                { "SensitiveGel", 23 },
                { "PlatinumServiceTicket", 26 },
                { "RelaxAroma", 28 },
                
                // レジアイテム - Checkout Items
                { "AfterCareSet", 1 },
                { "Candy", 1 },
                { "StampCard", 2 },
                { "MoistureLotion", 3 },
                { "MoistureCream", 4 },
                { "Coupon", 5 },
                { "MoistureMask", 7 },
                { "Towell", 8 },
                { "Serum", 11 },
                { "BronzeGift", 12 },
                { "PremiumCream", 13 },
                { "PremiumLotion", 15 },
                { "VIPStamp", 17 },
                { "PlatinumSet", 19 },
                { "SilverGift", 20 },
                { "PlatinumStamp", 22 },
                { "GoldGift", 25 },
                { "LuxurySet", 26 },
                
                // 施術アイテム - Treatment Consumables
                { "CoolingGelC", 1 },
                { "CoolingGelB", 6 },
                { "IcePack", 9 },
                { "CoolingGelA", 14 },
                
                // Placement/Useful Items
                { "WaterServer", 3 },
                { "AirCleaner", 5 },
                { "RobotCleaner", 8 },
                { "ARKit", 13 },
                { "MagazineRack", 17 },
                { "CoffeeMachine", 22 },
                { "AromaDiffuser", 26 },
            };
            
            // Find all ItemData assets
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            int updated = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                
                if (item != null && starLevelMap.TryGetValue(item.itemId, out int starLevel))
                {
                    if (item.requiredStarLevel != starLevel)
                    {
                        item.requiredStarLevel = starLevel;
                        EditorUtility.SetDirty(item);
                        Debug.Log($"[ItemDataStarLevelUpdater] Updated {item.itemId}: requiredStarLevel = {starLevel}");
                        updated++;
                    }
                }
            }
            
            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log($"[ItemDataStarLevelUpdater] Updated {updated} ItemData assets");
            EditorUtility.DisplayDialog("Complete", $"Updated {updated} ItemData requiredStarLevel values!", "OK");
        }
    }
}
