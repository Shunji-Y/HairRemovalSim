using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.UI;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Controls DirectionalLight rotation based on time of day.
    /// Rotates X axis from startAngle to endAngle during business hours.
    /// Resets to startAngle when DailySummaryPanel fades to black.
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        [Header("Light Reference")]
        [Tooltip("The DirectionalLight to rotate")]
        [SerializeField] private Light directionalLight;
        
        [Header("Rotation Settings")]
        [Tooltip("X rotation at start of day (9:00)")]
        [SerializeField] private float startAngle = 80f;
        
        [Tooltip("X rotation at end of day (19:00)")]
        [SerializeField] private float endAngle = -120f;
        
        private void Start()
        {
            // Auto-find DirectionalLight if not assigned
            if (directionalLight == null)
            {
                directionalLight = FindObjectOfType<Light>();
                if (directionalLight != null && directionalLight.type != LightType.Directional)
                {
                    directionalLight = null;
                }
            }
            
            // Subscribe to fade complete event for reset
            if (DailySummaryPanel.Instance != null)
            {
                DailySummaryPanel.Instance.OnFadeToBlackComplete += ResetLightRotation;
            }
            
            // Initialize to start angle
            ResetLightRotation();
        }
        
        private void OnDestroy()
        {
            if (DailySummaryPanel.Instance != null)
            {
                DailySummaryPanel.Instance.OnFadeToBlackComplete -= ResetLightRotation;
            }
        }
        
        private void Update()
        {
            if (directionalLight == null) return;
            if (GameManager.Instance == null) return;
            
            // Only update during Day state
            if (GameManager.Instance.CurrentState != GameManager.GameState.Day) return;
            
            // Get day progress (0 = start, 1 = end)
            float progress = GameManager.Instance.GetNormalizedTime();
            
            // Lerp X rotation from startAngle to endAngle
            float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
            
            // Apply rotation (keep Y and Z unchanged)
            Vector3 currentRotation = directionalLight.transform.eulerAngles;
            directionalLight.transform.eulerAngles = new Vector3(currentAngle, currentRotation.y, currentRotation.z);
        }
        
        /// <summary>
        /// Reset light to morning position
        /// </summary>
        private void ResetLightRotation()
        {
            if (directionalLight == null) return;
            
            Vector3 currentRotation = directionalLight.transform.eulerAngles;
            directionalLight.transform.eulerAngles = new Vector3(startAngle, currentRotation.y, currentRotation.z);
            
            Debug.Log($"[DayNightController] Light reset to {startAngle}°");
        }
    }
}
