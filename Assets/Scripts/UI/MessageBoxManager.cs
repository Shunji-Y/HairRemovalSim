using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Message categories for filtering and display
    /// </summary>
    public enum MessageCategory
    {
        Tutorial,   // チュートリアル
        Customer,   // お客情報
        Staff,      // スタッフ情報
        Shop        // 店舗情報
    }
    
    /// <summary>
    /// Message types that determine color
    /// </summary>
    public enum MessageType
    {
        Tutorial,   // 緑 - チュートリアル
        Info,       // 白 - 汎用情報
        Warning,    // 黄 - お客待ち、警告
        Complaint,  // 赤 - クレーム、痛みで帰った
        LevelUp     // 水色 - 星レベル/グレードアップ
    }
    
    /// <summary>
    /// Data structure for a message
    /// </summary>
    [System.Serializable]
    public class MessageData
    {
        public string id;               // Unique ID (for preventing duplicates)
        public string messageKey;       // Localization key or direct message
        public MessageCategory category;
        public MessageType type;
        public bool playSound = true;
        public bool showTime = true;    // Show in-game time
        public bool isBold = false;
        public bool persistent = false; // Don't auto-hide
        public string dismissAction;    // Action ID to dismiss this message
        
        // Optional: direct message (bypasses localization)
        public string directMessage;
        
        public MessageData(string id, string messageKey, MessageType type, MessageCategory category = MessageCategory.Shop)
        {
            this.id = id;
            this.messageKey = messageKey;
            this.type = type;
            this.category = category;
        }
    }
    
    /// <summary>
    /// Manages message box notifications in the top-left corner.
    /// Features: Object pooling, auto-hide after timeout, max message limit.
    /// </summary>
    public class MessageBoxManager : MonoBehaviour
    {
        public static MessageBoxManager Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform messageContainer;
        [SerializeField] private GameObject messagePrefab;
        
        [Header("Settings")]
        [SerializeField] private float autoHideDelay = 15f;
        [SerializeField] private int maxMessages = 10;
        [SerializeField] private int poolSize = 15;
        
        [Header("Colors")]
        [SerializeField] private Color tutorialColor = new Color(0.298f, 0.686f, 0.314f); // #4CAF50
        [SerializeField] private Color infoColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.757f, 0.027f); // #FFC107
        [SerializeField] private Color complaintColor = new Color(0.957f, 0.263f, 0.212f); // #F44336
        [SerializeField] private Color levelUpColor = new Color(0.0f, 0.749f, 1.0f); // #00BFFF 水色
        
        [Header("Audio")]
        [SerializeField] private AudioClip messageSound;
        [SerializeField] private AudioSource audioSource;
        
        // Active messages (newest at index 0)
        private List<MessageBoxUI> activeMessages = new List<MessageBoxUI>();
        
        // Object pool
        private Queue<MessageBoxUI> messagePool = new Queue<MessageBoxUI>();
        
        // Auto-hide timer
        private float hideTimer = 0f;
        private bool isVisible = false;
        
        // Displayed message IDs (prevent duplicates)
        private HashSet<string> displayedIds = new HashSet<string>();
        
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
            
            InitializePool();
        }
        
        private void Start()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!isVisible) return;
            
            // Always count down timer for non-persistent messages
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                // Remove all non-persistent messages
                for (int i = activeMessages.Count - 1; i >= 0; i--)
                {
                    if (!activeMessages[i].IsPersistent)
                    {
                        ReturnToPool(activeMessages[i]);
                    }
                }
                
                // Reset timer for next batch
                hideTimer = autoHideDelay;
            }
        }
        
        /// <summary>
        /// Initialize the object pool
        /// </summary>
        private void InitializePool()
        {
            if (messagePrefab == null || messageContainer == null) return;
            
            for (int i = 0; i < poolSize; i++)
            {
                var obj = Instantiate(messagePrefab, messageContainer);
                var msgUI = obj.GetComponent<MessageBoxUI>();
                if (msgUI != null)
                {
                    msgUI.Initialize(this);
                    obj.SetActive(false);
                    messagePool.Enqueue(msgUI);
                }
            }
        }
        
        /// <summary>
        /// Show a message
        /// </summary>
        public void ShowMessage(MessageData data)
        {
            if (data == null) return;
            
            // Check for duplicate ID
            if (!string.IsNullOrEmpty(data.id) && displayedIds.Contains(data.id))
            {
                return; // Already displayed
            }
            
            // Get message from pool
            MessageBoxUI msgUI = GetFromPool();
            if (msgUI == null) return;
            
            // Get localized text
            string message = !string.IsNullOrEmpty(data.directMessage) 
                ? data.directMessage 
                : (LocalizationManager.Instance?.Get(data.messageKey) ?? data.messageKey);
            
            // Get time/label string - Tutorial shows [Tips], others show time
            string timeStr = null;
            if (data.type == MessageType.Tutorial)
            {
                timeStr = "Tips";
            }
            else if (data.showTime)
            {
                timeStr = GetCurrentTimeString();
            }
            
            // Get color
            Color color = GetColorForType(data.type);
            
            // Setup the message UI
            msgUI.Setup(data.id, timeStr, message, color, data.isBold, data.persistent, data.playSound);
            
            // Add to active list (at beginning = top)
            activeMessages.Insert(0, msgUI);
            msgUI.transform.SetAsFirstSibling();
            
            // Track ID
            if (!string.IsNullOrEmpty(data.id))
            {
                displayedIds.Add(data.id);
            }
            
            // Remove oldest if over limit
            while (activeMessages.Count > maxMessages)
            {
                var oldest = activeMessages[activeMessages.Count - 1];
                ReturnToPool(oldest);
            }
            
            // Show panel and reset timer
            ShowPanel();
            
            // Play sound
            if (data.playSound && messageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(messageSound);
            }
            
            Debug.Log($"[MessageBoxManager] Showing message: {message}");
        }
        
        /// <summary>
        /// Show a simple message with defaults
        /// </summary>
        public void ShowMessage(string messageKey, MessageType type, bool persistent = false, string id = null)
        {
            var data = new MessageData(id, messageKey, type);
            data.persistent = persistent;
            ShowMessage(data);
        }
        
        /// <summary>
        /// Show a direct message (no localization)
        /// </summary>
        public void ShowDirectMessage(string message, MessageType type, bool persistent = false, string id = null)
        {
            var data = new MessageData(id, null, type);
            data.directMessage = message;
            data.persistent = persistent;
            ShowMessage(data);
        }
        
        /// <summary>
        /// Hide a specific message by ID
        /// </summary>
        public void DismissMessage(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            
            for (int i = activeMessages.Count - 1; i >= 0; i--)
            {
                if (activeMessages[i].MessageId == id)
                {
                    ReturnToPool(activeMessages[i]);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Dismiss all messages with a specific action
        /// </summary>
        public void DismissByAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            
            for (int i = activeMessages.Count - 1; i >= 0; i--)
            {
                if (activeMessages[i].DismissAction == actionId)
                {
                    ReturnToPool(activeMessages[i]);
                }
            }
        }
        
        /// <summary>
        /// Clear all messages
        /// </summary>
        public void ClearAll()
        {
            for (int i = activeMessages.Count - 1; i >= 0; i--)
            {
                ReturnToPool(activeMessages[i]);
            }
            HidePanel();
        }
        
        private MessageBoxUI GetFromPool()
        {
            if (messagePool.Count > 0)
            {
                var msg = messagePool.Dequeue();
                msg.gameObject.SetActive(true);
                return msg;
            }
            
            // Pool exhausted, create new
            if (messagePrefab != null && messageContainer != null)
            {
                var obj = Instantiate(messagePrefab, messageContainer);
                var msgUI = obj.GetComponent<MessageBoxUI>();
                if (msgUI != null)
                {
                    msgUI.Initialize(this);
                    return msgUI;
                }
            }
            
            return null;
        }
        
        public void ReturnToPool(MessageBoxUI msgUI)
        {
            if (msgUI == null) return;
            
            // Remove from active list
            activeMessages.Remove(msgUI);
            
            // Remove from tracked IDs
            if (!string.IsNullOrEmpty(msgUI.MessageId))
            {
                displayedIds.Remove(msgUI.MessageId);
            }
            
            // Return to pool
            msgUI.gameObject.SetActive(false);
            messagePool.Enqueue(msgUI);
            
            // Only hide panel if no messages remain at all
            if (activeMessages.Count == 0 && panel != null)
            {
                panel.SetActive(false);
                isVisible = false;
            }
        }
        
        private void ShowPanel()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
            
            // Only reset timer if panel was not already visible
            // This prevents persistent messages from resetting the timer for non-persistent ones
            if (!isVisible)
            {
                hideTimer = autoHideDelay;
            }
            isVisible = true;
        }
        
        private void HidePanel()
        {
            // Don't hide if persistent messages exist
            foreach (var msg in activeMessages)
            {
                if (msg.IsPersistent) return;
            }
            
            // Return all non-persistent messages to pool
            // Note: Panel visibility is handled by ReturnToPool when count reaches 0
            for (int i = activeMessages.Count - 1; i >= 0; i--)
            {
                if (!activeMessages[i].IsPersistent)
                {
                    ReturnToPool(activeMessages[i]);
                }
            }
            
            // Reset visibility flag but don't hide panel yet
            // Panel will be hidden when last message is returned to pool
            if (activeMessages.Count == 0)
            {
                isVisible = false;
            }
        }
        
        private Color GetColorForType(MessageType type)
        {
            return type switch
            {
                MessageType.Tutorial => tutorialColor,
                MessageType.Info => infoColor,
                MessageType.Warning => warningColor,
                MessageType.Complaint => complaintColor,
                MessageType.LevelUp => levelUpColor,
                _ => infoColor
            };
        }
        
        private string GetCurrentTimeString()
        {
            if (GameManager.Instance == null) return "";
            
            // Get normalized time (0-1) and convert to 9:00-19:00
            float normalizedTime = GameManager.Instance.GetNormalizedTime();
            float hours = 9f + (normalizedTime * 10f); // 9:00 to 19:00
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            
            return $"{hour:D2}:{minute:D2}";
        }
    }
}
