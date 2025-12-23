using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// ScriptableObject for advertisement configuration
    /// Each ad type has its own asset with adjustable parameters
    /// </summary>
    [CreateAssetMenu(fileName = "NewAdvertisement", menuName = "HairRemovalSim/Advertisement Data")]
    public class AdvertisementData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for this advertisement")]
        public string adId;
        
        [Tooltip("Display name (default, use nameKey for localization)")]
        public string displayName;
        
        [Tooltip("Localization key for name")]
        public string nameKey;
        
        [Tooltip("Description of the ad")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("Localization key for description")]
        public string descriptionKey;
        
        [Tooltip("Icon for UI display")]
        public Sprite icon;
        
        [Header("Cost & Requirements")]
        [Tooltip("Cost to run this advertisement")]
        public int cost = 0;
        
        [Tooltip("Required shop grade to unlock (1-5)")]
        [Range(1, 6)]
        public int requiredShopGrade = 1;
        
        [Header("Duration & Effect")]
        [Tooltip("How many days the ad lasts")]
        [Min(1)]
        public int durationDays = 1;
        
        [Tooltip("Initial attraction boost (points)")]
        public float attractionBoost = 5f;
        
        [Tooltip("Daily decay amount (points subtracted per day)")]
        public float decayPerDay = 0f;
        
        [Tooltip("VIP coefficient boost (0-100 scale)")]
        [Range(0f, 50f)]
        public float vipCoefficientBoost = 0f;
        
        [Header("Restrictions")]
        [Tooltip("Can only be used once per day")]
        public bool oncePerDay = false;
        
        [Tooltip("Cooldown in days before can use again (0 = no cooldown)")]
        [Min(0)]
        public int cooldownDays = 0;
        
        /// <summary>
        /// Check if this ad is unlocked for the given shop grade
        /// </summary>
        public bool IsUnlockedForGrade(int currentGrade)
        {
            return currentGrade >= requiredShopGrade;
        }
        
        /// <summary>
        /// Calculate the effect after decay for a given day
        /// Linear decay: attractionBoost - (decayPerDay * daysSinceStart)
        /// </summary>
        public float GetAttractionBoostForDay(int daysSinceStart)
        {
            if (daysSinceStart <= 0) return attractionBoost;
            if (daysSinceStart >= durationDays) return 0f;
            
            // Apply linear decay: boost - (decay * days)
            float remainingEffect = attractionBoost - (decayPerDay * daysSinceStart);
            return Mathf.Max(0f, remainingEffect);
        }
        
        /// <summary>
        /// Calculate VIP boost after decay
        /// Linear decay proportional to attraction decay
        /// </summary>
        public float GetVipBoostForDay(int daysSinceStart)
        {
            if (daysSinceStart <= 0) return vipCoefficientBoost;
            if (daysSinceStart >= durationDays) return 0f;
            
            // VIP boost decays at same rate ratio as attraction
            float decayRatio = attractionBoost > 0 ? (attractionBoost - (decayPerDay * daysSinceStart)) / attractionBoost : 0f;
            float remainingEffect = vipCoefficientBoost * Mathf.Max(0f, decayRatio);
            return Mathf.Max(0f, remainingEffect);
        }
    }
}
