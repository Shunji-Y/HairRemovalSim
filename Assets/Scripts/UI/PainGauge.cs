using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Circular pain gauge UI that displays customer pain level.
    /// Uses Unity's Image fillAmount with Radial360 fill method.
    /// </summary>
    public class PainGauge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage; // Radial fill image
        [SerializeField] private Image backgroundImage; // Background circle
        [SerializeField] private TextMeshProUGUI painText; // Optional: pain percentage text
        
        [Header("Colors")]
        [SerializeField] private Color lowPainColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color mediumPainColor = new Color(1f, 0.8f, 0f); // Yellow
        [SerializeField] private Color highPainColor = new Color(1f, 0.3f, 0.2f); // Red
        [SerializeField] private Color extremePainColor = new Color(0.8f, 0f, 0f); // Dark Red
        [SerializeField] private Color backgroundNormalColor = Color.white;
        [SerializeField] private Color backgroundBlockedColor = new Color(1f, 0.3f, 0.3f); // Red background when blocked
        
        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 5f; // Speed of fill animation
        [SerializeField] private float maxPainHoldDuration = 1f; // How long to hold at 100%
        
        [Header("Fill Range (for horseshoe effect)")]
        [SerializeField] private float fillMin = 0.08f; // Starting fill amount (7 o'clock)
        [SerializeField] private float fillMax = 0.92f; // Ending fill amount (5 o'clock)
        
        private float targetFillAmount = 0f;
        private Customer.CustomerController targetCustomer;
        private float maxPainHoldTimer = 0f;
        private bool isHoldingMaxPain = false;
        
        private void Update()
        {
            if (targetCustomer == null) return;
            
            // Check if customer just reached 100% pain
            float actualFill = targetCustomer.currentPain / targetCustomer.maxPain;
            
            // Handle 100% hold
            if (actualFill >= 1f && !isHoldingMaxPain)
            {
                isHoldingMaxPain = true;
                maxPainHoldTimer = maxPainHoldDuration;
            }
            
            if (isHoldingMaxPain)
            {
                maxPainHoldTimer -= Time.deltaTime;
                targetFillAmount = 1f; // Force 100% during hold
                
                if (maxPainHoldTimer <= 0f)
                {
                    isHoldingMaxPain = false;
                }
            }
            else
            {
                targetFillAmount = actualFill;
            }
            
            // Tutorial trigger: when pain first exceeds 50%
            if (actualFill >= 0.5f)
            {
                Core.TutorialManager.Instance?.TryShowTutorial("tut_pain_gauge");
            }
            
            // Smooth fill animation
            if (fillImage != null)
            {
                // Remap 0-1 to fillMin-fillMax for horseshoe effect
                float remappedFill = Mathf.Lerp(fillMin, fillMax, targetFillAmount);
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, remappedFill, Time.deltaTime * smoothSpeed);
                
                // Update color based on pain level
                fillImage.color = GetPainColor(targetCustomer.PainLevel, targetFillAmount);
            }
            
            // Update background color based on treatment blocked state
            if (backgroundImage != null)
            {
                bool isBlocked = !targetCustomer.CanReceiveTreatment();
                backgroundImage.color = isBlocked ? backgroundBlockedColor : backgroundNormalColor;
            }
            
            // Update text
            if (painText != null)
            {
                painText.text = $"{Mathf.RoundToInt(targetCustomer.currentPain)}%";
            }
        }
        
        private Color GetPainColor(int painLevel, float fillAmount)
        {
            switch (painLevel)
            {
                case 1:
                    return Color.Lerp(lowPainColor, mediumPainColor, fillAmount * 2f);
                case 2:
                    return Color.Lerp(mediumPainColor, highPainColor, (fillAmount - 0.5f) * 2f);
                case 3:
                    return extremePainColor;
                default:
                    return lowPainColor;
            }
        }
        
        /// <summary>
        /// Set the customer to track pain for
        /// </summary>
        public void SetCustomer(Customer.CustomerController customer)
        {
            targetCustomer = customer;
            isHoldingMaxPain = false;
            maxPainHoldTimer = 0f;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundNormalColor;
            }
        }
        
        /// <summary>
        /// Clear the customer reference and hide gauge
        /// </summary>
        public void ClearCustomer()
        {
            targetCustomer = null;
            isHoldingMaxPain = false;
            maxPainHoldTimer = 0f;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundNormalColor;
            }
        }
        
        /// <summary>
        /// Show the gauge
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Hide the gauge
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
