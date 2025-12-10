using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        
        [Header("References")]
        [SerializeField] private PCUIManager uiManager;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        
        [Header("Hover Effect")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.9f, 1f);
        
        private Button button;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
            
            if (nameText != null)
            {
                nameText.text = appName;
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
            if (uiManager != null)
            {
                uiManager.OpenApp(appType);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (iconImage != null)
            {
                iconImage.color = hoverColor;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (iconImage != null)
            {
                iconImage.color = normalColor;
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
