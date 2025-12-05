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

        // Progress tracking
        public float OverallProgress { get; private set; }
        
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
            OverallProgress = 0f;
        }

        public void UpdateProgress()
        {
            if (TargetBodyPart == null)
            {
                Debug.LogWarning("[TreatmentSession] No target body part!");
                OverallProgress = 0f;
                return;
            }

            // Overall progress is the single body part's completion
            OverallProgress = TargetBodyPart.CompletionPercentage;
        }

        public bool IsComplete()
        {
            // Consider complete if overall progress is effectively 100%
            return OverallProgress >= 99.9f;
        }

        public void EndSession()
        {
            IsActive = false;
        }
    }
}
