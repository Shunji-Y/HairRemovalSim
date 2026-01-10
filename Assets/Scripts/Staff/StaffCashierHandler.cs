using UnityEngine;
using System.Collections;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;
using HairRemovalSim.Core.Effects;

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
        public UI.CashRegister AssignedRegister => cashRegister;
        
        private void Start()
        {
            if (staffController == null)
                staffController = GetComponent<StaffController>();
        }
        
        /// <summary>
        /// Set the register this staff is assigned to
        /// </summary>
        public void SetAssignedRegister(UI.CashRegister register)
        {
            // Clear assignment from previous register
            if (cashRegister != null && cashRegister.AssignedStaff == staffController)
            {
                cashRegister.AssignedStaff = null;
            }
            
            cashRegister = register;
            
            // Set assignment on new register
            if (cashRegister != null)
            {
                cashRegister.AssignedStaff = staffController;
                
                // Dismiss waiting message since staff is now handling
                UI.MessageBoxManager.Instance?.DismissMessage("wait_cashier");
            }
            
            Debug.Log($"[StaffCashierHandler] {staffController?.StaffData?.Name} assigned to {register?.name ?? "no register"}");
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
            
            // Pause waiting timer - customer is being processed, gauge stays visible
            customer.PauseWaiting();
            
            // Set animation state
            staffController?.SetAnimInReceRegi(true);
            
            processingCoroutine = StartCoroutine(ProcessCustomerCoroutine(customer));
        }
        
        private IEnumerator ProcessCustomerCoroutine(CustomerController customer)
        {
            var rankData = staffController.StaffData?.profile?.rankData;
            float processingTime = rankData?.cashierProcessingTime ?? 10f;
            
            Debug.Log($"[StaffCashierHandler] {staffController.StaffData?.Name} processing payment for {customer.data?.customerName} for {processingTime}s");
            
            // Wait for customer to arrive at counter
            Transform targetPoint = cashRegister != null ? cashRegister.cashierPoint : null;
            if (targetPoint != null)
            {
                float arrivalThreshold = 1.5f; // Wait until close
                float timeout = 20f; // Safety timeout
                float elapsed = 0f;
                
                while (customer != null && Vector3.Distance(customer.transform.position, targetPoint.position) > arrivalThreshold)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed > timeout)
                    {
                        Debug.LogWarning($"[StaffCashierHandler] Timed out waiting for {customer.data?.customerName} to arrive. Proceeding anyway.");
                        break;
                    }
                    yield return null;
                }
            }

            Debug.Log($"[StaffCashierHandler] Customer arrived (or timed out). Starting process timer.");
            
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
                // Advance queue on failure
                cashRegister?.AdvanceQueue();
                yield break;
            }
            
            // SUCCESS - process payment
            ProcessPayment(customer, rankData);
            
            // Customer leaves
            customer.LeaveShop();
            
            Debug.Log($"[StaffCashierHandler] {customer.data?.customerName} payment processed by {staffController.StaffData?.Name}");
            
            FinishProcessing();
            
            // Advance the cashier queue to bring next customer to counter
            cashRegister?.AdvanceQueue();
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
            
            // Show angry leave popup
            if (UI.PopupNotificationManager.Instance != null)
            {
                UI.PopupNotificationManager.Instance.ShowAngryLeave(50);
            }
            
            // Show message for staff miss
            UI.MessageBoxManager.Instance?.ShowDirectMessage(
                LocalizationManager.Instance.Get("msg.staff_fail") ?? "スタッフがミスしてお客様が帰ってしまった！", 
                UI.MessageType.Complaint, 
                false, 
                "msg.staff_fail");
            
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
            
            // Staff cashier upsell - select item with 80%+ success rate
            var paymentPanel = UI.PaymentPanel.Instance;
            if (paymentPanel != null)
            {
                var selectedItem = paymentPanel.ConsumeHighSuccessRateCheckoutItem(customer, data.confirmedPrice);
                if (selectedItem.HasValue)
                {
                    // Item selected - roll for success/failure (same as player)
                    float roll = UnityEngine.Random.value;
                    bool upsellSucceeded = roll <= selectedItem.Value.successRate;
                    
                    Debug.Log($"[StaffCashierHandler] Checkout upsell attempt: {selectedItem.Value.itemId}, Rate: {selectedItem.Value.successRate:P0}, Roll: {roll:F2}, Success: {upsellSucceeded}");
                    
                    if (upsellSucceeded)
                    {
                        // Success: add price, get review bonus, apply effects
                        totalPayment += selectedItem.Value.price;
                        additionalReviewBonus = selectedItem.Value.reviewBonus;
                        
                        // Consume budget
                        customer.ConsumeAdditionalBudget(selectedItem.Value.price);
                        
                        var itemData = ItemDataRegistry.Instance?.GetItem(selectedItem.Value.itemId);
                        if (itemData != null)
                        {
                            // Apply checkout item effects
                            var ctx = EffectContext.CreateForRegister();
                            EffectHelper.ApplyEffects(itemData, ctx);
                            var spawner = FindObjectOfType<CustomerSpawner>();
                            if (spawner != null)
                            {
                                if (ctx.AttractionBoost > 0f) spawner.AddAttractionBoost(ctx.AttractionBoost);
                                if (ctx.NextDayAttractionBoost > 0f) spawner.AddNextDayAttractionBoost(ctx.NextDayAttractionBoost);
                            }
                        }
                        
                        Debug.Log($"[StaffCashierHandler] Cashier upsell success: +${selectedItem.Value.price}, +{additionalReviewBonus} review");
                        
                        // Show item success popup
                        if (UI.PopupNotificationManager.Instance != null)
                        {
                            UI.PopupNotificationManager.Instance.ShowItemResult(true);
                        }
                    }
                    else
                    {
                        // Failure: apply review penalty (item already consumed)
                        int penalty = customer.CalculateUpsellFailurePenalty(selectedItem.Value.successRate);
                        data.reviewPenalty += penalty;
                        Debug.Log($"[StaffCashierHandler] Cashier upsell failed! Review penalty: {penalty}");
                        
                        // Show item failure popup
                        if (UI.PopupNotificationManager.Instance != null)
                        {
                            UI.PopupNotificationManager.Instance.ShowItemResult(false);
                        }
                    }
                }
                // No eligible item = no upsell attempted
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
            
            // Apply debris penalty (1 debris = -1 point)
            if (Environment.HairDebrisManager.Instance != null)
            {
                finalReview -= Environment.HairDebrisManager.Instance.GetRemainingCount();
            }
            
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
            
            // Show popup notifications
            if (UI.PopupNotificationManager.Instance != null)
            {
                UI.PopupNotificationManager.Instance.ShowMoney(totalPayment);
                
                int moodIndex = finalReview >= 25 ? 3 : (finalReview >= 0 ? 2 : 1);
                UI.PopupNotificationManager.Instance.ShowReview(finalReview, moodIndex);
            }
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
        
        /// <summary>
        /// Finish processing - success case, reset customer wait time
        /// </summary>
        private void FinishProcessing()
        {
            // Clear animation state and trigger bow
            staffController?.SetAnimInReceRegi(false);
            staffController?.TriggerBow();
            
            // Reset customer's wait timer on successful checkout
            if (currentCustomer != null)
            {
                currentCustomer.ResetWaitTimer();
            }
            
            currentCustomer = null;
            isProcessing = false;
            processingCoroutine = null;
        }
        
        /// <summary>
        /// Cancel checkout processing (called when staff is reassigned)
        /// Returns customer to queue and resumes their wait timer
        /// </summary>
        public void CancelCheckout()
        {
            if (!isProcessing) return;
            
            Debug.Log($"[StaffCashierHandler] Canceling checkout for {currentCustomer?.data?.customerName}");
            
            // Clear animation state
            staffController?.SetAnimInReceRegi(false);
            
            // Stop the processing coroutine
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                processingCoroutine = null;
            }
            
            // Return customer to queue so player can interact
            if (currentCustomer != null)
            {
                if (cashRegister != null)
                {
                    cashRegister.ReturnCustomerToQueue(currentCustomer);
                }
                currentCustomer.ResumeWaitTimer();
            }
            
            currentCustomer = null;
            isProcessing = false;
        }
        
        private void OnDisable()
        {
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                // Don't call FinishProcessing here as it would reset wait timer
                currentCustomer = null;
                isProcessing = false;
                processingCoroutine = null;
            }
        }
    }
}
