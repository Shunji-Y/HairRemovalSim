using UnityEngine;
using HairRemovalSim.Player;

namespace HairRemovalSim.Tools
{
    /// <summary>
    /// Base class for left hand tools (support tools).
    /// Used with right click. Typically consumable items.
    /// </summary>
    public abstract class LeftHandTool : ToolBase
    {
        protected InteractionController interactionController;
        protected bool isEquipped = false;
        
        public override HandType GetHandType() => HandType.LeftHand;
        
        protected override void Awake()
        {
            base.Awake();
            // Left hand tools are usually consumable
            isUnbreakable = false;
        }
        
        /// <summary>
        /// Called when tool is equipped
        /// </summary>
        public virtual void Equip(InteractionController controller)
        {
            interactionController = controller;
            isEquipped = true;
            OnToolBroken += HandleToolBroken;
        }
        
        /// <summary>
        /// Called when tool is unequipped
        /// </summary>
        public virtual void Unequip()
        {
            isEquipped = false;
            OnToolBroken -= HandleToolBroken;
        }
        
        /// <summary>
        /// Handle tool breaking - remove from hand and destroy
        /// </summary>
        protected virtual void HandleToolBroken(ToolBase tool)
        {
            Debug.Log($"[{toolName}] Used up! Removing from hand.");
            
            if (interactionController != null)
            {
                interactionController.RemoveLeftHandTool();
            }
            
            Destroy(gameObject);
        }
    }
}
