using UnityEngine;
using UnityEngine.InputSystem;

namespace HairRemovalSim.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5.0f;
        public float acceleration = 10.0f; // Units per second to reach max speed
        public float deceleration = 15.0f; // Units per second to slow down
        public float mouseSensitivity = 0.1f;
        public float gravity = -9.81f;

        [Header("References")]
        public Transform cameraTransform;
        
        [Header("Crosshair UI")]
        [Tooltip("RectTransform of the crosshair UI element")]
        public RectTransform crosshairUI;



        private CharacterController characterController;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;

        private float verticalVelocity;
        private float xRotation = 0f;
        private bool canMove = true;
        private float currentSpeed = 0f; // Current movement speed
        
        // Crosshair position in viewport space (0-1)
        private Vector2 crosshairPosition = new Vector2(0.5f, 0.5f);
        
        /// <summary>
        /// Current crosshair position in viewport space (0-1). Use for raycasting and UI.
        /// </summary>
        public Vector2 CrosshairViewportPosition => crosshairPosition;
        
        /// <summary>
        /// Is the player currently in treatment mode (near a customer)
        /// </summary>
        public bool IsInTreatmentMode => isInTreatmentMode;

        public void SetMovementEnabled(bool enabled)
        {
            canMove = enabled;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            
            // Setup Actions
            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
        }

        private void Start()
        {
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Initialize Zoom State
            currentZoomLevel = 0f; // Start zoomed out
            UpdateCameraTransform();
        }

        private void Update()
        {
            // Only control player if not in a UI menu or Treatment Mode
            if (Cursor.visible || !canMove) return;

            HandleLook();
            HandleMovement();
            HandleZoom();
            UpdateCameraTransform();
        }

        private void HandleLook()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            
            // Calculate zoom-adjusted sensitivity (lower when zoomed in)
            float sensitivityMultiplier = isInTreatmentMode 
                ? Mathf.Lerp(1f, zoomedSensitivityMultiplier, currentZoomLevel)
                : 1f;
            
            float mouseX = lookInput.x * mouseSensitivity * sensitivityMultiplier;
            float mouseY = lookInput.y * mouseSensitivity * sensitivityMultiplier;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Rotate player body horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // During treatment mode, move crosshair based on zoom level
            if (isInTreatmentMode && currentZoomLevel > 0.01f)
            {
                // Speed scales from 0 at min zoom to max at full zoom
                float crosshairSpeed = crosshairMaxSpeed * currentZoomLevel;
                
                // Range also scales with zoom
                float currentMaxOffset = crosshairMaxOffset * currentZoomLevel;
                
                crosshairPosition.x += lookInput.x * crosshairSpeed;
                crosshairPosition.y += lookInput.y * crosshairSpeed;
                
                // Clamp crosshair to current zoom-based bounds
                crosshairPosition.x = Mathf.Clamp(crosshairPosition.x, 0.5f - currentMaxOffset, 0.5f + currentMaxOffset);
                crosshairPosition.y = Mathf.Clamp(crosshairPosition.y, 0.5f - currentMaxOffset, 0.5f + currentMaxOffset);
            }
            else
            {
                // At min zoom or not in treatment mode: crosshair stays at center
                crosshairPosition = Vector2.Lerp(crosshairPosition, new Vector2(0.5f, 0.5f), Time.deltaTime * 10f);
            }
            
            // Update crosshair UI position
            UpdateCrosshairUI();
        }
        
        private void UpdateCrosshairUI()
        {
            if (crosshairUI == null) return;
            
            // Convert viewport position to screen position
            Canvas canvas = crosshairUI.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Screen space overlay: use screen coordinates
                crosshairUI.position = new Vector3(
                    crosshairPosition.x * Screen.width,
                    crosshairPosition.y * Screen.height,
                    0f
                );
            }
            else if (canvas != null)
            {
                // Other render modes: convert to canvas local position
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                crosshairUI.anchoredPosition = new Vector2(
                    (crosshairPosition.x - 0.5f) * canvasRect.sizeDelta.x,
                    (crosshairPosition.y - 0.5f) * canvasRect.sizeDelta.y
                );
            }
        }

        [Header("Zoom Settings")]
        public float minZoomZ = 0.35f;
        public float maxZoomZ = 0.8f;
        public float minZoomFOV = 80f;
        public float maxZoomFOV = 60f;
        public float zoomSpeed = 5f;
        public float zoomResetDuration = 0.3f;
        
        [Header("Treatment Mode Settings")]
        [Tooltip("Camera sensitivity multiplier at max zoom (lower = slower)")]
        public float zoomedSensitivityMultiplier = 0.3f;
        [Tooltip("Crosshair movement speed at max zoom")]
        public float crosshairMaxSpeed = 0.003f;
        [Tooltip("How far crosshair can move from center at max zoom (0-0.5)")]
        public float crosshairMaxOffset = 0.3f;
        
        private float currentZoomLevel = 0f; // 0 = Zoom Out (Min), 1 = Zoom In (Max)
        private bool isInTreatmentMode = false;
        private bool isResettingZoom = false;
        private float zoomResetTimer = 0f;
        private float zoomResetStartLevel = 0f;
        
        /// <summary>
        /// Set treatment mode - zoom is only allowed during treatment
        /// </summary>
        public void SetTreatmentMode(bool enabled)
        {
            if (isInTreatmentMode == enabled) return;
            
            isInTreatmentMode = enabled;
            
            if (!enabled)
            {
                // Start smooth reset to min zoom
                isResettingZoom = true;
                zoomResetTimer = 0f;
                zoomResetStartLevel = currentZoomLevel;
                
                // Reset crosshair to center
                crosshairPosition = new Vector2(0.5f, 0.5f);
                UpdateCrosshairUI();
                
                Debug.Log("[PlayerController] Treatment mode OFF - resetting zoom and crosshair");
            }
            else
            {
                isResettingZoom = false;
                // Ensure crosshair starts at center
                crosshairPosition = new Vector2(0.5f, 0.5f);
                UpdateCrosshairUI();
                Debug.Log("[PlayerController] Treatment mode ON - zoom enabled");
            }
        }

        private void HandleZoom()
        {
            if (cameraTransform == null) return;
            
            // Handle zoom reset animation
            if (isResettingZoom)
            {
                zoomResetTimer += Time.deltaTime;
                float t = Mathf.Clamp01(zoomResetTimer / zoomResetDuration);
                currentZoomLevel = Mathf.Lerp(zoomResetStartLevel, 0f, t);
                
                if (t >= 1f)
                {
                    isResettingZoom = false;
                    currentZoomLevel = 0f;
                }
                return;
            }
            
            // Only allow zoom during treatment mode
            if (!isInTreatmentMode) return;
            
            if (Mouse.current == null) return;

            // Read mouse scroll wheel input from Input System
            Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
            float scrollInput = scrollDelta.y;
            
            if (Mathf.Abs(scrollInput) > 0.1f)
            {
                // Adjust zoom level
                currentZoomLevel += scrollInput * zoomSpeed * 0.001f;
                currentZoomLevel = Mathf.Clamp01(currentZoomLevel);
            }
        }

        private void UpdateCameraTransform()
        {
            if (cameraTransform == null) return;

            // 1. Update Rotation
            Quaternion localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            cameraTransform.localRotation = localRotation;

            // 2. Update Position (Zoom along view direction)
            // Interpolate FOV
            float targetFOV = Mathf.Lerp(minZoomFOV, maxZoomFOV, currentZoomLevel);
            
            // Interpolate Distance (Magnitude)
            float targetDistance = Mathf.Lerp(minZoomZ, maxZoomZ, currentZoomLevel);
            
            // Calculate position offset along the camera's forward vector
            // Since we are in local space of the player, and the camera is rotated by xRotation,
            // the "forward" direction relative to the player is 'localRotation * Vector3.forward'.
            Vector3 zoomOffset = localRotation * Vector3.forward * targetDistance;
            
            // Apply to camera
            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = targetFOV;
            }
            
            cameraTransform.localPosition = zoomOffset;
        }

        private void HandleMovement()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            
            float x = moveInput.x;
            float z = moveInput.y;

            Vector3 moveDirection = transform.right * x + transform.forward * z;
            
            // Accelerate or decelerate based on input
            if (moveDirection.magnitude > 0.1f)
            {
                // Accelerate towards max speed
                currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                // Decelerate to stop
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            }
            
            // Normalize movement direction and apply current speed
            Vector3 move = moveDirection.normalized * currentSpeed;
            
            // Gravity
            if (characterController.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalMove = move * Time.deltaTime;
            finalMove.y = verticalVelocity * Time.deltaTime;

            characterController.Move(finalMove);
        }
    }
}
