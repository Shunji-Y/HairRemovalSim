using HairRemovalSim.Player;

namespace HairRemovalSim.Interaction
{
    public interface IInteractable
    {
        void OnInteract(InteractionController interactor);
        void OnHoverEnter();
        void OnHoverExit();
        string GetInteractionPrompt(); // e.g., "Open Door", "Start Treatment"
    }
}
