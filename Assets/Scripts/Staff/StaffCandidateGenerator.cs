using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Generates random staff candidates for hiring.
    /// Filters by current shop grade.
    /// </summary>
    public class StaffCandidateGenerator : MonoBehaviour
    {
        public static StaffCandidateGenerator Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private StaffHiringConfig hiringConfig;
        
        [Header("Available Ranks")]
        [Tooltip("All available StaffRankData assets")]
        [SerializeField] private StaffRankData[] allRanks;
        
        [Header("Name Generation")]
        [SerializeField] private string[] firstNames = {
            "Yuki", "Kenji", "Emi", "Takeshi", "Sakura", "Hiroshi",
            "Mika", "Daichi", "Aoi", "Ryo", "Hana", "Satoshi"
        };
        [SerializeField] private string[] lastNames = {
            "Tanaka", "Sato", "Suzuki", "Takahashi", "Watanabe", "Yamamoto",
            "Nakamura", "Kobayashi", "Ito", "Kato", "Yoshida", "Yamada"
        };
        
        [Header("Photos")]
        [Tooltip("Available staff photos")]
        [SerializeField] private Sprite[] staffPhotos;
        
        // Current candidates
        private List<StaffProfile> currentCandidates = new List<StaffProfile>();
        private int lastRefreshDay = -1;
        
        public List<StaffProfile> CurrentCandidates => currentCandidates;
        public StaffHiringConfig HiringConfig => hiringConfig;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            // Subscribe to day change event
            GameEvents.OnDayChanged += OnDayChanged;
            RefreshCandidates();
        }
        
        private void OnDestroy()
        {
            GameEvents.OnDayChanged -= OnDayChanged;
        }
        
        private void OnDayChanged(int newDay)
        {
            RefreshCandidates();
            lastRefreshDay = newDay;
            Debug.Log($"[StaffCandidateGenerator] Refreshed candidates for day {newDay}");
        }
        
        /// <summary>
        /// Refresh candidates if needed (called on day change)
        /// </summary>
        public void CheckRefresh(int currentDay)
        {
            if (currentDay != lastRefreshDay)
            {
                RefreshCandidates();
                lastRefreshDay = currentDay;
            }
        }
        
        /// <summary>
        /// Force refresh candidates
        /// </summary>
        public void RefreshCandidates()
        {
            currentCandidates.Clear();
            
            int shopGrade = ShopManager.Instance?.ShopGrade ?? 1;
            
            if (StaffManager.Instance == null || StaffManager.Instance.availableProfiles == null)
            {
                Debug.LogWarning("[StaffCandidateGenerator] StaffManager or availableProfiles not found");
                return;
            }
            
            // Get candidate count based on grade
            int count = GetCandidateCountForGrade(shopGrade);
            
            // Get available profiles that match current grade restrictions
            var availableProfiles = GetAvailableProfilesForGrade(shopGrade);
            if (availableProfiles.Count == 0)
            {
                Debug.Log("[StaffCandidateGenerator] No profiles available at current grade");
                return;
            }
            
            // Shuffle profiles and take up to 'count' candidates
            ShuffleList(availableProfiles);
            
            for (int i = 0; i < Mathf.Min(count, availableProfiles.Count); i++)
            {
                var profileData = availableProfiles[i];
                var candidate = CreateCandidateFromProfileData(profileData);
                currentCandidates.Add(candidate);
            }
            
            Debug.Log($"[StaffCandidateGenerator] Generated {currentCandidates.Count} candidates for grade {shopGrade}");
        }
        
        /// <summary>
        /// Get profiles available at current shop grade
        /// </summary>
        private List<StaffProfileData> GetAvailableProfilesForGrade(int shopGrade)
        {
            var available = new List<StaffProfileData>();
            StaffRank maxRank = GetMaxRankForGrade(shopGrade);
            
            foreach (var profile in StaffManager.Instance.availableProfiles)
            {
                if (profile == null) continue;
                if (profile.Rank <= maxRank)
                {
                    available.Add(profile);
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Create a StaffProfile candidate from StaffProfileData
        /// </summary>
        private StaffProfile CreateCandidateFromProfileData(StaffProfileData profileData)
        {
            return new StaffProfile
            {
                staffId = profileData.staffId,
                displayName = profileData.staffName,
                photo = profileData.portrait,
                rankData = profileData.rankData,
                sourceProfileData = profileData,  // KEY: Link back to the ScriptableObject
                isHired = false,
                assignment = StaffAssignment.None
            };
        }
        
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        /// <summary>
        /// Get number of candidate cards for shop grade
        /// Grade 2: 1, Grade 3: 2, Grade 4: 3, Grade 5: 4, Grade 6+: 5
        /// </summary>
        private int GetCandidateCountForGrade(int shopGrade)
        {
            switch (shopGrade)
            {
                case 1: return 0; // No hiring at grade 1
                case 2: return 1;
                case 3: return 2;
                case 4: return 3;
                case 5: return 4;
                default: return 5; // Grade 6+
            }
        }
        
        /// <summary>
        /// Get max rank allowed for shop grade
        /// Grade 2: College only
        /// Grade 3: up to NewGrad
        /// Grade 4: up to MidCareer
        /// Grade 5: up to Veteran
        /// Grade 6+: All (including Professional)
        /// </summary>
        private StaffRank GetMaxRankForGrade(int shopGrade)
        {
            switch (shopGrade)
            {
                case 1: 
                case 2: return StaffRank.College;
                case 3: return StaffRank.NewGrad;
                case 4: return StaffRank.MidCareer;
                case 5: return StaffRank.Veteran;
                default: return StaffRank.Professional; // Grade 6+
            }
        }
        
        /// <summary>
        /// Get ranks available for given shop grade
        /// </summary>
        private List<StaffRankData> GetAvailableRanks(int shopGrade)
        {
            var available = new List<StaffRankData>();
            StaffRank maxRank = GetMaxRankForGrade(shopGrade);
            
            foreach (var rank in allRanks)
            {
                if (rank != null && rank.rank <= maxRank)
                {
                    available.Add(rank);
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Generate a single candidate
        /// </summary>
        private StaffProfile GenerateCandidate(List<StaffRankData> availableRanks)
        {
            var profile = new StaffProfile
            {
                staffId = System.Guid.NewGuid().ToString(),
                displayName = GenerateRandomName(),
                photo = GetRandomPhoto(),
                rankData = availableRanks[Random.Range(0, availableRanks.Count)],
                isHired = false,
                assignment = StaffAssignment.None
            };
            
            return profile;
        }
        
        private string GenerateRandomName()
        {
            string firstName = firstNames[Random.Range(0, firstNames.Length)];
            string lastName = lastNames[Random.Range(0, lastNames.Length)];
            return $"{firstName} {lastName}";
        }
        
        private Sprite GetRandomPhoto()
        {
            if (staffPhotos == null || staffPhotos.Length == 0) return null;
            return staffPhotos[Random.Range(0, staffPhotos.Length)];
        }
        
        /// <summary>
        /// Remove candidate from list (after hiring)
        /// </summary>
        public void RemoveCandidate(StaffProfile candidate)
        {
            currentCandidates.Remove(candidate);
        }
    }
}
