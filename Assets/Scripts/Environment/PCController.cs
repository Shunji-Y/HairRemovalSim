using UnityEngine;
using UnityEngine.InputSystem;
using HairRemovalSim.Player;
using HairRemovalSim.Interaction;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// PC controller that allows player to interact with in-game computer.
    /// Locks camera to PC screen and shows mouse cursor.
    /// </summary>
    public class PCController : MonoBehaviour, IInteractable
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraLockPosition; // Where camera looks when using PC
        [SerializeField] private float cameraTransitionDuration = 0.3f;
        [SerializeField] private float pcFOV = 34f; // FOV when using PC
        [SerializeField] private float normalFOV = 80f; // FOV when not using PC
        
        [Header("References")]
        [SerializeField] private PCUIManager uiManager;
        [SerializeField] private GameObject crosshair; // Crosshair UI to hide
        
        private bool isInUse = false;
        private bool isTransitioning = false; // Prevent interaction during camera transition
        private Coroutine cameraTransitionCoroutine;
        private PlayerController playerController;
        private Camera mainCamera;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private Transform originalCameraParent;
        private float originalFOV;
        
        private void Start()
        {
            mainCamera = Camera.main;
            if (uiManager != null)
            {
                uiManager.OnExitPC += ExitPC;
            }
        }
        
        private void OnDestroy()
        {
            if (uiManager != null)
            {
                uiManager.OnExitPC -= ExitPC;
            }
        }
        
        private void Update()
        {
            if (!isInUse) return;
            
            // ESC to exit from any screen
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitPC();
            }
        }
        
        public void OnInteract(InteractionController interactor)
        {
            // Prevent interaction during camera transition or when already in use
            if (isInUse || isTransitioning) return;
            
            playerController = interactor.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            EnterPC();
        }
        
        public string GetInteractionPrompt()
        {
            return (isInUse || isTransitioning) ? "" : Core.LocalizationManager.Instance?.Get("prompt.use_pc") ?? "Use PC";
        }
        
        public void OnHoverEnter() { }
        
        public void OnHoverExit() { }
        
        private void EnterPC()
        {
            isInUse = true;
            
            // Disable player movement
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Store original camera state
            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.position;
                originalCameraRotation = mainCamera.transform.rotation;
                originalCameraParent = mainCamera.transform.parent;
                originalFOV = mainCamera.fieldOfView;
                
                // Move camera to PC view position with FOV change
                if (cameraLockPosition != null)
                {
                    // Stop any existing camera transition
                    if (cameraTransitionCoroutine != null)
                    {
                        StopCoroutine(cameraTransitionCoroutine);
                    }
                    cameraTransitionCoroutine = StartCoroutine(TransitionCamera(cameraLockPosition.position, cameraLockPosition.rotation, pcFOV));
                }
            }
            
            // Hide crosshair
            if (crosshair != null)
            {
                crosshair.SetActive(false);
            }
            
            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Activate PC UI
            if (uiManager != null)
            {
                uiManager.ShowDesktop();
            }

            SoundManager.Instance.PlaySFX("sfx_click");
            
            Debug.Log("[PCController] Entered PC mode");
        }
        
        public void ExitPC()
        {
            if (!isInUse) return;
            
            isInUse = false;
            
            // Restore camera with original FOV
            if (mainCamera != null)
            {
                // Stop any existing camera transition
                if (cameraTransitionCoroutine != null)
                {
                    StopCoroutine(cameraTransitionCoroutine);
                }
                cameraTransitionCoroutine = StartCoroutine(TransitionCamera(originalCameraPosition, originalCameraRotation, normalFOV, true));
            }
            
            // Show crosshair
            if (crosshair != null)
            {
                crosshair.SetActive(true);
            }
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Re-enable player
            if (playerController != null)
            {
                playerController.enabled = true;
            }
            
            // Hide and reset PC UI
            if (uiManager != null)
            {
                uiManager.HideAndReset();
            }
            
            Debug.Log("[PCController] Exited PC mode");
        }
        
        private System.Collections.IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, float targetFOV, bool restoreParent = false)
        {
            if (mainCamera == null) yield break;
            
            isTransitioning = true;
            
            // Detach from parent during transition
            if (!restoreParent)
            {
                mainCamera.transform.SetParent(null);
            }
            
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            float startFOV = mainCamera.fieldOfView;
            float elapsed = 0f;
            
            while (elapsed < cameraTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / cameraTransitionDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                
                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                
                yield return null;
            }
            
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;
            mainCamera.fieldOfView = targetFOV;
            
            // Restore parent after returning
            if (restoreParent && originalCameraParent != null)
            {
                mainCamera.transform.SetParent(originalCameraParent);
            }
            
            isTransitioning = false;
            cameraTransitionCoroutine = null;
        }
    }
}
