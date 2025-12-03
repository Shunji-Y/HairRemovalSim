using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.Core;
using HairRemovalSim.Treatment;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.Core
{
    public class BodyPart : MonoBehaviour, IInteractable
    {
        [Header("Body Part Settings")]
        public string partName = "Unknown";
        public float skinSensitivity = 1.0f;
        public int hairCount = 100;
        
        [Header("Completion Settings")]
        [Tooltip("The number of white pixels remaining when all hair is removed (Manual Calibration)")]
        public int targetWhitePixelCount = 0;
        
        [Header("Completion Tracking")]
        [Range(0f, 100f)]
        [SerializeField] private float completionPercentage = 0f;
        public float CompletionPercentage => completionPercentage;
        
        [HideInInspector]
        public HairTreatmentController treatmentController;

        private List<Hair> hairs = new List<Hair>();
        private Dictionary<Transform, Hair> visualToDataMap = new Dictionary<Transform, Hair>();

        public void Initialize()
        {
            treatmentController = GetComponent<HairTreatmentController>();
            if (treatmentController == null)
            {
                treatmentController = gameObject.AddComponent<HairTreatmentController>();
            }
            
            treatmentController.Initialize();
            
            Debug.Log($"BodyPart {partName}: Initialized with Shader-based Hair System.");
        }

        public void RemoveHairAt(Vector2 uv)
        {
            if (treatmentController != null)
            {
                treatmentController.RemoveHairAt(uv);
                
                var customer = GetComponentInParent<Customer.CustomerController>();
                if (customer != null)
                {
                    customer.AddPain(0.5f * skinSensitivity); 
                }
            }
        }
        
        public void SetCompletion(float percentage)
        {
            completionPercentage = Mathf.Clamp(percentage, 0f, 100f);
        }

        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            // Treatment mode disabled - using direct decal interaction instead
            Debug.Log($"BodyPart {partName}: Direct interaction (treatment mode disabled)");
        }

        public void OnHoverEnter()
        {
            var outline = GetComponent<Effects.OutlineHighlighter>();
            if (outline != null) outline.enabled = true;
        }

        public void OnHoverExit()
        {
            var outline = GetComponent<Effects.OutlineHighlighter>();
            if (outline != null) outline.enabled = false;
        }

        public string GetInteractionPrompt()
        {
            return $"Start Treatment on {partName}";
        }

        // Legacy methods
        public void RemoveHair(Hair hair) { }
        public int RemoveHairsInRadius(Vector3 point, float radius) { return 0; }
        public Hair GetHairFromVisual(Transform visual) { return null; }
    }
}
