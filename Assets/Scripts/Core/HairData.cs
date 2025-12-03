using UnityEngine;

namespace HairRemovalSim.Core
{
    public enum HairType
    {
        Sparse,  // 少なめ
        Normal,  // 普通
        Thick    // 剛毛
    }

    public enum HairState
    {
        Hidden,     // 皮膚の下（まだ生えてない、または埋没）
        Visible,    // 表面に出ている
        Treated,    // 処理済み（毛穴だけ残る状態など）
        Removed     // 完全に除去された
    }

    [System.Serializable]
    public class Hair
    {
        public string id;
        public HairType type;
        public HairState state;
        
        [Range(0, 1)]
        public float length; // 0 to 1 (1 = full length)
        
        [Range(0, 1)]
        public float health; // 1 = healthy, 0 = broken/cut
        
        public Vector3 localPosition; // Position on the body part surface
        public Vector3 localNormal;   // Growth direction

        public Hair(HairType type, Vector3 pos, Vector3 normal)
        {
            this.id = System.Guid.NewGuid().ToString();
            this.type = type;
            this.state = HairState.Visible;
            this.length = 1.0f;
            this.health = 1.0f;
            this.localPosition = pos;
            this.localNormal = normal;
        }
    }
}
