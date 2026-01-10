using UnityEngine;
using HairRemovalSim.UI;

namespace HairRemovalSim.Core
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        public int CurrentMoney { get; private set; } = 1000; // Starting money
        public int CurrentDebt { get; private set; } = 0;
        
        // Event for daily stats tracking (positive = revenue, negative = expense)
        public event System.Action<int> OnMoneyChanged;
        
        [Header("Fixed Costs (Every 3 Days)")]
        public int rent = 50000; // 家賃（3日ごと）
        public int laborCost = 0; // 人件費（スタッフ数に依存、3日ごと）
        public int utilities = 5000; // 水道光熱費（3日ごと）
        public float loanInterestRate = 0.05f; // ローン利率 (5%)
        public int paymentCycleDays = 3; // 支払いサイクル（日数）

        // No need to override Awake if just calling base, but Singleton handles it.
        // If we want to keep it simple, we can rely on base.Awake.

        public void AddMoney(int amount)
        {
            CurrentMoney += amount;
            Debug.Log($"Money Added: {amount}. Total: {CurrentMoney}");
            GameEvents.TriggerMoneyChanged(CurrentMoney);
            OnMoneyChanged?.Invoke(amount);
        }

        public bool SpendMoney(int amount)
        {
            if (CurrentMoney >= amount)
            {
                CurrentMoney -= amount;
                Debug.Log($"Money Spent: {amount}. Total: {CurrentMoney}");
                GameEvents.TriggerMoneyChanged(CurrentMoney);
                OnMoneyChanged?.Invoke(-amount); // Negative for expenses
                return true;
            }
            return false;
        }

        public void TakeLoan(int amount)
        {
            CurrentMoney += amount;
            CurrentDebt += amount;
            GameEvents.TriggerMoneyChanged(CurrentMoney);
        }
        
        /// <summary>
        /// Alias for save/load compatibility
        /// </summary>
        public int LoanRemaining => CurrentDebt;
        
        /// <summary>
        /// Set money directly (for save/load)
        /// </summary>
        public void SetMoney(int amount)
        {
            CurrentMoney = Mathf.Max(0, amount);
            GameEvents.TriggerMoneyChanged(CurrentMoney);
            Debug.Log($"[EconomyManager] Money set to {CurrentMoney}");
        }
        
        /// <summary>
        /// Set loan directly (for save/load)
        /// </summary>
        public void SetLoan(int amount)
        {
            CurrentDebt = Mathf.Max(0, amount);
            Debug.Log($"[EconomyManager] Debt set to {CurrentDebt}");
        }
    }
}
