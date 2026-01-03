using UnityEngine;
using UnityEditor;
using HairRemovalSim.Staff;
using System.Collections.Generic;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Editor script to update StaffRankData requiredStarLevel values
    /// </summary>
    public class StaffRankDataStarLevelUpdater : EditorWindow
    {
        [MenuItem("HairRemovalSim/Update Staff Star Levels")]
        public static void UpdateStaffStarLevels()
        {
            // StaffRank -> requiredStarLevel mapping
            var starLevelMap = new Dictionary<StaffRank, int>
            {
                { StaffRank.College, 4 },       // 大学生
                { StaffRank.NewGrad, 8 },       // 新卒
                { StaffRank.MidCareer, 14 },    // 中堅
                { StaffRank.Veteran, 19 },      // ベテラン
                { StaffRank.Professional, 25 }, // プロフェッショナル
            };
            
            // Find all StaffRankData assets
            string[] guids = AssetDatabase.FindAssets("t:StaffRankData");
            int updated = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StaffRankData data = AssetDatabase.LoadAssetAtPath<StaffRankData>(path);
                
                if (data != null && starLevelMap.TryGetValue(data.rank, out int starLevel))
                {
                    if (data.requiredStarLevel != starLevel)
                    {
                        data.requiredStarLevel = starLevel;
                        EditorUtility.SetDirty(data);
                        Debug.Log($"[StaffRankDataUpdater] Updated {data.rank}: requiredStarLevel = {starLevel}");
                        updated++;
                    }
                }
            }
            
            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log($"[StaffRankDataUpdater] Updated {updated} StaffRankData assets");
            EditorUtility.DisplayDialog("Complete", $"Updated {updated} StaffRankData requiredStarLevel values!", "OK");
        }
    }
}
