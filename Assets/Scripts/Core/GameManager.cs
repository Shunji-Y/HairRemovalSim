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
            StartPreparation();
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
            // TODO: Open Management UI
        }

        public void StartNextDay()
        {
            DayCount++;
            StartPreparation();
        }
        
        // Helper to get current time as 0.0 - 1.0
        public float GetNormalizedTime()
        {
            return Mathf.Clamp01(currentTimeOfDay / dayLengthSeconds);
        }
    }
}
