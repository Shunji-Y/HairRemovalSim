using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;

namespace HairRemovalSim.Tools
{
    /// <summary>
    /// Base class for all tools. Contains common functionality.
    /// Use RightHandTool for main tools (left click) and LeftHandTool for support tools (right click).
    /// </summary>
    public abstract class ToolBase : MonoBehaviour, IInteractable
    {
        public enum ToolType
        {
            Single,
            Continuous
        }
        
        public enum HandType
        {
            RightHand,  // Main tools (laser, duct tape) - Left click
            LeftHand,   // Support tools (cooling gel) - Right click
            None        // Not holdable
        }

        [Header("Item Data")]
        [Tooltip("Reference to unified item data")]
        public ItemData itemData;
        
        [Header("Tool Settings")]
        public ToolType toolType = ToolType.Continuous;
        public float useInterval = 0.1f;
        
        [Header("Durability Override")]
        [Tooltip("Override itemData durability settings")]
        public bool overrideDurability = false;
        [Tooltip("If true, tool never breaks")]
        public bool isUnbreakable = true;
        [Tooltip("Maximum durability (uses before breaking)")]
        public int maxDurability = 3;
        [SerializeField] protected int currentDurability;
        
        // Properties from ItemData
        public string toolName => itemData != null ? itemData.GetLocalizedName() : gameObject.name;
        public string itemId => itemData != null ? itemData.itemId : "";
        public Sprite toolIcon => itemData?.icon;
        public Vector3 handPositionOffset => itemData != null ? itemData.handPosition : Vector3.zero;
        public Vector3 handRotationOffset => itemData != null ? itemData.handRotation : Vector3.zero;
        
        public abstract HandType GetHandType();
        
        /// <summary>
        /// Returns true if the tool is currently hovering over a valid target (e.g. skin).
        /// Used to prioritize tool usage over object interaction.
        /// </summary>
        public virtual bool IsHoveringTarget => false;
        
        /// <summary>
        /// Event fired when tool breaks (durability reaches 0)
        /// </summary>
        public event System.Action<ToolBase> OnToolBroken;

        protected float lastUseTime;
        
        public int CurrentDurability => currentDurability;
        public bool IsBroken => !isUnbreakable && currentDurability <= 0;

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

        // OutlineHighlighter is now managed automatically by InteractionController

        protected virtual void Awake()
        {
            // Ensure OutlineHighlighter exists for automatic highlighting
            if (GetComponent<Effects.OutlineHighlighter>() == null)
            {
                gameObject.AddComponent<Effects.OutlineHighlighter>();
            }
            
            // Initialize durability
            currentDurability = maxDurability;
        }
        
        /// <summary>
        /// Use one durability point. Returns true if tool is still usable, false if broken.
        /// </summary>
        protected bool UseDurability()
        {
            if (isUnbreakable) return true;
            
            currentDurability--;
            Debug.Log($"[{toolName}] Durability: {currentDurability}/{maxDurability}");
            
            if (currentDurability <= 0)
            {
                OnToolBroken?.Invoke(this);
                return false;
            }
            return true;
        }

        public virtual void OnHoverEnter()
        {
            // Highlighting now handled by InteractionController
        }

        public virtual void OnHoverExit()
        {
            // Highlighting now handled by InteractionController
        }

        public virtual string GetInteractionPrompt()
        {
            return $"Equip {toolName}";
        }
    }
}
