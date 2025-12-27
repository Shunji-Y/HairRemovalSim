using UnityEngine;
using System.Collections;
using HairRemovalSim.Customer;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Handles automatic reception processing when staff is assigned
    /// - Processes customer's request as-is
    /// - Price = plan fixed price + optional upsell
    /// - Applies review coefficient
    /// - Rolls for success based on customer wealth
    /// </summary>
    public class StaffReceptionHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StaffController staffController;
        
        [Header("State")]
        [SerializeField] private bool isProcessing = false;
        [SerializeField] private CustomerController currentCustomer;
        
        private UI.ReceptionManager receptionManager;
        private Coroutine processingCoroutine;
        
        public bool IsProcessing => isProcessing;
        
        private void Start()
        {
            if (staffController == null)
                staffController = GetComponent<StaffController>();
                
            receptionManager = UI.ReceptionManager.Instance;
        }
        
        private void Update()
        {
            // Only process if assigned to reception and at station
            if (staffController == null || staffController.StaffData == null) return;
            if (staffController.StaffData.assignment != StaffAssignment.Reception) return;
            if (staffController.CurrentState != StaffController.StaffState.AtStation) return;
            
            // Don't start new processing if already processing
            if (isProcessing) return;
            
            // Check for customers in queue
            TryProcessNextCustomer();
        }
        
        private void TryProcessNextCustomer()
        {
            if (receptionManager == null) return;
            
            // Get next customer from queue
            var customer = receptionManager.DequeueCustomerForStaff();
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
            
            Debug.Log($"[StaffReceptionHandler] {staffController.StaffData?.Name} processing {customer.data?.customerName} for {processingTime}s");
            
            // Wait for processing time
            yield return new WaitForSeconds(processingTime);
            
            // Check if customer is still valid
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[StaffReceptionHandler] Customer became invalid during processing");
                FinishProcessing();
                yield break;
            }
            
            // Roll for success based on customer wealth
            if (rankData != null && !rankData.RollSuccess(customer.data.wealth))
            {
                // FAILED - customer leaves with -50 review
                HandleFailure(customer);
                FinishProcessing();
                yield break;
            }
            
            // SUCCESS - process customer's request
            ProcessCustomerRequest(customer, rankData);
            
            // Find available bed and send customer
            BedController availableBed = FindAvailableBed();
            if (availableBed != null)
            {
                customer.GoToBed(availableBed);
                Debug.Log($"[StaffReceptionHandler] {customer.data?.customerName} sent to bed by {staffController.StaffData?.Name}");
            }
            else
            {
                // No bed available - add to waiting list
                receptionManager?.AddToWaitingList(customer);
                Debug.Log($"[StaffReceptionHandler] No available bed, {customer.data?.customerName} added to waiting list");
            }
            
            FinishProcessing();
        }
        
        /// <summary>
        /// Handle staff failure - customer leaves with bad review
        /// </summary>
        private void HandleFailure(CustomerController customer)
        {
            if (customer?.data == null) return;
            
            Debug.Log($"[StaffReceptionHandler] FAILED! {customer.data.customerName} is leaving due to staff failure");
            
            // Set review penalty to -50
            customer.data.reviewPenalty = 50; // Will result in negative review
            
            // Generate negative review
            if (Core.ShopManager.Instance != null)
            {
                Core.ShopManager.Instance.AddReview(-50, 0);
                Core.ShopManager.Instance.AddCustomerReview(1); // 1 star
            }
            
            customer.LeaveShop();
        }
        
        /// <summary>
        /// Process customer request - use their request plan as-is, price = plan fixed price
        /// </summary>
        private void ProcessCustomerRequest(CustomerController customer, StaffRankData rankData)
        {
            if (customer?.data == null) return;
            
            var data = customer.data;
            
            // Set confirmed values from customer's request
            data.confirmedParts = data.GetRequiredParts(); // Customer's requested parts
            data.confirmedPrice = CustomerPlanHelper.GetPlanPrice(data.requestPlan); // Fixed plan price
            
            // Apply review coefficient (simpler approach - store as multiplier)
            if (rankData != null)
            {
                float coefficient = rankData.reviewCoefficient;
                customer.SetStaffReviewCoefficient(coefficient);
                Debug.Log($"[StaffReceptionHandler] Set review coefficient: {coefficient}");
            }
            
            // Staff upsell - select item with 80%+ success rate
            var receptionPanel = UI.ReceptionPanel.Instance;
            if (receptionPanel != null)
            {
                var selectedItem = receptionPanel.ConsumeHighSuccessRateItem(customer);
                if (selectedItem.HasValue)
                {
                    // Item selected - roll for success/failure (same as player)
                    float roll = UnityEngine.Random.value;
                    bool upsellSucceeded = roll <= selectedItem.Value.successRate;
                    
                    Debug.Log($"[StaffReceptionHandler] Upsell attempt: {selectedItem.Value.itemId}, Rate: {selectedItem.Value.successRate:P0}, Roll: {roll:F2}, Success: {upsellSucceeded}");
                    
                    if (upsellSucceeded)
                    {
                        // Success: add price, apply effects
                        data.confirmedPrice += selectedItem.Value.price;
                        customer.SetUpsellSuccess(true);
                        
                        var itemData = Core.ItemDataRegistry.Instance?.GetItem(selectedItem.Value.itemId);
                        if (itemData != null)
                        {
                            customer.ApplyReceptionEffects(itemData);
                        }
                        
                        Debug.Log($"[StaffReceptionHandler] Upsell success: +${selectedItem.Value.price}");
                    }
                    else
                    {
                        // Failure: apply review penalty (item already consumed)
                        int penalty = customer.CalculateUpsellFailurePenalty(selectedItem.Value.successRate);
                        data.reviewPenalty += penalty;
                        Debug.Log($"[StaffReceptionHandler] Upsell failed! Review penalty: {penalty}");
                    }
                }
                // No eligible item = no upsell attempted
            }
            
            Debug.Log($"[StaffReceptionHandler] Confirmed: {data.confirmedParts}, Price: ${data.confirmedPrice}");
        }
        
        private BedController FindAvailableBed()
        {
            if (receptionManager?.beds == null) return null;
            
            foreach (var bed in receptionManager.beds)
            {
                if (bed != null && !bed.IsOccupied)
                {
                    return bed;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Finish processing - success case, reset customer wait time
        /// </summary>
        private void FinishProcessing()
        {
            // Reset customer's wait timer on successful reception
            if (currentCustomer != null)
            {
                currentCustomer.ResetWaitTimer();
            }
            
            currentCustomer = null;
            isProcessing = false;
            processingCoroutine = null;
        }
        
        /// <summary>
        /// Cancel reception processing (called when staff is reassigned)
        /// Returns customer to queue and resumes their wait timer
        /// </summary>
        public void CancelReception()
        {
            if (!isProcessing) return;
            
            Debug.Log($"[StaffReceptionHandler] Canceling reception for {currentCustomer?.data?.customerName}");
            
            // Stop the processing coroutine
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                processingCoroutine = null;
            }
            
            // Return customer to queue so player can interact
            if (currentCustomer != null && receptionManager != null)
            {
                receptionManager.ReturnCustomerToQueue(currentCustomer);
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
