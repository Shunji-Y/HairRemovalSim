using UnityEngine;
using System.Collections;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Handles automatic cashier processing when staff is assigned
    /// - Rolls for success based on customer wealth
    /// - Payment = reception confirmed price + upsell item if success
    /// - Applies review coefficient to final review
    /// </summary>
    public class StaffCashierHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StaffController staffController;
        
        [Header("State")]
        [SerializeField] private bool isProcessing = false;
        [SerializeField] private CustomerController currentCustomer;
        
        private UI.CashRegister cashRegister;
        private Coroutine processingCoroutine;
        
        public bool IsProcessing => isProcessing;
        
        private void Start()
        {
            if (staffController == null)
                staffController = GetComponent<StaffController>();
                
            cashRegister = FindObjectOfType<UI.CashRegister>();
        }
        
        private void Update()
        {
            // Only process if assigned to cashier and at station
            if (staffController == null || staffController.StaffData == null) return;
            if (staffController.StaffData.assignment != StaffAssignment.Cashier) return;
            if (staffController.CurrentState != StaffController.StaffState.AtStation) return;
            
            // Don't start new processing if already processing
            if (isProcessing) return;
            
            // Check for customers in queue
            TryProcessNextCustomer();
        }
        
        private void TryProcessNextCustomer()
        {
            if (cashRegister == null) return;
            
            // Get next customer from queue
            var customer = cashRegister.DequeueCustomerForStaff();
            if (customer != null)
            {
                StartProcessing(customer);
            }
        }
        
        private void StartProcessing(CustomerController customer)
        {
            if (customer == null) return;
            
            currentCustomer = customer;
            isProcessing = true;
            
            processingCoroutine = StartCoroutine(ProcessCustomerCoroutine(customer));
        }
        
        private IEnumerator ProcessCustomerCoroutine(CustomerController customer)
        {
            var rankData = staffController.StaffData?.profile?.rankData;
            float processingTime = rankData?.processingTime ?? 10f;
            
            Debug.Log($"[StaffCashierHandler] {staffController.StaffData?.Name} processing payment for {customer.data?.customerName} for {processingTime}s");
            
            // Wait for processing time
            yield return new WaitForSeconds(processingTime);
            
            // Check if customer is still valid
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[StaffCashierHandler] Customer became invalid during processing");
                FinishProcessing();
                yield break;
            }
            
            // Roll for success based on customer wealth
            if (rankData != null && !rankData.RollSuccess(customer.data.wealth))
            {
                // FAILED - customer leaves without paying, bad review
                HandleFailure(customer);
                FinishProcessing();
                yield break;
            }
            
            // SUCCESS - process payment
            ProcessPayment(customer, rankData);
            
            // Customer leaves
            customer.LeaveShop();
            
            Debug.Log($"[StaffCashierHandler] {customer.data?.customerName} payment processed by {staffController.StaffData?.Name}");
            
            FinishProcessing();
        }
        
        /// <summary>
        /// Handle staff failure - customer leaves without paying
        /// </summary>
        private void HandleFailure(CustomerController customer)
        {
            if (customer?.data == null) return;
            
            Debug.Log($"[StaffCashierHandler] FAILED! {customer.data.customerName} is leaving without paying");
            
            // Generate negative review
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReview(-50, customer.GetPainMaxCount());
                ShopManager.Instance.AddCustomerReview(1); // 1 star
            }
            
            customer.LeaveShop();
        }
        
        /// <summary>
        /// Process successful payment
        /// </summary>
        private void ProcessPayment(CustomerController customer, StaffRankData rankData)
        {
            if (customer?.data == null) return;
            
            var data = customer.data;
            
            // Base payment = reception confirmed price (already includes reception upsell)
            int totalPayment = data.confirmedPrice;
            int additionalReviewBonus = 0;
            
            // Try cashier upsell - consume actual item from CheckoutItemSlot
            if (rankData != null && rankData.RollItemUsage())
            {
                var paymentPanel = UI.PaymentPanel.Instance;
                if (paymentPanel != null)
                {
                    var consumedItem = paymentPanel.ConsumeRandomCheckoutItem();
                    if (consumedItem.HasValue)
                    {
                        totalPayment += consumedItem.Value.price;
                        additionalReviewBonus = consumedItem.Value.reviewBonus;
                        Debug.Log($"[StaffCashierHandler] Cashier upsell: {consumedItem.Value.itemId}, +${consumedItem.Value.price}, +{consumedItem.Value.reviewBonus} review");
                    }
                    else
                    {
                        Debug.Log("[StaffCashierHandler] Cashier upsell roll succeeded but no items in CheckoutItemSlots");
                    }
                }
            }
            
            // Add money
            EconomyManager.Instance?.AddMoney(totalPayment);
            
            // Calculate review with staff coefficient
            int baseReview = customer.GetBaseReviewValue();
            float coefficient = customer.GetStaffReviewCoefficient();
            
            // Apply coefficient: final = base * coefficient
            int finalReview = Mathf.RoundToInt(baseReview * coefficient);
            finalReview -= data.reviewPenalty; // Subtract any penalties (pain, etc.)
            finalReview += additionalReviewBonus; // Add item bonus
            
            // Submit review
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReview(finalReview, customer.GetPainMaxCount());
                int stars = GetStarsFromReview(finalReview);
                ShopManager.Instance.AddCustomerReview(stars);
                
                // Record for daily attraction update
                var spawner = FindObjectOfType<CustomerSpawner>();
                spawner?.RecordDailyReview(finalReview);
            }
            
            Debug.Log($"[StaffCashierHandler] Payment: ${totalPayment}, Review: {finalReview} (base: {baseReview} × coef: {coefficient} - penalty: {data.reviewPenalty} + bonus: {additionalReviewBonus})");
        }
        
        /// <summary>
        /// Convert review value to star rating
        /// </summary>
        private int GetStarsFromReview(int review)
        {
            if (review >= 40) return 5;
            if (review >= 25) return 4;
            if (review >= 10) return 3;
            if (review >= 0) return 2;
            return 1;
        }
        
        private void FinishProcessing()
        {
            currentCustomer = null;
            isProcessing = false;
            processingCoroutine = null;
        }
        
        private void OnDisable()
        {
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                FinishProcessing();
            }
        }
    }
}
