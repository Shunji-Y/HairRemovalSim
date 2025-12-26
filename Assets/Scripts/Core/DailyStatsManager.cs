using UnityEngine;
using System;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Tracks daily statistics for the summary panel.
    /// Resets at the start of each new day.
    /// </summary>
    public class DailyStatsManager : MonoBehaviour
    {
        public static DailyStatsManager Instance { get; private set; }
        
        // Daily tracking
        private int todayRevenue = 0;
        private int todayExpenses = 0;
        private int customersToday = 0;
        private int angryCustomersToday = 0;
        private int reviewSum = 0;
        private int reviewCount = 0;
        
        // Public accessors
        public int TodayRevenue => todayRevenue;
        public int TodayExpenses => todayExpenses;
        public int TodayProfit => todayRevenue - todayExpenses;
        public int CustomersToday => customersToday;
        public int AngryCustomersToday => angryCustomersToday;
        public float AverageReviewToday => reviewCount > 0 ? (float)reviewSum / reviewCount : 0f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to game events
            // GameEvents.OnShopOpened += OnNewDayStart; // Removed to prevent reset on shop open (fixes loan issue)
        }
        
        private void Start()
        {
            // Late bind to EconomyManager (may not be ready in OnEnable)
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;
                Debug.Log("[DailyStatsManager] Subscribed to EconomyManager.OnMoneyChanged");
            }
            else
            {
                Debug.LogWarning("[DailyStatsManager] EconomyManager.Instance is null in Start!");
            }
        }
        
        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
            }
            
            GameEvents.OnShopOpened -= OnNewDayStart;
        }
        
        private void OnMoneyChanged(int amount)
        {
            if (amount > 0)
            {
                todayRevenue += amount;
            }
            else
            {
                todayExpenses += Mathf.Abs(amount);
            }
        }
        
        /// <summary>
        /// Called when a customer spawns
        /// </summary>
        public void RecordCustomerSpawned()
        {
            customersToday++;
        }
        
        /// <summary>
        /// Called when a customer leaves angry
        /// </summary>
        public void RecordAngryCustomer()
        {
            angryCustomersToday++;
        }
        
        /// <summary>
        /// Called when a review is given
        /// </summary>
        public void RecordReview(int reviewValue)
        {
            reviewSum += reviewValue;
            reviewCount++;
        }
        
        private void OnNewDayStart()
        {
            ResetForNewDay();
        }
        
        /// <summary>
        /// Reset all daily stats for a new day
        /// </summary>
        public void ResetForNewDay()
        {
            todayRevenue = 0;
            todayExpenses = 0;
            customersToday = 0;
            angryCustomersToday = 0;
            reviewSum = 0;
            reviewCount = 0;
            
            Debug.Log("[DailyStatsManager] Reset for new day");
        }
    }
}
