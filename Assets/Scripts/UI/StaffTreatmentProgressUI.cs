using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Staff;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// World-space UI that shows staff treatment progress
    /// Fades in when player approaches, fades out when far
    /// Shows warning when treatment is paused due to missing tools
    /// </summary>
    public class StaffTreatmentProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private GameObject warningPanel;
        
        [Header("Fill Material (for HDR Emission)")]
        [SerializeField] private Image fillImage;
        [SerializeField] private string emissionColorProperty = "_EmissionColor";
        
        [Header("Settings")]
        [SerializeField] private float fadeInDistance = 5f;
        [SerializeField] private float fadeOutDistance = 8f;
        [SerializeField] private float fadeSpeed = 3f;
        
        [Header("Colors (HDR)")]
        [SerializeField] [ColorUsage(true, true)] private Color progressColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] [ColorUsage(true, true)] private Color pausedColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        private StaffTreatmentHandler treatmentHandler;
        private Transform playerTransform;
        private float targetAlpha = 0f;
        private Material fillMaterial;
        private bool isInitialized = false;
        
        /// <summary>
        /// Reference to the bed this UI is attached to
        /// </summary>
        public Environment.BedController LinkedBed { get; private set; }
        
        private void Start()
        {
            // Auto-find player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // Get material instance for emission color changes
            if (fillImage != null)
            {
                fillMaterial = fillImage.material; // Creates instance
            }
            
            // Initialize hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
            
            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }
            
            // Try to find linked bed from parent
            if (LinkedBed == null)
            {
                LinkedBed = GetComponentInParent<Environment.BedController>();
            }
        }
        
        /// <summary>
        /// Initialize with a treatment handler (called when staff starts treatment)
        /// </summary>
        public void Initialize(StaffTreatmentHandler handler, Environment.BedController bed = null)
        {
            // Unsubscribe from previous handler
            if (treatmentHandler != null && isInitialized)
            {
                treatmentHandler.OnProgressChanged -= OnProgressChanged;
                treatmentHandler.OnToolStatusChanged -= OnToolStatusChanged;
            }
            
            treatmentHandler = handler;
            if (bed != null) LinkedBed = bed;
            
            if (treatmentHandler != null)
            {
                treatmentHandler.OnProgressChanged += OnProgressChanged;
                treatmentHandler.OnToolStatusChanged += OnToolStatusChanged;
                isInitialized = true;
            }
        }
        
        private void OnDestroy()
        {
            if (treatmentHandler != null)
            {
                treatmentHandler.OnProgressChanged -= OnProgressChanged;
                treatmentHandler.OnToolStatusChanged -= OnToolStatusChanged;
            }
            
            // Clean up material instance
            if (fillMaterial != null)
            {
                Destroy(fillMaterial);
            }
        }
        
        private void Update()
        {
            if (treatmentHandler == null || canvasGroup == null) return;
            
            // Only show when staff is processing
            bool shouldShow = treatmentHandler.IsProcessing && treatmentHandler.CurrentCustomer != null;
            
            if (shouldShow && playerTransform != null)
            {
                // Calculate distance to player
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                
                if (distance <= fadeInDistance)
                {
                    targetAlpha = 1f;
                }
                else if (distance >= fadeOutDistance)
                {
                    targetAlpha = 0f;
                }
                else
                {
                    // Lerp between distances
                    float t = (distance - fadeInDistance) / (fadeOutDistance - fadeInDistance);
                    targetAlpha = 1f - t;
                }
            }
            else
            {
                targetAlpha = 0f;
            }
            
            // Smooth fade
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            
            //// Face camera
            //if (Camera.main != null)
            //{
            //    transform.LookAt(Camera.main.transform);
            //    transform.Rotate(0, 180, 0);
            //}
        }
        
        private void OnProgressChanged(float current, float max)
        {
            if (progressSlider != null && max > 0)
            {
                progressSlider.value = current / max;
            }
            
            if (progressText != null)
            {
                int percent = max > 0 ? Mathf.RoundToInt((current / max) * 100f) : 0;
                progressText.text = $"{percent}%";
            }
        }
        
        private void OnToolStatusChanged(bool isPaused, string message)
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(isPaused);
            }
            
            if (warningText != null)
            {
                warningText.text = isPaused ? $"!{message}!" : "";
            }
            
            if (statusText != null)
            {
                statusText.text = isPaused ? "停止中" : "脱毛進行中";
            }
            
            // Change emission color on material
            SetFillEmissionColor(isPaused ? pausedColor : progressColor);
        }
        
        private void SetFillEmissionColor(Color color)
        {
            if (fillMaterial != null && fillMaterial.HasProperty(emissionColorProperty))
            {
                fillMaterial.SetColor(emissionColorProperty, color);
            }
        }
    }
}
