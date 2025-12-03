using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;

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
        private BodyPart currentBodyPart;
        private RaycastHit currentHit;
        private bool isHoveringBodyPart = false;
        private Material[] currentMaterials;
        private bool isEquipped = false;

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
        }

        private void Update()
        {
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

        private void UpdateDecalPosition()
        {
            if (mainCamera == null) return;

            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10f))
            {
                // Debug.Log($"Raycast Hit: {hit.collider.name}");
                BodyPart bodyPart = hit.collider.GetComponent<BodyPart>();
                if (bodyPart != null)
                {
                    // Check if we switched to a different BodyPart
                    if (currentBodyPart != null && currentBodyPart != bodyPart && currentMaterials != null)
                    {
                        // Disable decal on the previous BodyPart
                        foreach (var mat in currentMaterials)
                        {
                            if (mat != null) mat.SetFloat("_DecalEnabled", 0);
                        }
                    }

                    isHoveringBodyPart = true;
                    currentBodyPart = bodyPart;
                    currentHit = hit;

                    Renderer renderer = bodyPart.GetComponent<Renderer>();
                    if (renderer != null && renderer.materials != null)
                    {
                        currentMaterials = renderer.materials;

                        // Calculate UV rect for decal display
                        UVRect uvRect = CalculateUVRect(hit, decalWidth, decalHeight);

                        // Set UV-based decal parameters on ALL materials
                        foreach (var mat in currentMaterials)
                        {
                            if (mat == null) continue;
                            mat.SetVector("_DecalUVCenter", uvRect.center);
                            mat.SetVector("_DecalUVSize", uvRect.size);
                            mat.SetFloat("_DecalUVAngle", uvRect.angle);
                            mat.SetColor("_DecalColor", emissionColor);
                            mat.SetFloat("_DecalEnabled", 1);
                        }
                    }
                    
                    return;
                }
            }
            
            // No body part hit - disable decal
            if (currentMaterials != null)
            {
                foreach (var mat in currentMaterials)
                {
                    if (mat != null) mat.SetFloat("_DecalEnabled", 0);
                }
            }
            isHoveringBodyPart = false;
            currentBodyPart = null;
        }

        public override void OnUseDown()
        {
            Debug.Log($"DuctTape: OnUseDown. Hovering: {isHoveringBodyPart}, BodyPart: {(currentBodyPart != null ? currentBodyPart.name : "null")}");
            if (!isHoveringBodyPart || currentBodyPart == null) return;

            Debug.Log($"DuctTape: ToolType: {toolType}, Time: {Time.time}, LastUse: {lastUseTime}, Interval: {useInterval}");

            // For Single type tools, use immediately
            if (toolType == ToolType.Single)
            {
                if (Time.time - lastUseTime >= useInterval)
                {
                    PerformRemoval();
                    lastUseTime = Time.time;
                }
                else
                {
                    Debug.Log("DuctTape: Single use blocked by interval.");
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
            if (currentMaterials != null)
            {
                foreach (var mat in currentMaterials)
                {
                    if (mat != null) mat.SetFloat("_DecalEnabled", 0);
                }
            }
            
            // Update completion when releasing
            if (currentBodyPart != null && currentBodyPart.treatmentController != null)
            {
                currentBodyPart.treatmentController.UpdateCompletion();
            }
        }

        public override void OnUseDrag(Vector3 delta)
        {
            // Only for Continuous tools
            if (toolType == ToolType.Continuous && isHoveringBodyPart && currentBodyPart != null)
            {
                if (Time.time - lastUseTime >= useInterval)
                {
                    PerformRemoval();
                    lastUseTime = Time.time;
                }
            }
        }



        private Vector3 CalculateCameraAlignedTangent(Vector3 normal)
        {
            // Project camera right vector onto the surface plane
            Vector3 cameraRight = mainCamera.transform.right;
            Vector3 tangent = Vector3.ProjectOnPlane(cameraRight, normal).normalized;
            
            // Handle degenerate case (looking straight down/up relative to surface)
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = Vector3.Cross(normal, mainCamera.transform.up).normalized;
            }
            
            return tangent;
        }

        private void PerformRemoval()
        {
            // Check if customer is ready
            var customer = currentBodyPart.GetComponentInParent<Customer.CustomerController>();
            if (customer != null)
            {
                if (!customer.IsReadyForTreatment)
                {
                    Debug.LogWarning($"DuctTape: Customer {customer.name} is NOT ready for treatment. State: {customer.IsReadyForTreatment}");
                    return;
                }
            }

            UVRect uvRect = CalculateUVRect(currentHit, decalWidth, decalHeight);
            Debug.Log($"DuctTape: Removing at UV {uvRect.center}, Size {uvRect.size}, Angle {uvRect.angle * Mathf.Rad2Deg} deg");
            
            if (currentBodyPart.treatmentController != null)
            {
                currentBodyPart.treatmentController.RemoveHairInRect(uvRect.center, uvRect.size, uvRect.angle);
            }
            else
            {
                Debug.LogWarning("DuctTape: No TreatmentController found on body part.");
            }
        }

        private UVRect CalculateUVRect(RaycastHit centerHit, float width, float height)
        {
            Vector3 normal = centerHit.normal;
            Vector3 tangent = CalculateCameraAlignedTangent(normal);
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            // Calculate world positions for 3 points to determine UV basis
            Vector3 center = centerHit.point;
            Vector3 right = center + tangent * (width * 0.5f);
            Vector3 up = center + bitangent * (height * 0.5f);

            Vector2 centerUV = centerHit.textureCoord;
            Vector2 rightUV = centerUV;
            Vector2 upUV = centerUV;
            
            // Raycast to find UVs of right and up points
            float rayOffset = 0.1f;
            RaycastHit hit;
            
            if (currentBodyPart.GetComponent<Collider>().Raycast(new Ray(right + normal * rayOffset, -normal), out hit, rayOffset * 2f))
            {
                rightUV = hit.textureCoord;
            }
            if (currentBodyPart.GetComponent<Collider>().Raycast(new Ray(up + normal * rayOffset, -normal), out hit, rayOffset * 2f))
            {
                upUV = hit.textureCoord;
            }

            // Calculate UV space vectors
            Vector2 uvTangent = rightUV - centerUV;
            Vector2 uvBitangent = upUV - centerUV;

            // Calculate dimensions in UV space (magnitude of vectors)
            // Note: We multiply by 2 because we used half-width/height
            float uvWidth = uvTangent.magnitude * 2.0f;
            float uvHeight = uvBitangent.magnitude * 2.0f;

            // Calculate rotation angle in UV space
            // Angle of uvTangent relative to UV X-axis
            float angle = Mathf.Atan2(uvTangent.y, uvTangent.x);

            // Define expected size and maximum allowed size
            float expectedWidth = width * uvScaleFactor;
            float expectedHeight = height * uvScaleFactor;
            float maxAllowedWidth = expectedWidth * 5.0f; // Allow up to 5x expected size
            float maxAllowedHeight = expectedHeight * 5.0f;

            // Sanity check - minimum values
            if (uvWidth < 0.0001f || float.IsNaN(uvWidth)) uvWidth = expectedWidth;
            if (uvHeight < 0.0001f || float.IsNaN(uvHeight)) uvHeight = expectedHeight;
            if (float.IsNaN(angle)) angle = 0f;

            // Safety check - maximum values to prevent removing entire body part
            if (uvWidth > maxAllowedWidth)
            {
                Debug.LogWarning($"DuctTape: UV width ({uvWidth}) exceeds maximum ({maxAllowedWidth}). Clamping to expected size. Decal may be distorted.");
                uvWidth = expectedWidth;
            }
            if (uvHeight > maxAllowedHeight)
            {
                Debug.LogWarning($"DuctTape: UV height ({uvHeight}) exceeds maximum ({maxAllowedHeight}). Clamping to expected size. Decal may be distorted.");
                uvHeight = expectedHeight;
            }

            return new UVRect 
            { 
                center = centerUV, 
                size = new Vector2(uvWidth, uvHeight),
                angle = angle
            };
        }

        // Called when tool is equipped
        public void OnEquip()
        {
            isEquipped = true;
            Debug.Log("DuctTape: Equipped");
        }

        // Called when tool is unequipped
        public void OnUnequip()
        {
            isEquipped = false;
            if (currentMaterials != null)
            {
                foreach (var mat in currentMaterials)
                {
                    if (mat != null) mat.SetFloat("_DecalEnabled", 0);
                }
                currentMaterials = null;
            }
            Debug.Log("DuctTape: Unequipped");
        }

        private void OnDisable()
        {
            OnUnequip();
        }
    }
}
