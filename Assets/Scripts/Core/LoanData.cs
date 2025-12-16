using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Loan type definition - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewLoan", menuName = "HairRemovalSim/Loan Data")]
    public class LoanData : ScriptableObject
    {
        [Header("Basic Info")]
        public string loanId;
        public string displayName;
        public Sprite icon;
        
        [Header("Loan Terms")]
        [Tooltip("Maximum amount that can be borrowed")]
        public int maxAmount = 100000;
        
        [Tooltip("Daily interest rate (e.g., 0.001 = 0.1% per day)")]
        [Range(0.0001f, 0.01f)]
        public float dailyInterestRate = 0.001f;
        
        [Tooltip("Loan term in days")]
        public int termDays = 30;
        
        [Header("Display Info")]
        [TextArea]
        public string description;
        
        /// <summary>
        /// Calculate daily payment for a given borrowed amount (simple interest, daily)
        /// </summary>
        public int CalculateDailyPayment(int borrowedAmount)
        {
            // Total repayment = Principal + Interest
            // Interest = Principal × Rate × Days
            float totalInterest = borrowedAmount * dailyInterestRate * termDays;
            float totalRepayment = borrowedAmount + totalInterest;
            
            // Daily payment = Total / Days (rounded up)
            return Mathf.CeilToInt(totalRepayment / termDays);
        }
        
        /// <summary>
        /// Calculate total repayment amount
        /// </summary>
        public int CalculateTotalRepayment(int borrowedAmount)
        {
            float totalInterest = borrowedAmount * dailyInterestRate * termDays;
            return Mathf.CeilToInt(borrowedAmount + totalInterest);
        }
    }
}
