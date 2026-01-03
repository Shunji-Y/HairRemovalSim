using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Generates random staff candidates for hiring.
    /// Filters by current star level.
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
            
            int currentStarLevel = ShopManager.Instance?.StarRating ?? 1;
            
            if (StaffManager.Instance == null || StaffManager.Instance.availableProfiles == null)
            {
                Debug.LogWarning("[StaffCandidateGenerator] StaffManager or availableProfiles not found");
                return;
            }
            
            // Get candidate count based on star level
            int count = GetCandidateCountForStarLevel(currentStarLevel);
            
            // Get available profiles that match current star level restrictions
            var availableProfiles = GetAvailableProfilesForStarLevel(currentStarLevel);
            if (availableProfiles.Count == 0)
            {
                Debug.Log("[StaffCandidateGenerator] No profiles available at current star level");
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
            
            Debug.Log($"[StaffCandidateGenerator] Generated {currentCandidates.Count} candidates for star level {currentStarLevel}");
        }
        
        /// <summary>
        /// Get profiles available at current star level
        /// </summary>
        private List<StaffProfileData> GetAvailableProfilesForStarLevel(int currentStarLevel)
        {
            var available = new List<StaffProfileData>();
            
            foreach (var profile in StaffManager.Instance.availableProfiles)
            {
                if (profile == null || profile.rankData == null) continue;
                
                // Check if this rank is unlocked at current star level
                if (profile.rankData.requiredStarLevel <= currentStarLevel)
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
        /// Get number of candidate cards based on star level
        /// ★1-3: 0, ★4-7: 1, ★8-13: 2, ★14-18: 3, ★19-24: 4, ★25+: 5
        /// </summary>
        private int GetCandidateCountForStarLevel(int starLevel)
        {
            if (starLevel < 4) return 0;     // No hiring below ★4
            if (starLevel < 8) return 1;     // ★4-7: 1 candidate
            if (starLevel < 14) return 2;    // ★8-13: 2 candidates
            if (starLevel < 19) return 3;    // ★14-18: 3 candidates
            if (starLevel < 25) return 4;    // ★19-24: 4 candidates
            return 5;                        // ★25+: 5 candidates
        }
        
        /// <summary>
        /// Get ranks available for given star level (using requiredStarLevel)
        /// </summary>
        private List<StaffRankData> GetAvailableRanks(int starLevel)
        {
            var available = new List<StaffRankData>();
            
            foreach (var rank in allRanks)
            {
                if (rank != null && rank.requiredStarLevel <= starLevel)
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
