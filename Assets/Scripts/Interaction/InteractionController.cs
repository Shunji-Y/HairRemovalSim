using UnityEngine;
using UnityEngine.InputSystem;
using HairRemovalSim.Interaction;
using HairRemovalSim.Tools;
using HairRemovalSim.UI;
using HairRemovalSim.Effects;

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

        [Header("Right Hand Equipment")]
        public Transform rightHandPoint;
        [Tooltip("Pivot point that follows decal position on mesh surface")]
        public Transform decalPivot;
        public ToolBase currentTool; // Right hand tool (left click)
        
        /// <summary>
        /// Public accessor for the current right-hand tool
        /// </summary>
        public ToolBase CurrentTool => currentTool;
        
        [Header("Left Hand Equipment")]
        public Transform leftHandPoint;
        public ToolBase leftHandTool; // Left hand tool (right click)

        private IInteractable currentInteractable;
        private OutlineHighlighter currentHighlighter;
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
            // Skip input when UI panels are open
            if (UI.WarehousePanel.Instance != null && UI.WarehousePanel.Instance.IsOpen)
            {
                return;
            }
            if (UI.ReceptionPanel.Instance != null && UI.ReceptionPanel.Instance.IsOpen)
            {
                return;
            }
            
            HandleRaycast();
            HandleInput();
        }

        private void HandleRaycast()
        {
            // Get ray from crosshair position (uses PlayerController's crosshair in treatment mode)
            Ray ray;
            var playerController = GetComponent<PlayerController>();
            Camera mainCamera = Camera.main;
            
            if (playerController != null && playerController.IsInTreatmentMode && mainCamera != null)
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
                ray = new Ray(cameraTransform.position, cameraTransform.forward);
            }
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
            {
                // Handle OutlineHighlighter (automatic highlighting)
                OutlineHighlighter highlighter = hit.collider.GetComponent<OutlineHighlighter>();
                if (highlighter != currentHighlighter)
                {
                    // Unhighlight previous (use explicit null check for Unity destroyed objects)
                    if (currentHighlighter != null)
                    {
                        currentHighlighter.Unhighlight();
                    }
                    // Highlight new
                    currentHighlighter = highlighter;
                    if (currentHighlighter != null)
                    {
                        currentHighlighter.Highlight();
                    }
                }
                
                // Handle IInteractable
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
                // Clear highlighter when not looking at anything
                if (currentHighlighter != null)
                {
                    currentHighlighter.Unhighlight();
                    currentHighlighter = null;
                }
                
                ClearCurrentInteractable();
            }
        }

        private void HandleInput()
        {
            // Dual-Hand Input System:
            // Left Click (interactAction) = Interact or Right Hand Tool
            // Right Click (attackAction) = Left Hand Tool
            
            // 1. Interaction (Priority - Left Click)
            // UNLESS tool is aiming at a valid target (e.g. skin treatment)
            bool prioritizeTool = currentTool != null && currentTool.IsHoveringTarget;

            if (interactAction.WasPressedThisFrame() && !prioritizeTool)
            {
                if (currentInteractable != null)
                {
                   // Debug.Log("Input: Interact (Object Interaction)");
                    currentInteractable.OnInteract(this);
                    
                    // Hide prompt after interaction
                    HUDManager.Instance?.HideInteractionPrompt();
                    return; // Consume input
                }
            }

            // 2. Right Hand Tool Usage (Left Click)
            if (currentTool != null)
            {
                if (interactAction.WasPressedThisFrame())
                {
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
            
            // 3. Left Hand Tool Usage (Right Click)
            if (leftHandTool != null)
            {
                if (attackAction.WasPressedThisFrame())
                {
                    leftHandTool.OnUseDown();
                }
                else if (attackAction.WasReleasedThisFrame())
                {
                    leftHandTool.OnUseUp();
                }
                
                // Re-check null in case tool was destroyed during OnUseDown
                if (leftHandTool != null && attackAction.IsPressed())
                {
                    leftHandTool.OnUseDrag(Vector3.zero);
                }
            }
        }

        public void EquipTool(ToolBase tool)
        {
            if (tool.GetHandType() == ToolBase.HandType.RightHand)
            {
                EquipRightHand(tool);
            }
            else
            {
                EquipLeftHand(tool);
            }
        }
        
        /// <summary>
        /// Unequip current right hand tool without dropping it.
        /// Used when placing tool back on a slot.
        /// </summary>
        public void UnequipCurrentTool()
        {
            if (currentTool == null) return;
            
            // Call Unequip on right hand tool (hides decal etc)
            var rightHandTool = currentTool as RightHandTool;
            if (rightHandTool != null) rightHandTool.Unequip();
            
            // Detach from hand but don't drop
            currentTool.transform.SetParent(null);
            
            // Update UI
            if (EquippedToolUI.Instance != null) EquippedToolUI.Instance.SetRightHandUI(null);
            
            Debug.Log($"Unequipped (Right Hand): {currentTool.toolName}");
            currentTool = null;
        }
        
        private void EquipRightHand(ToolBase tool)
        {
            if (currentTool != null)
            {
                // Call Unequip on old right hand tool (hides decal etc)
                var oldRightHandTool = currentTool as RightHandTool;
                if (oldRightHandTool != null) oldRightHandTool.Unequip();
                
                // Drop current tool
                DropTool(currentTool);
            }

            currentTool = tool;
            
            // Disable physics
            DisableToolPhysics(tool);

            // Check if this is a RightHandTool with decal tracking
            var rightHandTool = tool as RightHandTool;
            
            // Parent to appropriate transform based on tool settings
            if (rightHandTool != null && rightHandTool.followDecalPosition && decalPivot != null)
            {
                // Parent to DecalPivot for surface-following tools
                tool.transform.SetParent(decalPivot);
                tool.transform.localPosition = rightHandTool.decalTrackingPositionOffset;
                tool.transform.localRotation = Quaternion.Euler(rightHandTool.decalTrackingRotationOffset);
            }
            else
            {
                // Parent to RightHandPoint for normal hand-held tools
                tool.transform.SetParent(rightHandPoint);
                tool.transform.localPosition = tool.handPositionOffset;
                tool.transform.localRotation = Quaternion.Euler(tool.handRotationOffset);
            }
            

            
            // Call Equip if supported
            var newRectangleLaser = tool as RectangleLaser;
            if (newRectangleLaser != null) newRectangleLaser.Equip();
            
            // Update UI
            if (EquippedToolUI.Instance != null) EquippedToolUI.Instance.SetRightHandUI(tool);
            
            // Tutorial trigger for laser equipment
            if (tool is Tools.RectangleLaser && tool.itemData != null)
            {
                if (tool.itemData.targetArea == Core.ToolTargetArea.Face)
                {
                    Core.TutorialManager.Instance?.TryShowTutorial("tut_face_laser");
                }
                else if (tool.itemData.targetArea == Core.ToolTargetArea.Body)
                {
                    Core.TutorialManager.Instance?.TryShowTutorial("tut_body_laser");
                }
            }
            
            Debug.Log($"Equipped (Right Hand): {tool.toolName}");
        }
        
        private void EquipLeftHand(ToolBase tool)
        {
            if (leftHandTool != null)
            {
                // Unequip current left hand tool
                var oldLeftTool = leftHandTool as LeftHandTool;
                if (oldLeftTool != null) oldLeftTool.Unequip();
                
                // Drop current left hand tool
                DropTool(leftHandTool);
            }

            leftHandTool = tool;
            
            // Disable physics
            DisableToolPhysics(tool);

            // Parent to LeftHandPoint
            tool.transform.SetParent(leftHandPoint);
            tool.transform.localPosition = tool.handPositionOffset;
            tool.transform.localRotation = Quaternion.Euler(tool.handRotationOffset);
            
            // Call Equip if LeftHandTool
            var newLeftTool = tool as LeftHandTool;
            if (newLeftTool != null) newLeftTool.Equip(this);
            
            // Update UI
            if (EquippedToolUI.Instance != null) EquippedToolUI.Instance.SetLeftHandUI(tool);
            
            Debug.Log($"Equipped (Left Hand): {tool.toolName}");
        }
        
        /// <summary>
        /// Remove left hand tool (called when tool breaks)
        /// </summary>
        public void RemoveLeftHandTool()
        {
            leftHandTool = null;
            
            // Update UI
            if (EquippedToolUI.Instance != null) EquippedToolUI.Instance.SetLeftHandUI(null);
        }
        
        private void DropTool(ToolBase tool)
        {
            tool.transform.SetParent(null);
            Collider col = tool.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            tool.gameObject.AddComponent<Rigidbody>();
        }
        
        private void DisableToolPhysics(ToolBase tool)
        {
            Rigidbody rb = tool.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider col = tool.GetComponent<Collider>();
            if (col != null) col.enabled = false;
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
                var rightHandTool = currentTool as RightHandTool;
                float smoothSpeed = rightHandTool != null ? rightHandTool.decalTrackingSmoothSpeed : 10f;
                
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
                    // Use UV tangent as up direction
                    targetRotation = Quaternion.LookRotation(normal, uvTangent);
                }
                else
                {
                    // Fallback: use world up
                    targetRotation = Quaternion.LookRotation(normal, Vector3.up);
                }
                
                // Smooth rotation
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
            var rightHandTool = currentTool as RightHandTool;
            if (rightHandTool != null && rightHandTool.followDecalPosition && rightHandPoint != null)
            {
                if (currentTool.transform.parent != rightHandPoint)
                {
                    currentTool.transform.SetParent(rightHandPoint);
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
            var rightHandTool = currentTool as RightHandTool;
            if (rightHandTool != null && rightHandTool.followDecalPosition && decalPivot != null)
            {
                if (currentTool.transform.parent != decalPivot)
                {
                    currentTool.transform.SetParent(decalPivot);
                    currentTool.transform.localPosition = rightHandTool.decalTrackingPositionOffset;
                    currentTool.transform.localRotation = Quaternion.Euler(rightHandTool.decalTrackingRotationOffset);
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
