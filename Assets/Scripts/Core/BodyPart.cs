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
            
            Debug.Log($"[BodyPart] {partName}: Initialized with Shader-based Hair System.");
        }
        
        /// <summary>
        /// Bake the current pose of SkinnedMeshRenderer into MeshCollider for accurate hit detection
        /// Call this when customer lies down on the bed
        /// </summary>
        public void BakeMeshForCollider()
        {
            var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            var meshCollider = GetComponent<MeshCollider>();
            
            if (skinnedMeshRenderer == null)
            {
                Debug.LogWarning($"[BodyPart] {partName}: No SkinnedMeshRenderer found for BakeMesh");
                return;
            }
            
            // Create or get the baked mesh
            Mesh bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);
            
            // Create MeshCollider if it doesn't exist
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            
            // Update the collider with the baked mesh
            meshCollider.sharedMesh = bakedMesh;
            
            Debug.Log($"[BodyPart] {partName}: MeshCollider updated with baked mesh ({bakedMesh.vertexCount} vertices)");
        }
        
        /// <summary>
        /// Reset this body part for reuse (object pooling)
        /// </summary>
        public void Reset()
        {
            completionPercentage = 0f;
            
            if (treatmentController != null)
            {
                treatmentController.ClearMask();
            }
            
            Debug.Log($"[BodyPart] {partName} reset to 0% completion");
        }
        
        /// <summary>
        /// Reset this body part's color to white (FFFFFF, intensity 0)
        /// </summary>
        public void ResetColor()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.materials != null)
            {
                Color whiteColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                
                // Handle multiple materials
                foreach (var mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        mat.SetColor("_BodyColor", whiteColor);
                    }
                }
                
                Debug.Log($"[BodyPart] {partName} color reset to white");
            }
        }
        
        public void RemoveHairAt(Vector2 uv)
        {
            if (treatmentController != null)
            {
                // Default to submesh 0 if not specified, use small brush size
                treatmentController.ApplyTreatment(uv, new Vector2(0.01f, 0.01f), 0f, 0);
                
                var customer = GetComponentInParent<Customer.CustomerController>();
                if (customer != null)
                {
                    customer.AddPain(0.5f * skinSensitivity); 
                }
            }
        }
        
        public void UpdateCompletion(float percentage)
        {
            SetCompletion(percentage);
        }

        public void SetCompletion(float percentage)
        {
            float previousCompletion = completionPercentage;
            completionPercentage = Mathf.Clamp(percentage, 0f, 100f);
            
            // Auto-reset color when reaching 100%
            if (previousCompletion < 100f && completionPercentage >= 100f)
            {
                ResetColor();
                Debug.Log($"[BodyPart] {partName} reached 100%! Color reset to white.");
            }
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
