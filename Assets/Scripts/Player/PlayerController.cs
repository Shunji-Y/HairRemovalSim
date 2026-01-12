using HairRemovalSim.UI;
using HairRemovalSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HairRemovalSim.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

    


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
        [Tooltip("Image component of the crosshair (for sprite switching)")]
        public UnityEngine.UI.Image crosshairImage;
        [Tooltip("Sprite to use when NOT in treatment mode")]
        public Sprite normalCrosshairSprite;
        [Tooltip("Sprite to use during treatment mode")]
        public Sprite treatmentCrosshairSprite;



        private CharacterController characterController;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;

        private float verticalVelocity;
        private float xRotation = 0f;
        private bool canMove = true;
        private float currentSpeed = 0f; // Current movement speed
        private bool isPlayingFootsteps = false; // Footstep sound state
        
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

            Instance = this;
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
        
        // Player position override for station interactions
        private bool isPositionOverrideActive = false;
        private Vector3 originalPlayerPosition;
        private Quaternion originalPlayerRotation;
        private float originalXRotation;
        private Coroutine positionOverrideCoroutine;
        
        [Header("Station Interaction")]
        [Tooltip("Duration for smooth camera/player movement to station")]
        public float stationMoveDuration = 0.3f;
        
        /// <summary>
        /// Move player to a fixed position (for station interactions like reception/cashier)
        /// </summary>
        public void SetCameraOverride(Vector3 position, Quaternion rotation)
        {
            if (!isPositionOverrideActive)
            {
                // Save original state
                originalPlayerPosition = transform.position;
                originalPlayerRotation = transform.rotation;
                originalXRotation = xRotation;
            }
            
            isPositionOverrideActive = true;
            
            // Stop any existing movement
            if (positionOverrideCoroutine != null)
            {
                StopCoroutine(positionOverrideCoroutine);
            }
            
            // Start smooth movement
            positionOverrideCoroutine = StartCoroutine(SmoothMoveToPosition(position, rotation));
        }
        
        /// <summary>
        /// Clear position override and return player to original position
        /// </summary>
        public void ClearCameraOverride()
        {
            if (!isPositionOverrideActive) return;
            
            isPositionOverrideActive = false;
            
            // Stop any existing movement
            if (positionOverrideCoroutine != null)
            {
                StopCoroutine(positionOverrideCoroutine);
            }
            
            // Start smooth camera angle reset (player stays in place)
            positionOverrideCoroutine = StartCoroutine(SmoothResetCameraAngle());
        }
        
        private System.Collections.IEnumerator SmoothMoveToPosition(Vector3 targetPosition, Quaternion targetRotation, bool isReturning = false)
        {
            // Disable CharacterController during movement
            if (characterController != null)
            {
                characterController.enabled = false;
            }
            
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float startXRotation = xRotation;
            
            // Calculate target X rotation
            float targetXRotation = targetRotation.eulerAngles.x;
            if (targetXRotation > 180) targetXRotation -= 360;
            
            // Only use Y rotation for player body
            Quaternion targetBodyRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            
            float elapsed = 0f;
            while (elapsed < stationMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / stationMoveDuration);
                
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRotation, targetBodyRotation, t);
                xRotation = Mathf.Lerp(startXRotation, targetXRotation, t);
                
                UpdateCameraTransform();
                yield return null;
            }

            targetPosition.y = transform.position.y;

            // Ensure final position is exact
            transform.position = targetPosition;
            transform.rotation = targetBodyRotation;
            xRotation = targetXRotation;
            UpdateCameraTransform();
            
            // Re-enable CharacterController
            if (characterController != null)
            {
                characterController.enabled = true;
            }
            
            positionOverrideCoroutine = null;
            Debug.Log($"[PlayerController] Player smoothly moved to {targetPosition}");
        }
        
        /// <summary>
        /// Smoothly reset camera angle to default (0 = looking forward), player stays in place
        /// </summary>
        private System.Collections.IEnumerator SmoothResetCameraAngle()
        {
            float startXRotation = xRotation;
            float targetXRotation = 0f; // Look forward
            
            float elapsed = 0f;
            while (elapsed < stationMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / stationMoveDuration);
                
                xRotation = Mathf.Lerp(startXRotation, targetXRotation, t);
                UpdateCameraTransform();
                yield return null;
            }
            
            xRotation = targetXRotation;
            UpdateCameraTransform();
            
            positionOverrideCoroutine = null;
            Debug.Log("[PlayerController] Camera angle reset, player stayed in place");
        }
        
        /// <summary>
        /// Check if position override is currently active
        /// </summary>
        public bool IsCameraOverrideActive => isPositionOverrideActive;

        private void Update()
        {
            // Only control player if not in a UI menu or Treatment Mode
            if (Cursor.visible || !canMove) return;
            
            // Skip normal camera control when override is active
            if (isPositionOverrideActive) return;

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
                
                // Switch to normal crosshair sprite and size
                if (crosshairImage != null && normalCrosshairSprite != null)
                {
                    crosshairImage.sprite = normalCrosshairSprite;
                }
                if (crosshairUI != null)
                {
                    crosshairUI.sizeDelta = new Vector2(40f, 40f);
                }
                
                Debug.Log("[PlayerController] Treatment mode OFF - resetting zoom and crosshair");
            }
            else
            {
                isResettingZoom = false;
                // Ensure crosshair starts at center
                crosshairPosition = new Vector2(0.5f, 0.5f);
                UpdateCrosshairUI();
                
                // Switch to treatment crosshair sprite and size
                if (crosshairImage != null && treatmentCrosshairSprite != null)
                {
                    crosshairImage.sprite = treatmentCrosshairSprite;
                }
                if (crosshairUI != null)
                {
                    crosshairUI.sizeDelta = new Vector2(60f, 60f);
                }
                
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
            // Skip if CharacterController is disabled (e.g., during DOTween shake)
            if (characterController == null || !characterController.enabled) return;
            
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
            
            // Footstep sound
            bool isMoving = currentSpeed > 0.1f && characterController.isGrounded;
            if (isMoving && !isPlayingFootsteps)
            {
                SoundManager.Instance?.PlayLoopSFX("sfx_footstep");
                isPlayingFootsteps = true;
            }
            else if (!isMoving && isPlayingFootsteps)
            {
                SoundManager.Instance?.StopLoopSFX("sfx_footstep");
                isPlayingFootsteps = false;
            }
            
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
