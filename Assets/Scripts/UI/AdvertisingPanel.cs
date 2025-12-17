using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// PC Panel for managing advertisements
    /// Shows available ads, active ads, and current effects
    /// </summary>
    public class AdvertisingPanel : MonoBehaviour
    {
        public static AdvertisingPanel Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text activeAdsCountText;
        
        [Header("Current Effects")]
        [SerializeField] private TMP_Text attractionRateText;
        [SerializeField] private TMP_Text baseAttractionText;      // 基本集客率 (黄)
        [SerializeField] private TMP_Text adBoostText;             // 広告効果 (緑)
        [SerializeField] private TMP_Text vipCoefficientText;
        [SerializeField] private TMP_Text baseVipText;             // 基本VIP
        [SerializeField] private TMP_Text adVipBoostText;          // 広告VIPブースト
        [SerializeField] private Slider attractionSlider;
        [SerializeField] private Slider vipSlider;
        
        [Header("Available Ads")]
        [SerializeField] private Transform availableAdsContainer;
        [SerializeField] private GameObject adCardPrefab;
        
        [Header("Active Ads")]
        [SerializeField] private Transform activeAdsContainer;
        [SerializeField] private GameObject activeAdCardPrefab;
        
        private List<GameObject> adCards = new List<GameObject>();
        private List<GameObject> activeAdCards = new List<GameObject>();
        
        public bool IsOpen => panel != null && panel.activeSelf;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void OnEnable()
        {
            RefreshDisplay();
            
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
            
            if (AdvertisingManager.Instance != null)
                AdvertisingManager.Instance.OnAdsUpdated += RefreshDisplay;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
            
            if (AdvertisingManager.Instance != null)
                AdvertisingManager.Instance.OnAdsUpdated -= RefreshDisplay;
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshDisplay();
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
        
        public void RefreshDisplay()
        {
            UpdateHeader();
            UpdateCurrentEffects();
            RefreshAvailableAds();
            RefreshActiveAds();
        }
        
        private void UpdateHeader()
        {
            if (titleText != null)
                titleText.text = L?.Get("advertising.title") ?? "Advertising";
            
            if (moneyText != null)
            {
                int money = EconomyManager.Instance?.CurrentMoney ?? 0;
                moneyText.text = $"${money:N0}";
            }
            
            if (activeAdsCountText != null && AdvertisingManager.Instance != null)
            {
                int active = AdvertisingManager.Instance.ActiveAdCount;
                int max = AdvertisingManager.Instance.MaxActiveAds;
                activeAdsCountText.text = $"{active}/{max}";
            }
        }
        
        private void UpdateCurrentEffects()
        {
            // Get CustomerSpawner for attraction rate and VIP coefficient
            var customerSpawner = FindObjectOfType<HairRemovalSim.Customer.CustomerSpawner>();
            var shopManager = ShopManager.Instance;
            var adManager = AdvertisingManager.Instance;
            
            // Calculate breakdown
            float baseAttraction = customerSpawner?.BaseAttractionRate ?? 30f;
            float starBonus = shopManager?.GetStarRatingBonus() ?? 0f;
            float adBoost = adManager?.GetAttractionBoost() ?? 0f;
            float totalAttraction = baseAttraction + starBonus + adBoost;
            totalAttraction = Mathf.Clamp(totalAttraction, 0f, 100f);
            
            float baseVip = shopManager?.GetVipCoefficientFromReviews() ?? 50f;
            float adVipBoost = adManager?.GetVipBoost() ?? 0f;
            float totalVip = Mathf.Clamp(baseVip + adVipBoost, 0f, 100f);
            
            // Total display
            if (attractionRateText != null)
                attractionRateText.text = $"{totalAttraction:F1}%";
            
            if (vipCoefficientText != null)
                vipCoefficientText.text = $"{totalVip:F0}";
            
            // Breakdown display - Base (yellow)
            if (baseAttractionText != null)
            {
                float baseTotal = baseAttraction + starBonus;
                string starSign = starBonus >= 0 ? "+" : "";
                baseAttractionText.text = L?.Get("advertising.base_rate") ?? "Base";
                baseAttractionText.text += $": {baseTotal:F0}% ({baseAttraction:F0}{starSign}{starBonus:F0})";
            }
            
            // Breakdown display - Ad boost (green)
            if (adBoostText != null)
            {
                if (adBoost > 0)
                    adBoostText.text = $"{L?.Get("advertising.ad_boost") ?? "Ads"}: +{adBoost:F1}%";
                else
                    adBoostText.text = $"{L?.Get("advertising.ad_boost") ?? "Ads"}: ---";
            }
            
            // VIP breakdown
            if (baseVipText != null)
                baseVipText.text = $"{L?.Get("advertising.base_vip") ?? "Base VIP"}: {baseVip:F0}";
            
            if (adVipBoostText != null)
            {
                if (adVipBoost > 0)
                    adVipBoostText.text = $"{L?.Get("advertising.ad_vip_boost") ?? "Ads"}: +{adVipBoost:F0}";
                else
                    adVipBoostText.text = $"{L?.Get("advertising.ad_vip_boost") ?? "Ads"}: ---";
            }
            
            if (attractionSlider != null)
            {
                attractionSlider.minValue = 0;
                attractionSlider.maxValue = 100;
                attractionSlider.value = totalAttraction;
            }
            
            if (vipSlider != null)
            {
                vipSlider.minValue = 0;
                vipSlider.maxValue = 100;
                vipSlider.value = totalVip;
            }
        }
        
        private void RefreshAvailableAds()
        {
            // Clear existing
            foreach (var card in adCards)
            {
                if (card != null) Destroy(card);
            }
            adCards.Clear();
            
            if (AdvertisingManager.Instance == null || adCardPrefab == null || availableAdsContainer == null)
                return;
            
            int currentGrade = ShopManager.Instance?.StarRating ?? 1;
            
            foreach (var adData in AdvertisingManager.Instance.AvailableAds)
            {
                var cardObj = Instantiate(adCardPrefab, availableAdsContainer);
                var cardUI = cardObj.GetComponent<AdCardUI>();
                
                if (cardUI != null)
                {
                    cardUI.Setup(adData, currentGrade, OnAdStartRequested);
                }
                
                adCards.Add(cardObj);
            }
        }
        
        private void RefreshActiveAds()
        {
            // Clear existing
            foreach (var card in activeAdCards)
            {
                if (card != null) Destroy(card);
            }
            activeAdCards.Clear();
            
            if (AdvertisingManager.Instance == null || activeAdCardPrefab == null || activeAdsContainer == null)
                return;
            
            foreach (var activeAd in AdvertisingManager.Instance.ActiveAds)
            {
                var adData = AdvertisingManager.Instance.GetAdData(activeAd.adId);
                if (adData == null) continue;
                
                var cardObj = Instantiate(activeAdCardPrefab, activeAdsContainer);
                var cardUI = cardObj.GetComponent<ActiveAdCardUI>();
                
                if (cardUI != null)
                {
                    int remainingDays = AdvertisingManager.Instance.GetRemainingDays(activeAd.adId);
                    cardUI.Setup(adData, remainingDays);
                }
                
                activeAdCards.Add(cardObj);
            }
        }
        
        private void OnAdStartRequested(AdvertisementData adData)
        {
            if (AdvertisingManager.Instance == null) return;
            
            bool success = AdvertisingManager.Instance.StartAdvertisement(adData);
            
            if (success)
            {
                Debug.Log($"[AdvertisingPanel] Started ad: {adData.displayName}");
                RefreshDisplay();
            }
        }
    }
}
