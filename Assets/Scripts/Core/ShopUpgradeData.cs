using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Asset data for each shop upgrade level.
    /// Contains only asset-safe references (cost, images, localization).
    /// Scene object references are handled by ShopUpgradeConfig component.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopUpgrade_Grade", menuName = "HairRemovalSim/Shop Upgrade Data")]
    public class ShopUpgradeData : ScriptableObject
    {
        [Header("Grade Info")]
        [Tooltip("The grade this upgrade unlocks (2-6)")]
        [Range(1, 7)]
        public int targetGrade = 2;
        
        [Header("Cost")]
        [Tooltip("Cost to upgrade to this grade")]
        public int upgradeCost = 10000;
        
        [Header("Visuals")]
        [Tooltip("Preview image of the upgraded shop")]
        public Sprite previewImage;
        
        [Header("Localization")]
        [Tooltip("Localization key for grade name (e.g., upgrade.grade2.name)")]
        public string nameKey;
        
        [Header("Benefits")]
        [Tooltip("List of benefit localization keys (displayed as speech bubbles)")]
        public List<string> benefitKeys = new List<string>();
        
        /// <summary>
        /// Get benefit count
        /// </summary>
        public int BenefitCount => benefitKeys?.Count ?? 0;
    }
}
