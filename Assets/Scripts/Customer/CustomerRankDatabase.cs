using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Database containing all 30 customer ranks with lookup methods
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerRankDatabase", menuName = "HairRemovalSim/Customer Rank Database")]
    public class CustomerRankDatabase : ScriptableObject
    {
        [Header("All Customer Ranks (30 total)")]
        [Tooltip("All customer ranks, ordered by star level")]
        public List<CustomerRankData> allRanks = new List<CustomerRankData>();
        
        // Cache for quick lookups
        private Dictionary<int, CustomerRankData> _starLevelCache;
        private Dictionary<CustomerTier, List<CustomerRankData>> _tierCache;
        
        private void OnEnable()
        {
            BuildCache();
        }
        
        private void BuildCache()
        {
            _starLevelCache = new Dictionary<int, CustomerRankData>();
            _tierCache = new Dictionary<CustomerTier, List<CustomerRankData>>();
            
            foreach (CustomerTier tier in System.Enum.GetValues(typeof(CustomerTier)))
            {
                _tierCache[tier] = new List<CustomerRankData>();
            }
            
            foreach (var rank in allRanks)
            {
                if (rank == null) continue;
                _starLevelCache[rank.requiredStarLevel] = rank;
                _tierCache[rank.tier].Add(rank);
            }
        }
        
        /// <summary>
        /// Get rank by star level (1-30)
        /// </summary>
        public CustomerRankData GetRankByStarLevel(int starLevel)
        {
            if (_starLevelCache == null) BuildCache();
            return _starLevelCache.TryGetValue(starLevel, out var rank) ? rank : null;
        }
        
        /// <summary>
        /// Get all ranks for a tier
        /// </summary>
        public List<CustomerRankData> GetRanksForTier(CustomerTier tier)
        {
            if (_tierCache == null) BuildCache();
            return _tierCache.TryGetValue(tier, out var ranks) ? ranks : new List<CustomerRankData>();
        }
        
        /// <summary>
        /// Get all unlocked ranks for current star level and grade
        /// </summary>
        public List<CustomerRankData> GetUnlockedRanks(int starLevel, int grade)
        {
            if (_starLevelCache == null) BuildCache();
            
            return allRanks.Where(r => r != null && r.IsUnlocked(starLevel, grade)).ToList();
        }
        
        /// <summary>
        /// Get all unlocked ranks within a specific tier
        /// </summary>
        public List<CustomerRankData> GetUnlockedRanksInTier(CustomerTier tier, int starLevel, int grade)
        {
            int requiredGrade = CustomerRankData.GetRequiredGradeForTier(tier);
            if (grade < requiredGrade) return new List<CustomerRankData>();
            
            return GetRanksForTier(tier).Where(r => r.requiredStarLevel <= starLevel).ToList();
        }
        
        /// <summary>
        /// Get highest unlocked tier for current grade
        /// </summary>
        public CustomerTier GetHighestUnlockedTier(int grade)
        {
            if (grade >= 6) return CustomerTier.Richest;
            if (grade >= 5) return CustomerTier.Rich;
            if (grade >= 4) return CustomerTier.Normal;
            if (grade >= 2) return CustomerTier.Poor;
            return CustomerTier.Poorest;
        }
        
        /// <summary>
        /// Get a random customer rank from unlocked ranks (weighted by tier)
        /// Higher tiers have higher chance when unlocked
        /// </summary>
        public CustomerRankData GetRandomUnlockedRank(int starLevel, int grade)
        {
            var unlockedRanks = GetUnlockedRanks(starLevel, grade);
            if (unlockedRanks.Count == 0) 
            {
                Debug.LogWarning($"[CustomerRankDatabase] No unlocked ranks for Star:{starLevel}, Grade:{grade}");
                return null;
            }
            
            // Get ranks from highest unlocked tier
            CustomerTier highestTier = GetHighestUnlockedTier(grade);
            var tierRanks = GetUnlockedRanksInTier(highestTier, starLevel, grade);
            
            if (tierRanks.Count == 0)
            {
                tierRanks = unlockedRanks;
            }
            
            // Weighted selection: higher requiredStarLevel = higher weight
            // Weight = requiredStarLevel^2 (so ★3 has 9 weight, ★1 has 1 weight)
            // This ensures newer ranks appear more often while older ranks still have chances
            float totalWeight = 0f;
            foreach (var rank in tierRanks)
            {
                totalWeight += rank.requiredStarLevel * rank.requiredStarLevel;
            }
            
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            
            CustomerRankData selected = tierRanks[tierRanks.Count - 1]; // Fallback to last
            foreach (var rank in tierRanks)
            {
                cumulative += rank.requiredStarLevel * rank.requiredStarLevel;
                if (roll <= cumulative)
                {
                    selected = rank;
                    break;
                }
            }
            
            Debug.Log($"[CustomerRankDatabase] Star:{starLevel}, Grade:{grade} -> Selected: {selected.rankName} (★{selected.requiredStarLevel}) from {tierRanks.Count} ranks");
            return selected;
        }
        
        /// <summary>
        /// Get random rank from a specific tier (equal probability for each sublevel)
        /// </summary>
        public CustomerRankData GetRandomRankFromTier(CustomerTier tier, int starLevel, int grade)
        {
            var tierRanks = GetUnlockedRanksInTier(tier, starLevel, grade);
            if (tierRanks.Count == 0) return null;
            return tierRanks[Random.Range(0, tierRanks.Count)];
        }
    }
}
