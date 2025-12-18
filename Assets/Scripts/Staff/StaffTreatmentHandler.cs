using UnityEngine;
using System.Collections;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Staff
{
    /// <summary>
    /// Handles automatic treatment processing when staff is assigned to a bed
    /// - Disables player detection during treatment
    /// - Rolls for item usage based on rank
    /// - Calculates treatment time based on rank and parts
    /// - Applies success/failure based on wealth and item availability
    /// </summary>
    public class StaffTreatmentHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StaffController staffController;
        
        [Header("State")]
        [SerializeField] private bool isProcessing = false;
        [SerializeField] private CustomerController currentCustomer;
        [SerializeField] private BedController assignedBed;
        
        [Header("Progress")]
        [SerializeField] private float treatmentProgress = 0f;
        [SerializeField] private float treatmentDuration = 0f;
        
        private Coroutine processingCoroutine;
        
        public bool IsProcessing => isProcessing;
        public float TreatmentProgress => treatmentProgress;
        public float TreatmentDuration => treatmentDuration;
        
        // Event for UI slider updates
        public event System.Action<float, float> OnProgressChanged; // (current, max)
        
        private void Start()
        {
            if (staffController == null)
                staffController = GetComponent<StaffController>();
        }
        
        private void Update()
        {
            // Only process if assigned to treatment and at station
            if (staffController == null || staffController.StaffData == null) return;
            if (staffController.StaffData.assignment != StaffAssignment.Treatment) return;
            if (staffController.CurrentState != StaffController.StaffState.AtStation) return;
            
            // Don't start new processing if already processing
            if (isProcessing) return;
            
            // Get assigned bed
            assignedBed = staffController.GetAssignedBed();
            if (assignedBed == null) return;
            
            // Check for customer in treatment state
            var customer = assignedBed.CurrentCustomer;
            if (customer != null && customer.CurrentState == CustomerController.CustomerState.InTreatment)
            {
                // Check if bed is not already being processed by staff
                if (!assignedBed.IsStaffTreatmentInProgress)
                {
                    StartProcessing(customer);
                }
            }
        }
        
        private void StartProcessing(CustomerController customer)
        {
            if (customer == null) return;
            
            currentCustomer = customer;
            isProcessing = true;
            
            // Disable player detection
            assignedBed.StartStaffTreatment();
            
            processingCoroutine = StartCoroutine(ProcessTreatmentCoroutine(customer));
        }
        
        private IEnumerator ProcessTreatmentCoroutine(CustomerController customer)
        {
            var rankData = staffController.StaffData?.profile?.rankData;
            if (rankData == null)
            {
                Debug.LogError("[StaffTreatmentHandler] No rank data available!");
                FinishProcessing();
                yield break;
            }
            
            // Get treatment part count
            int partCount = GetTreatmentPartCount(customer);
            
            // Calculate treatment time
            treatmentDuration = rankData.CalculateTreatmentTime(partCount);
            treatmentProgress = 0f;
            
            Debug.Log($"[StaffTreatmentHandler] {staffController.StaffData?.Name} starting treatment on {customer.data?.customerName}");
            Debug.Log($"[StaffTreatmentHandler] Parts: {partCount}, Duration: {treatmentDuration}s");
            
            // Check if staff will attempt to use item
            bool shouldUseItem = Random.value < rankData.treatmentItemUsageRate;
            bool itemUsed = false;
            bool itemMissing = false;
            
            if (shouldUseItem)
            {
                // Try to consume item from bed's shelf
                var consumedItem = assignedBed.ConsumeShelfItem();
                if (consumedItem.HasValue)
                {
                    itemUsed = true;
                    Debug.Log($"[StaffTreatmentHandler] Used treatment item: {consumedItem.Value.itemId}");
                }
                else
                {
                    // Staff tried to use item but none available
                    itemMissing = true;
                    Debug.LogWarning("[StaffTreatmentHandler] Staff tried to use item but none available on shelf!");
                }
            }
            
            // Wait for treatment time
            while (treatmentProgress < treatmentDuration)
            {
                // Check if customer is still valid
                if (customer == null || !customer.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("[StaffTreatmentHandler] Customer became invalid during treatment");
                    FinishProcessing();
                    yield break;
                }
                
                treatmentProgress += Time.deltaTime;
                OnProgressChanged?.Invoke(treatmentProgress, treatmentDuration);
                
                yield return null;
            }
            
            // Calculate success rate
            float successRate = rankData.GetSuccessRate(customer.data.wealth);
            
            // Apply item missing penalty
            if (itemMissing)
            {
                successRate -= rankData.missingItemPenalty;
                Debug.Log($"[StaffTreatmentHandler] Success rate reduced by {rankData.missingItemPenalty * 100}% due to missing item");
            }
            
            successRate = Mathf.Clamp01(successRate);
            
            // Roll for success
            bool success = Random.value < successRate;
            
            if (success)
            {
                HandleSuccess(customer, rankData);
            }
            else
            {
                HandleFailure(customer);
            }
            
            FinishProcessing();
        }
        
        /// <summary>
        /// Get number of body parts in customer's treatment plan
        /// </summary>
        private int GetTreatmentPartCount(CustomerController customer)
        {
            if (customer?.data == null) return 1;
            
            // Get required body parts flags from request plan
            var requiredParts = CustomerPlanHelper.GetRequiredParts(customer.data.requestPlan);
            
            // Count set bits (number of body parts)
            int count = 0;
            int value = (int)requiredParts;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            
            return Mathf.Max(1, count);
        }
        
        /// <summary>
        /// Handle successful treatment - complete all parts, send to cashier
        /// </summary>
        private void HandleSuccess(CustomerController customer, StaffRankData rankData)
        {
            if (customer?.data == null) return;
            
            Debug.Log($"[StaffTreatmentHandler] SUCCESS! {customer.data.customerName} treatment completed");
            
            // Apply review coefficient
            customer.SetStaffReviewCoefficient(rankData.reviewCoefficient);
            
            // Force complete all parts
            var treatmentController = customer.GetComponentInChildren<Treatment.HairTreatmentController>();
            if (treatmentController != null)
            {
                treatmentController.ForceCompleteAllParts();
            }
            
            // Complete treatment (will trigger cashier flow)
            customer.CompleteTreatment();
        }
        
        /// <summary>
        /// Handle failed treatment - customer leaves without paying
        /// </summary>
        private void HandleFailure(CustomerController customer)
        {
            if (customer?.data == null) return;
            
            Debug.Log($"[StaffTreatmentHandler] FAILED! {customer.data.customerName} is leaving due to staff failure");
            
            // Generate negative review
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddReview(-50, customer.GetPainMaxCount());
                ShopManager.Instance.AddCustomerReview(1); // 1 star
            }
            
            // Release bed first
            if (assignedBed != null)
            {
                assignedBed.ClearCustomer();
            }
            
            // Customer gets dressed, stands up, then leaves (no payment)
            customer.FailAndLeave();
        }
        
        private void FinishProcessing()
        {
            // Re-enable player detection
            if (assignedBed != null)
            {
                assignedBed.EndStaffTreatment();
            }
            
            currentCustomer = null;
            isProcessing = false;
            treatmentProgress = 0f;
            treatmentDuration = 0f;
            processingCoroutine = null;
            
            OnProgressChanged?.Invoke(0f, 0f);
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
