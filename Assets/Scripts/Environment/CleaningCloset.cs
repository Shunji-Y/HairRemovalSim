using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Cleaning supply closet that stores the vacuum cleaner.
    /// Player can open door and equip/return vacuum cleaner.
    /// </summary>
    public class CleaningCloset : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [Tooltip("The door transform to animate")]
        [SerializeField] private Transform door;
        
        [Tooltip("The vacuum cleaner inside the closet")]
        [SerializeField] private GameObject vacuumInCloset;
        
        [Tooltip("Vacuum cleaner prefab to spawn when equipped")]
        [SerializeField] private GameObject vacuumPrefab;
        
        [Tooltip("ItemData for the vacuum cleaner")]
        [SerializeField] private ItemData vacuumItemData;
        
        [Header("Door Animation")]
        [SerializeField] private float doorOpenAngle = 90f;
        [SerializeField] private float doorAnimSpeed = 5f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip doorCloseSound;
        
        private bool isDoorOpen = false;
        private bool isVacuumTaken = false;
        private float currentDoorAngle = 0f;
        private float targetDoorAngle = 0f;
        private AudioSource audioSource;
        private InteractionController lastInteractor;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        private void Update()
        {
            // Animate door
            if (door != null && !Mathf.Approximately(currentDoorAngle, targetDoorAngle))
            {
                currentDoorAngle = Mathf.MoveTowards(currentDoorAngle, targetDoorAngle, doorAnimSpeed * Time.deltaTime * 90f);
                door.localRotation = Quaternion.Euler(0,0 , currentDoorAngle);
            }
        }
        
        public void OnInteract(InteractionController interactor)
        {
            lastInteractor = interactor;
            var currentTool = interactor.CurrentTool;
            
            // Case 1: Player has vacuum equipped -> Return it
            if (currentTool != null && currentTool.itemData?.toolType == TreatmentToolType.Vacuum)
            {
                ReturnVacuum(interactor);
                return;
            }
            
            // Case 2: Door is closed -> Open door
            if (!isDoorOpen)
            {
                OpenDoor();
                return;
            }
            
            // Case 3: Door is open and vacuum is inside -> Take vacuum
            if (isDoorOpen && !isVacuumTaken)
            {
                TakeVacuum(interactor);
                return;
            }
            
            // Case 4: Door is open and vacuum is taken -> Close door
            if (isDoorOpen && isVacuumTaken)
            {
                CloseDoor();
                return;
            }
        }
        
        private void OpenDoor()
        {
            isDoorOpen = true;
            targetDoorAngle = doorOpenAngle;
            
            if (audioSource != null && doorOpenSound != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }
            
            Debug.Log("[CleaningCloset] Door opened");
        }
        
        private void CloseDoor()
        {
            isDoorOpen = false;
            targetDoorAngle = 0f;
            
            if (audioSource != null && doorCloseSound != null)
            {
                audioSource.PlayOneShot(doorCloseSound);
            }
            
            Debug.Log("[CleaningCloset] Door closed");
        }
        
        private void TakeVacuum(InteractionController interactor)
        {
            if (vacuumPrefab == null || vacuumItemData == null)
            {
                Debug.LogWarning("[CleaningCloset] Vacuum prefab or item data not set");
                return;
            }
            
            // Check if during business hours - vacuum can only be used before/after hours
            // For now, allow anytime - can add time check later
            
            // Hide vacuum in closet
            if (vacuumInCloset != null)
            {
                vacuumInCloset.SetActive(false);
            }
            
            // Spawn and equip vacuum
            var vacuumObj = Instantiate(vacuumPrefab);
            vacuumObj.SetActive(true); // Ensure active
            var tool = vacuumObj.GetComponent<Tools.ToolBase>();
            
            if (tool != null)
            {
                tool.itemData = vacuumItemData;
                interactor.EquipTool(tool);
                isVacuumTaken = true;
                
                Debug.Log("[CleaningCloset] Vacuum taken");
            }
            else
            {
                Destroy(vacuumObj);
                Debug.LogWarning("[CleaningCloset] Vacuum prefab missing ToolBase component");
            }
        }
        
        private void ReturnVacuum(InteractionController interactor)
        {
            var currentTool = interactor.CurrentTool;
            if (currentTool == null) return;
            
            // Unequip and destroy the vacuum instance
            interactor.UnequipCurrentTool();
            Destroy(currentTool.gameObject);
            
            // Show vacuum in closet
            if (vacuumInCloset != null)
            {
                vacuumInCloset.SetActive(true);
            }
            
            isVacuumTaken = false;
            
            // Close door
            CloseDoor();
            
            Debug.Log("[CleaningCloset] Vacuum returned");
        }
        
        public string GetInteractionPrompt()
        {
            // Check if player has vacuum
            if (lastInteractor?.CurrentTool?.itemData?.toolType == TreatmentToolType.Vacuum)
            {
                return "Return vacuum";
            }
            
            if (!isDoorOpen)
            {
                return "Open closet";
            }
            
            if (!isVacuumTaken)
            {
                return "Take vacuum";
            }
            
            return "Close closet";
        }
        
        public bool CanInteract(InteractionController interactor)
        {
            return true;
        }
        
        public void OnHoverEnter()
        {
            // Optional: highlight
        }
        
        public void OnHoverExit()
        {
            // Optional: remove highlight
        }
    }
}
