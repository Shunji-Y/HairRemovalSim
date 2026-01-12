using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using System.Collections;
using HairRemovalSim.UI;

namespace HairRemovalSim.Environment
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [Tooltip("The door object to rotate")]
        public Transform doorTransform;
        
        [Tooltip("Rotation when door is closed (Y-axis)")]
        public float closedRotationY = 0f;
        
        [Tooltip("Rotation when door is open (Y-axis)")]
        public float openRotationY = 90f;
        
        [Tooltip("Door animation speed")]
        public float doorAnimationSpeed = 2f;
        
        [Header("Player Reset Settings")]
        [Tooltip("Position to reset player after daily summary")]
        public Transform playerResetPosition;
        
        // OutlineHighlighter is now managed automatically by InteractionController
        // Just add OutlineHighlighter component to the door object in Unity
        
        private bool isDoorOpen = false;
        private Coroutine doorAnimationCoroutine;

        private void Awake()
        {
            // Ensure OutlineHighlighter exists for automatic highlighting
            if (GetComponent<Effects.OutlineHighlighter>() == null)
            {
                gameObject.AddComponent<Effects.OutlineHighlighter>();
            }
        }
        
        private void Start()
        {
            // Subscribe to fade complete event for player reset (when screen is black)
            if (UI.DailySummaryPanel.Instance != null)
            {
                UI.DailySummaryPanel.Instance.OnFadeToBlackComplete += OnFadeToBlackComplete;
            }
            
            // Set initial door state (closed)
            if (doorTransform != null)
            {
                doorTransform.localRotation = Quaternion.Euler(0, closedRotationY, 0);
            }
        }
        
        private void OnDestroy()
        {
            if (UI.DailySummaryPanel.Instance != null)
            {
                UI.DailySummaryPanel.Instance.OnFadeToBlackComplete -= OnFadeToBlackComplete;
            }
        }

        public void OnHoverEnter()
        {
            // Highlighting now handled by InteractionController
        }

        public void OnHoverExit()
        {
            // Highlighting now handled by InteractionController
        }

        public void OnInteract(InteractionController interactor)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Preparation)
            {
                // Dismiss good morning message when opening shop
                MessageBoxManager.Instance?.DismissMessage("msg_good_morning");
                
                // Complete salon open tutorial (first door interact)
                Core.TutorialManager.Instance?.CompleteByAction("door_first_interact");
                
                GameManager.Instance.OpenShop();
                Debug.Log("Door Interacted: Opening Shop!");

                MessageBoxManager.Instance.ShowDirectMessage(
                    LocalizationManager.Instance.Get("msg.salon_opened") ?? "サロンをオープンしました!", 
                    MessageType.Info,
                    false,
                    "msg.salon_opened");

                // Open the door
                OpenDoor();
                SoundManager.Instance.PlaySFX("sfx_open_door");
            }
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Night)
            {
                if (UI.DailySummaryPanel.Instance.IsShowing) return;

                // Dismiss day end message when going home
                MessageBoxManager.Instance?.DismissMessage("msg_day_end");
                
                // Complete day end tutorial (night door interact)
                Core.TutorialManager.Instance?.CompleteByAction("door_night_interact");
                
                // Check if customers are still in the shop
                var spawner = FindObjectOfType<Customer.CustomerSpawner>();
                if (spawner != null && spawner.GetActiveCustomerCount() > 0)
                {
                    Debug.LogWarning($"[DoorController] Cannot close shop! {spawner.GetActiveCustomerCount()} customer(s) still inside.");
                    // TODO: Show UI message to player
                    return;
                }
                
                // Close the door
                CloseDoor();
                
                // Show daily summary panel instead of directly going to next day
                if (UI.DailySummaryPanel.Instance != null)
                {
                    // Check if already showing to prevent loop
                    if (UI.DailySummaryPanel.Instance.IsShowing) return;

                    UI.DailySummaryPanel.Instance.Show();
                    Debug.Log("Door Interacted: Showing Daily Summary");
                }
                else
                {
                    // Fallback if panel not found
                    GameManager.Instance.StartNextDay();
                    Debug.Log("Door Interacted: Going Home / Next Day (no summary panel)");
                }
            }
        }
        
        /// <summary>
        /// Open the door with animation
        /// </summary>
        public void OpenDoor()
        {
            if (doorTransform == null) return;
            
            isDoorOpen = true;
            if (doorAnimationCoroutine != null)
            {
                StopCoroutine(doorAnimationCoroutine);
            }
            doorAnimationCoroutine = StartCoroutine(AnimateDoor(openRotationY));
            Debug.Log("[DoorController] Opening door");
        }
        
        /// <summary>
        /// Close the door with animation
        /// </summary>
        public void CloseDoor()
        {
            if (doorTransform == null) return;
            
            isDoorOpen = false;
            if (doorAnimationCoroutine != null)
            {
                StopCoroutine(doorAnimationCoroutine);
            }
            doorAnimationCoroutine = StartCoroutine(AnimateDoor(closedRotationY));
            Debug.Log("[DoorController] Closing door");
        }
        
        private IEnumerator AnimateDoor(float targetRotationY)
        {
            Quaternion startRotation = doorTransform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
            
            float elapsed = 0f;
            float duration = 1f / doorAnimationSpeed;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                doorTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            if(!isDoorOpen)
                SoundManager.Instance.PlaySFX("sfx_close_door");

            doorTransform.localRotation = targetRotation;
            doorAnimationCoroutine = null;
        }
        
        /// <summary>
        /// Called when screen fades to black (for player position reset)
        /// </summary>
        private void OnFadeToBlackComplete()
        {
            ResetPlayerPosition();
        }
        
        /// <summary>
        /// Reset player to the designated position
        /// </summary>
        public void ResetPlayerPosition()
        {
            if (playerResetPosition == null)
            {
                Debug.LogWarning("[DoorController] Player reset position not set!");
                return;
            }
            
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                // Disable CharacterController temporarily to allow position change
                var characterController = playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                }
                
                playerController.transform.position = playerResetPosition.position;
                playerController.transform.rotation = playerResetPosition.rotation;
                
                if (characterController != null)
                {
                    characterController.enabled = true;
                }
                
                Debug.Log($"[DoorController] Player reset to position: {playerResetPosition.position}");
            }
        }

        public string GetInteractionPrompt()
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Preparation)
            {
                return Core.LocalizationManager.Instance?.Get("prompt.open_shop") ?? "Open Shop";
            }
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Night)
            {
                return Core.LocalizationManager.Instance?.Get("prompt.go_home") ?? "Go Home (Next Day)";
            }
            return "";
        }
    }
}
