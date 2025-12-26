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
            
            // Initial state
            UpdateProgress(1f);
            
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
                UpdateProgress(HairDebrisManager.Instance.GetCleaningProgress());
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
        
        private void UpdateProgress(float progress)
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
