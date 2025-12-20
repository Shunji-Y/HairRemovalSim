using UnityEngine;
using HairRemovalSim.Tools;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Unified item data for all items in the game.
    /// Contains information for tools, consumables, store, shelf, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "HairRemovalSim/Item Data")]
    public class ItemData : ScriptableObject
    {
        // ==========================================
        // 【共通情報】Common Information
        // ==========================================
        [Header("【共通情報】")]
        [Tooltip("Unique identifier for this item")]
        public string itemId;
        
        [Tooltip("Display name shown in UI")]
        public string displayName;
        
        [Tooltip("Icon used in all UI (equipped, store, inventory, shelf)")]
        public Sprite icon;
        
        [Tooltip("Prefab to spawn for this item")]
        public GameObject prefab;
        
        [Tooltip("Description of the item")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("Localization key for item name (optional, uses displayName if empty)")]
        public string nameKey;
        
       // [Tooltip("Localization key for item description (optional, uses description if empty)")]
      //  public string descriptionKey;
        
        // ==========================================
        // 【施術用ツール】Treatment Tool Settings
        // ==========================================
        [Header("【施術用ツール - 基本】")]
        [Tooltip("Which hand holds this item")]
        public ToolBase.HandType handType = ToolBase.HandType.RightHand;
        
        [Tooltip("Position offset when held in hand")]
        public Vector3 handPosition = Vector3.zero;
        
        [Tooltip("Rotation offset when held in hand (Euler angles)")]
        public Vector3 handRotation = Vector3.zero;
        
        [Tooltip("Maximum durability (0 = infinite)")]
        public float maxDurability = 0f;
        
        // ==========================================
        // 【施術ツール性能】Treatment Tool Performance
        // ==========================================
        [Header("【施術用ツール - 性能】")]
        [Tooltip("Type of treatment tool")]
        public TreatmentToolType toolType = TreatmentToolType.None;

        [Tooltip("Intensity of the burn effect (redness) on skin after treatment. 0.0=none, 1.0=full")]
        [Range(0f, 1f)]
        public float burnIntensity = 1.0f;
        
        [Tooltip("Target body area for this tool")]
        public ToolTargetArea targetArea = ToolTargetArea.Body;
        
        [Tooltip("Scope - Effect range (0=narrow, 100=ultra wide)")]
        [Range(0, 100)]
        public int statScope = 50;
        
        [Tooltip("Pain - Pain level (0=painless, 100=extreme pain)")]
        [Range(0, 100)]
        public int statPain = 0;
        
        [Tooltip("Power - Hair removal effectiveness (0=weak, 100=max power)")]
        [Range(0, 100)]
        public int statPower = 50;
        
        [Tooltip("Speed - Fire rate (0=slow, 100=continuous)")]
        [Range(0, 100)]
        public int statSpeed = 50;
        
        [Tooltip("Does this tool require hair to be shaved first?")]
        public bool requiresShaving = false;
        
        [Tooltip("Maximum hair length this tool can handle (0=any length)")]
        [Range(0f, 1f)]
        public float maxHairLengthForUse = 0f;
        
        // ==========================================
        // 【消耗品】Consumable / Shelf Settings
        // ==========================================
        [Header("【消耗品】")]
        [Tooltip("Position offset when placed on shelf")]
        public Vector3 shelfPosition = Vector3.zero;
        
        [Tooltip("Rotation offset when placed on shelf (Euler angles)")]
        public Vector3 shelfRotation = Vector3.zero;
        
        [Tooltip("Scale when placed on shelf (use (1,1,1) to keep prefab scale)")]
        public Vector3 shelfScale = Vector3.one;
        
        [Tooltip("Maximum stack count on a single shelf slot")]
        [Range(1, 4)]
        public int maxStackOnShelf = 1;
        
        [Tooltip("Maximum stack count in a single warehouse slot")]
        [Range(1, 99)]
        public int maxWarehouseStack = 10;
        
        [Tooltip("Can this item be placed on treatment shelf from warehouse?")]
        public bool canPlaceOnShelf = true;
        
        [Tooltip("Can this item be placed in reception desk slots (EXTRA ITEMS)?")]
        public bool canPlaceAtReception = false;
        
        // ==========================================
        // 【レジ】Checkout Settings
        // ==========================================
        [Header("【レジ】")]
        [Tooltip("Can this item be used at checkout?")]
        public bool canUseAtCheckout = false;
        
        [Tooltip("Review bonus when used at checkout (positive = good)")]
        public int reviewBonus = 0;
        
        [Tooltip("Upsell price added to total (0 = free review compensation item)")]
        public int upsellPrice = 0;
        
        // ==========================================
        // 【ストア・購入】Store / Purchase Settings
        // ==========================================
        [Header("【ストア・購入】")]
        [Tooltip("Description shown in store UI")]
        [TextArea(2, 4)]
        public string storeDescription;
        
        [Tooltip("Purchase price")]
        public int price = 100;
        
        [Tooltip("Maximum quantity per store order")]
        public int maxPurchasePerOrder = 1;
        
        [Tooltip("Is this item available in the store?")]
        public bool availableInStore = true;
        
        [Tooltip("Required shop grade to unlock this item (1-10)")]
        [Range(1, 10)]
        public int requiredShopGrade = 1;
        
        [Tooltip("Category for sorting/filtering")]
        public ItemCategory category = ItemCategory.Tool;
        
        // ==========================================
        // Helper Properties
        // ==========================================
        public bool IsHoldable => handType != ToolBase.HandType.None;
        public bool HasDurability => maxDurability > 0f;
        public bool IsTreatmentTool => toolType != TreatmentToolType.None;
        public bool IsShaver => toolType == TreatmentToolType.Shaver;
        public bool IsLaser => toolType == TreatmentToolType.Laser;
        
        /// <summary>
        /// Check if this tool can be used on hair of given length
        /// </summary>
        public bool CanUseOnHairLength(float hairLength)
        {
            // If maxHairLengthForUse is 0, can use on any length
            if (maxHairLengthForUse <= 0f) return true;
            return hairLength <= maxHairLengthForUse;
        }
        
        /// <summary>
        /// Check if this item is unlocked for the given shop grade
        /// </summary>
        public bool IsUnlockedForGrade(int shopGrade)
        {
            return shopGrade >= requiredShopGrade;
        }
        
        /// <summary>
        /// Check if this tool can treat the specified body part based on targetArea
        /// Face: Beard, Armpits only
        /// Body: Everything except Beard, Armpits
        /// All: Everything
        /// </summary>
        public bool CanTreatBodyPart(string bodyPartName)
        {
            if (string.IsNullOrEmpty(bodyPartName)) return false;
            
            // Determine if body part is "Face" category (Beard, Armpits)
            bool isFacePart = bodyPartName.Contains("Beard") || 
                              bodyPartName.Contains("Armpit");
            
            switch (targetArea)
            {
                case ToolTargetArea.Face:
                    return isFacePart;
                case ToolTargetArea.Body:
                    return !isFacePart;
                case ToolTargetArea.All:
                    return true;
                default:
                    return true;
            }
        }
    }
    
    /// <summary>
    /// Type of treatment tool
    /// </summary>
    public enum TreatmentToolType
    {
        None,       // Not a treatment tool
        Shaver,     // シェーバー
        Laser,      // レーザー
        Other       // その他
    }
    
    /// <summary>
    /// Target body area for treatment tools
    /// </summary>
    public enum ToolTargetArea
    {
        Body,       // 体用
        Face,       // ひげ用/顔用
        All         // 全身対応
    }
    
    public enum ItemCategory
    {
        Tool,           // 施術用ツール（旧式の分類）
        TreatmentTool,  // 施術用ツール（レーザー、シェーバー等）
        Consumable,     // 消耗品（ジェル等）
        Furniture,      // 家具
        Upgrade,        // アップグレード
        Other           // その他
    }
}
