using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Staff;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI card for a hired staff member in the manage panel.
    /// Shows name, photo, rank, assignment location, and fire button.
    /// </summary>
    public class StaffManageCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image photoImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text assignmentText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button fireButton;
        
        private HiredStaffData staffData;
        private System.Action<HiredStaffData> onFireClicked;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        /// <summary>
        /// Setup the card with hired staff data
        /// </summary>
        public void Setup(HiredStaffData data, System.Action<HiredStaffData> fireCallback)
        {
            staffData = data;
            onFireClicked = fireCallback;
            
            // Set photo
            if (photoImage != null && data.profile?.portrait != null)
            {
                photoImage.sprite = data.profile.portrait;
                photoImage.enabled = true;
            }
            else if (photoImage != null)
            {
                photoImage.enabled = false;
            }
            
            // Set name
            if (nameText != null)
                nameText.text = data.Name;
            
            // Set rank
            if (rankText != null)
                rankText.text = data.profile?.rankData?.GetDisplayName() ?? L?.Get("ui.unknown") ?? "Unknown";
            
            // Set assignment location
            if (assignmentText != null)
                assignmentText.text = GetAssignmentDisplayText(data);
            
            // Set status (working/waiting)
            if (statusText != null)
            {
                if (data.isActive)
                    statusText.text = $"<color=green>{L?.Get("ui.staff_working") ?? "Working"}</color>";
                else
                    statusText.text = $"<color=yellow>{L?.Get("ui.staff_starts_tomorrow") ?? "Starts Tomorrow"}</color>";
            }
            
            // Setup fire button
            if (fireButton != null)
            {
                fireButton.onClick.RemoveAllListeners();
                fireButton.onClick.AddListener(OnFireButtonClicked);
            }
        }
        
        private string GetAssignmentDisplayText(HiredStaffData data)
        {
            switch (data.assignment)
            {
                case StaffAssignment.None:
                    return L?.Get("ui.assignment_none") ?? "Unassigned";
                case StaffAssignment.Reception:
                    return L?.Get("ui.assignment_reception") ?? "Reception";
                case StaffAssignment.Cashier:
                    return L?.Get("ui.assignment_cashier") ?? "Cashier";
                case StaffAssignment.Treatment:
                    return L?.Get("ui.assignment_treatment", data.assignedBedIndex + 1) ?? $"Treatment (Bed {data.assignedBedIndex + 1})";
                case StaffAssignment.Restock:
                    return L?.Get("ui.assignment_restock") ?? "Restock";
                default:
                    return L?.Get("ui.unknown") ?? "Unknown";
            }
        }
        
        private void OnFireButtonClicked()
        {
            onFireClicked?.Invoke(staffData);
        }
    }
}

