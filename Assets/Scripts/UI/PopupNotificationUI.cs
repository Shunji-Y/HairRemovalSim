using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual popup notification that floats and fades
    /// </summary>
    public class PopupNotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float floatDistance = 100f;
        
        private RectTransform rectTransform;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }
        
        /// <summary>
        /// Initialize and show the popup
        /// </summary>
        /// <param name="icon">Icon sprite to display</param>
        /// <param name="text">Value text (e.g., "+$50")</param>
        /// <param name="textColor">Color of the text</param>
        /// <param name="isPositive">True = float up, False = float down</param>
        public void Show(Sprite icon, string text, Color textColor, bool isPositive)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false;
            }
            
            if (valueText != null)
            {
                valueText.text = text;
                valueText.color = textColor;
            }
            
            StartCoroutine(AnimatePopup(isPositive));
        }
        
        private IEnumerator AnimatePopup(bool isPositive)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            float direction = isPositive ? 1f : -1f;
            Vector2 endPos = startPos + new Vector2(0, floatDistance * direction);
            
            canvasGroup.alpha = 0f;
            
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0, floatDistance * 0.3f * direction), t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            
            // Display (continue floating)
            Vector2 midPos = rectTransform.anchoredPosition;
            elapsed = 0f;
            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / displayDuration;
                rectTransform.anchoredPosition = Vector2.Lerp(midPos, endPos - new Vector2(0, floatDistance * 0.2f * direction), t);
                yield return null;
            }
            
            // Fade out
            Vector2 fadeStartPos = rectTransform.anchoredPosition;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(fadeStartPos, endPos, t);
                yield return null;
            }
            
            // Destroy after animation
            Destroy(gameObject);
        }
    }
}
