using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.UI;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Interactable warehouse object that opens the warehouse UI when interacted.
    /// </summary>
    public class WarehouseInteractable : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "[E] Open Warehouse";
        
        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }
        
        public void OnHoverEnter()
        {
            // Optional: Add highlight effect
        }
        
        public void OnHoverExit()
        {
            // Optional: Remove highlight effect
        }
        
        public void OnInteract(InteractionController interactor)
        {
            if (WarehousePanel.Instance != null)
            {
                WarehousePanel.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[WarehouseInteractable] WarehousePanel.Instance is null");
            }
        }
    }
}
