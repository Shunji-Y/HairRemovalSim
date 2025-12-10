using System.Collections.Generic;
using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Customer;

namespace HairRemovalSim.Treatment
{
    [System.Serializable]
    public class TreatmentSession
    {
        public CustomerController Customer { get; private set; }
        public Core.BodyPart TargetBodyPart { get; private set; } // Single body part for UV mask system
        public float StartTime { get; private set; }
        public bool IsActive { get; private set; }
        
        public TreatmentSession(CustomerController customer)
        {
            Customer = customer;
            
            // Find the single BodyPart component (should be on Body child object)
            TargetBodyPart = customer.GetComponentInChildren<Core.BodyPart>();
            
            if (TargetBodyPart == null)
            {
                Debug.LogError($"[TreatmentSession] No BodyPart found for {customer.data.customerName}!");
            }
            else
            {
                Debug.Log($"[TreatmentSession] Created for {customer.data.customerName} - Target: {TargetBodyPart.partName}, Plan: {customer.data.selectedTreatmentPlan.GetDisplayName()}");
            }
            
            StartTime = Time.time;
            IsActive = true;
        }

        /// <summary>
        /// Check if all target body parts are complete
        /// Simple count comparison: completed parts == total target parts
        /// </summary>
        public bool AreAllPartsComplete()
        {
            if (TargetBodyPart == null) return false;
            
            var treatmentController = TargetBodyPart.GetComponent<HairTreatmentController>();
            if (treatmentController == null) return false;
            
            // Get target count and completed count
            int totalParts = treatmentController.GetTargetPartCount();
            int completedParts = treatmentController.GetCompletedPartCount();
            
            if (totalParts <= 0) return false;
            
            bool allComplete = completedParts >= totalParts;
            
            if (allComplete)
            {
                Debug.Log($"[TreatmentSession] All parts complete! ({completedParts}/{totalParts})");
            }
            
            return allComplete;
        }
        
        /// <summary>
        /// Get per-part completion dictionary
        /// </summary>
        public Dictionary<string, float> GetPartCompletions()
        {
            if (TargetBodyPart == null) return null;
            
            var treatmentController = TargetBodyPart.GetComponent<HairTreatmentController>();
            if (treatmentController == null) return null;
            
            return treatmentController.GetPerPartCompletion();
        }

        public void EndSession()
        {
            IsActive = false;
        }
    }
}
