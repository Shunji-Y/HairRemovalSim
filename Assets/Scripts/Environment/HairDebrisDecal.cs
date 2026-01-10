using UnityEngine;
using UnityEngine.UI;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;
using DG.Tweening;
using System.Collections;
using UnityEngine.Rendering.Universal;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Hair debris that can be cleaned with vacuum cleaner.
    /// Child icon uses billboard to always face camera.
    /// </summary>
    public class HairDebrisDecal : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private float interactionRadius = 0.5f;
        
        [Header("Billboard")]
        [Tooltip("The child transform with the hair icon (will billboard towards camera)")]
        [SerializeField] private Transform iconTransform;
        
        [Header("Highlight")]
        [Tooltip("Image component for the hair icon (for highlight effect)")]
        [SerializeField] private Image iconImage;
        
        [SerializeField] private Color normalColor = Color.white;
        [ColorUsage(true, true)]
        [SerializeField] private Color highlightColor = new Color(2f, 2f, 1f);
        
        private bool isCleaned = false;
        private Camera mainCamera;
        private bool isHighlighted = false;
        
        public bool IsCleaned => isCleaned;

        DecalProjector projector;
        
        private void Start()
        {
            mainCamera = Camera.main;
            
            // Cache original color
            if (iconImage != null)
            {
                normalColor = iconImage.color;
            }
            projector = GetComponent<DecalProjector>();
        }
        
        private void LateUpdate()
        {
            // Billboard: icon always faces camera
            if (iconTransform != null && mainCamera != null)
            {
                iconTransform.rotation = mainCamera.transform.rotation;
            }
        }
        
        public void OnInteract(InteractionController interactor)
        {
            if (isCleaned) return;
            
            // Check if player has vacuum cleaner equipped
            var currentTool = interactor.CurrentTool;
            if (currentTool == null || currentTool.itemData == null)
            {
                Debug.Log("[HairDebrisDecal] Need vacuum cleaner to clean");
                return;
            }
            
            // Check if it's a vacuum cleaner (by toolType)
            if (currentTool.itemData.toolType != Core.TreatmentToolType.Vacuum)
            {
                Debug.Log("[HairDebrisDecal] Need vacuum cleaner to clean");
                return;
            }
            
            // Clean this debris
            Clean();
        }
        
        public string GetInteractionPrompt()
        {
            return isCleaned ? "" : "Clean hair";
        }
        
        public bool CanInteract(InteractionController interactor)
        {
            if (isCleaned) return false;
            
            // Only interactable with vacuum cleaner
            var currentTool = interactor?.CurrentTool;
            if (currentTool?.itemData?.toolType == Core.TreatmentToolType.Vacuum)
            {
                return true;
            }
            return false;
        }
        
        public void OnHoverEnter()
        {
            if (isHighlighted) return;
            isHighlighted = true;
            
            // Highlight icon - Image.color is per-instance
            if (iconImage != null)
            {
                iconImage.color = highlightColor;
            }
        }
        
        public void OnHoverExit()
        {
            if (!isHighlighted) return;
            isHighlighted = false;
            
            // Remove highlight
            if (iconImage != null)
            {
                iconImage.color = normalColor;
            }
        }
        
        /// <summary>
        /// Reset state for object pool reuse
        /// </summary>
        public void ResetState()
        {
            isCleaned = false;
            isHighlighted = false;
            
            // Reset color
            if (iconImage != null)
            {
                iconImage.color = normalColor;
            }
        }
        
        /// <summary>
        /// Show or hide the icon (used during business hours)
        /// </summary>
        public void SetIconVisible(bool visible)
        {
            if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Clean this debris
        /// </summary>
        public void Clean()
        {
            Debug.Log($"[HairDebrisDecal] Clean() - Before shake - Position: {PlayerController.Instance.transform.position}");

            SoundManager.Instance.PlaySFX("Clean");
            PlayerController.Instance.transform.DOKill();

            // Disable CharacterController during shake to prevent position override
            var characterController = PlayerController.Instance.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }
            
            PlayerController.Instance.transform.DOShakePosition(1, 0.02f, 60, 45)
                .OnComplete(() =>
                {
                    // Re-enable CharacterController after shake
                    if (characterController != null)
                    {
                        characterController.enabled = true;
                    }
                });
            
            StartCoroutine(FadeDecal());
            // Notify manager (manager handles deactivation/pooling)
        }


        IEnumerator FadeDecal()
        {
            var elapsed = 0f;

            while (elapsed < 0.8f)
            {
                projector.fadeFactor = Mathf.Lerp(projector.fadeFactor,0,Time.deltaTime*5);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isCleaned) yield break;

            isCleaned = true;

            // Remove highlight before pooling
            OnHoverExit();
            if (HairDebrisManager.Instance != null)
            {
                HairDebrisManager.Instance.OnDebrisCleaned(this);
            }

            Debug.Log("[HairDebrisDecal] Cleaned!");
        }
    }
}
