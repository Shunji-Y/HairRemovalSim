using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Scene-based configuration for shop upgrades.
    /// Place this component in the scene and assign scene objects directly.
    /// Links to ShopUpgradeData for meta information (cost, images, localization).
    /// </summary>
    public class ShopUpgradeConfig : MonoBehaviour
    {
        [Header("Data Reference")]
        [Tooltip("Reference to ShopUpgradeData asset for cost/meta info")]
        public ShopUpgradeData upgradeData;
        
        [Header("Scene References")]
        [Tooltip("Expansion room GameObject to activate when upgrading")]
        public GameObject expansionRoom;
        
        [Tooltip("Walls to hide when expanding")]
        public List<GameObject> wallsToHide = new List<GameObject>();
        
        [Tooltip("New beds to activate and add to shop")]
        public List<BedController> newBeds = new List<BedController>();
        
        [Tooltip("Shop model GameObject to activate after upgrade (legacy, optional)")]
        public GameObject shopModel;
        
        /// <summary>
        /// Target grade from linked data
        /// </summary>
        public int TargetGrade => upgradeData != null ? upgradeData.targetGrade : 0;
        
        /// <summary>
        /// Upgrade cost from linked data
        /// </summary>
        public int UpgradeCost => upgradeData != null ? upgradeData.upgradeCost : 0;
        
        /// <summary>
        /// Preview image from linked data
        /// </summary>
        public Sprite PreviewImage => upgradeData?.previewImage;
        
        /// <summary>
        /// Name localization key from linked data
        /// </summary>
        public string NameKey => upgradeData?.nameKey;
        
        /// <summary>
        /// Benefit keys from linked data
        /// </summary>
        public List<string> BenefitKeys => upgradeData?.benefitKeys;
        
        private void OnValidate()
        {
            // Auto-name based on grade for easier identification
            if (upgradeData != null)
            {
                gameObject.name = $"ShopUpgradeConfig_Grade{upgradeData.targetGrade}";
            }
        }
    }
}
