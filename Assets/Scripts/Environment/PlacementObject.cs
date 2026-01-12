using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Component for placement objects (water server, decorations, etc.) that displays
    /// localized description when player hovers crosshair over them.
    /// Attach to any object with a collider to show description on hover.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlacementObject : MonoBehaviour, IInteractable
    {
        [Header("Localization")]
        [Tooltip("The name key from ItemData (e.g., 'item.water_server'). Description uses {nameKey}.desc")]
        [SerializeField] private string nameKey;
        
        [Header("Settings")]
        [Tooltip("If true, shows interaction prompt. If false, only shows description.")]
        [SerializeField] private bool showInteractionPrompt = false;
        
        /// <summary>
        /// Get the localized description for this placement object
        /// </summary>
        public string GetDescription()
        {
            if (string.IsNullOrEmpty(nameKey)) return "";
            
            string descKey = $"{nameKey}.desc";
            string desc = LocalizationManager.Instance?.Get(descKey);
            
            // Return empty if localization not found (don't show the key itself)
            if (string.IsNullOrEmpty(desc) || desc == descKey)
                return "";
                
            return desc;
        }
        
        /// <summary>
        /// Get the localized name for this placement object
        /// </summary>
        public string GetLocalizedName()
        {
            if (string.IsNullOrEmpty(nameKey)) return "";
            
            string name = LocalizationManager.Instance?.Get(nameKey);
            
            if (string.IsNullOrEmpty(name) || name == nameKey)
                return "";
                
            return name;
        }
        
        #region IInteractable Implementation
        
        public void OnInteract(InteractionController interactor)
        {
            // Placement objects are info-only, no interaction
            // Override in subclass if interaction is needed
        }
        
        public void OnHoverEnter()
        {
            // Show name and description in HUD
            string name = GetLocalizedName();
            string desc = GetDescription();
            
            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(desc))
            {
                UI.HUDManager.Instance?.ShowPlacementDescription(name, desc);
            }
        }
        
        public void OnHoverExit()
        {
            // Hide description
            UI.HUDManager.Instance?.HidePlacementDescription();
        }
        
        public string GetInteractionPrompt()
        {
            // Return empty string to not show "Press E" prompt
            // Only show description, not interaction prompt
            if (!showInteractionPrompt)
                return "";
                
            return GetLocalizedName();
        }
        
        #endregion
    }
}
