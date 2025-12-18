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
            int count = hiringConfig?.candidateCount ?? 6;
            
            // Get available ranks for current grade
            var availableRanks = GetAvailableRanks(shopGrade);
            if (availableRanks.Count == 0)
            {
                Debug.Log("[StaffCandidateGenerator] No ranks available at current grade");
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                var candidate = GenerateCandidate(availableRanks);
                currentCandidates.Add(candidate);
            }
            
            Debug.Log($"[StaffCandidateGenerator] Generated {count} candidates for grade {shopGrade}");
        }
        
        /// <summary>
        /// Get ranks available for given shop grade
        /// </summary>
        private List<StaffRankData> GetAvailableRanks(int shopGrade)
        {
            var available = new List<StaffRankData>();
            
            foreach (var rank in allRanks)
            {
                if (rank != null && rank.requiredGrade <= shopGrade)
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
