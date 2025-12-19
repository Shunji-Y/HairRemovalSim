using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Staff;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI card for a staff candidate in the hire panel.
    /// Shows name, photo, rank, daily cost, and hire button.
    /// </summary>
    public class StaffCandidateCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image photoImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button hireButton;
        
        [Header("Rank Stars")]
        [SerializeField] private Transform starContainer;
        [SerializeField] private GameObject starPrefab;
        
        private StaffProfile candidate;
        private System.Action<StaffProfile> onHireClicked;
        
        // Shorthand for localization
        private LocalizationManager L => LocalizationManager.Instance;
        
        /// <summary>
        /// Setup the card with candidate data
        /// </summary>
        public void Setup(StaffProfile staffCandidate, System.Action<StaffProfile> hireCallback)
        {
            candidate = staffCandidate;
            onHireClicked = hireCallback;
            
            // Set photo
            if (photoImage != null && candidate.photo != null)
            {
                photoImage.sprite = candidate.photo;
                photoImage.enabled = true;
            }
            else if (photoImage != null)
            {
                photoImage.enabled = false;
            }
            
            // Set name
            if (nameText != null)
                nameText.text = candidate.displayName;
            
            // Set rank (localized)
            if (rankText != null)
                rankText.text = L?.Get("ui.staff_rank", candidate.GetRankDisplayName()) ?? $"Rank: {candidate.GetRankDisplayName()}";
            
            // Set daily cost
            if (costText != null)
                costText.text = L?.Get("ui.staff_daily_cost", candidate.DailySalary) ?? $"Cost/Day: ¥{candidate.DailySalary:N0}";
            
            // Setup stars based on rank
            SetupRankStars();
            
            // Setup hire button
            if (hireButton != null)
            {
                hireButton.onClick.RemoveAllListeners();
                hireButton.onClick.AddListener(OnHireButtonClicked);
                
                // Check if can hire more
                UpdateHireButtonState();
            }
        }
        
        private void SetupRankStars()
        {
            if (starContainer == null || starPrefab == null || candidate?.rankData == null)
                return;
            
            // Clear existing stars
            foreach (Transform child in starContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Stars based on rank level (College=1, NewGrad=2, MidCareer=3, Veteran=4, Professional=5)
            int starCount = (int)candidate.rankData.rank + 1;
            
            for (int i = 0; i < starCount; i++)
            {
                Instantiate(starPrefab, starContainer);
            }
        }
        
        private void UpdateHireButtonState()
        {
            if (hireButton == null) return;
            
            bool canHire = CanHireMore();
            hireButton.interactable = canHire;
        }
        
        private bool CanHireMore()
        {
            var config = StaffCandidateGenerator.Instance?.HiringConfig;
            if (config == null) return false;
            
            int shopGrade = Core.ShopManager.Instance?.ShopGrade ?? 1;
            int currentStaff = StaffManager.Instance?.HiredStaffCount ?? 0;
            
            return config.CanHireMore(currentStaff, shopGrade);
        }
        
        private void OnHireButtonClicked()
        {
            Debug.Log($"[StaffCandidateCardUI] Hire button clicked for {candidate?.displayName}");
            onHireClicked?.Invoke(candidate);
        }
    }
}
