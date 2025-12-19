using System;

namespace HairRemovalSim.Core
{
    public static class GameEvents
    {
        // Economy Events
        public static event Action<int> OnMoneyChanged;
        public static void TriggerMoneyChanged(int newAmount) => OnMoneyChanged?.Invoke(newAmount);

        // Time/Day Events
        public static event Action<int> OnDayChanged;
        public static void TriggerDayChanged(int newDay) => OnDayChanged?.Invoke(newDay);

        public static event Action<float> OnTimeUpdated;
        public static void TriggerTimeUpdated(float normalizedTime) => OnTimeUpdated?.Invoke(normalizedTime);
        
        // Shop Events
        public static event Action OnShopOpened;
        public static void TriggerShopOpened() => OnShopOpened?.Invoke();
        
        public static event Action OnShopClosed;
        public static void TriggerShopClosed() => OnShopClosed?.Invoke();
    }
}
