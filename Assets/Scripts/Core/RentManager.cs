using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Individual rent payment card
    /// </summary>
    [Serializable]
    public class RentPaymentCard
    {
        public int generatedDay;        // Day this bill was created
        public int dueDay;              // Day by which this must be paid
        public int amount;              // Rent amount
        public bool isPaid;             // Whether this is paid
        public int paidDay;             // Day when paid (-1 if not paid)
        public bool isOverdue;          // Whether past due date
        public int overdueSinceDay;     // Day when became overdue
        
        public RentPaymentCard(int billDay, int gracePeriod, int rentAmount)
        {
            generatedDay = billDay;
            dueDay = billDay + gracePeriod;
            amount = rentAmount;
            isPaid = false;
            paidDay = -1;
            isOverdue = false;
            overdueSinceDay = -1;
        }
        
        public int GetDaysUntilDue(int currentDay)
        {
            return Mathf.Max(0, dueDay - currentDay);
        }
    }
    
    /// <summary>
    /// Manages rent payments
    /// - Rent is due every X days (1, 4, 7, 10... for 3-day interval)
    /// - Each rent card has a grace period before becoming overdue
    /// - Multiple unpaid rent cards can accumulate
    /// </summary>
    public class RentManager : Singleton<RentManager>
    {
        [Header("Settings")]
        [Tooltip("Fallback rent if ShopManager not available")]
        [SerializeField] private int fallbackRentAmount = 50;
        
        [Tooltip("Days between rent bills (e.g., 3 = due on days 1, 4, 7, 10...)")]
        [SerializeField] private int rentIntervalDays = 3;
        
        [Tooltip("Days after bill generation before rent becomes overdue")]
        [SerializeField] private int gracePeriodDays = 3;
        
        [Tooltip("Days after overdue before game over")]
        [SerializeField] private int overdueGraceDays = 3;
        
        // All rent cards (unpaid accumulate)
        private List<RentPaymentCard> rentCards = new List<RentPaymentCard>();
        private int lastBillDay = 0; // Last day a bill was generated
        
        // Events
        public Action OnRentDue;
        public Action OnRentPaid;
        public Action OnRentOverdue;
        public Action OnGameOverDueToRent;
        
        /// <summary>
        /// Get current rent amount (from ShopManager based on grade)
        /// </summary>
        public int RentAmount => ShopManager.Instance?.GetCurrentRent() ?? fallbackRentAmount;
        public int RentIntervalDays => rentIntervalDays;
        public List<RentPaymentCard> RentCards => rentCards;
        
        /// <summary>
        /// Check if there are any unpaid rent cards
        /// </summary>
        public bool HasPendingPayment(int currentDay)
        {
            return rentCards.Any(c => !c.isPaid);
        }
        
        /// <summary>
        /// Check if any rent is overdue
        /// </summary>
        public bool IsOverdue => rentCards.Any(c => c.isOverdue && !c.isPaid);
        
        /// <summary>
        /// Get all unpaid rent cards
        /// </summary>
        public List<RentPaymentCard> GetUnpaidCards()
        {
            return rentCards.Where(c => !c.isPaid).ToList();
        }
        
        /// <summary>
        /// Get total due (all unpaid cards)
        /// </summary>
        public int GetTotalDue()
        {
            return rentCards.Where(c => !c.isPaid).Sum(c => c.amount);
        }
        
        /// <summary>
        /// Get count of overdue cards
        /// </summary>
        public int GetOverdueCount()
        {
            return rentCards.Count(c => c.isOverdue && !c.isPaid);
        }
        
        /// <summary>
        /// Process start of day
        /// </summary>
        public void ProcessDayStart(int currentDay)
        {
            // Check if it's time for a new bill (fixed schedule: 1, 4, 7, 10...)
            int scheduledBillDay = GetScheduledBillDayForDay(currentDay);
            
            // Generate new bill if we haven't for this period
            if (scheduledBillDay > lastBillDay)
            {
                var newCard = new RentPaymentCard(scheduledBillDay, gracePeriodDays, RentAmount);
                rentCards.Add(newCard);
                lastBillDay = scheduledBillDay;
                Debug.Log($"[RentManager] New rent bill generated on day {scheduledBillDay}, due by day {newCard.dueDay}, amount: ${RentAmount}");
                OnRentDue?.Invoke();
            }
            
            // Check for overdue cards and apply penalties
            int newOverdue = 0;
            bool triggerGameOver = false;
            
            foreach (var card in rentCards)
            {
                if (card.isPaid) continue;
                
                // Check if became overdue
                if (!card.isOverdue && currentDay > card.dueDay)
                {
                    card.isOverdue = true;
                    card.overdueSinceDay = currentDay;
                    newOverdue++;
                    Debug.Log($"[RentManager] Rent from day {card.generatedDay} is now OVERDUE!");
                    OnRentOverdue?.Invoke();
                }
                
                // Check for game over
                if (card.isOverdue && card.overdueSinceDay > 0)
                {
                    int daysOverdue = currentDay - card.overdueSinceDay;
                    if (daysOverdue >= overdueGraceDays)
                    {
                        triggerGameOver = true;
                    }
                }
            }
            
            if (triggerGameOver)
            {
                Debug.Log($"[RentManager] GAME OVER - Rent overdue too long!");
                OnGameOverDueToRent?.Invoke();
            }
        }
        
        /// <summary>
        /// Get the scheduled bill day that applies to a given day
        /// </summary>
        private int GetScheduledBillDayForDay(int day)
        {
            int periodsElapsed = (day - 1) / rentIntervalDays;
            return 1 + (periodsElapsed * rentIntervalDays);
        }
        
        /// <summary>
        /// Pay a specific rent card
        /// </summary>
        public bool PayRentCard(RentPaymentCard card, int currentDay)
        {
            if (card == null || card.isPaid) return false;
            
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(card.amount))
            {
                card.isPaid = true;
                card.paidDay = currentDay;
                Debug.Log($"[RentManager] Rent from day {card.generatedDay} paid on day {currentDay}");
                OnRentPaid?.Invoke();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Pay oldest unpaid rent (for backwards compatibility)
        /// </summary>
        public bool PayRent(int currentDay)
        {
            var unpaid = GetUnpaidCards().OrderBy(c => c.generatedDay).FirstOrDefault();
            if (unpaid != null)
            {
                return PayRentCard(unpaid, currentDay);
            }
            return false;
        }
        
        /// <summary>
        /// Get status text for a card
        /// </summary>
        public string GetStatusText(RentPaymentCard card, int currentDay)
        {
            if (card.isPaid) return "支払済み";
            
            if (card.isOverdue)
            {
                int daysOverdue = currentDay - card.overdueSinceDay;
                return $"<color=red>滞納中 ({daysOverdue}/{overdueGraceDays}日)</color>";
            }
            
            int daysRemaining = card.GetDaysUntilDue(currentDay);
            if (daysRemaining <= 0)
                return "<color=orange>期限: 本日</color>";
            return $"期限: {daysRemaining}日後";
        }
        
        /// <summary>
        /// Clean up paid cards (optional)
        /// </summary>
        public void CleanupPaidCards()
        {
            rentCards.RemoveAll(c => c.isPaid);
        }
    }
}
