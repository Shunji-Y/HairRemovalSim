using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI component for individual message box items.
    /// Displays time (optional), message text with configurable color.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MessageBoxUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI messageText;
        
        [Header("Background Colors")]
        [SerializeField] private Color defaultBgColor = new Color(0, 0, 0, 0.7f);
        
        private MessageBoxManager manager;
        private CanvasGroup canvasGroup;
        private string messageId;
        private bool isPersistent;
        private string dismissAction;
        private MessageType messageType;
        private float spawnTime;
        
        public string MessageId => messageId;
        public bool IsPersistent => isPersistent;
        public string DismissAction => dismissAction;
        public MessageType MessageType => messageType;
        public float SpawnTime => spawnTime;
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        /// <summary>
        /// Initialize with manager reference
        /// </summary>
        public void Initialize(MessageBoxManager mgr)
        {
            manager = mgr;
        }
        
        /// <summary>
        /// Setup the message display
        /// </summary>
        public void Setup(string id, string time, string message, Color textColor, bool bold, bool persistent, bool playSound, MessageType type = MessageType.Info)
        {
            messageId = id;
            isPersistent = persistent;
            messageType = type;
            spawnTime = Time.time; // Record spawn time for auto-hide
            
            // Time display
            if (timeText != null)
            {
                if (!string.IsNullOrEmpty(time))
                {
                    timeText.text = $"[{time}]";
                    timeText.gameObject.SetActive(true);
                    timeText.color = textColor;
                }
                else
                {
                    timeText.gameObject.SetActive(false);
                }
            }
            
            // Message display
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = textColor;
                messageText.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            }
            
            // Background
            if (background != null)
            {
                background.color = defaultBgColor;
            }
            
            // Ensure visible
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
            // Force layout rebuild for ContentSizeFitter
            Canvas.ForceUpdateCanvases();
            
            if (messageText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            
            if (transform.parent != null)
            {
                var parentRect = transform.parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }
            }
        }
        
        /// <summary>
        /// Set the dismiss action ID
        /// </summary>
        public void SetDismissAction(string action)
        {
            dismissAction = action;
        }
        
        /// <summary>
        /// Called when clicking the message (optional close button)
        /// </summary>
        public void OnClick()
        {
            if (!isPersistent && manager != null)
            {
                manager.ReturnToPool(this);
            }
        }
    }
}
