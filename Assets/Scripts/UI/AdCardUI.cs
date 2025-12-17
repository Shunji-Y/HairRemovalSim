using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Card UI for available advertisements in the advertising panel
    /// </summary>
    public class AdCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text effectText;
        
        [Header("Lock State")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TMP_Text lockedText;
        
        [Header("Start Button")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private Image cardBackground;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color freeColor = new Color(0.9f, 1f, 0.9f);
        
        private AdvertisementData adData;
        private System.Action<AdvertisementData> onStartCallback;
        private bool isLocked;
        
        // Localization shorthand
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void OnEnable()
        {
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Setup(AdvertisementData data, int currentGrade, System.Action<AdvertisementData> onStart)
        {
            adData = data;
            isLocked = !data.IsUnlockedForGrade(currentGrade);
            onStartCallback = onStart;
            
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClicked);
            }
            
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (adData == null) return;
            
            // Icon
            if (iconImage != null && adData.icon != null)
                iconImage.sprite = adData.icon;
            
            // Name (localized)
            if (nameText != null)
            {
                string localizedName = L?.Get(adData.nameKey);
                nameText.text = string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")
                    ? adData.displayName
                    : localizedName;
            }
            
            // Description
            if (descriptionText != null)
            {
                string localizedDesc = L?.Get(adData.descriptionKey);
                descriptionText.text = string.IsNullOrEmpty(localizedDesc) || localizedDesc.StartsWith("[")
                    ? adData.description
                    : localizedDesc;
            }
            
            // Cost
            if (costText != null)
            {
                if (adData.cost == 0)
                    costText.text = L?.Get("advertising.free") ?? "FREE";
                else
                    costText.text = $"${adData.cost:N0}";
            }
            
            // Duration
            if (durationText != null)
            {
                string dayLabel = adData.durationDays == 1 
                    ? (L?.Get("advertising.day") ?? "day")
                    : (L?.Get("advertising.days") ?? "days");
                durationText.text = $"{adData.durationDays} {dayLabel}";
            }
            
            // Effect
            if (effectText != null)
            {
                string effect = $"+{adData.attractionBoost:F0}%";
                if (adData.vipCoefficientBoost > 0)
                    effect += $" | VIP +{adData.vipCoefficientBoost:F0}";
                if (adData.decayRatePerDay > 0)
                    effect += $" | -{adData.decayRatePerDay:F0}%/d";
                effectText.text = effect;
            }
            
            // Lock state
            UpdateLockState();
            
            // Button state
            UpdateStartButton();
            
            // Background color
            if (cardBackground != null)
            {
                if (isLocked)
                    cardBackground.color = lockedColor;
                else if (adData.cost == 0)
                    cardBackground.color = freeColor;
                else
                    cardBackground.color = normalColor;
            }
        }
        
        private void UpdateLockState()
        {
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            
            if (lockedText != null && isLocked)
            {
                lockedText.text = L?.Get("advertising.locked_grade", adData.requiredShopGrade)
                    ?? $"Grade {adData.requiredShopGrade} Required";
            }
        }
        
        private void UpdateStartButton()
        {
            if (startButton == null) return;
            
            if (isLocked)
            {
                startButton.interactable = false;
                if (startButtonText != null)
                    startButtonText.text = L?.Get("advertising.locked") ?? "LOCKED";
                return;
            }
            
            // Check if can start
            if (AdvertisingManager.Instance != null)
            {
                bool canStart = AdvertisingManager.Instance.CanStartAd(adData, out string reason);
                startButton.interactable = canStart;
                
                if (startButtonText != null)
                {
                    if (canStart)
                        startButtonText.text = L?.Get("advertising.start") ?? "START";
                    else
                        startButtonText.text = reason;
                }
            }
        }
        
        private void OnStartClicked()
        {
            if (adData == null || isLocked) return;
            onStartCallback?.Invoke(adData);
        }
    }
}
