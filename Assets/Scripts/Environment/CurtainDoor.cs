using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using System.Collections;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Controls a curtain door that can be opened/closed via player interaction
    /// </summary>
    public class CurtainDoor : MonoBehaviour, IInteractable
    {
        [Header("Animation Settings")]
        [Tooltip("Time in seconds to open/close the door")]
        public float animationDuration = 0.3f;
        
        [Tooltip("Local Z offset when door is open")]
        public float openZOffset = 1.62f;
        
        [Tooltip("Scale X change when door is open")]
        public float openScaleXOffset = -0.005f;
        
        [Header("State")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isAnimating = false;
        
        // Saved initial state
        private Vector3 closedPosition;
        private Vector3 closedScale;
        private Vector3 openPosition;
        private Vector3 openScale;
        
        public bool IsOpen => isOpen;
        public bool IsAnimating => isAnimating;
        
        /// <summary>
        /// Event fired when door finishes opening
        /// </summary>
        public event System.Action OnDoorOpened;
        
        /// <summary>
        /// Event fired when door finishes closing
        /// </summary>
        public event System.Action OnDoorClosed;
        
        private void Start()
        {
            // Save initial (closed) position and scale
            closedPosition = transform.localPosition;
            closedScale = transform.localScale;
            
            // Calculate open position and scale
            openPosition = closedPosition + new Vector3(0f, 0f, openZOffset);
            openScale = closedScale + new Vector3(openScaleXOffset, 0f, 0f);
        }
        
        /// <summary>
        /// Toggle door open/close state
        /// </summary>
        public void Toggle()
        {
            if (isAnimating) return;
            
            if (isOpen)
                Close();
            else
                Open();
        }
        
        /// <summary>
        /// Open the door with animation
        /// </summary>
        public void Open()
        {
            if (isOpen || isAnimating) return;
            StartCoroutine(AnimateDoor(true));
        }
        
        /// <summary>
        /// Close the door with animation
        /// </summary>
        public void Close()
        {
            if (!isOpen || isAnimating) return;
            StartCoroutine(AnimateDoor(false));
        }
        
        /// <summary>
        /// Force door to open/closed state instantly (no animation)
        /// </summary>
        public void SetState(bool open)
        {
            isOpen = open;
            transform.localPosition = open ? openPosition : closedPosition;
            transform.localScale = open ? openScale : closedScale;
        }
        
        private IEnumerator AnimateDoor(bool opening)
        {
            isAnimating = true;
            
            Vector3 startPos = transform.localPosition;
            Vector3 startScale = transform.localScale;
            Vector3 endPos = opening ? openPosition : closedPosition;
            Vector3 endScale = opening ? openScale : closedScale;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                
                // Use smooth easing
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                transform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
                transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
                
                yield return null;
            }
            
            // Ensure final state
            transform.localPosition = endPos;
            transform.localScale = endScale;
            
            isOpen = opening;
            isAnimating = false;
            
            // Fire events
            if (opening)
                OnDoorOpened?.Invoke();
            else
                OnDoorClosed?.Invoke();
            
            Debug.Log($"[CurtainDoor] {name} is now {(isOpen ? "OPEN" : "CLOSED")}");
        }
        
        // IInteractable Implementation
        public void OnInteract(InteractionController interactor)
        {
            // Block door interaction during treatment (when slider is visible)
            var playerController = interactor?.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInTreatmentMode)
            {
                Debug.Log("[CurtainDoor] Cannot interact with door during treatment");
                return;
            }
            
            Toggle();
        }
        
        public void OnHoverEnter()
        {
            // Optional: Add highlight effect
        }
        
        public void OnHoverExit()
        {
            // Optional: Remove highlight effect
        }
        
        public string GetInteractionPrompt()
        {
            // Hide prompt during treatment
            var player = FindObjectOfType<PlayerController>();
            if (player != null && player.IsInTreatmentMode)
            {
                return ""; // No prompt during treatment
            }
            return isOpen ? "Close Curtain" : "Open Curtain";
        }
    }
}
