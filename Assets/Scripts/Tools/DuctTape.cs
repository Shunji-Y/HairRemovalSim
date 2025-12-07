using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;
using HairRemovalSim.Treatment;

namespace HairRemovalSim.Tools
{
    public class DuctTape : ToolBase
    {
        [Header("Duct Tape Settings")]
        public float decalWidth = 0.08f;
        public float decalHeight = 0.12f;
        public float uvScaleFactor = 1.0f; // Adjust this to match decal visual with removal area
        [ColorUsage(true, true)]
        public Color emissionColor = new Color(2f, 2f, 0.5f, 1f);
        
        private Camera mainCamera;
        private PlayerController playerController;
        private BodyPart currentBodyPart;
        private RaycastHit currentHit;
        private bool isHoveringBodyPart = false;
        private Material[] currentMaterials;
        private bool isEquipped = false;
        private HairTreatmentController previousTreatmentController;

        private struct UVRect
        {
            public Vector2 center;
            public Vector2 size;
            public float angle; // Radians
        }

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
            playerController = FindObjectOfType<PlayerController>();
        }
        
        private bool wasInTreatmentMode = false;

        private void Update()
        {
            // Check if treatment mode just ended - hide decal
            bool isInTreatmentMode = playerController != null && playerController.IsInTreatmentMode;
            if (wasInTreatmentMode && !isInTreatmentMode)
            {
                // Treatment ended - hide decal and reset state
                HideCurrentDecal();
            }
            wasInTreatmentMode = isInTreatmentMode;
            
            if (isEquipped)
            {
                UpdateDecalPosition();
            }
            else if (currentMaterials != null)
            {
                foreach (var mat in currentMaterials)
                {
                    if (mat != null) mat.SetFloat("_DecalEnabled", 0);
                }
                currentMaterials = null;
            }
        }
        
        private void HideCurrentDecal()
        {
            if (currentBodyPart != null)
            {
                var controller = currentBodyPart.GetComponent<HairTreatmentController>();
                if (controller != null)
                {
                    controller.HideDecal();
                }
            }
            if (previousTreatmentController != null)
            {
                previousTreatmentController.HideDecal();
                previousTreatmentController = null;
            }
            currentBodyPart = null;
            isHoveringBodyPart = false;
        }

        private void UpdateDecalPosition()
        {
            if (mainCamera == null) return;

            // Get ray from crosshair position (uses PlayerController's crosshair in treatment mode)
            Ray ray;
            if (playerController != null && playerController.IsInTreatmentMode)
            {
                // Use crosshair viewport position
                Vector3 viewportPoint = new Vector3(
                    playerController.CrosshairViewportPosition.x,
                    playerController.CrosshairViewportPosition.y,
                    0f
                );
                ray = mainCamera.ViewportPointToRay(viewportPoint);
            }
            else
            {
                // Default to camera center
                ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            }
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10f))
            {
                // Try to get HairTreatmentController directly (it should be on the same object as BodyPart)
                HairTreatmentController treatmentController = hit.collider.GetComponent<HairTreatmentController>();
                
                // If not found, try BodyPart and get controller from it
                if (treatmentController == null)
                {
                    BodyPart bodyPart = hit.collider.GetComponent<BodyPart>();
                    if (bodyPart != null)
                    {
                        treatmentController = bodyPart.GetComponent<HairTreatmentController>();
                    }
                }

                if (treatmentController != null)
                {
                    // Check if the customer is in treatment state
                    var customerController = treatmentController.GetComponentInParent<HairRemovalSim.Customer.CustomerController>();
                    if (customerController != null && customerController.CurrentState != HairRemovalSim.Customer.CustomerController.CustomerState.InTreatment)
                    {
                        // Customer is not in treatment state - hide decal and skip
                        if (previousTreatmentController != null)
                        {
                            previousTreatmentController.HideDecal();
                            previousTreatmentController = null;
                        }
                        isHoveringBodyPart = false;
                        currentBodyPart = null;
                        return;
                    }
                    
                    // Check if we switched to a different BodyPart
                    if (currentBodyPart != null && currentBodyPart != treatmentController.GetComponent<BodyPart>())
                    {
                        // Hide decal on previous controller if we're switching to a different one
                        if (previousTreatmentController != null && previousTreatmentController != treatmentController)
                        {
                            previousTreatmentController.HideDecal();
                        }
                        previousTreatmentController = treatmentController;
                    }

                    isHoveringBodyPart = true;
                    currentBodyPart = treatmentController.GetComponent<BodyPart>();
                    currentHit = hit;

                    Renderer renderer = treatmentController.GetComponent<Renderer>();
                    if (renderer != null && renderer.materials != null)
                    {
                        currentMaterials = renderer.materials;

                        // Calculate UV rect for decal display
                        UVRect uvRect = CalculateUVRect(hit, decalWidth, decalHeight);
                        
                        // Determine submesh index to update the correct material/mask
                        int subMeshIndex = GetSubMeshIndex(hit);
                        
                        // Update decal on the specific submesh
                        treatmentController.UpdateDecal(uvRect.center, uvRect.angle, new Vector2(decalWidth, decalHeight), emissionColor, subMeshIndex);
                        
                        return;
                    }
                }
            }
            
            // No body part hit - disable decal
            if (currentBodyPart != null)
            {
                var controller = currentBodyPart.GetComponent<HairTreatmentController>();
                if (controller != null)
                {
                    controller.HideDecal();
                }
            }
            
            isHoveringBodyPart = false;
            currentBodyPart = null;
        }
        
        private int GetSubMeshIndex(RaycastHit hit)
        {
            // Debug.Log($"[DuctTape] Hit Collider: {hit.collider.GetType().Name}, TriangleIndex: {hit.triangleIndex}");

            if (hit.triangleIndex == -1) 
            {
                // Debug.LogWarning("[DuctTape] Hit triangle index is -1. Raycast might not be hitting a MeshCollider.");
                return 0; // Fallback
            }

            Mesh mesh = null;

            if (hit.collider is MeshCollider meshCollider)
            {
                mesh = meshCollider.sharedMesh;
            }
            else
            {
                // If hitting a primitive collider on a SkinnedMeshRenderer, we can't get submesh index from triangle index.
                // We need to assume based on material or something else?
                // Or maybe we are hitting the SkinnedMeshRenderer's baked mesh collider?
                SkinnedMeshRenderer smr = hit.collider.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    mesh = smr.sharedMesh;
                    // Note: Raycast against non-MeshCollider (like Capsule) returns triangleIndex -1 usually.
                    // If we have a MeshCollider attached to the same object as SMR, it should work.
                }
            }

            if (mesh != null)
            {
                int triangleCounter = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int triangleCount = (int)mesh.GetIndexCount(i) / 3;
                    if (hit.triangleIndex < triangleCounter + triangleCount)
                    {
                        // Debug.Log($"[DuctTape] Hit SubMesh Index: {i}");
                        return i;
                    }
                    triangleCounter += triangleCount;
                }
            }
            
            return 0;
        }

        public override void OnUseDown()
        {
            if (!isHoveringBodyPart || currentBodyPart == null) return;

            // For Single type tools, use immediately
            if (toolType == ToolType.Single)
            {
                if (Time.time - lastUseTime >= useInterval)
                {
                    PerformRemoval();
                    lastUseTime = Time.time;
                }
            }
            // Continuous tools also start on down
            else
            {
                PerformRemoval();
                lastUseTime = Time.time;
            }
        }

        public override void OnUseUp()
        {
            if (currentBodyPart != null)
            {
                var controller = currentBodyPart.GetComponent<HairTreatmentController>();
                if (controller != null)
                {
                    controller.HideDecal();
                }
            }
        }

        private void PerformRemoval()
        {
            if (currentHit.collider == null) return;
            
            HairTreatmentController controller = currentHit.collider.GetComponent<HairTreatmentController>();
            if (controller == null)
            {
                controller = currentHit.collider.GetComponentInParent<HairTreatmentController>();
            }

            if (controller != null)
            {
                UVRect uvRect = CalculateUVRect(currentHit, decalWidth, decalHeight);
                int subMeshIndex = GetSubMeshIndex(currentHit);
                
                // Use normalized UV (0-1) for mask painting, original UV is for decal display
                Vector2 normalizedUV = GetNormalizedUV(uvRect.center);
                
                // Apply treatment to the specific submesh mask with size and angle
                controller.ApplyTreatment(normalizedUV, new Vector2(decalWidth, decalHeight), uvRect.angle, subMeshIndex);
                
                // Add pain
                var customer = controller.GetComponentInParent<Customer.CustomerController>();
                if (customer != null)
                {
                    customer.AddPain(painMultiplier);
                }
                // Play sound or effect here
            }
        }

        private UVRect CalculateUVRect(RaycastHit hit, float width, float height)
        {
            UVRect rect = new UVRect();
            // Use original UV for decal display (matches mesh UVs)
            rect.center = hit.textureCoord;
            rect.size = new Vector2(width, height);
            rect.angle = 0f;
            return rect;
        }
        
        // Get normalized UV for mask painting (0-1 range)
        private Vector2 GetNormalizedUV(Vector2 uv)
        {
            return new Vector2(
                Mathf.Repeat(uv.x, 1.0f),
                Mathf.Repeat(uv.y, 1.0f)
            );
        }
        
        public override void OnUseDrag(Vector3 delta)
        {
            // Optional: Implement drag logic if needed for duct tape (e.g. stretching)
        }
        
        public void Equip()
        {
            // base.Equip(); // ToolBase doesn't have Equip
            isEquipped = true;
        }

        public void Unequip()
        {
            // base.Unequip(); // ToolBase doesn't have Unequip
            isEquipped = false;
            // Hide decal
            if (currentBodyPart != null)
            {
                var controller = currentBodyPart.GetComponent<HairTreatmentController>();
                if (controller != null)
                {
                    controller.HideDecal();
                }
            }
        }
        
        /// <summary>
        /// Check if a UV coordinate is inside a completed body part's region
        /// </summary>
        private bool IsUVInCompletedPart(HairRemovalSim.Customer.CustomerController customer, string partName, Vector2 uv)
        {
            if (customer == null || customer.bodyPartsDatabase == null) return false;
            
            var partDef = customer.bodyPartsDatabase.GetPartByName(partName);
            if (partDef == null || partDef.uvRegions == null) return false;
            
            foreach (var region in partDef.uvRegions)
            {
                if (region.rect.Contains(uv))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
