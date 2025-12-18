using UnityEngine;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Editor helper to create Staff ScriptableObjects and prefab
    /// </summary>
    public class StaffSetupHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [ContextMenu("Create Default Staff Rank Data Assets")]
        public void CreateDefaultRankAssets()
        {
            string basePath = "Assets/Data/Staff/Ranks/";
            
            // Ensure directory exists
            if (!System.IO.Directory.Exists(Application.dataPath + "/Data/Staff/Ranks"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Data/Staff/Ranks");
            }
            
            CreateRankAsset(basePath, StaffRank.College, "大学生", "staff_rank_college", 20f, 30f, 5, 2.5f, 100);
            CreateRankAsset(basePath, StaffRank.NewGrad, "新卒社員", "staff_rank_newgrad", 15f, 45f, 10, 3.0f, 150);
            CreateRankAsset(basePath, StaffRank.MidCareer, "中堅社員", "staff_rank_midcareer", 12f, 60f, 15, 3.5f, 200);
            CreateRankAsset(basePath, StaffRank.Veteran, "ベテラン", "staff_rank_veteran", 9f, 80f, 20, 4.0f, 300);
            CreateRankAsset(basePath, StaffRank.Professional, "プロフェッショナル", "staff_rank_pro", 5f, 100f, 30, 4.5f, 500);
            
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("[StaffSetupHelper] Created default staff rank assets");
        }
        
        private void CreateRankAsset(string basePath, StaffRank rank, string displayName, string nameKey,
            float treatmentTime, float itemUsage, int slots, float avgStars, int salary)
        {
            var asset = ScriptableObject.CreateInstance<StaffRankData>();
            asset.rank = rank;
            asset.displayName = displayName;
            asset.nameKey = nameKey;
            asset.treatmentTimePerPart = treatmentTime;
            asset.itemUsageProbability = itemUsage;
            asset.itemSlotCount = slots;
            asset.averageReviewStars = avgStars;
            asset.reviewStarVariance = 0.5f;
            asset.dailySalary = salary;
            
            string assetPath = basePath + "StaffRank_" + rank.ToString() + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
        }
        
        [ContextMenu("Create Sample Staff Profile")]
        public void CreateSampleProfile()
        {
            string basePath = "Assets/Data/Staff/Profiles/";
            
            // Ensure directory exists
            if (!System.IO.Directory.Exists(Application.dataPath + "/Data/Staff/Profiles"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Data/Staff/Profiles");
            }
            
            // Find college rank data
            var rankAssets = UnityEditor.AssetDatabase.FindAssets("StaffRank_College");
            StaffRankData collegeRank = null;
            if (rankAssets.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(rankAssets[0]);
                collegeRank = UnityEditor.AssetDatabase.LoadAssetAtPath<StaffRankData>(path);
            }
            
            var profile = ScriptableObject.CreateInstance<StaffProfileData>();
            profile.staffId = "staff_sample_01";
            profile.staffName = "テスト太郎";
            profile.rankData = collegeRank;
            profile.speedModifier = 1.0f;
            profile.friendlinessModifier = 0f;
            
            UnityEditor.AssetDatabase.CreateAsset(profile, basePath + "StaffProfile_Sample.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log("[StaffSetupHelper] Created sample staff profile");
        }
        
        [ContextMenu("Create Staff Prefab (Capsule)")]
        public void CreateStaffPrefab()
        {
            string prefabPath = "Assets/Prefabs/Staff/";
            
            // Ensure directory exists
            if (!System.IO.Directory.Exists(Application.dataPath + "/Prefabs/Staff"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs/Staff");
            }
            
            // Create capsule
            GameObject staffObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            staffObj.name = "StaffPrefab";
            
            // Set material color to distinguish from customers
            var renderer = staffObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.6f, 1.0f); // Blue color for staff
                renderer.material = mat;
            }
            
            // Add NavMeshAgent
            var agent = staffObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            
            // Add StaffController
            staffObj.AddComponent<StaffController>();
            
            // Create prefab
            GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(staffObj, prefabPath + "StaffPrefab.prefab");
            
            // Destroy temp object
            DestroyImmediate(staffObj);
            
            Debug.Log($"[StaffSetupHelper] Created staff prefab at {prefabPath}");
        }
#endif
    }
}
