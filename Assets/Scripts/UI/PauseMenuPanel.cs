using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Pause menu panel - opened with ESC key when no other panels are open.
    /// Features: Save, Load, Settings, Return to Title, Quit Game
    /// </summary>
    public class PauseMenuPanel : MonoBehaviour
    {
        public static PauseMenuPanel Instance { get; private set; }
        
        [Header("Panel References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject slotSelectionPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button quitButton;
        
        [Header("Slot Selection")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TextMeshProUGUI[] slotTexts;
        [SerializeField] private Button slotBackButton;
        
        [Header("Settings")]
        [SerializeField] private string titleSceneName = "TitleScene";
        
        private bool isPaused = false;
        private bool isSaveMode = false; // true = save, false = load
        
        public bool IsPaused => isPaused;
        
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
            
            SetupButtons();
        }
        
        private void Start()
        {
            // Hide all panels initially
            if (menuPanel != null) menuPanel.SetActive(false);
            if (slotSelectionPanel != null) slotSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        
        private void Update()
        {
            // ESC key handling (using Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }
        
        private void HandleEscapeKey()
        {
            // If slot selection is open, go back to menu
            if (slotSelectionPanel != null && slotSelectionPanel.activeSelf)
            {
                ShowMainMenu();
                return;
            }
            
            // If settings is open, go back to menu
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                ShowMainMenu();
                return;
            }
            
            // Toggle pause menu
            if (isPaused)
            {
                Resume();
            }
            else
            {
                // Check if other panels are open first
                if (!CanOpenPauseMenu())
                {
                    return; // Let the other panel handle ESC
                }
                Pause();
            }
        }
        
        /// <summary>
        /// Check if pause menu can be opened (no other panels are active)
        /// </summary>
        private bool CanOpenPauseMenu()
        {
            // Check common panels that handle their own ESC
            if (ReceptionPanel.Instance != null && ReceptionPanel.Instance.gameObject.activeInHierarchy)
            {
                var panel = ReceptionPanel.Instance.transform.Find("Panel");
                if (panel != null && panel.gameObject.activeSelf) return false;
            }
            
            if (PaymentPanel.Instance != null && PaymentPanel.Instance.gameObject.activeInHierarchy)
            {
                var panel = PaymentPanel.Instance.transform.Find("Panel");
                if (panel != null && panel.gameObject.activeSelf) return false;
            }
            
            if (WarehousePanel.Instance != null && WarehousePanel.Instance.IsOpen) return false;
            
            // Check if cursor is visible (usually means a UI is open)
            if (Cursor.visible && !isPaused) return false;
            
            return true;
        }
        
        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (titleButton != null)
                titleButton.onClick.AddListener(OnTitleClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            if (slotBackButton != null)
                slotBackButton.onClick.AddListener(ShowMainMenu);
            
            // Setup slot buttons
            for (int i = 0; i < slotButtons.Length && i < SaveManager.MAX_SLOTS; i++)
            {
                int slotIndex = i;
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.AddListener(() => OnSlotSelected(slotIndex));
                }
            }
        }
        
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            if (menuPanel != null) menuPanel.SetActive(true);
            
            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("[PauseMenuPanel] Game paused");
        }
        
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            if (menuPanel != null) menuPanel.SetActive(false);
            if (slotSelectionPanel != null) slotSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[PauseMenuPanel] Game resumed");
        }
        
        private void OnSaveClicked()
        {
            isSaveMode = true;
            ShowSlotSelection();
        }
        
        private void OnLoadClicked()
        {
            isSaveMode = false;
            ShowSlotSelection();
        }
        
        private void OnSettingsClicked()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }
        
        private void OnTitleClicked()
        {
            Time.timeScale = 1f;
            isPaused = false;
            
            // Load title scene
            if (!string.IsNullOrEmpty(titleSceneName))
            {
                SceneManager.LoadScene(titleSceneName);
            }
            else
            {
                Debug.LogWarning("[PauseMenuPanel] Title scene name not set");
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("[PauseMenuPanel] Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void ShowMainMenu()
        {
            if (menuPanel != null) menuPanel.SetActive(true);
            if (slotSelectionPanel != null) slotSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        
        private void ShowSlotSelection()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (slotSelectionPanel != null) slotSelectionPanel.SetActive(true);
            
            RefreshSlotDisplay();
        }
        
        private void RefreshSlotDisplay()
        {
            if (SaveManager.Instance == null) return;
            
            for (int i = 0; i < slotTexts.Length && i < SaveManager.MAX_SLOTS; i++)
            {
                var info = SaveManager.Instance.GetSlotInfo(i);
                
                if (info.exists)
                {
                    slotTexts[i].text = $"スロット {i + 1}: Day {info.day} - {info.date}";
                }
                else
                {
                    slotTexts[i].text = $"スロット {i + 1}: 空き";
                }
                
                // Disable empty slots for loading
                if (slotButtons[i] != null)
                {
                    slotButtons[i].interactable = isSaveMode || info.exists;
                }
            }
        }
        
        private void OnSlotSelected(int slot)
        {
            if (SaveManager.Instance == null) return;
            
            if (isSaveMode)
            {
                if (SaveManager.Instance.Save(slot))
                {
                    Debug.Log($"[PauseMenuPanel] Saved to slot {slot}");
                    
                    string msg = LocalizationManager.Instance.Get("msg.save_complete") ?? "スロット{0}にセーブしました";
                    msg = string.Format(msg, slot + 1);
                    
                    MessageBoxManager.Instance?.ShowDirectMessage(msg, MessageType.Info);
                }
            }
            else
            {
                if (SaveManager.Instance.Load(slot))
                {
                    Debug.Log($"[PauseMenuPanel] Loaded from slot {slot}");
                    
                    string msg = LocalizationManager.Instance.Get("msg.load_complete") ?? "スロット{0}からロードしました";
                    msg = string.Format(msg, slot + 1);
                    
                    MessageBoxManager.Instance?.ShowDirectMessage(msg, MessageType.Info);
                }
            }
            
            Resume();
        }
    }
}
