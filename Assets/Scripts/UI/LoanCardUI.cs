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
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("Grade Lock")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TMP_Text lockedText;
        
        private LoanData loanData;
        private System.Action<LoanData> onApplyCallback;
        private bool isActive;
        private bool isLocked;
        
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
        
        public void Setup(LoanData data, bool active, System.Action<LoanData> onApply, bool locked = false)
        {
            loanData = data;
            isActive = active;
            isLocked = locked;
            onApplyCallback = onApply;
            
            if (applyButton != null)
            {
                applyButton.interactable = !isActive && !isLocked;
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
            {
                string localizedName = !string.IsNullOrEmpty(loanData.nameKey) ? L?.Get(loanData.nameKey) : null;
                nameText.text = (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("["))
                    ? localizedName
                    : loanData.displayName;
            }
            
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
                if (isLocked)
                {
                    buttonText.text = L?.Get("loan.locked", loanData.requiredShopGrade) ?? $"Grade {loanData.requiredShopGrade}";
                }
                else
                {
                    string activeText = L?.Get("loan.active") ?? "Active";
                    string applyText = L?.Get("loan.apply") ?? "Apply";
                    buttonText.text = isActive ? activeText : applyText;
                }
            }
            
            // Locked overlay
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            if (lockedText != null && isLocked)
                lockedText.text = L?.Get("loan.locked_grade", loanData.requiredShopGrade) ?? $"Requires Grade {loanData.requiredShopGrade}";
            
            if (backgroundImage != null)
            {
                if (isLocked)
                    backgroundImage.color = lockedColor;
                else if (isActive)
                    backgroundImage.color = activeColor;
                else
                    backgroundImage.color = normalColor;
            }
        }
        
        private void OnApplyClicked()
        {
            onApplyCallback?.Invoke(loanData);
        }
    }
}
