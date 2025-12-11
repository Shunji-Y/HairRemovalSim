using UnityEngine;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// ScriptableObject containing data for a store item.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoreItem", menuName = "HairRemovalSim/Store Item")]
    public class StoreItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName = "New Item";
        [TextArea(2, 4)]
        public string description = "Item description";
        public Sprite icon;
        
        [Header("Pricing")]
        public int price = 100;
        
        [Header("Stock")]
        public int maxPurchasePerOrder = 99; // Max quantity per purchase
        
        /// <summary>
        /// Get formatted price string
        /// </summary>
        public string GetPriceString()
        {
            return $"¥{price:N0}";
        }
    }
}
