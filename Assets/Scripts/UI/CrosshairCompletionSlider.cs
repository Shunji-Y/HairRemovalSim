using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Displays a completion slider next to the crosshair when hovering over a target body part.
    /// </summary>
    public class CrosshairCompletionSlider : MonoBehaviour
    {
        public static CrosshairCompletionSlider Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private GameObject sliderRoot;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private TextMeshProUGUI partNameText;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color completedColor = new Color(1f, 0.9f, 0.2f, 1f);
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        [SerializeField] private float sliderLerpSpeed = 5f;
        
        private CanvasGroup canvasGroup;
        private bool isVisible = false;
        private string currentPartName;
        private float targetSliderValue = 0f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            canvasGroup = sliderRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = sliderRoot.AddComponent<CanvasGroup>();
            }
            
            // Start hidden
            canvasGroup.alpha = 0f;
            sliderRoot.SetActive(false);
        }
        
        private void Update()
        {
            if (!isVisible || progressSlider == null) return;
            
            // Lerp slider value towards target
            progressSlider.value = Mathf.Lerp(progressSlider.value, targetSliderValue, Time.deltaTime * sliderLerpSpeed);
        }
        
        /// <summary>
        /// Show the slider with the given completion percentage
        /// </summary>
        public void Show(string partName, float completionPercent)
        {
            if (sliderRoot == null || progressSlider == null) return;
            
            currentPartName = partName;
            
            // Set target slider value for lerp
            targetSliderValue = Mathf.Clamp01(completionPercent / 100f);
            
            // Update percentage text
            if (percentageText != null)
            {
                percentageText.text = $"{Mathf.RoundToInt(completionPercent)}%";
            }
            
            // Update part name text (localized)
            if (partNameText != null)
            {
                // Try to get localized name using key format: part.{partName}
                string locKey = $"part.{partName}";
                string localizedName = HairRemovalSim.Core.LocalizationManager.Instance?.Get(locKey);
                
                // Fallback to raw part name if localization not found
                if (string.IsNullOrEmpty(localizedName) || localizedName == locKey)
                {
                    localizedName = partName;
                }
                
                partNameText.text = localizedName;
            }
            
            // Update color based on completion
            if (fillImage != null)
            {
                fillImage.color = completionPercent >= 100f ? completedColor : normalColor;
            }
            
            // Update text color to match
            if (percentageText != null)
            {
                percentageText.color = completionPercent >= 100f ? completedColor : normalColor;
            }
            
            // Fade in if not already visible
            if (!isVisible)
            {
                isVisible = true;
                sliderRoot.SetActive(true);
                
                // Initialize slider to target immediately on first show
                progressSlider.value = targetSliderValue;
                
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
            }
        }
        
        /// <summary>
        /// Hide the slider
        /// </summary>
        public void Hide()
        {
            if (!isVisible) return;
            
            isVisible = false;
            currentPartName = null;
            
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => sliderRoot.SetActive(false));
        }
        
        /// <summary>
        /// Check if currently showing for a specific part
        /// </summary>
        public bool IsShowingPart(string partName)
        {
            return isVisible && currentPartName == partName;
        }
        
        /// <summary>
        /// Get current part name being displayed
        /// </summary>
        public string GetCurrentPartName()
        {
            return currentPartName;
        }
    }
}
