using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Desktop icon that opens an app when clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PCDesktopIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Settings")]
        [SerializeField] private PCUIManager.AppType appType;
        [SerializeField] private string appName = "App";
        string localizedAppName;
        
        [Header("References")]
        [SerializeField] private PCUIManager uiManager;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        
        [Header("Hover Effect")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        
        [Header("Lock Display")]
        [SerializeField] private GameObject lockOverlay; // Optional: overlay image for locked state
        
        private Button button;
        private bool isLocked = false;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
            
            if (nameText != null)
            {
                localizedAppName = LocalizationManager.Instance.Get(appName);
                nameText.text = localizedAppName;
            }
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }
        
        private void OnClick()
        {
            if (isLocked) return;
            
            if (uiManager != null)
            {
                uiManager.OpenApp(appType);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked) return;
            
            if (iconImage != null)
            {
                iconImage.color = hoverColor;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (iconImage != null)
            {
                iconImage.color = isLocked ? lockedColor : normalColor;
            }
        }
        
        /// <summary>
        /// Set locked state (grayed out, non-clickable)
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            
            if (button != null)
            {
                button.interactable = !locked;
            }
            
            if (iconImage != null)
            {
                iconImage.color = locked ? lockedColor : normalColor;
            }

            if (nameText != null)
            {
                var lockText = LocalizationManager.Instance.Get("ui.lock");
                nameText.text = locked ? lockText :  localizedAppName;
            }
            
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(locked);
            }
        }
        
        /// <summary>
        /// Set the UI manager reference (useful for runtime setup)
        /// </summary>
        public void SetUIManager(PCUIManager manager)
        {
            uiManager = manager;
        }
    }
}
