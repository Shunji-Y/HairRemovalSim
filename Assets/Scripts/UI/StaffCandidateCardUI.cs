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
            
            // Prefer sourceProfileData if available, otherwise use candidate fields
            var profileData = candidate.sourceProfileData;
            
            // Set photo
            Sprite photoSprite = profileData != null ? profileData.portrait : candidate.photo;
            if (photoImage != null && photoSprite != null)
            {
                photoImage.sprite = photoSprite;
                photoImage.enabled = true;
            }
            else if (photoImage != null)
            {
                photoImage.enabled = false;
            }
            
            // Set name (prefer StaffProfileData.staffName)
            string displayName = profileData != null ? profileData.staffName : candidate.displayName;
            if (nameText != null)
                nameText.text = displayName;
            
            // Set rank (prefer StaffProfileData.Rank)
            string rankDisplay = profileData != null ? profileData.rankData?.GetDisplayName() : candidate.GetRankDisplayName();
            if (rankText != null)
                rankText.text = L?.Get("ui.staff_rank", rankDisplay) ?? $"Rank: {rankDisplay}";
            
            // Set daily cost (prefer StaffProfileData.DailySalary)
            int salary = profileData != null ? profileData.DailySalary : candidate.DailySalary;
            if (costText != null)
                costText.text = L?.Get("ui.staff_daily_cost", salary) ?? $"Cost/Day: ¥{salary:N0}";
            
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
