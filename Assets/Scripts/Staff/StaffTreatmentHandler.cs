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
        
        [Header("Tool Status")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private string missingToolsMessage = "";
        
        private Coroutine processingCoroutine;
        
        public bool IsProcessing => isProcessing;
        public bool IsPaused => isPaused;
        public string MissingToolsMessage => missingToolsMessage;
        public float TreatmentProgress => treatmentProgress;
        public float TreatmentDuration => treatmentDuration;
        public BedController AssignedBed => assignedBed;
        public CustomerController CurrentCustomer => currentCustomer;
        
        // Event for UI slider updates
        public event System.Action<float, float> OnProgressChanged; // (current, max)
        
        // Event for tool warning updates
        public event System.Action<bool, string> OnToolStatusChanged; // (isPaused, message)
        
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
            
            // Stop customer's wait timer - staff is handling them now
            customer.StopWaiting();
            
            // Set animation state
            staffController?.SetAnimInTreatment(true);
            
            // Register with bed for UI
            assignedBed.RegisterStaffHandler(this);
            
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
                
                // Check for required tools
                string missingTools = CheckMissingTools(rankData);
                
                if (!string.IsNullOrEmpty(missingTools))
                {
                    // Tools missing - pause treatment
                    if (!isPaused)
                    {
                        isPaused = true;
                        missingToolsMessage = missingTools;
                        OnToolStatusChanged?.Invoke(true, missingToolsMessage);
                        Debug.LogWarning($"[StaffTreatmentHandler] Treatment paused - missing: {missingTools}");
                    }
                    // Don't progress time while paused
                }
                else
                {
                    // All tools available - resume treatment
                    if (isPaused)
                    {
                        isPaused = false;
                        missingToolsMessage = "";
                        OnToolStatusChanged?.Invoke(false, "");
                        Debug.Log("[StaffTreatmentHandler] Treatment resumed - all tools available");
                    }
                    
                    treatmentProgress += Time.deltaTime;
                    OnProgressChanged?.Invoke(treatmentProgress, treatmentDuration);
                }
                
                yield return null;
            }
            
            // Clear pause state
            isPaused = false;
            missingToolsMessage = "";
            OnToolStatusChanged?.Invoke(false, "");
            
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
        /// Check for missing treatment tools
        /// Returns comma-separated list of missing tools, or empty string if all available
        /// </summary>
        private string CheckMissingTools(StaffRankData rankData)
        {
            if (assignedBed == null) return "ベッド未設定";
            
            var missingList = new System.Collections.Generic.List<string>();
            int staffRankGrade = GetStaffRankGrade(rankData);
            
            // Check shaver on TreatmentShelf slot [0,0]
            bool hasShaver = false;
            if (assignedBed.installedShelves != null)
            {
                foreach (var shelf in assignedBed.installedShelves)
                {
                    if (shelf == null) continue;
                    var slotData = shelf.GetSlotData(0, 0);
                    if (slotData != null && !string.IsNullOrEmpty(slotData.itemId) && slotData.quantity > 0)
                    {
                        // Check if it's a shaver
                        var itemData = ItemDataRegistry.Instance?.GetItem(slotData.itemId);
                        if (itemData != null && itemData.toolType == TreatmentToolType.Shaver)
                        {
                            hasShaver = true;
                            break;
                        }
                    }
                }
            }
            if (!hasShaver)
            {
                missingList.Add("シェーバー");
            }
            
            // Check lasers
            if (assignedBed.laserBody != null)
            {
                var faceLaser = assignedBed.laserBody.GetItem(ToolTargetArea.Face);
                var bodyLaser = assignedBed.laserBody.GetItem(ToolTargetArea.Body);
                
                // Check if either laser has TargetArea.All (covers both face and body)
                bool faceHasAllLaser = faceLaser != null && faceLaser.targetArea == ToolTargetArea.All;
                bool bodyHasAllLaser = bodyLaser != null && bodyLaser.targetArea == ToolTargetArea.All;
                bool hasAllLaser = faceHasAllLaser || bodyHasAllLaser;
                
                // Face laser check
                bool faceCovered = false;
                if (faceLaser != null)
                {
                    if (faceLaser.requiredShopGrade > staffRankGrade)
                    {
                        missingList.Add("顔用レーザー(ランク不足)");
                    }
                    else
                    {
                        faceCovered = true;
                    }
                }
                else if (hasAllLaser)
                {
                    // All-type laser in body slot can cover face
                    var allLaser = bodyHasAllLaser ? bodyLaser : faceLaser;
                    if (allLaser.requiredShopGrade <= staffRankGrade)
                    {
                        faceCovered = true;
                    }
                }
                
                if (!faceCovered && !hasAllLaser)
                {
                    missingList.Add("顔用レーザー");
                }
                
                // Body laser check
                bool bodyCovered = false;
                if (bodyLaser != null)
                {
                    if (bodyLaser.requiredShopGrade > staffRankGrade)
                    {
                        missingList.Add("体用レーザー(ランク不足)");
                    }
                    else
                    {
                        bodyCovered = true;
                    }
                }
                else if (hasAllLaser)
                {
                    // All-type laser in face slot can cover body
                    var allLaser = faceHasAllLaser ? faceLaser : bodyLaser;
                    if (allLaser.requiredShopGrade <= staffRankGrade)
                    {
                        bodyCovered = true;
                    }
                }
                
                if (!bodyCovered && !hasAllLaser)
                {
                    missingList.Add("体用レーザー");
                }
            }
            else
            {
                missingList.Add("レーザー本体");
            }
            
            return string.Join("、", missingList);
        }
        
        /// <summary>
        /// Get staff rank as shop grade equivalent (1-5)
        /// College=1, NewGrad=2, MidCareer=3, Veteran=4, Pro=5
        /// </summary>
        private int GetStaffRankGrade(StaffRankData rankData)
        {
            if (rankData == null) return 1;
            
            switch (rankData.rank)
            {
                case StaffRank.College: return 1;
                case StaffRank.NewGrad: return 2;
                case StaffRank.MidCareer: return 3;
                case StaffRank.Veteran: return 4;
                case StaffRank.Professional: return 5;
                default: return 1;
            }
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
            
            // Show angry leave popup
            if (UI.PopupNotificationManager.Instance != null)
            {
                UI.PopupNotificationManager.Instance.ShowAngryLeave(50);
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
            // Clear animation state
            staffController?.SetAnimInTreatment(false);
            
            // Re-enable player detection
            if (assignedBed != null)
            {
                assignedBed.EndStaffTreatment();
                assignedBed.UnregisterStaffHandler();
            }
            
            currentCustomer = null;
            isProcessing = false;
            isPaused = false;
            missingToolsMessage = "";
            treatmentProgress = 0f;
            treatmentDuration = 0f;
            processingCoroutine = null;
            
            OnProgressChanged?.Invoke(0f, 0f);
            OnToolStatusChanged?.Invoke(false, "");
        }
        
        /// <summary>
        /// Cancel treatment (called when staff is reassigned during treatment)
        /// Does not complete or fail customer - leaves them for player to handle
        /// </summary>
        public void CancelTreatment()
        {
            if (!isProcessing) return;
            
            Debug.Log($"[StaffTreatmentHandler] Cancelling treatment - staff reassigned");
            
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                processingCoroutine = null;
            }
            
            // Resume customer's wait timer since staff is no longer handling them
            if (currentCustomer != null)
            {
                currentCustomer.ResumeWaitTimer();
            }
            
            FinishProcessing();
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
