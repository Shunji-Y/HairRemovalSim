using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// 全ての体部位定義を管理するデータベース
    /// </summary>
    [CreateAssetMenu(fileName = "BodyPartsDatabase", menuName = "Hair Removal Sim/Body Parts Database")]
    public class BodyPartsDatabase : ScriptableObject
    {
        [Header("All Body Parts")]
        [Tooltip("全ての体部位定義のリスト")]
        public List<BodyPartDefinition> allParts = new List<BodyPartDefinition>();
        
        /// <summary>
        /// 名前で部位を検索
        /// </summary>
        public BodyPartDefinition GetPartByName(string partName)
        {
            return allParts.FirstOrDefault(p => p.partName == partName);
        }
        
        /// <summary>
        /// マスク値で部位を検索
        /// </summary>
        public BodyPartDefinition GetPartByMaskValue(float maskValue, float tolerance = 0.05f)
        {
            return allParts.FirstOrDefault(p => Mathf.Abs(p.maskValue - maskValue) < tolerance);
        }
        
        /// <summary>
        /// マテリアルインデックスで部位リストを取得
        /// </summary>
        public List<BodyPartDefinition> GetPartsByMaterial(int materialIndex)
        {
            return allParts.Where(p => p.materialIndex == materialIndex).ToList();
        }
        
        /// <summary>
        /// UV座標がどの部位に属するか検索
        /// </summary>
        public BodyPartDefinition GetPartByUV(Vector2 uv, int materialIndex)
        {
            var partsInMaterial = GetPartsByMaterial(materialIndex);
            return partsInMaterial.FirstOrDefault(p => p.ContainsUV(uv));
        }
    }
}
