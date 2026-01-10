using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI for cleaning progress gauge shown after business hours.
    /// Shows how much of the shop has been cleaned.
    /// </summary>
    public class CleaningProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private Image fillImage;
        
        [Header("Colors")]
        [SerializeField] private Color dirtyColor = new Color(0.8f, 0.4f, 0.2f);
        [SerializeField] private Color cleanColor = new Color(0.2f, 0.8f, 0.4f);
        
        // Flag to track if cleaning was actually needed (debris existed)
        private bool wasCleaningNeeded = false;
        // Flag to prevent tutorial from triggering multiple times
        private bool cleaningTutorialTriggered = false;
        
        private void Start()
        {
            // Subscribe to debris manager events
            if (HairDebrisManager.Instance != null)
            {
                HairDebrisManager.Instance.OnCleaningProgressChanged += UpdateProgress;
            }
            
            // Subscribe to shop events
            GameEvents.OnShopOpened += OnShopOpened;
            GameEvents.OnShopClosed += OnShopClosed;
            
            // Initial state (don't trigger tutorials on startup)
            UpdateProgressInternal(1f, false);
            
            // Hide by default
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (HairDebrisManager.Instance != null)
            {
                HairDebrisManager.Instance.OnCleaningProgressChanged -= UpdateProgress;
            }
            
            GameEvents.OnShopOpened -= OnShopOpened;
            GameEvents.OnShopClosed -= OnShopClosed;
        }
        
        private void OnShopOpened()
        {
            // Hide UI and hide debris icons during business hours
            Hide();
            
            if (HairDebrisManager.Instance != null)
            {
                HairDebrisManager.Instance.SetIconsVisible(false);
            }
        }
        
        private void OnShopClosed()
        {
            // Show UI and show debris icons after hours
            Show();
            
            if (HairDebrisManager.Instance != null)
            {
                HairDebrisManager.Instance.SetIconsVisible(true);
            }
        }
        
        /// <summary>
        /// Show the cleaning progress UI
        /// </summary>
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            
            // Update with current progress
            if (HairDebrisManager.Instance != null)
            {
                float progress = HairDebrisManager.Instance.GetCleaningProgress();
                
                // Track if cleaning is actually needed (debris exists = progress < 1)
                if (progress < 1f)
                {
                    wasCleaningNeeded = true;
                }
                
                // Don't trigger tutorials on initial Show, only on actual cleaning progress
                UpdateProgressInternal(progress, false);
                UpdateRemainingCount();
            }
        }
        
        /// <summary>
        /// Hide the cleaning progress UI
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        
        /// <summary>
        /// Called by HairDebrisManager when cleaning progress changes
        /// </summary>
        private void UpdateProgress(float progress)
        {
            UpdateProgressInternal(progress, true);
        }
        
        /// <summary>
        /// Internal progress update with tutorial trigger control
        /// </summary>
        private void UpdateProgressInternal(float progress, bool allowTutorialTrigger)
        {
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
            
            if (progressText != null)
            {
                progressText.text = $"{progress:P0}";
            }
            
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(dirtyColor, cleanColor, progress);
            }
            
            // Tutorial trigger when cleaning is 100% complete (Day 1 only)
            // Only trigger if: cleaning was needed, player actually cleaned, and not already triggered
            if (allowTutorialTrigger && progress >= 1f && GameManager.Instance?.DayCount == 1 
                && wasCleaningNeeded && !cleaningTutorialTriggered)
            {
                cleaningTutorialTriggered = true;
                
                // Complete tut_cleaning tutorial
                Core.TutorialManager.Instance?.CompleteByAction("cleaning_complete");
                // Show store open tutorial
                Core.TutorialManager.Instance?.TryShowTutorial("tut_store_open");
            }
            
            UpdateRemainingCount();
        }
        
        private void UpdateRemainingCount()
        {
            if (remainingText != null && HairDebrisManager.Instance != null)
            {
                int remaining = HairDebrisManager.Instance.GetRemainingCount();
                int total = HairDebrisManager.Instance.GetTotalSpawnedEver();
                
                if (total == 0)
                {
                    remainingText.text = "No debris today";
                }
                else
                {
                    remainingText.text = $"Remaining: {remaining}/{total}";
                }
            }
        }
    }
}
