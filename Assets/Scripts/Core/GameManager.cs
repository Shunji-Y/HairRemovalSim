using UnityEngine;
using HairRemovalSim.UI;

namespace HairRemovalSim.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public enum GameState
        {
            Preparation, // Before 9:00
            Day,         // 9:00 - 19:00 (Treatment)
            Night        // 19:00 - (Management)
        }

        public GameState CurrentState { get; private set; }
        public int DayCount { get; private set; } = 1;
        public int CurrentDay => DayCount; // Alias for SaveManager
        
        // Events for TutorialManager and other systems
        public event System.Action<int> OnDayStarted;
        public event System.Action<int> OnDayEnded;

        [Header("Settings")]
        [Tooltip("Length of one game day in real seconds (e.g., 600 = 10 mins)")]
        public float dayLengthSeconds = 600f;

        private float currentTimeOfDay;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Subscribe to loan game over event
            if (LoanManager.Instance != null)
            {
                LoanManager.Instance.OnGameOverDueToDebt += OnLoanGameOver;
            }
            
            StartPreparation();
        }
        
        private void OnLoanGameOver()
        {
            Debug.Log("[GameManager] GAME OVER - Debt collection!");
            // TODO: Show game over screen
            CurrentState = GameState.Preparation;
        }

        private void Update()
        {
            if (CurrentState == GameState.Day)
            {
                UpdateDayTimer();
            }
        }

        public void StartPreparation()
        {
            CurrentState = GameState.Preparation;
            currentTimeOfDay = 0f;
            Debug.Log($"Day {DayCount} Preparation Phase. Waiting for Open.");
            
            GameEvents.TriggerDayChanged(DayCount);
            GameEvents.TriggerTimeUpdated(0f); // 9:00 AM
            
            // Process unpaid loans (mark as overdue if past due)
            if (LoanManager.Instance != null)
            {
                LoanManager.Instance.ProcessDayStart(DayCount);
            }
            
            // Process rent (check for overdue)
            if (RentManager.Instance != null)
            {
                RentManager.Instance.ProcessDayStart(DayCount);
            }
            
            // Process ads (cleanup expired)
            if (AdvertisingManager.Instance != null)
            {
                AdvertisingManager.Instance.ProcessDayStart();
            }
            

            // Refresh payment panel
            bool hasLoans = LoanManager.Instance != null && LoanManager.Instance.HasPaymentsDue();
            bool hasRent = RentManager.Instance != null && RentManager.Instance.HasPendingPayment(DayCount);
            
            if (hasLoans || hasRent)
            {
                UI.PaymentListPanel.Instance?.RefreshDisplay();
            }

            // Tutorial triggers based on day
            if (DayCount == 1)
            {
                // Day 1: Salon open tutorial
                TutorialManager.Instance?.TryShowTutorial("tut_salon_open");
            }
            else
            {
                // Day 2+: Good morning message (persistent until door is used)
                MessageBoxManager.Instance?.ShowMessage("msg.good_morning", MessageType.Info, true, "msg_good_morning");
                
                if (DayCount == 2)
                {
                    // Day 2: Bank and Ad tutorials
                    TutorialManager.Instance?.TryShowTutorial("tut_bank_open");
                    TutorialManager.Instance?.TryShowTutorial("tut_ad_open");
                }
            }

            SoundManager.Instance.PlayAmbient("bgm_city");
        }

        public void OpenShop()
        {
            if (CurrentState != GameState.Preparation) return;

            StartDay();
        }

        public void StartDay()
        {
            CurrentState = GameState.Day;
            Debug.Log($"Shop Opened! Day {DayCount} Started (9:00 AM).");
            
            GameEvents.TriggerShopOpened();
            
            // Notify tutorial and other systems
            OnDayStarted?.Invoke(DayCount);
        }

        private void UpdateDayTimer()
        {
            currentTimeOfDay += Time.deltaTime;
            GameEvents.TriggerTimeUpdated(GetNormalizedTime());
            
            if (currentTimeOfDay >= dayLengthSeconds)
            {
                EndDay();
            }
        }

        private void EndDay()
        {
            CurrentState = GameState.Night;
            Debug.Log("Day Ended (19:00 PM). Entering Management Phase.");
            
            GameEvents.TriggerShopClosed();
            
            // Tutorial triggers based on day
            if (DayCount == 1)
            {
                // Day 1: Cleaning and day end tutorials
                TutorialManager.Instance?.TryShowTutorial("tut_day_end");

                TutorialManager.Instance?.TryShowTutorial("tut_cleaning");
            }
            else
            {
                // Day 2+: Day end message (persistent until DailySummaryPanel shows)
                MessageBoxManager.Instance?.ShowMessage("msg.day_end", MessageType.Info, true, "msg_day_end");
            }
            
            // Notify tutorial and other systems
            OnDayEnded?.Invoke(DayCount);
        }

        public void StartNextDay()
        {
            // Clear any lingering messages from previous day
        //    UI.MessageBoxManager.Instance?.ClearAllMessages();
            
            DayCount++;
            
            // Deliver pending orders from previous day
            if (Store.InventoryManager.Instance != null)
            {
                Store.InventoryManager.Instance.ProcessPendingOrders();
            }


            StartPreparation();
        }
        
        /// <summary>
        /// Set day count (for save/load)
        /// </summary>
        public void SetDay(int day)
        {
            DayCount = Mathf.Max(1, day);
            Debug.Log($"[GameManager] Day set to {DayCount}");
        }
        
        // Helper to get current time as 0.0 - 1.0
        public float GetNormalizedTime()
        {
            return Mathf.Clamp01(currentTimeOfDay / dayLengthSeconds);
        }
        
        /// <summary>
        /// Set game time for debug purposes (hours: 10-19)
        /// </summary>
        public void SetTimeForDebug(float hourOfDay)
        {
            // Convert hour to normalized time (10:00 = 0, 19:00 = 1)
            float normalizedTime = Mathf.InverseLerp(10f, 19f, hourOfDay);
            currentTimeOfDay = normalizedTime * dayLengthSeconds;
            
            if (hourOfDay >= 19f)
            {
                EndDay();
            }
            
            GameEvents.TriggerTimeUpdated(GetNormalizedTime());
            Debug.Log($"[GameManager] Time set to {hourOfDay}:00 (normalized: {normalizedTime:F2})");
        }
    }
}
