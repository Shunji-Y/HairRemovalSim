using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.Tools
{
    public abstract class ToolBase : MonoBehaviour, IInteractable
    {
        public enum ToolType
        {
            Single,
            Continuous
        }

        public string toolName;
        public ToolType toolType = ToolType.Continuous;
        public float useInterval = 0.1f; // Time between uses for continuous tools
        public float painMultiplier = 1.0f;

        protected float lastUseTime;

        public abstract void OnUseDown();
        public abstract void OnUseUp();
        public abstract void OnUseDrag(Vector3 delta);

        // IInteractable Implementation
        public virtual void OnInteract(InteractionController interactor)
        {
            Debug.Log($"ToolBase: OnInteract called for {toolName}");
            if (interactor != null)
            {
                interactor.EquipTool(this);
            }
            else
            {
                Debug.LogError("ToolBase: Interactor is null!");
            }
        }

        private Effects.OutlineHighlighter highlighter;

        protected virtual void Awake()
        {
            highlighter = GetComponent<Effects.OutlineHighlighter>();
            if (highlighter == null) highlighter = gameObject.AddComponent<Effects.OutlineHighlighter>();
        }

        public virtual void OnHoverEnter()
        {
            highlighter?.Highlight();
        }

        public virtual void OnHoverExit()
        {
            highlighter?.Unhighlight();
        }

        public virtual string GetInteractionPrompt()
        {
            return $"Equip {toolName}";
        }
    }
}
