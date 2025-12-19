using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages screen transition effects like whiteout/blackout
    /// </summary>
    public class ScreenTransitionManager : MonoBehaviour
    {
        public static ScreenTransitionManager Instance { get; private set; }
        
        [Header("Overlay")]
        [SerializeField] private Image overlayImage;
        [SerializeField] private Canvas overlayCanvas;
        
        [Header("Default Timing")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float holdDuration = 1.4f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        
        private Coroutine currentTransition;
        private bool isTransitioning;
        
        public bool IsTransitioning => isTransitioning;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Ensure overlay starts hidden
            if (overlayImage != null)
            {
                overlayImage.color = new Color(1, 1, 1, 0);
                overlayImage.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Perform whiteout transition with callback at peak
        /// </summary>
        /// <param name="onPeak">Called when screen is fully white</param>
        /// <param name="onComplete">Called when transition is complete</param>
        public void DoWhiteout(System.Action onPeak = null, System.Action onComplete = null)
        {
            DoTransition(Color.white, fadeInDuration, holdDuration, fadeOutDuration, onPeak, onComplete);
        }
        
        /// <summary>
        /// Perform custom transition
        /// </summary>
        public void DoTransition(Color color, float fadeIn, float hold, float fadeOut, 
            System.Action onPeak = null, System.Action onComplete = null)
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
            currentTransition = StartCoroutine(TransitionCoroutine(color, fadeIn, hold, fadeOut, onPeak, onComplete));
        }
        
        private IEnumerator TransitionCoroutine(Color color, float fadeIn, float hold, float fadeOut,
            System.Action onPeak, System.Action onComplete)
        {
            isTransitioning = true;
            
            if (overlayImage == null)
            {
                Debug.LogError("[ScreenTransitionManager] Overlay image not assigned!");
                isTransitioning = false;
                yield break;
            }
            
            // Show overlay
            overlayImage.gameObject.SetActive(true);
            
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeIn);
                overlayImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            overlayImage.color = new Color(color.r, color.g, color.b, 1f);
            
            // Peak callback (execute upgrade logic here)
            onPeak?.Invoke();
            
            // Hold
            yield return new WaitForSecondsRealtime(hold);
            
            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                overlayImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            overlayImage.color = new Color(color.r, color.g, color.b, 0f);
            
            // Hide overlay
            overlayImage.gameObject.SetActive(false);
            
            isTransitioning = false;
            currentTransition = null;
            
            // Complete callback
            onComplete?.Invoke();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Create Overlay UI")]
        private void CreateOverlayUI()
        {
            // Find or create canvas
            if (overlayCanvas == null)
            {
                var canvasObj = new GameObject("ScreenTransitionCanvas");
                canvasObj.transform.SetParent(transform);
                overlayCanvas = canvasObj.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 9999;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create overlay image
            if (overlayImage == null)
            {
                var imageObj = new GameObject("OverlayImage");
                imageObj.transform.SetParent(overlayCanvas.transform, false);
                overlayImage = imageObj.AddComponent<Image>();
                overlayImage.color = new Color(1, 1, 1, 0);
                overlayImage.raycastTarget = true;
                
                var rect = overlayImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                
                imageObj.SetActive(false);
            }
            
            Debug.Log("[ScreenTransitionManager] Overlay UI created!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
