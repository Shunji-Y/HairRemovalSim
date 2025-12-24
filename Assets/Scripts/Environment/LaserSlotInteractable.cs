using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Interactable for laser body slots. Allows players to equip/swap lasers.
    /// </summary>
    public class LaserSlotInteractable : MonoBehaviour, IInteractable
    {
        [Header("Highlight Settings")]
        [ColorUsage(true, true)]
        [SerializeField] private Color itemHighlightColor = new Color(1.2f, 1.2f, 0.8f);
        [ColorUsage(true, true)]
        [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private float slotIndicatorSize = 0.15f;
        
        private LaserBody laserBody;
        private ToolTargetArea slotType;
        private InteractionController interactionController;
        
        // Highlight state
        private bool isHighlighting = false;
        private Renderer[] itemRenderers;
        private Color[] originalColors;
        private GameObject emptySlotIndicator;
        
        public void Initialize(LaserBody body, ToolTargetArea type)
        {
            laserBody = body;
            slotType = type;
        }
        
        public void OnInteract(InteractionController interactor)
        {
            if (laserBody == null)
            {
                Debug.LogWarning("[LaserSlotInteractable] LaserBody not set");
                return;
            }
            
            interactionController = interactor;
            
            // Get player's currently equipped tool
            var currentTool = interactor.CurrentTool;
            ItemData currentToolData = null;
            GameObject currentToolObject = null;
            
            if (currentTool != null)
            {
                currentToolData = currentTool.itemData;
                currentToolObject = currentTool.gameObject;
            }
            
            // Get item in this slot
            var slotItem = laserBody.GetItem(slotType);
            var slotInstance = laserBody.GetInstance(slotType);
            
            // Case 1: Slot has item and player has no tool -> Equip from slot
            if (slotItem != null && currentTool == null)
            {
                EquipFromSlot();
                return;
            }
            
            // Case 2: Slot is empty and player has matching tool -> Place in slot
            if (slotItem == null && currentToolData != null)
            {
                if (currentToolData.targetArea == slotType || currentToolData.targetArea == ToolTargetArea.All)
                {
                    PlaceCurrentToolInSlot(currentToolData, currentToolObject);
                }
                else
                {
                    Debug.Log($"[LaserSlotInteractable] Cannot place {currentToolData.targetArea} tool in {slotType} slot");
                }
                return;
            }
            
            // Case 3: Slot has item and player has tool -> Swap or return current tool
            if (slotItem != null && currentToolData != null)
            {
                // Only allow laser-to-laser swaps. Non-lasers must be returned elsewhere.
                bool currentIsLaser = currentToolData.toolType == TreatmentToolType.Laser;
                bool targetAreaMatches = (currentToolData.targetArea == slotType || currentToolData.targetArea == ToolTargetArea.All);
                
                if (currentIsLaser && targetAreaMatches)
                {
                    // Same type laser - swap tools
                    SwapTools(currentToolData, currentToolObject);
                }
                else
                {
                    // Non-laser or different area - return current tool to appropriate location, then equip from this slot
                    ReturnCurrentToolToOriginalSlot(currentToolData, currentToolObject);
                    EquipFromSlot();
                }
                return;
            }
            
            // Case 4: Slot is empty and player has no tool -> Nothing to do
            Debug.Log("[LaserSlotInteractable] Slot is empty and no tool equipped");
        }
        
        private void EquipFromSlot()
        {
            var (item, instance) = laserBody.RemoveItem(slotType);
            if (item == null || instance == null) return;
            
            // Equip via InteractionController
            if (interactionController != null)
            {
                var tool = instance.GetComponent<Tools.ToolBase>();
                if (tool != null)
                {
                    interactionController.EquipTool(tool);
                    Debug.Log($"[LaserSlotInteractable] Equipped {item.name} from {slotType} slot");
                }
            }
            
            // Clear highlight after equipping
            OnHoverExit();
        }
        
        private void PlaceCurrentToolInSlot(ItemData toolData, GameObject toolObject)
        {
            if (interactionController == null) return;
            
            // Unequip current tool
            interactionController.UnequipCurrentTool();
            
            // Place in slot
            laserBody.PlaceItem(slotType, toolData, toolObject);
            Debug.Log($"[LaserSlotInteractable] Placed {toolData.name} in {slotType} slot");
            
            // Clear highlight after placing
            OnHoverExit();
        }
        
        private void SwapTools(ItemData currentToolData, GameObject currentToolObject)
        {
            // Remove from slot first
            var (slotItem, slotInstance) = laserBody.RemoveItem(slotType);
            
            // Unequip current tool
            if (interactionController != null)
            {
                interactionController.UnequipCurrentTool();
            }
            
            // Place current tool in slot
            laserBody.PlaceItem(slotType, currentToolData, currentToolObject);
            
            // Equip slot item
            if (slotInstance != null && interactionController != null)
            {
                var tool = slotInstance.GetComponent<Tools.ToolBase>();
                if (tool != null)
                {
                    interactionController.EquipTool(tool);
                }
            }
            
            Debug.Log($"[LaserSlotInteractable] Swapped tools in {slotType} slot");
        }
        
        private void ReturnCurrentToolToOriginalSlot(ItemData toolData, GameObject toolObject)
        {
            if (interactionController == null) return;
            
            // Check if tool is a laser using toolType, not targetArea
            bool isLaser = toolData.toolType == TreatmentToolType.Laser;
            
            // Unequip current tool
            interactionController.UnequipCurrentTool();
            
            if (isLaser && laserBody != null)
            {
                // Laser -> return to LaserBody
                ToolTargetArea originalSlot = toolData.targetArea;
                laserBody.PlaceItem(originalSlot, toolData, toolObject);
                Debug.Log($"[LaserSlotInteractable] Returned laser {toolData.name} to {originalSlot} slot");
            }
            else
            {
                // Non-laser (shaver, etc.) -> place on TreatmentShelf slot 0
                PlaceNonLaserOnShelf(toolData, toolObject);
            }
        }
        
        /// <summary>
        /// Place non-laser tool on TreatmentShelf slot 0
        /// </summary>
        private void PlaceNonLaserOnShelf(ItemData toolData, GameObject toolObject)
        {
            // Find BedController that has this LaserBody
            var beds = Object.FindObjectsOfType<BedController>();
            BedController targetBed = null;
            
            foreach (var bed in beds)
            {
                if (bed.laserBody == laserBody)
                {
                    targetBed = bed;
                    break;
                }
            }
            
            if (targetBed == null || targetBed.installedShelves == null || targetBed.installedShelves.Length == 0)
            {
                Debug.LogWarning("[LaserSlotInteractable] Cannot find TreatmentShelf to place tool");
                return;
            }
            
            // Find first shelf with space
            foreach (var shelf in targetBed.installedShelves)
            {
                if (shelf == null) continue;
                
                string itemId = toolData.itemId;
                if (shelf.PlaceItemDirect(0, 0, itemId, toolObject))
                {
                    Debug.Log($"[LaserSlotInteractable] Placed {toolData.name} on TreatmentShelf slot [0,0]");
                    return;
                }
            }
            
            Debug.LogWarning("[LaserSlotInteractable] No available slot on TreatmentShelf");
        }
        
        public string GetInteractionPrompt()
        {
            var slotItem = laserBody?.GetItem(slotType);
            string slotName = slotType == ToolTargetArea.Face ? "Face" : "Body";
            
            if (slotItem != null)
            {
                return $"Equip {slotItem.name}";
            }
            else
            {
                return $"Place laser ({slotName})";
            }
        }
        
        public bool CanInteract(InteractionController interactor)
        {
            return laserBody != null;
        }
        
        public void OnHoverEnter()
        {
            if (isHighlighting) return;
            if (laserBody == null) return;
            
            var instance = laserBody.GetInstance(slotType);
            
            isHighlighting = true;
            
            if (instance != null)
            {
                // Highlight existing item
                var rendererList = new System.Collections.Generic.List<Renderer>();
                var colorList = new System.Collections.Generic.List<Color>();
                
                var renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    if (rend.material != null && rend.material.HasProperty("_Color"))
                    {
                        rendererList.Add(rend);
                        colorList.Add(rend.material.color);
                        rend.material.color = itemHighlightColor;
                    }
                }
                
                itemRenderers = rendererList.ToArray();
                originalColors = colorList.ToArray();
            }
            else
            {
                // Show empty slot indicator
                ShowEmptySlotIndicator(true);
            }
        }
        
        public void OnHoverExit()
        {
            if (!isHighlighting) return;
            isHighlighting = false;
            
            // Restore original colors
            if (itemRenderers != null && originalColors != null)
            {
                for (int i = 0; i < itemRenderers.Length && i < originalColors.Length; i++)
                {
                    if (itemRenderers[i] != null && itemRenderers[i].material != null && 
                        itemRenderers[i].material.HasProperty("_Color"))
                    {
                        itemRenderers[i].material.color = originalColors[i];
                    }
                }
            }
            
            itemRenderers = null;
            originalColors = null;
            
            // Hide empty slot indicator
            ShowEmptySlotIndicator(false);
        }
        
        private void ShowEmptySlotIndicator(bool show)
        {
            if (show)
            {
                if (emptySlotIndicator == null)
                {
                    emptySlotIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    emptySlotIndicator.name = "EmptySlotIndicator";
                    
                    // Remove collider
                    var col = emptySlotIndicator.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    
                    // Set material
                    var rend = emptySlotIndicator.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material = new Material(Shader.Find("Sprites/Default"));
                        rend.material.color = emptySlotColor;
                    }
                }
                
                emptySlotIndicator.transform.position = transform.position;
                emptySlotIndicator.transform.rotation = transform.rotation;
                emptySlotIndicator.transform.localScale = new Vector3(slotIndicatorSize, slotIndicatorSize * 0.5f, slotIndicatorSize);
                emptySlotIndicator.SetActive(true);
            }
            else if (emptySlotIndicator != null)
            {
                emptySlotIndicator.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (emptySlotIndicator != null)
            {
                Destroy(emptySlotIndicator);
            }
        }
    }
}
