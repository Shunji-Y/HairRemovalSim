using UnityEngine;
using UnityEngine.UI;

namespace HairRemovalSim.Customer
{
    /// <summary>
    /// 3D world-space wait time gauge displayed above customer's head.
    /// Billboard effect to always face camera.
    /// </summary>
    public class WaitTimeGaugeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float warningThreshold = 0.5f;
        [SerializeField] private float criticalThreshold = 0.8f;
        
        [Header("Position")]
        [SerializeField] private float heightOffset = 2.2f;
        
        private CustomerController customer;
        private Camera mainCamera;
        private Transform customerTransform;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            customer = GetComponentInParent<CustomerController>();
            
            if (customer != null)
            {
                customerTransform = customer.transform;
            }
            
            // Initialize hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        private void LateUpdate()
        {
            if (customer == null || mainCamera == null) return;
            
            // Position above customer's head
            if (customerTransform != null)
            {
                transform.position = customerTransform.position + Vector3.up * heightOffset;
            }
            
            // Billboard - face camera
            transform.rotation = mainCamera.transform.rotation;
            
            // Update visibility and value
            // Show if actively waiting OR if paused (waitTimer > 0 but not incrementing)
            float progress = customer.GetWaitProgress();
            bool shouldShow = customer.IsWaiting || progress > 0f;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = shouldShow ? 1f : 0f;
            }
            
            if (shouldShow && slider != null)
            {
                slider.value = progress;
                
                // Update color based on progress
                UpdateColor(progress);
            }
        }
        
        private void UpdateColor(float progress)
        {
            if (fillImage == null) return;
            
            Color targetColor;
            if (progress >= criticalThreshold)
            {
                targetColor = criticalColor;
            }
            else if (progress >= warningThreshold)
            {
                float t = (progress - warningThreshold) / (criticalThreshold - warningThreshold);
                targetColor = Color.Lerp(warningColor, criticalColor, t);
            }
            else
            {
                float t = progress / warningThreshold;
                targetColor = Color.Lerp(normalColor, warningColor, t);
            }
            
            fillImage.color = targetColor;
        }
        
        /// <summary>
        /// Initialize with customer reference
        /// </summary>
        public void Initialize(CustomerController targetCustomer)
        {
            customer = targetCustomer;
            customerTransform = customer?.transform;
        }
        
        /// <summary>
        /// Reset gauge state (called when customer is pooled/respawned)
        /// </summary>
        public void ResetGauge()
        {
            if (slider != null)
            {
                slider.value = 0f;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            if (fillImage != null)
            {
                fillImage.color = normalColor;
            }
        }
    }
}
