using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages loans and daily payment cards
    /// - Each day generates a new payment card for each active loan
    /// - Each card has 3-day grace period
    /// - 3 overdue cards = game over
    /// </summary>
    public class LoanManager : Singleton<LoanManager>
    {
        [Header("Settings")]
        [Tooltip("Number of overdue cards before game over")]
        [SerializeField] private int maxOverdueCards = 3;
        
        [Tooltip("Late fee rate (e.g. 0.1 = 10% of daily payment)")]
        [SerializeField] private float lateFeeRate = 0.1f;
        
        [Tooltip("Prepayment fee rate (e.g. 0.05 = 5% of remaining principal)")]
        [SerializeField] private float prepaymentFeeRate = 0.05f;
        
        [Header("Available Loans")]
        [SerializeField] private List<LoanData> availableLoans = new List<LoanData>();
        
        // Active loans (parent loans)
        private List<ActiveLoan> activeLoans = new List<ActiveLoan>();
        
        // Daily payment cards
        private List<LoanPaymentCard> paymentCards = new List<LoanPaymentCard>();
        
        // Events
        public System.Action<ActiveLoan> OnLoanTaken;
        public System.Action<ActiveLoan> OnLoanPaid;
        public System.Action<LoanPaymentCard> OnCardGenerated;
        public System.Action<LoanPaymentCard> OnCardPaid;
        public System.Action<LoanPaymentCard> OnCardOverdue;
        public System.Action OnGameOverDueToDebt;
        
        public List<LoanData> AvailableLoans => availableLoans;
        public List<ActiveLoan> ActiveLoans => activeLoans;
        public List<LoanPaymentCard> PaymentCards => paymentCards;
        public int MaxOverdueCards => maxOverdueCards;
        
        /// <summary>
        /// Get count of overdue cards
        /// </summary>
        public int GetOverdueCount()
        {
            return paymentCards.Count(c => c.isOverdue && !c.isPaid);
        }
        
        /// <summary>
        /// Get all unpaid cards
        /// </summary>
        public List<LoanPaymentCard> GetUnpaidCards()
        {
            return paymentCards.Where(c => !c.isPaid).ToList();
        }
        
        /// <summary>
        /// Check if there are any payments due
        /// </summary>
        public bool HasPaymentsDue()
        {
            return paymentCards.Any(c => !c.isPaid);
        }
        
        /// <summary>
        /// Check if a specific loan type is already active
        /// </summary>
        public bool HasActiveLoan(string loanId)
        {
            return activeLoans.Any(l => l.loanData.loanId == loanId && !l.IsFullyPaid());
        }
        
        /// <summary>
        /// Take a new loan
        /// </summary>
        public bool TakeLoan(LoanData loanData, int amount)
        {
            if (loanData == null || amount <= 0) return false;
            if (amount > loanData.maxAmount) amount = loanData.maxAmount;
            if (HasActiveLoan(loanData.loanId)) return false;
            
            int currentDay = GameManager.Instance != null ? GameManager.Instance.DayCount : 1;
            
            var newLoan = new ActiveLoan(loanData, amount, currentDay);
            activeLoans.Add(newLoan);
            
            // Add money to economy
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(amount);
            }
            
            // Generate first payment card immediately
            GeneratePaymentCard(newLoan, currentDay);
            
            Debug.Log($"[LoanManager] Loan taken: {loanData.displayName}, Amount: ${amount}, Daily: ${newLoan.dailyPayment}");
            OnLoanTaken?.Invoke(newLoan);
            
            return true;
        }
        
        /// <summary>
        /// Generate a payment card for a loan
        /// </summary>
        private void GeneratePaymentCard(ActiveLoan loan, int currentDay)
        {
            if (loan.IsFullyPaid()) return;
            if (loan.HasReachedTermLimit()) return; // Don't generate beyond term
            
            var card = new LoanPaymentCard(loan, currentDay);
            paymentCards.Add(card);
            loan.RecordCardGenerated();
            
            Debug.Log($"[LoanManager] Card {loan.generatedCards}/{loan.termDays} generated for {loan.loanData.displayName}: ${card.baseAmount}, Due: Day {card.dueDay}");
            OnCardGenerated?.Invoke(card);
        }
        
        /// <summary>
        /// Pay a card
        /// </summary>
        public bool PayCard(LoanPaymentCard card)
        {
            if (card == null || card.isPaid) return false;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(card.TotalAmount))
            {
                card.MarkPaid(currentDay);
                OnCardPaid?.Invoke(card);
                
                // Check if parent loan is fully paid
                if (card.parentLoan.IsFullyPaid())
                {
                    activeLoans.Remove(card.parentLoan);
                    OnLoanPaid?.Invoke(card.parentLoan);
                    Debug.Log($"[LoanManager] Loan fully paid: {card.parentLoan.loanData.displayName}");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Process start of day
        /// - Generate new cards for active loans
        /// - Mark cards as overdue if past due date
        /// </summary>
        public void ProcessDayStart(int currentDay)
        {
            // Generate new cards for active loans (one per day per loan)
            foreach (var loan in activeLoans.ToArray())
            {
                if (loan.IsFullyPaid()) continue;
                
                // Check if we already generated a card for today
                bool hasCardForToday = paymentCards.Any(c => 
                    c.parentLoan == loan && c.generatedDay == currentDay);
                
                if (!hasCardForToday)
                {
                    GeneratePaymentCard(loan, currentDay);
                }
            }
            
            // Apply compound daily late fees to overdue cards
            int newOverdue = 0;
            foreach (var card in paymentCards)
            {
                if (card.isPaid) continue;
                
                if (currentDay > card.dueDay)
                {
                    bool wasOverdue = card.isOverdue;
                    card.ApplyDailyLateFee(lateFeeRate, currentDay);
                    
                    if (!wasOverdue && card.isOverdue)
                    {
                        OnCardOverdue?.Invoke(card);
                        newOverdue++;
                        Debug.Log($"[LoanManager] Card overdue: {card.parentLoan.loanData.displayName}, Day {card.generatedDay}");
                    }
                }
            }
            
            // Check for game over
            int totalOverdue = GetOverdueCount();
            if (totalOverdue >= maxOverdueCards)
            {
                Debug.Log($"[LoanManager] GAME OVER - {totalOverdue} cards overdue!");
                OnGameOverDueToDebt?.Invoke();
            }
            else if (newOverdue > 0)
            {
                Debug.Log($"[LoanManager] {newOverdue} new overdue cards. Total: {totalOverdue}/{maxOverdueCards}");
            }
        }
        
        /// <summary>
        /// Get total debt (all unpaid cards including late fees)
        /// </summary>
        public int GetTotalDebt()
        {
            return paymentCards.Where(c => !c.isPaid).Sum(c => c.TotalAmount);
        }
        
        /// <summary>
        /// Clean up paid cards (optional, for memory)
        /// </summary>
        public void CleanupPaidCards()
        {
            paymentCards.RemoveAll(c => c.isPaid);
        }
        
        /// <summary>
        /// Calculate prepayment fee for a loan
        /// </summary>
        public int GetPrepaymentFee(ActiveLoan loan)
        {
            if (loan == null) return 0;
            return Mathf.CeilToInt(loan.remainingPrincipal * prepaymentFeeRate);
        }
        
        /// <summary>
        /// Get total accumulated late fees for a loan's unpaid cards
        /// </summary>
        public int GetAccumulatedLateFees(ActiveLoan loan)
        {
            if (loan == null) return 0;
            return paymentCards
                .Where(c => c.parentLoan == loan && !c.isPaid)
                .Sum(c => c.lateFee);
        }
        
        /// <summary>
        /// Calculate total prepayment amount (remaining principal + fee + late fees)
        /// </summary>
        public int CalculatePrepaymentAmount(ActiveLoan loan)
        {
            if (loan == null) return 0;
            int principal = loan.remainingPrincipal;
            int fee = GetPrepaymentFee(loan);
            int lateFees = GetAccumulatedLateFees(loan);
            return principal + fee + lateFees;
        }
        
        /// <summary>
        /// Prepay a loan (pay off all remaining balance early)
        /// </summary>
        public bool Prepay(ActiveLoan loan)
        {
            if (loan == null) return false;
            
            int currentDay = GameManager.Instance?.DayCount ?? 1;
            int prepaymentAmount = CalculatePrepaymentAmount(loan);
            
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(prepaymentAmount))
            {
                // Mark all unpaid cards for this loan as paid
                foreach (var card in paymentCards.Where(c => c.parentLoan == loan && !c.isPaid).ToArray())
                {
                    card.isPaid = true;
                    card.paidDay = currentDay;
                }
                
                // Mark loan as fully paid
                loan.paidDays = loan.termDays;
                loan.remainingPrincipal = 0;
                
                // Remove from active loans
                activeLoans.Remove(loan);
                
                Debug.Log($"[LoanManager] Prepaid loan: {loan.loanData.displayName}, Amount: ${prepaymentAmount}");
                OnLoanPaid?.Invoke(loan);
                
                return true;
            }
            
            return false;
        }
    }
}
