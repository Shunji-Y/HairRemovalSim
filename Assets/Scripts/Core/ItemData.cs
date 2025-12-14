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
        
        // ==========================================
        // 【施術用ツール】Treatment Tool Settings
        // ==========================================
        [Header("【施術用ツール】")]
        [Tooltip("Which hand holds this item")]
        public ToolBase.HandType handType = ToolBase.HandType.RightHand;
        
        [Tooltip("Position offset when held in hand")]
        public Vector3 handPosition = Vector3.zero;
        
        [Tooltip("Rotation offset when held in hand (Euler angles)")]
        public Vector3 handRotation = Vector3.zero;
        
        [Tooltip("Maximum durability (0 = infinite)")]
        public float maxDurability = 0f;
        
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
        // 【その他】Store / Other Settings
        // ==========================================
        [Header("【その他】")]
        [Tooltip("Description shown in store UI")]
        [TextArea(2, 4)]
        public string storeDescription;
        
        [Tooltip("Purchase price")]
        public int price = 100;
        
        [Tooltip("Maximum quantity per store order")]
        public int maxPurchasePerOrder = 1;
        
        [Tooltip("Is this item available in the store?")]
        public bool availableInStore = true;
        
        [Tooltip("Category for sorting/filtering")]
        public ItemCategory category = ItemCategory.Tool;
        
        // Helper properties
        public bool IsHoldable => handType != ToolBase.HandType.None;
        public bool HasDurability => maxDurability > 0f;
    }
    
    public enum ItemCategory
    {
        Tool,           // 施術用ツール
        Consumable,     // 消耗品（ジェル等）
        Furniture,      // 家具
        Upgrade,        // アップグレード
        Other           // その他
    }
}
