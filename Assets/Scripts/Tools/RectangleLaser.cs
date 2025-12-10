using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;
using HairRemovalSim.Treatment;

namespace HairRemovalSim.Tools
{
    public class RectangleLaser : ToolBase
    {
        [Header("Rectangle Laser Settings")]
        public float decalWidth = 0.08f;
        public float decalHeight = 0.12f;
        public float uvScaleFactor = 1.0f; // Adjust this to match decal visual with removal area
        [ColorUsage(true, true)]
        public Color emissionColor = new Color(2f, 2f, 0.5f, 1f);
        
        private Camera mainCamera;
        private PlayerController playerController;
        private InteractionController interactionController;
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
            interactionController = FindObjectOfType<InteractionController>();
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
                    
                    
                    // Note: We allow decal to follow even when pain is at max
                    // Treatment application is blocked in ApplyTreatment() instead
                    
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
                        
                        // Update DecalPivot position/rotation if enabled (tool will follow via parenting)
                        if (followDecalPosition && interactionController != null)
                        {
                            // Position: hit point + offset along normal
                            Vector3 pivotPosition = hit.point + hit.normal * decalTrackingDistance;
                            
                            // Get UV tangent direction for orientation
                            Vector3 uvTangent = GetUVTangentDirection(hit);
                            
                            interactionController.UpdateDecalPivot(pivotPosition, hit.normal, uvTangent);
                            interactionController.MoveToolToDecalPivot();
                        }
                        
                        return;
                    }
                }
            }
            
            // No body part hit - disable decal and return tool to hand
            if (currentBodyPart != null)
            {
                var controller = currentBodyPart.GetComponent<HairTreatmentController>();
                if (controller != null)
                {
                    controller.HideDecal();
                }
            }
            
            // Return tool to hand when not hovering
            if (followDecalPosition && interactionController != null)
            {
                interactionController.ReturnToolToHand();
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
                // Check if customer can receive treatment (not in pain stage 3)
                var customer = controller.GetComponentInParent<Customer.CustomerController>();
                if (customer != null && !customer.CanReceiveTreatment())
                {
                    // Customer is in extreme pain - skip treatment but allow decal to follow
                    return;
                }
                
                UVRect uvRect = CalculateUVRect(currentHit, decalWidth, decalHeight);
                int subMeshIndex = GetSubMeshIndex(currentHit);
                
                // Use normalized UV (0-1) for mask painting, original UV is for decal display
                Vector2 normalizedUV = GetNormalizedUV(uvRect.center);
                
                // Apply treatment to the specific submesh mask with size and angle
                controller.ApplyTreatment(normalizedUV, new Vector2(decalWidth, decalHeight), uvRect.angle, subMeshIndex);
                
                // Add pain (Pain SE is handled by CustomerController)
                if (customer != null)
                {
                    customer.AddPain(painMultiplier);
                }
                
                // Play laser sound effect
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("RectangleLaser");
                }
                
                // Play laser smoke effect at hit point
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect("LaserSmokeEffect", currentHit.point);
                    
                    // Play laser beam effect attached to the tool
                    EffectManager.Instance.PlayEffectAttached("LaserBeamEffect", transform, new Vector3(0.083f, 0.371f, 0f));
                }
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
        
        /// <summary>
        /// Get the UV tangent direction in world space from the raycast hit.
        /// This is used to orient the tool along the UV U-axis direction.
        /// </summary>
        private Vector3 GetUVTangentDirection(RaycastHit hit)
        {
            // Try to get mesh tangent data
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
            {
                // Fallback: use world up projected onto the surface
                return Vector3.ProjectOnPlane(Vector3.up, hit.normal).normalized;
            }
            
            Mesh mesh = meshCollider.sharedMesh;
            Vector4[] tangents = mesh.tangents;
            
            if (tangents == null || tangents.Length == 0)
            {
                // No tangent data, fallback
                return Vector3.ProjectOnPlane(Vector3.up, hit.normal).normalized;
            }
            
            // Get triangle vertices
            int[] triangles = mesh.triangles;
            int triIndex = hit.triangleIndex * 3;
            
            if (triIndex + 2 >= triangles.Length)
            {
                return Vector3.ProjectOnPlane(Vector3.up, hit.normal).normalized;
            }
            
            int i0 = triangles[triIndex];
            int i1 = triangles[triIndex + 1];
            int i2 = triangles[triIndex + 2];
            
            // Get tangents at each vertex
            Vector4 t0 = i0 < tangents.Length ? tangents[i0] : Vector4.zero;
            Vector4 t1 = i1 < tangents.Length ? tangents[i1] : Vector4.zero;
            Vector4 t2 = i2 < tangents.Length ? tangents[i2] : Vector4.zero;
            
            // Interpolate tangent using barycentric coordinates
            Vector3 bary = hit.barycentricCoordinate;
            Vector3 tangent = (new Vector3(t0.x, t0.y, t0.z) * bary.x +
                               new Vector3(t1.x, t1.y, t1.z) * bary.y +
                               new Vector3(t2.x, t2.y, t2.z) * bary.z).normalized;
            
            // Transform tangent to world space
            Transform meshTransform = meshCollider.transform;
            tangent = meshTransform.TransformDirection(tangent);
            
            if (tangent.sqrMagnitude < 0.001f)
            {
                return Vector3.ProjectOnPlane(Vector3.up, hit.normal).normalized;
            }
            
            return tangent.normalized;
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
