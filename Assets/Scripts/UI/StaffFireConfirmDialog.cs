using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Staff;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Confirmation dialog for firing a staff member.
    /// </summary>
    public class StaffFireConfirmDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private HiredStaffData targetStaff;
        private System.Action<HiredStaffData> onConfirm;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
            
            // NOTE: Dialog should be set inactive in the Unity Inspector, not here
            // because Awake() runs when SetActive(true) is first called
        }
        
        /// <summary>
        /// Show the confirmation dialog
        /// </summary>
        public void Show(HiredStaffData staff, System.Action<HiredStaffData> confirmCallback)
        {
            targetStaff = staff;
            onConfirm = confirmCallback;
            
            if (messageText != null)
            {
                string msg = L?.Get("ui.fire_confirm", staff.Name) ?? $"Are you sure you want to fire {staff.Name}?";
                string warning = L?.Get("ui.cannot_undo") ?? "This action cannot be undone.";
                messageText.text = $"{msg}\n\n{warning}";
            }
            
            gameObject.SetActive(true);
        }
        
        private void OnConfirmClicked()
        {
            gameObject.SetActive(false);
            onConfirm?.Invoke(targetStaff);
        }
        
        private void OnCancelClicked()
        {
            gameObject.SetActive(false);
        }
    }
}

