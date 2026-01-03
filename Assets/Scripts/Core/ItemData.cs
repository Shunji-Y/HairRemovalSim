using UnityEngine;
using HairRemovalSim.Tools;
using HairRemovalSim.Core.Effects;
using System.Collections.Generic;

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
        
        [Tooltip("Icon used in all UI (equipped, store, inventory, shelf)")]
        public Sprite icon;
        
        [Tooltip("Prefab to spawn for this item")]
        public GameObject prefab;
        
        [Tooltip("Localization key for item name and description (name: nameKey, desc: nameKey.desc)")]
        public string nameKey;
        
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
        
        [Tooltip("If true, this item cannot be sold in ToolShopPanel")]
        public bool cantSell = false;
        
        // ==========================================
        // 【レジ】Checkout Settings
        // ==========================================
        [Header("【レジ】")]
        [Tooltip("Review bonus when used at checkout (positive = good)")]
        public int reviewBonus = 0;
        
        [Tooltip("Upsell price added to total (0 = free review compensation item)")]
        public int upsellPrice = 0;
        
        // ==========================================
        // 【特殊効果】Special Effects
        // ==========================================
        [Header("【特殊効果】")]
        [Tooltip("List of special effects (pain reduction, attraction boost, etc.)")]
        public List<EffectData> effects = new List<EffectData>();
        
        // ==========================================
        // 【設置アイテム】Placement Item Settings
        // ==========================================
        [Header("【設置アイテム】")]
        [Tooltip("Fixed placement slot ID for this item (e.g., 'air_purifier_slot')")]
        public string placementSlotId;
        
        [Tooltip("Prefab to spawn when placed")]
        public GameObject placementPrefab;

        // ==========================================
        // 【ストア・購入】Store / Purchase Settings
        // ==========================================
        [Header("【ストア・購入】")]
        [Tooltip("Purchase price")]
        public int price = 100;
        
        [Tooltip("Maximum quantity per store order")]
        public int maxPurchasePerOrder = 1;
        
        [Tooltip("Is this item available in the store?")]
        public bool availableInStore = true;
        
        [Tooltip("Required shop grade to unlock this item (1-10)")]
        [Range(1, 10)]
        public int requiredShopGrade = 1;
        
        [Tooltip("Required star level to unlock this item (1-30, takes priority over grade)")]
        [Range(1, 30)]
        public int requiredStarLevel = 1;
        
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
        
        // Category-based placement helpers (backward compatibility)
        public bool CanPlaceOnShelf => category == ItemCategory.Shelf || category == ItemCategory.Consumable;
        public bool CanPlaceAtReception => category == ItemCategory.Reception;
        public bool CanUseAtCheckout => category == ItemCategory.Checkout;
        
        /// <summary>
        /// Get localized name using nameKey, fallback to ScriptableObject name
        /// </summary>
        public string GetLocalizedName()
        {
            if (string.IsNullOrEmpty(nameKey)) return name;
            
            var L = LocalizationManager.Instance;
            if (L == null) return name;
            
            string localized = L.Get(nameKey);
            // If localization returns the key itself (not found), use SO name
            return (!string.IsNullOrEmpty(localized) && !localized.StartsWith("[")) ? localized : name;
        }
        
        /// <summary>
        /// Get localized description using nameKey.desc, fallback to empty
        /// </summary>
        public string GetLocalizedDescription()
        {
            if (string.IsNullOrEmpty(nameKey)) return "";
            
            var L = LocalizationManager.Instance;
            if (L == null) return "";
            
            string localized = L.Get(nameKey + ".desc");
            return (!string.IsNullOrEmpty(localized) && !localized.StartsWith("[")) ? localized : "";
        }
        

        
        /// <summary>
        /// Check if this item is unlocked for the given shop grade (legacy)
        /// </summary>
        public bool IsUnlockedForGrade(int shopGrade)
        {
            return shopGrade >= requiredShopGrade;
        }
        
        /// <summary>
        /// Check if this item is unlocked for the given star level
        /// </summary>
        public bool IsUnlockedForStarLevel(int starLevel)
        {
            return starLevel >= requiredStarLevel;
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
        Vacuum,     // 掃除機
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
        PlacementItem,  // 設置アイテム（観葉植物、空気清浄機等）
        Furniture,      // 家具
        Upgrade,        // アップグレード
        Useful,         // 便利アイテム（配達プラン、会員証等）
        Shelf,          // 棚に配置可能（Treatment Shelf用）
        Reception,      // 受付に配置可能
        Checkout,       // レジで使用可能
        Other           // その他
    }
}
