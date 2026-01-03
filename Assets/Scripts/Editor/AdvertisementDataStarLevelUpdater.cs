using UnityEngine;
using UnityEditor;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Editor script to update AdvertisementData requiredStarLevel based on spreadsheet
    /// </summary>
    public class AdvertisementDataStarLevelUpdater : EditorWindow
    {
        [MenuItem("HairRemovalSim/Update Advertisement Star Levels")]
        public static void UpdateAdvertisementStarLevels()
        {
            // Ad ID -> requiredStarLevel mapping from spreadsheet
            var starLevelMap = new Dictionary<string, int>
            {
                { "FreeSNS", 1 },           // 無料SNS運用
                { "Flyer", 4 },             // チラシ配り
                { "PaidSNS", 7 },           // SNS有料広告
                { "Magazine", 10 },         // 雑誌広告
                { "TrainAd", 14 },          // 中刷り
                { "TVCommercial", 17 },     // テレビCM
                { "BillboardAd", 20 },      // 巨大ビル広告
                { "VideoAd", 24 },          // 動画配信サイト広告
                { "Influencer", 27 },       // インフルエンサーマーケティング
            };
            
            // Find all AdvertisementData assets
            string[] guids = AssetDatabase.FindAssets("t:AdvertisementData");
            int updated = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AdvertisementData ad = AssetDatabase.LoadAssetAtPath<AdvertisementData>(path);
                
                if (ad != null && starLevelMap.TryGetValue(ad.adId, out int starLevel))
                {
                    if (ad.requiredStarLevel != starLevel)
                    {
                        ad.requiredStarLevel = starLevel;
                        EditorUtility.SetDirty(ad);
                        Debug.Log($"[AdStarLevelUpdater] Updated {ad.adId}: requiredStarLevel = {starLevel}");
                        updated++;
                    }
                }
            }
            
            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log($"[AdStarLevelUpdater] Updated {updated} AdvertisementData assets");
            EditorUtility.DisplayDialog("Complete", $"Updated {updated} AdvertisementData requiredStarLevel values!", "OK");
        }
    }
}
