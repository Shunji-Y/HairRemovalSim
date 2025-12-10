using UnityEngine;
using UnityEngine.InputSystem;
using HairRemovalSim.Interaction;
using HairRemovalSim.Tools;
using HairRemovalSim.UI;

namespace HairRemovalSim.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class InteractionController : MonoBehaviour
    {
        [Header("Settings")]
        public float interactionDistance = 3.0f;
        public LayerMask interactionLayer;

        [Header("References")]
        public Transform cameraTransform;

        [Header("Equipment")]
        public Transform handPoint;
        [Tooltip("Pivot point that follows decal position on mesh surface")]
        public Transform decalPivot;
        public ToolBase currentTool;

        private IInteractable currentInteractable;
        private PlayerInput playerInput;
        private InputAction interactAction;
        private InputAction attackAction;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            interactAction = playerInput.actions["Interact"];
            attackAction = playerInput.actions["Attack"];
            
            // Create DecalPivot if not assigned
            if (decalPivot == null)
            {
                GameObject pivotObj = new GameObject("DecalPivot");
                decalPivot = pivotObj.transform;
                // Don't parent to player - it will move independently
            }
        }

        private void Update()
        {
            HandleRaycast();
            HandleInput();
        }

        private void HandleRaycast()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    if (currentInteractable != interactable)
                    {
                        if (currentInteractable != null) currentInteractable.OnHoverExit();
                        currentInteractable = interactable;
                        currentInteractable.OnHoverEnter();
                        
                        // Show prompt UI
                        HUDManager.Instance?.ShowInteractionPrompt(currentInteractable.GetInteractionPrompt());
                    }
                }
                else
                {
                    ClearCurrentInteractable();
                }
            }
            else
            {
                ClearCurrentInteractable();
            }
        }

        private void HandleInput()
        {
            // Unified Input Handling (Left Click = Interact)
            
            // 1. Interaction (Priority)
            if (interactAction.WasPressedThisFrame())
            {
                if (currentInteractable != null)
                {
                    Debug.Log("Input: Interact (Object Interaction)");
                    currentInteractable.OnInteract(this);
                    return; // Consume input
                }
            }

            // 2. Tool Usage (Fallback)
            if (currentTool != null)
            {
                if (interactAction.WasPressedThisFrame())
                {
                    Debug.Log("Input: Interact (Tool Use Down)");
                    currentTool.OnUseDown();
                }
                else if (interactAction.WasReleasedThisFrame())
                {
                    currentTool.OnUseUp();
                }
                
                if (interactAction.IsPressed())
                {
                    currentTool.OnUseDrag(Vector3.zero);
                }
            }
        }

        public void EquipTool(ToolBase tool)
        {
            if (currentTool != null)
            {
                // Call Unequip if supported
                var oldRectangleLaser = currentTool as RectangleLaser;
                if (oldRectangleLaser != null) oldRectangleLaser.Unequip();
                
                // Drop current tool
                currentTool.transform.SetParent(null);
                currentTool.GetComponent<Collider>().enabled = true;
                currentTool.gameObject.AddComponent<Rigidbody>();
            }

            currentTool = tool;
            
            // Disable physics
            Rigidbody rb = tool.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider col = tool.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Parent to appropriate transform based on tool settings
            if (tool.followDecalPosition && decalPivot != null)
            {
                // Parent to DecalPivot for surface-following tools
                tool.transform.SetParent(decalPivot);
                tool.transform.localPosition = tool.decalTrackingPositionOffset;
                tool.transform.localRotation = Quaternion.Euler(tool.decalTrackingRotationOffset);
            }
            else
            {
                // Parent to HandPoint for normal hand-held tools
                tool.transform.SetParent(handPoint);
                tool.transform.localPosition = tool.handPositionOffset;
                tool.transform.localRotation = Quaternion.Euler(tool.handRotationOffset);
            }
            
            // Call Equip if supported
            var newRectangleLaser = tool as RectangleLaser;
            if (newRectangleLaser != null) newRectangleLaser.Equip();
            
            Debug.Log($"Equipped: {tool.toolName} (followDecal: {tool.followDecalPosition})");
        }
        
        /// <summary>
        /// Update DecalPivot position and rotation. Called by tool when hovering over body part.
        /// Uses UV tangent for orientation, both position and rotation are smoothly interpolated.
        /// </summary>
        public void UpdateDecalPivot(Vector3 position, Vector3 normal, Vector3 uvTangent)
        {
            if (decalPivot != null)
            {
                // Smooth speed from tool settings
                float smoothSpeed = currentTool != null ? currentTool.decalTrackingSmoothSpeed : 10f;
                
                // Smooth position to prevent snapping at polygon edges
                if (smoothSpeed > 0)
                {
                    decalPivot.position = Vector3.Lerp(decalPivot.position, position, Time.deltaTime * smoothSpeed);
                }
                else
                {
                    decalPivot.position = position;
                }
                
                // Target rotation: forward = normal (towards surface), up = UV tangent direction
                // Use LookRotation with normal as forward and tangent as up hint
                Quaternion targetRotation;
                if (uvTangent.sqrMagnitude > 0.001f)
                {
                    // Calculate up vector that's perpendicular to normal using tangent as reference
                    Vector3 right = Vector3.Cross(normal, uvTangent).normalized;
                    Vector3 adjustedUp = Vector3.Cross(right, normal).normalized;
                    targetRotation = Quaternion.LookRotation(normal, adjustedUp);
                }
                else
                {
                    targetRotation = Quaternion.LookRotation(normal);
                }
                
                // Smooth rotation to prevent abrupt changes
                if (smoothSpeed > 0)
                {
                    decalPivot.rotation = Quaternion.Slerp(decalPivot.rotation, targetRotation, Time.deltaTime * smoothSpeed);
                }
                else
                {
                    decalPivot.rotation = targetRotation;
                }
            }
        }
        
        /// <summary>
        /// Return tool from DecalPivot back to HandPoint when decal is not visible
        /// </summary>
        public void ReturnToolToHand()
        {
            if (currentTool != null && currentTool.followDecalPosition && handPoint != null)
            {
                if (currentTool.transform.parent != handPoint)
                {
                    currentTool.transform.SetParent(handPoint);
                    currentTool.transform.localPosition = currentTool.handPositionOffset;
                    currentTool.transform.localRotation = Quaternion.Euler(currentTool.handRotationOffset);
                }
            }
        }
        
        /// <summary>
        /// Move tool to DecalPivot when decal is visible
        /// </summary>
        public void MoveToolToDecalPivot()
        {
            if (currentTool != null && currentTool.followDecalPosition && decalPivot != null)
            {
                if (currentTool.transform.parent != decalPivot)
                {
                    currentTool.transform.SetParent(decalPivot);
                    currentTool.transform.localPosition = currentTool.decalTrackingPositionOffset;
                    currentTool.transform.localRotation = Quaternion.Euler(currentTool.decalTrackingRotationOffset);
                }
            }
        }

        private void ClearCurrentInteractable()
        {
            if (currentInteractable != null)
            {
                currentInteractable.OnHoverExit();
                currentInteractable = null;
                
                HUDManager.Instance?.HideInteractionPrompt();
            }
        }
    }
}
