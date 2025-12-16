using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Active loan - tracks parent loan data and generates daily payment cards
    /// </summary>
    [Serializable]
    public class ActiveLoan
    {
        [Header("Loan Reference")]
        public LoanData loanData;
        
        [Header("Loan State")]
        public int borrowedAmount;      // Original borrowed amount
        public int remainingPrincipal;  // Remaining principal to pay
        public int dailyPayment;        // Fixed daily payment amount
        public int totalRepayment;      // Total amount to repay
        
        [Header("Timeline")]
        public int startDay;            // Day when loan was taken
        public int termDays;            // Number of days to repay
        public int paidDays;            // Number of days paid so far
        public int generatedCards;      // Number of cards generated so far
        
        /// <summary>
        /// Create a new active loan
        /// </summary>
        public ActiveLoan(LoanData data, int amount, int currentDay)
        {
            loanData = data;
            borrowedAmount = amount;
            remainingPrincipal = amount;
            totalRepayment = data.CalculateTotalRepayment(amount);
            dailyPayment = data.CalculateDailyPayment(amount);
            
            startDay = currentDay;
            termDays = data.termDays;
            paidDays = 0;
            generatedCards = 0;
        }
        
        /// <summary>
        /// Get remaining days
        /// </summary>
        public int GetRemainingDays()
        {
            return Mathf.Max(0, termDays - paidDays);
        }
        
        /// <summary>
        /// Check if fully paid
        /// </summary>
        public bool IsFullyPaid()
        {
            return paidDays >= termDays || remainingPrincipal <= 0;
        }
        
        /// <summary>
        /// Check if term limit reached (no more cards should be generated)
        /// </summary>
        public bool HasReachedTermLimit()
        {
            return generatedCards >= termDays;
        }
        
        /// <summary>
        /// Record a payment
        /// </summary>
        public void RecordPayment()
        {
            paidDays++;
            int principalPortion = Mathf.CeilToInt((float)borrowedAmount / termDays);
            remainingPrincipal = Mathf.Max(0, remainingPrincipal - principalPortion);
        }
        
        /// <summary>
        /// Record that a card was generated
        /// </summary>
        public void RecordCardGenerated()
        {
            generatedCards++;
        }
    }
    
    /// <summary>
    /// Daily loan payment card - represents one day's payment
    /// Each card has 3-day grace period
    /// </summary>
    [Serializable]
    public class LoanPaymentCard
    {
        public const int GRACE_PERIOD_DAYS = 3;
        
        public ActiveLoan parentLoan;   // Reference to parent loan
        public int generatedDay;        // Day this card was generated
        public int dueDay;              // Day by which this must be paid
        public int baseAmount;          // Original payment amount
        public int lateFee;             // Accumulated late fee (compounds daily)
        public bool isPaid;             // Whether this card is paid
        public int paidDay;             // Day when paid (-1 if not paid)
        public bool isOverdue;          // Whether past due date
        public int lastLateFeeDay;      // Last day late fee was applied
        
        /// <summary>
        /// Total amount due (base + late fee)
        /// </summary>
        public int TotalAmount => baseAmount + lateFee;
        
        public LoanPaymentCard(ActiveLoan loan, int currentDay)
        {
            parentLoan = loan;
            generatedDay = currentDay;
            dueDay = currentDay + GRACE_PERIOD_DAYS;
            baseAmount = loan.dailyPayment;
            lateFee = 0;
            isPaid = false;
            paidDay = -1;
            isOverdue = false;
            lastLateFeeDay = -1;
        }
        
        public int GetDaysUntilDue(int currentDay)
        {
            return Mathf.Max(0, dueDay - currentDay);
        }
        
        public void MarkPaid(int currentDay)
        {
            isPaid = true;
            paidDay = currentDay;
            parentLoan.RecordPayment();
        }
        
        /// <summary>
        /// Apply compound late fee for a new day
        /// Called each day for overdue cards
        /// </summary>
        public void ApplyDailyLateFee(float lateFeeRate, int currentDay)
        {
            if (isPaid) return;
            if (currentDay <= dueDay) return; // Not overdue yet
            if (lastLateFeeDay >= currentDay) return; // Already applied today
            
            if (!isOverdue)
            {
                isOverdue = true;
            }
            
            // Compound interest: apply rate to (base + existing late fee)
            int currentTotal = baseAmount + lateFee;
            int dailyFee = Mathf.CeilToInt(currentTotal * lateFeeRate);
            lateFee += dailyFee;
            lastLateFeeDay = currentDay;
            
            Debug.Log($"[LoanPaymentCard] Compound late fee: +${dailyFee} (Total late fee: ${lateFee})");
        }
    }
}
