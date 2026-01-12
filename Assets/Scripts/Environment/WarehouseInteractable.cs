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
        public string GetInteractionPrompt()
        {
            return Core.LocalizationManager.Instance?.Get("prompt.open_warehouse") ?? "Open Warehouse";
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
