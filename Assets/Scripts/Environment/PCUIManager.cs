using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Manages PC UI navigation - desktop icons and app panels.
    /// Handles back navigation (right-click) and screen transitions.
    /// </summary>
    public class PCUIManager : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("If true, all panels are unlocked from the start")]
        [SerializeField] private bool isDebug = false;
        
        [Header("Panels")]
        [SerializeField] private GameObject desktopPanel;
        [SerializeField] private GameObject storePanel;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject loanPanel;
        [SerializeField] private GameObject paymentPanel;
        [SerializeField] private GameObject adsPanel;
        [SerializeField] private GameObject reviewPanel;
        [SerializeField] private GameObject staffPanel;
        
        [Header("Desktop Icons (for lock/unlock display)")]
        [SerializeField] private PCDesktopIcon storeIcon;
        [SerializeField] private PCDesktopIcon equipmentIcon;
        [SerializeField] private PCDesktopIcon upgradeIcon;
        [SerializeField] private PCDesktopIcon loanIcon;
        [SerializeField] private PCDesktopIcon paymentIcon;
        [SerializeField] private PCDesktopIcon adsIcon;
        [SerializeField] private PCDesktopIcon reviewIcon;
        [SerializeField] private PCDesktopIcon staffIcon;
        
        [Header("Settings")]
        [SerializeField] private bool rightClickToGoBack = true;
        
        public event Action OnExitPC;
        
        private Stack<GameObject> navigationStack = new Stack<GameObject>();
        private GameObject currentPanel;
        private bool isActive = false;
        
        public enum AppType
        {
            Store,
            Equipment,
            Upgrade,
            Loan,
            Payment,
            Ads,
            Review,
            Staff
        }
        
        private void Update()
        {
            if (!isActive) return;
            
            // Play click sound on left click
            if (Mouse.current.leftButton.wasPressedThisFrame ||Mouse.current.rightButton.wasPressedThisFrame)
            {
                SoundManager.Instance?.PlaySFX("sfx_click");
            }

            // Right-click to go back
            if (rightClickToGoBack && Mouse.current.rightButton.wasPressedThisFrame)
            {
                GoBack();
            }
        }
        
        /// <summary>
        /// Show desktop (initial screen)
        /// </summary>
        public void ShowDesktop()
        {
            isActive = true;
            navigationStack.Clear();
            
            HideAllPanels();
            
            if (desktopPanel != null)
            {
                desktopPanel.SetActive(true);
                currentPanel = desktopPanel;
            }
            
            // Refresh icon states based on unlock conditions
            RefreshDesktopIcons();
        }
        
        /// <summary>
        /// Refresh desktop icons to show locked/unlocked state
        /// </summary>
        public void RefreshDesktopIcons()
        {
            if (storeIcon != null) storeIcon.SetLocked(!IsAppUnlocked(AppType.Store));
            if (equipmentIcon != null) equipmentIcon.SetLocked(!IsAppUnlocked(AppType.Equipment));
            if (upgradeIcon != null) upgradeIcon.SetLocked(!IsAppUnlocked(AppType.Upgrade));
            if (loanIcon != null) loanIcon.SetLocked(!IsAppUnlocked(AppType.Loan));
            if (paymentIcon != null) paymentIcon.SetLocked(!IsAppUnlocked(AppType.Payment));
            if (adsIcon != null) adsIcon.SetLocked(!IsAppUnlocked(AppType.Ads));
            if (reviewIcon != null) reviewIcon.SetLocked(!IsAppUnlocked(AppType.Review));
            if (staffIcon != null) staffIcon.SetLocked(!IsAppUnlocked(AppType.Staff));
        }
        
        /// <summary>
        /// Check if an app is unlocked based on game progression
        /// </summary>
        public bool IsAppUnlocked(AppType appType)
        {
            // Debug mode: all unlocked
            if (isDebug) return true;
            
            var gameManager = GameManager.Instance;
            var shopManager = ShopManager.Instance;
            
            int day = gameManager != null ? gameManager.DayCount : 1;
            bool isNight = gameManager != null && gameManager.CurrentState == GameManager.GameState.Night;
            int starRating = shopManager != null ? shopManager.StarRating : 1;
            
            switch (appType)
            {
                case AppType.Payment:
                    // Always available
                    return true;
                    
                case AppType.Store:
                case AppType.Review:
                    // Day 1 Night or later
                    return (day == 1 && isNight) || day >= 2;
                    
                case AppType.Loan:
                    return day >= 3;
                case AppType.Ads:
                    // Day 2 or later
                    return day >= 2;
                    
                case AppType.Equipment:
                    // Star 3 or higher
                    return starRating >= 3;
                    
                case AppType.Staff:
                    // Star 4 or higher
                    return starRating >= 4;
                    
                case AppType.Upgrade:
                    // Star 5 or higher
                    return starRating >= 5;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Open an app from desktop
        /// </summary>
        public void OpenApp(AppType appType)
        {
            // Check unlock condition
            if (!IsAppUnlocked(appType))
            {
                Debug.Log($"[PCUIManager] App {appType} is locked");
                return;
            }
            
            GameObject targetPanel = GetPanelForApp(appType);
            if (targetPanel == null) return;
            
            // Hide current app panel (but keep desktop visible)
            if (currentPanel != null && currentPanel != desktopPanel)
            {
                currentPanel.SetActive(false);
            }
            
            // Push to stack for back navigation (only if not desktop)
            if (currentPanel != null && currentPanel != desktopPanel)
            {
                navigationStack.Push(currentPanel);
            }
            
            // Show target app
            targetPanel.SetActive(true);
            currentPanel = targetPanel;
            
            Debug.Log($"[PCUIManager] Opened app: {appType}");
        }
        
        /// <summary>
        /// Go back to previous screen
        /// </summary>
        public void GoBack()
        {
            // If viewing an app (not desktop), close it and return to desktop
            if (currentPanel != null && currentPanel != desktopPanel)
            {
                currentPanel.SetActive(false);
                currentPanel = desktopPanel;
                navigationStack.Clear();
                Debug.Log("[PCUIManager] Closed app, returned to desktop");
                return;
            }
            
            // Already at desktop - exit PC
            OnExitPC?.Invoke();
        }
        
        /// <summary>
        /// Hide UI and reset navigation
        /// </summary>
        public void HideAndReset()
        {
            isActive = false;
            navigationStack.Clear();
            HideAllPanels();
            currentPanel = null;
        }
        
        private void HideAllPanels()
        {
            if (desktopPanel != null) desktopPanel.SetActive(false);
            if (storePanel != null) storePanel.SetActive(false);
            if (equipmentPanel != null) equipmentPanel.SetActive(false);
            if (upgradePanel != null) upgradePanel.SetActive(false);
            if (loanPanel != null) loanPanel.SetActive(false);
            if (paymentPanel != null) paymentPanel.SetActive(false);
            if (adsPanel != null) adsPanel.SetActive(false);
            if (reviewPanel != null) reviewPanel.SetActive(false);
            if (staffPanel != null) staffPanel.SetActive(false);
        }
        
        private GameObject GetPanelForApp(AppType appType)
        {
            switch (appType)
            {
                case AppType.Store: return storePanel;
                case AppType.Equipment: return equipmentPanel;
                case AppType.Upgrade: return upgradePanel;
                case AppType.Loan: return loanPanel;
                case AppType.Payment: return paymentPanel;
                case AppType.Ads: return adsPanel;
                case AppType.Review: return reviewPanel;
                case AppType.Staff: return staffPanel;
                default: return null;
            }
        }
    }
}

