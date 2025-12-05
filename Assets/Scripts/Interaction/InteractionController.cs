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
                var oldDuctTape = currentTool as DuctTape;
                if (oldDuctTape != null) oldDuctTape.Unequip();
                
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

            tool.transform.SetParent(handPoint);
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;
            
            // Call Equip if supported
            var newDuctTape = tool as DuctTape;
            if (newDuctTape != null) newDuctTape.Equip();
            
            Debug.Log($"Equipped: {tool.toolName}");
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
