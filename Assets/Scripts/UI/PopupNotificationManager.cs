using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Manages popup notifications for money, review, and item success/failure
    /// </summary>
    public class PopupNotificationManager : MonoBehaviour
    {
        public static PopupNotificationManager Instance { get; private set; }
        
        [Header("Prefab")]
        [SerializeField] private GameObject popupPrefab;
        
        [Header("Container")]
        [SerializeField] private RectTransform popupContainer;
        
        [Header("Icons")]
        [SerializeField] private Sprite moneyIcon;
        [SerializeField] private Sprite[] moodIcons; // 0=VeryAngry, 1=Angry, 2=Neutral, 3=Happy, 4=VeryHappy
        [SerializeField] private Sprite itemSuccessIcon;
        [SerializeField] private Sprite itemFailIcon;
        
        [Header("Colors")]
        [SerializeField] private Color moneyPlusColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color moneyMinusColor = new Color(0.9f, 0.2f, 0.2f); // Red
        [SerializeField] private Color reviewPlusColor = new Color(1f, 0.6f, 0f); // Orange
        [SerializeField] private Color reviewMinusColor = new Color(0.9f, 0.2f, 0.2f); // Red
        [SerializeField] private Color itemSuccessColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color itemFailColor = new Color(0.9f, 0.2f, 0.2f); // Red
        
        [Header("Spawn Settings")]
        [SerializeField] private float minX = -300f;
        [SerializeField] private float maxX = 300f;
        [SerializeField] private float minY = -100f;
        [SerializeField] private float maxY = 100f;
        [SerializeField] private float popupVerticalSpacing = 60f; // Spacing between sequential popups
        
        [Header("Sound IDs (from SoundManager)")]
        [SerializeField] private string moneyPlusSoundId = "money_plus";
        [SerializeField] private string moneyMinusSoundId = "money_minus";
        [SerializeField] private string reviewPlusSoundId = "review_plus";
        [SerializeField] private string reviewMinusSoundId = "review_minus";
        
        // Sequential popup tracking
        private float lastSpawnTime;
        private int spawnSequenceIndex;
        private const float SEQUENCE_RESET_TIME = 0.5f; // Reset sequence after this many seconds
        
        private void Awake()
        {
            Instance = this;
        }
        
        /// <summary>
        /// Show money popup (+$50 or -$50)
        /// </summary>
        public void ShowMoney(int amount)
        {
            if (amount == 0) return;
            
            bool isPositive = amount > 0;
            string text = isPositive ? $"+${amount}" : $"-${Mathf.Abs(amount)}";
            Color color = isPositive ? moneyPlusColor : moneyMinusColor;
            
            SpawnPopup(moneyIcon, text, color, isPositive);
            PlaySound(isPositive ? moneyPlusSoundId : moneyMinusSoundId);
        }
        
        /// <summary>
        /// Show review popup with mood icon
        /// </summary>
        public void ShowReview(int reviewChange, int moodIndex = 2)
        {
            if (reviewChange == 0) return;
            
            bool isPositive = reviewChange > 0;
            string text = isPositive ? $"+{reviewChange}" : $"{reviewChange}";
            Color color = isPositive ? reviewPlusColor : reviewMinusColor;
            
            Sprite icon = GetMoodIcon(moodIndex);
            SpawnPopup(icon, text, color, isPositive);
            PlaySound(isPositive ? reviewPlusSoundId : reviewMinusSoundId);
        }
        
        /// <summary>
        /// Show item success/failure popup
        /// </summary>
        public void ShowItemResult(bool success, string itemName = "Item")
        {
            Sprite icon = success ? itemSuccessIcon : itemFailIcon;
            Color color = success ? itemSuccessColor : itemFailColor;
            string text = "Item";
            
            // Always float up for item results
            SpawnPopup(icon, text, color, success);
        }
        
        /// <summary>
        /// Show money and optional review/item at reception complete
        /// </summary>
        public void ShowReceptionComplete(int moneyGained, int reviewChange, bool hasUpsellItem, bool upsellSuccess)
        {
            ShowMoney(moneyGained);
            
            if (reviewChange != 0)
            {
                ShowReview(reviewChange);
            }
            
            if (hasUpsellItem)
            {
                ShowItemResult(upsellSuccess);
            }
        }
        
        /// <summary>
        /// Show money and optional item at checkout complete
        /// </summary>
        public void ShowCheckoutComplete(int moneyGained, bool hasUpsellItem, bool upsellSuccess)
        {
            ShowMoney(moneyGained);
            
            if (hasUpsellItem)
            {
                ShowItemResult(upsellSuccess);
            }
        }
        
        /// <summary>
        /// Show review popup when customer leaves angry
        /// </summary>
        public void ShowAngryLeave(int reviewPenalty)
        {
            // Angry mood icon (index 0 or 1)
            ShowReview(-Mathf.Abs(reviewPenalty), 0);
        }
        
        private Sprite GetMoodIcon(int moodIndex)
        {
            if (moodIcons == null || moodIcons.Length == 0) return null;
            moodIndex = Mathf.Clamp(moodIndex, 0, moodIcons.Length - 1);
            return moodIcons[moodIndex];
        }
        
        private void SpawnPopup(Sprite icon, string text, Color color, bool isPositive)
        {
            if (popupPrefab == null || popupContainer == null) return;
            
            // Track sequence for spacing
            float currentTime = Time.time;
            if (currentTime - lastSpawnTime > SEQUENCE_RESET_TIME)
            {
                spawnSequenceIndex = 0;
            }
            else
            {
                spawnSequenceIndex++;
            }
            lastSpawnTime = currentTime;
            
            GameObject popupObj = Instantiate(popupPrefab, popupContainer);
            RectTransform rt = popupObj.GetComponent<RectTransform>();
            
            // Position with vertical offset for sequential popups
            float x = Random.Range(minX, maxX);
            float baseY = Random.Range(minY, maxY);
            float yOffset = spawnSequenceIndex * popupVerticalSpacing;
            rt.anchoredPosition = new Vector2(x, baseY + yOffset);
            
            // Initialize and show
            var popup = popupObj.GetComponent<PopupNotificationUI>();
            if (popup != null)
            {
                popup.Show(icon, text, color, isPositive);
            }
        }
        
        private void PlaySound(string soundId)
        {
            if (!string.IsNullOrEmpty(soundId) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundId);
            }
        }
    }
}
