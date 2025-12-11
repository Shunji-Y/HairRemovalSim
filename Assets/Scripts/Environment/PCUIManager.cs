using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Manages PC UI navigation - desktop icons and app panels.
    /// Handles back navigation (right-click) and screen transitions.
    /// </summary>
    public class PCUIManager : MonoBehaviour
    {
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
        }
        
        /// <summary>
        /// Open an app from desktop
        /// </summary>
        public void OpenApp(AppType appType)
        {
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
