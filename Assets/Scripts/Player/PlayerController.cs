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



        private CharacterController characterController;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;

        private float verticalVelocity;
        private float xRotation = 0f;
        private bool canMove = true;
        private float currentSpeed = 0f; // Current movement speed

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
            
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // Rotate player body horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // Camera rotation is now handled in UpdateCameraTransform
        }

        [Header("Zoom Settings")]
        public float minZoomZ = 0.35f;
        public float maxZoomZ = 0.8f;
        public float minZoomFOV = 80f;
        public float maxZoomFOV = 60f;
        public float zoomSpeed = 5f;
        
        private float currentZoomLevel = 0f; // 0 = Zoom Out (Min), 1 = Zoom In (Max)

        private void HandleZoom()
        {
            if (cameraTransform == null || Mouse.current == null) return;

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
