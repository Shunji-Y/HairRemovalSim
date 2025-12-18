using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Staff;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Dialog for selecting staff assignment when hiring.
    /// Shows 4 assignment buttons and confirm/cancel.
    /// </summary>
    public class StaffHireDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text candidateInfoText;
        
        [Header("Assignment Buttons")]
        [SerializeField] private Button receptionButton;
        [SerializeField] private Button cashierButton;
        [SerializeField] private Button treatmentButton;
        [SerializeField] private Button restockButton;
        
        [Header("Action Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color selectedColor = new Color(0.6f, 1f, 0.6f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        private StaffProfile currentCandidate;
        private StaffAssignment selectedAssignment = StaffAssignment.None;
        private int selectedBedIndex = -1;
        
        private System.Action<StaffProfile, StaffAssignment, int> onConfirm;
        private System.Action onCancel;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Start()
        {
            // Setup button listeners
            if (receptionButton != null)
                receptionButton.onClick.AddListener(() => SelectAssignment(StaffAssignment.Reception));
            if (cashierButton != null)
                cashierButton.onClick.AddListener(() => SelectAssignment(StaffAssignment.Cashier));
            if (treatmentButton != null)
                treatmentButton.onClick.AddListener(() => SelectAssignment(StaffAssignment.Treatment));
            if (restockButton != null)
                restockButton.onClick.AddListener(() => SelectAssignment(StaffAssignment.Restock));
            
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Show the dialog for a candidate
        /// </summary>
        public void Show(StaffProfile candidate, 
                        System.Action<StaffProfile, StaffAssignment, int> confirmCallback,
                        System.Action cancelCallback)
        {
            currentCandidate = candidate;
            onConfirm = confirmCallback;
            onCancel = cancelCallback;
            selectedAssignment = StaffAssignment.None;
            selectedBedIndex = -1;
            
            // Set candidate info
            if (titleText != null)
                titleText.text = L?.Get("ui.select_assignment") ?? "Select Assignment";
            
            if (candidateInfoText != null)
                candidateInfoText.text = $"{candidate.displayName}\n{candidate.GetRankDisplayName()}\n{L?.Get("ui.per_day", candidate.DailySalary) ?? $"¥{candidate.DailySalary:N0}/day"}";
            
            // Update button states based on availability
            UpdateAssignmentButtons();
            UpdateConfirmButton();
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Update button interactability based on position availability
        /// </summary>
        private void UpdateAssignmentButtons()
        {
            if (StaffManager.Instance == null) return;
            
            // Reception - only one allowed
            bool receptionAvailable = !StaffManager.Instance.IsPositionOccupied(StaffAssignment.Reception, -1);
            SetButtonState(receptionButton, receptionAvailable);
            
            // Cashier - only one allowed
            bool cashierAvailable = !StaffManager.Instance.IsPositionOccupied(StaffAssignment.Cashier, -1);
            SetButtonState(cashierButton, cashierAvailable);
            
            // Treatment - auto-select first available bed
            int availableBed = StaffManager.Instance.GetFirstAvailableBedIndex();
            bool treatmentAvailable = availableBed >= 0;
            SetButtonState(treatmentButton, treatmentAvailable);
            
            // Restock - only one allowed
            bool restockAvailable = !StaffManager.Instance.IsPositionOccupied(StaffAssignment.Restock, -1);
            SetButtonState(restockButton, restockAvailable);
        }
        
        private void SetButtonState(Button button, bool available)
        {
            if (button == null) return;
            
            button.interactable = available;
            
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = available ? normalColor : disabledColor;
            }
        }
        
        /// <summary>
        /// Select an assignment
        /// </summary>
        private void SelectAssignment(StaffAssignment assignment)
        {
            selectedAssignment = assignment;
            
            // For treatment, auto-select first available bed
            if (assignment == StaffAssignment.Treatment)
            {
                selectedBedIndex = StaffManager.Instance?.GetFirstAvailableBedIndex() ?? -1;
            }
            else
            {
                selectedBedIndex = -1;
            }
            
            // Update visual feedback
            UpdateButtonVisuals();
            UpdateConfirmButton();
        }
        
        private void UpdateButtonVisuals()
        {
            SetButtonSelected(receptionButton, selectedAssignment == StaffAssignment.Reception);
            SetButtonSelected(cashierButton, selectedAssignment == StaffAssignment.Cashier);
            SetButtonSelected(treatmentButton, selectedAssignment == StaffAssignment.Treatment);
            SetButtonSelected(restockButton, selectedAssignment == StaffAssignment.Restock);
        }
        
        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null || !button.interactable) return;
            
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? selectedColor : normalColor;
            }
        }
        
        private void UpdateConfirmButton()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = selectedAssignment != StaffAssignment.None;
            }
        }
        
        private void OnConfirmClicked()
        {
            if (selectedAssignment == StaffAssignment.None) return;
            
            gameObject.SetActive(false);
            onConfirm?.Invoke(currentCandidate, selectedAssignment, selectedBedIndex);
        }
        
        private void OnCancelClicked()
        {
            gameObject.SetActive(false);
            onCancel?.Invoke();
        }
    }
}
