using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Card UI for displaying active advertisements
    /// Shows current effect and remaining days
    /// </summary>
    public class ActiveAdCardUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text remainingDaysText;
        [SerializeField] private TMP_Text currentEffectText;
        [SerializeField] private Slider effectSlider;
        
        private AdvertisementData adData;
        private int remainingDays;
        
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
        
        public void Setup(AdvertisementData data, int remaining)
        {
            adData = data;
            remainingDays = remaining;
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (adData == null) return;
            
            // Icon
            if (iconImage != null && adData.icon != null)
                iconImage.sprite = adData.icon;
            
            // Name
            if (nameText != null)
            {
                string localizedName = L?.Get(adData.nameKey);
                nameText.text = string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")
                    ? adData.displayName
                    : localizedName;
            }
            
            // Remaining days
            if (remainingDaysText != null)
            {
                string dayLabel = remainingDays == 1
                    ? (L?.Get("advertising.day_left") ?? "day left")
                    : (L?.Get("advertising.days_left") ?? "days left");
                remainingDaysText.text = $"{remainingDays} {dayLabel}";
            }
            
            // Current effect (with decay applied)
            if (currentEffectText != null)
            {
                int daysSinceStart = adData.durationDays - remainingDays;
                float currentBoost = adData.GetAttractionBoostForDay(daysSinceStart);
                float currentVip = adData.GetVipBoostForDay(daysSinceStart);
                
                string effect = $"+{currentBoost:F1}%";
                if (currentVip > 0)
                    effect += $" | VIP +{currentVip:F1}";
                currentEffectText.text = effect;
            }
            
            // Effect slider (show decay progress)
            if (effectSlider != null)
            {
                int daysSinceStart = adData.durationDays - remainingDays;
                float currentBoost = adData.GetAttractionBoostForDay(daysSinceStart);
                effectSlider.minValue = 0;
                effectSlider.maxValue = adData.attractionBoost;
                effectSlider.value = currentBoost;
            }
        }
    }
}
