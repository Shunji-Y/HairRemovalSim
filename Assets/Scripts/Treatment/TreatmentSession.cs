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
        public List<Core.BodyPart> TargetBodyParts { get; private set; }
        public float StartTime { get; private set; }
        public bool IsActive { get; private set; }

        // Progress tracking
        public float OverallProgress { get; private set; }
        
        public TreatmentSession(CustomerController customer)
        {
            Customer = customer;
            TargetBodyParts = new List<Core.BodyPart>(customer.GetComponentsInChildren<Core.BodyPart>());
            StartTime = Time.time;
            IsActive = true;
            OverallProgress = 0f;
        }

        public void UpdateProgress()
        {
            if (TargetBodyParts.Count == 0)
            {
                OverallProgress = 100f;
                return;
            }

            float totalCompletion = 0f;
            foreach (var part in TargetBodyParts)
            {
                totalCompletion += part.CompletionPercentage;
            }

            OverallProgress = totalCompletion / TargetBodyParts.Count;
        }

        public bool IsComplete()
        {
            // Consider complete if overall progress is effectively 100%
            // Individual parts clamp to 100, so average should reach 100
            return OverallProgress >= 99.9f;
        }

        public void EndSession()
        {
            IsActive = false;
        }
    }
}
