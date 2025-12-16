using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual loan card in bank panel
    /// </summary>
    public class LoanCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text maxAmountText;
        [SerializeField] private TMP_Text interestRateText;
        [SerializeField] private TMP_Text termText;
        [SerializeField] private Button applyButton;
        [SerializeField] private TMP_Text buttonText;
        
        [Header("Visual States")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = new Color(0.7f, 0.7f, 0.7f);
        
        private LoanData loanData;
        private System.Action<LoanData> onApplyCallback;
        
        public void Setup(LoanData data, bool isActive, System.Action<LoanData> onApply)
        {
            loanData = data;
            onApplyCallback = onApply;
            
            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;
            
            if (nameText != null)
                nameText.text = data.displayName;
            
            if (maxAmountText != null)
                maxAmountText.text = $"Max: ${data.maxAmount:N0}";
            
            if (interestRateText != null)
                interestRateText.text = $"Rate: {data.dailyInterestRate * 100:F2}%/day";
            
            if (termText != null)
                termText.text = $"Term: {data.termDays} days";
            
            // Update button state
            if (applyButton != null)
            {
                applyButton.interactable = !isActive;
                applyButton.onClick.RemoveAllListeners();
                applyButton.onClick.AddListener(OnApplyClicked);
            }
            
            if (buttonText != null)
                buttonText.text = isActive ? "Active" : "Apply";
            
            if (backgroundImage != null)
                backgroundImage.color = isActive ? activeColor : normalColor;
        }
        
        private void OnApplyClicked()
        {
            onApplyCallback?.Invoke(loanData);
        }
    }
}
