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
        private bool isActive;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void OnEnable()
        {
            if (L != null)
                L.OnLocaleChanged += RefreshDisplay;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= RefreshDisplay;
        }
        
        public void Setup(LoanData data, bool active, System.Action<LoanData> onApply)
        {
            loanData = data;
            isActive = active;
            onApplyCallback = onApply;
            
            if (applyButton != null)
            {
                applyButton.interactable = !isActive;
                applyButton.onClick.RemoveAllListeners();
                applyButton.onClick.AddListener(OnApplyClicked);
            }
            
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (loanData == null) return;
            
            if (iconImage != null && loanData.icon != null)
                iconImage.sprite = loanData.icon;
            
            if (nameText != null)
                nameText.text = loanData.displayName;
            
            if (maxAmountText != null)
                maxAmountText.text = L?.Get("loan.max", loanData.maxAmount) ?? $"Max: ${loanData.maxAmount:N0}";
            
            if (interestRateText != null)
            {
                float rate = loanData.dailyInterestRate * 100;
                interestRateText.text = L?.Get("loan.rate", rate.ToString("F2")) ?? $"Rate: {rate:F2}%/day";
            }
            
            if (termText != null)
                termText.text = L?.Get("loan.term", loanData.termDays) ?? $"Term: {loanData.termDays} days";
            
            if (buttonText != null)
            {
                string activeText = L?.Get("loan.active") ?? "Active";
                string applyText = L?.Get("loan.apply") ?? "Apply";
                buttonText.text = isActive ? activeText : applyText;
            }
            
            if (backgroundImage != null)
                backgroundImage.color = isActive ? activeColor : normalColor;
        }
        
        private void OnApplyClicked()
        {
            onApplyCallback?.Invoke(loanData);
        }
    }
}
