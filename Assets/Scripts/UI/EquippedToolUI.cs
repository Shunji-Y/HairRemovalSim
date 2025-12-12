using UnityEngine;
using UnityEngine.UI;
using HairRemovalSim.Tools;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Manages the equipped tool UI display at the bottom of the screen.
    /// Shows tool icon and durability gauge for both hands.
    /// </summary>
    public class EquippedToolUI : Singleton<EquippedToolUI>
    {
        [Header("Left Hand (Right Click) UI")]
        [SerializeField] private Image leftHandIcon;
        [SerializeField] private Image leftHandDurabilityFill;
        [SerializeField] private Image leftHandDurabilityBackground;
        
        [Header("Right Hand (Left Click) UI")]
        [SerializeField] private Image rightHandIcon;
        [SerializeField] private Image rightHandDurabilityFill;
        [SerializeField] private Image rightHandDurabilityBackground;
        
        private ToolBase currentRightTool;
        private ToolBase currentLeftTool;
        
        private void Start()
        {
            // Initially hide all
            SetLeftHandUI(null);
            SetRightHandUI(null);
        }
        
        private void Update()
        {
            // Update durability gauges
            UpdateDurabilityGauge(currentRightTool, rightHandDurabilityFill);
            UpdateDurabilityGauge(currentLeftTool, leftHandDurabilityFill);
        }
        
        /// <summary>
        /// Set the right hand tool display
        /// </summary>
        public void SetRightHandUI(ToolBase tool)
        {
            currentRightTool = tool;
            
            if (tool == null)
            {
                // Hide everything
                if (rightHandIcon != null) rightHandIcon.gameObject.SetActive(false);
                if (rightHandDurabilityFill != null) rightHandDurabilityFill.gameObject.SetActive(false);
                if (rightHandDurabilityBackground != null) rightHandDurabilityBackground.gameObject.SetActive(false);
            }
            else
            {
                // Show icon
                if (rightHandIcon != null)
                {
                    rightHandIcon.gameObject.SetActive(true);
                    if (tool.toolIcon != null)
                    {
                        rightHandIcon.sprite = tool.toolIcon;
                    }
                }
                
                // Show/hide durability based on isUnbreakable
                bool showDurability = !tool.isUnbreakable;
                if (rightHandDurabilityFill != null) rightHandDurabilityFill.gameObject.SetActive(showDurability);
                if (rightHandDurabilityBackground != null) rightHandDurabilityBackground.gameObject.SetActive(showDurability);
            }
        }
        
        /// <summary>
        /// Set the left hand tool display
        /// </summary>
        public void SetLeftHandUI(ToolBase tool)
        {
            currentLeftTool = tool;
            
            if (tool == null)
            {
                // Hide everything
                if (leftHandIcon != null) leftHandIcon.gameObject.SetActive(false);
                if (leftHandDurabilityFill != null) leftHandDurabilityFill.gameObject.SetActive(false);
                if (leftHandDurabilityBackground != null) leftHandDurabilityBackground.gameObject.SetActive(false);
            }
            else
            {
                // Show icon
                if (leftHandIcon != null)
                {
                    leftHandIcon.gameObject.SetActive(true);
                    if (tool.toolIcon != null)
                    {
                        leftHandIcon.sprite = tool.toolIcon;
                    }
                }
                
                // Show/hide durability based on isUnbreakable
                bool showDurability = !tool.isUnbreakable;
                if (leftHandDurabilityFill != null) leftHandDurabilityFill.gameObject.SetActive(showDurability);
                if (leftHandDurabilityBackground != null) leftHandDurabilityBackground.gameObject.SetActive(showDurability);
            }
        }
        
        private void UpdateDurabilityGauge(ToolBase tool, Image fillImage)
        {
            if (fillImage == null || tool == null) return;
            if (tool.isUnbreakable) return;
            
            // Calculate fill amount (0 to 1)
            float durabilityPercent = (float)tool.CurrentDurability / tool.maxDurability;
            fillImage.fillAmount = durabilityPercent;
        }
    }
}
