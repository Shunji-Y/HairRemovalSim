using UnityEngine;
using System;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Data class for an active advertisement
    /// </summary>
    [System.Serializable]
    public class ActiveAdvertisement
    {
        public string adId;
        public int startDay;
        public int endDay;
        
        public ActiveAdvertisement(string id, int start, int duration)
        {
            adId = id;
            startDay = start;
            endDay = start + duration;
        }
        
        public bool IsExpired(int currentDay) => currentDay >= endDay;
        public int GetDaysSinceStart(int currentDay) => currentDay - startDay;
    }
    
    /// <summary>
    /// Manages active advertisements and calculates their combined effects
    /// </summary>
    public class AdvertisingManager : Singleton<AdvertisingManager>
    {
        [Header("Settings")]
        [Tooltip("Maximum number of active ads allowed")]
        [SerializeField] private int maxActiveAds = 3;
        
        [Header("Available Ads")]
        [SerializeField] private List<AdvertisementData> availableAds = new List<AdvertisementData>();
        
        // Active advertisements
        private List<ActiveAdvertisement> activeAds = new List<ActiveAdvertisement>();
        
        // Track once-per-day ads
        private Dictionary<string, int> lastUsedDay = new Dictionary<string, int>();
        
        // Events
        public event Action OnAdsUpdated;
        
        // Properties
        public int MaxActiveAds => maxActiveAds;
        public int ActiveAdCount => activeAds.Count;
        public IReadOnlyList<AdvertisementData> AvailableAds => availableAds;
        public IReadOnlyList<ActiveAdvertisement> ActiveAds => activeAds;
        
        /// <summary>
        /// Check if an ad is currently active
        /// </summary>
        public bool IsAdActive(string adId)
        {
            return activeAds.Exists(a => a.adId == adId);
        }
        
        /// <summary>
        /// Check if an advertisement can be started
        /// Returns false with reason string for UI display
        /// </summary>
        public bool CanStartAd(AdvertisementData adData, out string reason)
        {
            reason = "";
            if (adData == null)
            {
                reason = "INVALID";
                return false;
            }
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            int currentStarLevel = ShopManager.Instance?.StarRating ?? 1;
            
            // Check if unlocked
            if (!adData.IsUnlockedForStarLevel(currentStarLevel))
            {
                reason = LocalizationManager.Instance?.Get("advertising.locked") ?? "LOCKED";
                return false;
            }
            
            // Check if already active
            if (IsAdActive(adData.adId))
            {
                reason = LocalizationManager.Instance?.Get("advertising.now_active") ?? "NOW ACTIVE";
                return false;
            }
            
            // Check max active ads
            CleanupExpiredAds();
            if (activeAds.Count >= maxActiveAds)
            {
                reason = LocalizationManager.Instance?.Get("advertising.max_reached") ?? "MAX ADS";
                return false;
            }
            
            // Check once-per-day restriction
            if (adData.oncePerDay)
            {
                if (lastUsedDay.TryGetValue(adData.adId, out int lastDay) && lastDay == currentDay)
                {
                    reason = LocalizationManager.Instance?.Get("advertising.used_today") ?? "USED TODAY";
                    return false;
                }
            }
            
            // Check cooldown
            if (adData.cooldownDays > 0)
            {
                if (lastUsedDay.TryGetValue(adData.adId, out int lastDay))
                {
                    int daysSinceLast = currentDay - lastDay;
                    if (daysSinceLast < adData.cooldownDays)
                    {
                        int daysLeft = adData.cooldownDays - daysSinceLast;
                        reason = LocalizationManager.Instance?.Get("advertising.cooldown", daysLeft) ?? $"CD: {daysLeft}d";
                        return false;
                    }
                }
            }
            
            // Check money
            if (adData.cost > 0)
            {
                int currentMoney = EconomyManager.Instance?.CurrentMoney ?? 0;
                if (currentMoney < adData.cost)
                {
                    reason = LocalizationManager.Instance?.Get("advertising.no_money") ?? "NO FUNDS";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get attraction rate boost from active ads only
        /// </summary>
        public float GetAttractionBoost()
        {
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            float total = 0f;
            
            foreach (var activeAd in activeAds)
            {
                var adData = GetAdData(activeAd.adId);
                if (adData != null && !activeAd.IsExpired(currentDay))
                {
                    int daysSinceStart = activeAd.GetDaysSinceStart(currentDay);
                    total += adData.GetAttractionBoostForDay(daysSinceStart);
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Get VIP coefficient boost from active ads only
        /// </summary>
        public float GetVipBoost()
        {
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            float total = 0f;
            
            foreach (var activeAd in activeAds)
            {
                var adData = GetAdData(activeAd.adId);
                if (adData != null && !activeAd.IsExpired(currentDay))
                {
                    int daysSinceStart = activeAd.GetDaysSinceStart(currentDay);
                    total += adData.GetVipBoostForDay(daysSinceStart);
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Try to start an advertisement
        /// </summary>
        public bool StartAdvertisement(AdvertisementData adData)
        {
            if (adData == null) return false;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            int currentStarLevel = ShopManager.Instance?.StarRating ?? 1;
            
            // Check if unlocked
            if (!adData.IsUnlockedForStarLevel(currentStarLevel))
            {
                Debug.Log($"[AdvertisingManager] Ad '{adData.displayName}' requires star level {adData.requiredStarLevel}");
                return false;
            }
            
            // Check max active ads
            CleanupExpiredAds();
            if (activeAds.Count >= maxActiveAds)
            {
                Debug.Log($"[AdvertisingManager] Max active ads reached ({maxActiveAds})");
                return false;
            }
            
            // Check if this ad is already active (same ad can only run once at a time)
            if (IsAdActive(adData.adId))
            {
                Debug.Log($"[AdvertisingManager] Ad '{adData.displayName}' is already running");
                return false;
            }
            
            // Check once-per-day restriction
            if (adData.oncePerDay)
            {
                if (lastUsedDay.TryGetValue(adData.adId, out int lastDay) && lastDay == currentDay)
                {
                    Debug.Log($"[AdvertisingManager] Ad '{adData.displayName}' already used today");
                    return false;
                }
            }
            
            // Check cooldown
            if (adData.cooldownDays > 0)
            {
                if (lastUsedDay.TryGetValue(adData.adId, out int lastDay))
                {
                    int daysSinceLast = currentDay - lastDay;
                    if (daysSinceLast < adData.cooldownDays)
                    {
                        Debug.Log($"[AdvertisingManager] Ad '{adData.displayName}' on cooldown ({adData.cooldownDays - daysSinceLast} days left)");
                        return false;
                    }
                }
            }
            
            // Check and spend money
            if (adData.cost > 0)
            {
                if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendMoney(adData.cost))
                {
                    Debug.Log($"[AdvertisingManager] Not enough money for '{adData.displayName}'");
                    return false;
                }
            }
            
            // Determine effective start day based on when ad is purchased
            // - During Preparation or Day: starts today
            // - During Night (after business hours): starts tomorrow
            int effectiveStartDay = currentDay;
            if (GameManager.Instance?.CurrentState == GameManager.GameState.Night)
            {
                effectiveStartDay = currentDay + 1;
                Debug.Log($"[AdvertisingManager] Ad purchased after business hours - will start tomorrow (Day {effectiveStartDay})");
            }
            
            // Create active ad
            var activeAd = new ActiveAdvertisement(adData.adId, effectiveStartDay, adData.durationDays);
            activeAds.Add(activeAd);
            lastUsedDay[adData.adId] = currentDay;
            
            Debug.Log($"[AdvertisingManager] Started '{adData.displayName}' - Duration: {adData.durationDays}d, Attraction: +{adData.attractionBoost}, Start: Day {effectiveStartDay}");
            
            OnAdsUpdated?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Called at the start of each day to clean up expired ads
        /// </summary>
        public void ProcessDayStart()
        {
            CleanupExpiredAds();
            OnAdsUpdated?.Invoke();
        }
        
        private void CleanupExpiredAds()
        {
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            activeAds.RemoveAll(ad => ad.IsExpired(currentDay));
        }
        
        /// <summary>
        /// Get advertisement data by ID
        /// </summary>
        public AdvertisementData GetAdData(string adId)
        {
            return availableAds.Find(ad => ad.adId == adId);
        }
        
        
        /// <summary>
        /// Get remaining days for an active ad
        /// </summary>
        public int GetRemainingDays(string adId)
        {
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            var activeAd = activeAds.Find(ad => ad.adId == adId);
            if (activeAd == null) return 0;
            return Mathf.Max(0, activeAd.endDay - currentDay);
        }
    }
}
