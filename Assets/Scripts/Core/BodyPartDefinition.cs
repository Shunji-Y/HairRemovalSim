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
        
        // Bitmap cache for fast UV lookup (64x64 grid)
        private const int CACHE_SIZE = 64;
        private bool[] uvBitmapCache;
        private bool isCacheInitialized = false;
        
        /// <summary>
        /// Initialize the UV bitmap cache for fast lookup.
        /// Call this once before using ContainsUVCached().
        /// </summary>
        public void InitializeCache()
        {
            if (isCacheInitialized) return;
            
            uvBitmapCache = new bool[CACHE_SIZE * CACHE_SIZE];
            
            for (int y = 0; y < CACHE_SIZE; y++)
            {
                for (int x = 0; x < CACHE_SIZE; x++)
                {
                    Vector2 uv = new Vector2((float)x / CACHE_SIZE, (float)y / CACHE_SIZE);
                    bool contains = false;
                    
                    foreach (var region in uvRegions)
                    {
                        if (region.rect.Contains(uv))
                        {
                            contains = true;
                            break;
                        }
                    }
                    
                    uvBitmapCache[y * CACHE_SIZE + x] = contains;
                }
            }
            
            isCacheInitialized = true;
        }
        
        /// <summary>
        /// Fast cached UV lookup. Call InitializeCache() first.
        /// </summary>
        public bool ContainsUVCached(Vector2 uv)
        {
            if (!isCacheInitialized)
            {
                // Fallback to slow path if not initialized
                return ContainsUV(uv);
            }
            
            // Inline clamp to avoid Mathf.Clamp function call overhead
            int x = (int)(uv.x * CACHE_SIZE);
            int y = (int)(uv.y * CACHE_SIZE);
            x = x < 0 ? 0 : (x >= CACHE_SIZE ? CACHE_SIZE - 1 : x);
            y = y < 0 ? 0 : (y >= CACHE_SIZE ? CACHE_SIZE - 1 : y);
            return uvBitmapCache[y * CACHE_SIZE + x];
        }
        
        /// <summary>
        /// Fast cached UV lookup using pixel coordinates directly (avoids float-to-int conversion).
        /// </summary>
        public bool ContainsPixel(int pixelX, int pixelY, int textureSize)
        {
            if (!isCacheInitialized)
            {
                Vector2 uv = new Vector2((float)pixelX / textureSize, (float)pixelY / textureSize);
                return ContainsUV(uv);
            }
            
            // Map pixel coordinates to cache coordinates with inline clamp
            int cacheX = pixelX * CACHE_SIZE / textureSize;
            int cacheY = pixelY * CACHE_SIZE / textureSize;
            cacheX = cacheX < 0 ? 0 : (cacheX >= CACHE_SIZE ? CACHE_SIZE - 1 : cacheX);
            cacheY = cacheY < 0 ? 0 : (cacheY >= CACHE_SIZE ? CACHE_SIZE - 1 : cacheY);
            return uvBitmapCache[cacheY * CACHE_SIZE + cacheX];
        }
        
        /// <summary>
        /// Invalidate cache (call when uvRegions are modified at runtime)
        /// </summary>
        public void InvalidateCache()
        {
            isCacheInitialized = false;
            uvBitmapCache = null;
        }
        
        /// <summary>
        /// UV座標がこの部位に含まれるか判定（オリジナル - 低速だが正確）
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
