using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.Environment
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        // OutlineHighlighter is now managed automatically by InteractionController
        // Just add OutlineHighlighter component to the door object in Unity

        private void Awake()
        {
            // Ensure OutlineHighlighter exists for automatic highlighting
            if (GetComponent<Effects.OutlineHighlighter>() == null)
            {
                gameObject.AddComponent<Effects.OutlineHighlighter>();
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
                GameManager.Instance.OpenShop();
                Debug.Log("Door Interacted: Opening Shop!");
                
                // Optional: Play door open animation/sound
            }
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Night)
            {
                // Check if customers are still in the shop
                var spawner = FindObjectOfType<Customer.CustomerSpawner>();
                if (spawner != null && spawner.GetActiveCustomerCount() > 0)
                {
                    Debug.LogWarning($"[DoorController] Cannot close shop! {spawner.GetActiveCustomerCount()} customer(s) still inside.");
                    // TODO: Show UI message to player
                    return;
                }
                
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

        public string GetInteractionPrompt()
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Preparation)
            {
                return "Open Shop";
            }
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Night)
            {
                return "Go Home (Next Day)";
            }
            return "";
        }
    }
}
