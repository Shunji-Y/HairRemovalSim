using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// 体の部位定義（UV範囲ベース）
    /// ScriptableObjectとしてInspectorで編集可能
    /// </summary>
    [CreateAssetMenu(fileName = "BodyPartDefinition", menuName = "Hair Removal Sim/Body Part Definition")]
    public class BodyPartDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string partName = "LeftUpperArm";
        
        [Tooltip("どのマテリアルに属するか（0=Arm, 1=Leg, 2=Body, 3=Head）")]
        public int materialIndex = 0;
        
        [Tooltip("BodyPartMaskテクスチャのR値（0.0〜1.0）")]
        [Range(0f, 1f)]
        public float maskValue = 0.1f;
        
        [Header("UV Regions")]
        [Tooltip("この部位に対応するUV範囲のリスト。複数定義可能（例：背中が左右に分かれている場合）")]
        public List<UVRegion> uvRegions = new List<UVRegion>();
        
        [Header("Editor Preview")]
        [Tooltip("プレビュー背景に表示する画像（UVレイアウトやテクスチャ）。オプション。")]
        public Texture2D previewTexture;
        
        /// <summary>
        /// UV座標がこの部位に含まれるか判定
        /// </summary>
        public bool ContainsUV(Vector2 uv)
        {
            foreach (var region in uvRegions)
            {
                if (region.rect.Contains(uv))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// この部位の全UV領域の合計面積（0-1）
        /// </summary>
        public float GetTotalArea()
        {
            float totalArea = 0f;
            foreach (var region in uvRegions)
            {
                totalArea += region.rect.width * region.rect.height;
            }
            return totalArea;
        }
    }
    
    /// <summary>
    /// UV空間での矩形領域
    /// </summary>
    [System.Serializable]
    public class UVRegion
    {
        [Tooltip("UV範囲（X=U min, Y=V min, Width=U幅, Height=V幅）")]
        public Rect rect = new Rect(0, 0, 1, 1);
        
        [Tooltip("この領域の説明（例：「左側」「右側」）")]
        public string description = "";
        
        public UVRegion()
        {
            rect = new Rect(0, 0, 1, 1);
        }
        
        public UVRegion(float x, float y, float width, float height, string desc = "")
        {
            rect = new Rect(x, y, width, height);
            description = desc;
        }
    }
}
