using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Tools;
using HairRemovalSim.Player;
using HairRemovalSim.Core;
using HairRemovalSim.UI;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Unified shelf slot interactable.
    /// Handles placement and pickup with left/right hand separation.
    /// </summary>
    public class ShelfSlotInteractable : MonoBehaviour, IInteractable
    {
        [Header("Slot Info")]
        public TreatmentShelf shelf;
        public int row;
        public int col;
        
        // Highlight state
        private bool isHighlighting = false;
        private Renderer[] itemRenderers;
        private Color[] originalColors;
        private GameObject emptySlotIndicator;
        
        public void Initialize(TreatmentShelf parentShelf, int slotRow, int slotCol)
        {
            shelf = parentShelf;
            row = slotRow;
            col = slotCol;
        }
        
        private bool IsSlotEmpty()
        {
            if (shelf == null) return true;
            var slotData = shelf.GetSlotData(row, col);
            return slotData == null || string.IsNullOrEmpty(slotData.itemId) || slotData.quantity <= 0;
        }
        
        private GameObject GetSlotItem()
        {
            if (shelf == null) return null;
            var slotData = shelf.GetSlotData(row, col);
            if (slotData == null || slotData.instances == null || slotData.instances.Count == 0)
                return null;
            return slotData.instances[slotData.instances.Count - 1];
        }
        
        /// <summary>
        /// Get the hand type of items in this slot (None if empty)
        /// </summary>
        private ToolBase.HandType GetSlotHandType()
        {
            var itemObj = GetSlotItem();
            if (itemObj == null) return ToolBase.HandType.None;
            
            var tool = itemObj.GetComponent<ToolBase>();
            return tool != null ? tool.GetHandType() : ToolBase.HandType.None;
        }
        
        /// <summary>
        /// Check if the given tool can be stacked in this slot
        /// </summary>
        private bool CanStackInSlot(ToolBase tool)
        {
            if (IsSlotEmpty()) return true;
            
            var slotData = shelf.GetSlotData(row, col);
            if (slotData == null) return false;
            
            string toolItemId = !string.IsNullOrEmpty(tool.itemId) ? tool.itemId : tool.toolName.Replace(" ", "");
            
            // Must be same item
            if (slotData.itemId != toolItemId) return false;
            
            // Check max stack
            var itemData = ItemDataRegistry.Instance?.GetItem(slotData.itemId);
            if (itemData == null) return false;
            
            return slotData.quantity < itemData.maxStackOnShelf;
        }
        
        public string GetInteractionPrompt()
        {
            var player = FindObjectOfType<InteractionController>();
            if (player == null) return null;
            
            bool hasItem = !IsSlotEmpty();
            var slotHandType = GetSlotHandType();
            
            if (hasItem)
            {
                var slotData = shelf.GetSlotData(row, col);
                string itemName = slotData?.itemId ?? "Item";
                
                // Check if player can pick up with correct hand
                if (slotHandType == ToolBase.HandType.RightHand)
                {
                    if (player.currentTool == null)
                        return $"[E] Take {itemName}";
                    else if (GetToolHandType(player.currentTool) == ToolBase.HandType.RightHand)
                    {
                        if (CanStackInSlot(player.currentTool))
                            return $"[E] Add {player.currentTool.toolName}";
                        else
                            return $"[E] Swap {itemName}";
                    }
                }
                else if (slotHandType == ToolBase.HandType.LeftHand)
                {
                    if (player.leftHandTool == null)
                        return $"[E] Take {itemName}";
                    else if (CanStackInSlot(player.leftHandTool))
                        return $"[E] Add {player.leftHandTool.toolName}";
                    else
                        return $"[E] Swap {itemName}";
                }
                return null; // Can't interact (hand full with different type)
            }
            else
            {
                // Empty slot - check if player can place
                if (player.currentTool != null)
                    return $"[E] Place {player.currentTool.toolName}";
                if (player.leftHandTool != null)
                    return $"[E] Place {player.leftHandTool.toolName}";
            }
            
            return null;
        }
        
        public void OnHoverEnter()
        {
            if (isHighlighting) return;
            isHighlighting = true;
            
            if (!IsSlotEmpty())
            {
                // Highlight items in slot
                HighlightItems(true);
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
            
            // Restore item colors
            HighlightItems(false);
            
            // Hide empty slot indicator
            ShowEmptySlotIndicator(false);
        }
        
        private void HighlightItems(bool highlight)
        {
            if (shelf == null) return;
            var slotData = shelf.GetSlotData(row, col);
            if (slotData == null || slotData.instances == null) return;
            
            if (highlight)
            {
                // Store original colors and apply highlight
                var rendererList = new System.Collections.Generic.List<Renderer>();
                var colorList = new System.Collections.Generic.List<Color>();
                
                foreach (var instance in slotData.instances)
                {
                    if (instance == null) continue;
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        if (rend.material.HasProperty("_Color"))
                        {
                            rendererList.Add(rend);
                            colorList.Add(rend.material.color);
                            rend.material.color = shelf.highlightColor;
                        }
                    }
                }
                itemRenderers = rendererList.ToArray();
                originalColors = colorList.ToArray();
            }
            else if (itemRenderers != null && originalColors != null)
            {
                // Restore original colors
                for (int i = 0; i < itemRenderers.Length && i < originalColors.Length; i++)
                {
                    if (itemRenderers[i] != null && itemRenderers[i].material.HasProperty("_Color"))
                    {
                        itemRenderers[i].material.color = originalColors[i];
                    }
                }
                itemRenderers = null;
                originalColors = null;
            }
        }
        
        private void ShowEmptySlotIndicator(bool show)
        {
            if (shelf == null) return;
            
            if (show)
            {
                if (emptySlotIndicator == null)
                {
                    // Create indicator quad
                    emptySlotIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    emptySlotIndicator.name = "SlotIndicator";
                    
                    // Remove collider
                    var collider = emptySlotIndicator.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);
                    
                    // Set material
                    var rend = emptySlotIndicator.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material = new Material(Shader.Find("Sprites/Default"));
                        rend.material.color = shelf.emptySlotColor;
                    }
                }
                
                // Position at slot (use shelf transform for rotation)
                Vector3 slotPos = shelf.GetSlotPosition(row, col);
                emptySlotIndicator.transform.position = slotPos + shelf.transform.up * 0.01f;
                emptySlotIndicator.transform.rotation = shelf.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
                emptySlotIndicator.transform.localScale = new Vector3(shelf.slotWidth, shelf.slotDepth, 1f);
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
        
        
        public void OnInteract(InteractionController interactor)
        {
            if (interactor == null || shelf == null) return;
            
            // Cannot interact with shelf while vacuum is equipped
            var currentTool = interactor.CurrentTool;
            if (currentTool?.itemData?.toolType == TreatmentToolType.Vacuum)
            {
                Debug.Log("[ShelfSlotInteractable] Cannot interact while vacuum is equipped");
                return;
            }
            
            // Restore colors before any action
            OnHoverExit();
            
            bool hasItem = !IsSlotEmpty();
            var slotHandType = GetSlotHandType();
            
            if (hasItem)
            {
                // Slot has item - determine action based on slot's hand type
                if (slotHandType == ToolBase.HandType.RightHand)
                {
                    HandleRightHandSlot(interactor);
                }
                else if (slotHandType == ToolBase.HandType.LeftHand)
                {
                    HandleLeftHandSlot(interactor);
                }
            }
            else
            {
                // Empty slot - place held item
                // For slot [0,0] (shaver slot): prioritize right hand
                // For other slots: prioritize left hand (to avoid laser placement issues)
                if (row == 0 && col == 0)
                {
                    // Shaver slot - right hand first
                    if (interactor.currentTool != null)
                    {
                        PlaceItem(interactor, interactor.currentTool);
                    }
                }
                else
                {
                    // Other slots - left hand first
                    if (interactor.leftHandTool != null)
                    {
                        PlaceItem(interactor, interactor.leftHandTool);
                    }
                    else if (interactor.currentTool != null)
                    {
                        PlaceItem(interactor, interactor.currentTool);
                    }
                }
            }
        }
        
        private void HandleRightHandSlot(InteractionController player)
        {
            // Slot has right hand item
            if (player.currentTool == null)
            {
                // Right hand empty - just pickup
                PickupItem(player);
            }
            else if (GetToolHandType(player.currentTool) == ToolBase.HandType.RightHand)
            {
                if (CanStackInSlot(player.currentTool))
                {
                    // Same item and room for more - add to stack
                    PlaceItem(player, player.currentTool);
                }
                else
                {
                    // Right hand has right tool - swap
                    SwapItems(player, player.currentTool);
                }
            }
            // If holding left hand tool, can't interact with right hand slot
        }
        
        private void HandleLeftHandSlot(InteractionController player)
        {
            // Slot has left hand item
            if (player.leftHandTool == null)
            {
                // Left hand empty - just pickup
                PickupItem(player);
            }
            else if (CanStackInSlot(player.leftHandTool))
            {
                // Same item and room for more - add to stack
                PlaceItem(player, player.leftHandTool);
            }
            else
            {
                // Left hand full with different item - auto-place held item and pickup new one
                if (AutoPlaceLeftHandItem(player, player.leftHandTool))
                {
                    // Successfully placed held item, now pickup the new one
                    PickupItem(player);
                }
                // If auto-place failed, do nothing (no swap that would drop items)
            }
        }
        
        /// <summary>
        /// Auto-place left hand item on shelf following priority:
        /// 1. Find slot with same item type that has room for more
        /// 2. Find empty slot
        /// Returns true if successfully placed
        /// </summary>
        private bool AutoPlaceLeftHandItem(InteractionController player, ToolBase heldTool)
        {
            if (shelf == null || heldTool == null) return false;
            
            string heldItemId = !string.IsNullOrEmpty(heldTool.itemId) 
                ? heldTool.itemId 
                : heldTool.toolName.Replace(" ", "");
            
            var itemData = ItemDataRegistry.Instance?.GetItem(heldItemId);
            if (itemData == null) return false;
            
            int maxStack = itemData.maxStackOnShelf;
            
            // Priority 1: Find slot with same item that has room
            for (int r = 0; r < shelf.RowCount; r++)
            {
                for (int c = 0; c < shelf.ColumnCount; c++)
                {
                    // Skip shaver slot [0,0]
                    if (r == 0 && c == 0) continue;
                    
                    // Skip the slot we're trying to pick up from
                    if (r == row && c == col) continue;
                    
                    var slotData = shelf.GetSlotData(r, c);
                    if (slotData != null && slotData.itemId == heldItemId && slotData.quantity < maxStack)
                    {
                        // Found matching slot with room - place here
                        return PlaceItemAtSlot(player, heldTool, r, c);
                    }
                }
            }
            
            // Priority 2: Find empty slot
            for (int r = 0; r < shelf.RowCount; r++)
            {
                for (int c = 0; c < shelf.ColumnCount; c++)
                {
                    // Skip shaver slot [0,0]
                    if (r == 0 && c == 0) continue;
                    
                    // Skip the slot we're trying to pick up from
                    if (r == row && c == col) continue;
                    
                    var slotData = shelf.GetSlotData(r, c);
                    if (slotData == null || string.IsNullOrEmpty(slotData.itemId) || slotData.quantity <= 0)
                    {
                        // Found empty slot - place here
                        return PlaceItemAtSlot(player, heldTool, r, c);
                    }
                }
            }
            
            Debug.LogWarning($"[ShelfSlotInteractable] No available slot to auto-place {heldItemId}");
            return false;
        }
        
        /// <summary>
        /// Place item at specific slot (helper for auto-place)
        /// </summary>
        private bool PlaceItemAtSlot(InteractionController player, ToolBase tool, int targetRow, int targetCol)
        {
            string itemId = !string.IsNullOrEmpty(tool.itemId) ? tool.itemId : tool.toolName.Replace(" ", "");
            
            // Unequip left hand tool
            var leftHandTool = tool as LeftHandTool;
            if (leftHandTool != null) leftHandTool.Unequip();
            
            // Unparent
            tool.transform.SetParent(null);
            
            // Place on shelf
            if (!shelf.PlaceItemDirect(targetRow, targetCol, itemId, tool.gameObject))
            {
                Debug.LogWarning($"[ShelfSlotInteractable] Failed to auto-place {itemId} at [{targetRow},{targetCol}]");
                return false;
            }
            
            // Clear player's left hand reference
            player.leftHandTool = null;
            if (UI.EquippedToolUI.Instance != null)
                UI.EquippedToolUI.Instance.SetLeftHandUI(null);
            
            Debug.Log($"[ShelfSlotInteractable] Auto-placed {itemId} at [{targetRow},{targetCol}]");
            return true;
        }
        
        private ToolBase.HandType GetToolHandType(ToolBase tool)
        {
            return tool != null ? tool.GetHandType() : ToolBase.HandType.None;
        }
        
        private void SwapItems(InteractionController player, ToolBase heldTool)
        {
            GameObject shelfItemObj = GetSlotItem();
            if (shelfItemObj == null) return;
            
            ToolBase shelfTool = shelfItemObj.GetComponent<ToolBase>();
            if (shelfTool == null) return;
            
            // Verify hand types match
            var heldHandType = GetToolHandType(heldTool);
            var slotHandType = GetSlotHandType();
            if (heldHandType != slotHandType)
            {
                Debug.LogWarning($"[ShelfSlotInteractable] Cannot swap: held={heldHandType}, slot={slotHandType}");
                return;
            }
            
            // Check if held tool is a laser - if so, return to LaserBody instead of placing on shelf
            bool heldIsLaser = heldTool.itemData != null && 
                (heldTool.itemData.targetArea == Core.ToolTargetArea.Face || 
                 heldTool.itemData.targetArea == Core.ToolTargetArea.Body);
            
            if (heldIsLaser)
            {
                // Return laser to LaserBody, then pickup shelf item
                ReturnLaserToBody(player, heldTool);
                PickupItem(player);
                return;
            }
            
            string heldItemId = !string.IsNullOrEmpty(heldTool.itemId) ? heldTool.itemId : heldTool.toolName.Replace(" ", "");
            
            // Unequip held tool (handle both hand types)
            var rightHandTool = heldTool as RightHandTool;
            if (rightHandTool != null) rightHandTool.Unequip();
            
            var leftHandTool = heldTool as LeftHandTool;
            if (leftHandTool != null) leftHandTool.Unequip();
            
            // Remove shelf item from data FIRST
            shelf.RemoveItemDirect(row, col, shelfItemObj);
            
            // Save original parent for recovery
            Transform originalParent = heldTool.transform.parent;
            Vector3 originalPosition = heldTool.transform.position;
            Quaternion originalRotation = heldTool.transform.rotation;
            
            // Unparent held tool
            heldTool.transform.SetParent(null);
            
            // Try to place held tool on shelf
            if (!shelf.PlaceItemDirect(row, col, heldItemId, heldTool.gameObject))
            {
                Debug.LogWarning("[ShelfSlotInteractable] Failed to place held item during swap - recovering");
                // Recovery: restore original position
                heldTool.transform.SetParent(originalParent);
                heldTool.transform.position = originalPosition;
                heldTool.transform.rotation = originalRotation;
                // Put shelf item back
                shelf.PlaceItemDirect(row, col, shelfTool.itemId, shelfItemObj);
                return;
            }
            
            // Clear player's held tool reference
            if (heldTool == player.currentTool)
            {
                player.currentTool = null;
            }
            else if (heldTool == player.leftHandTool)
            {
                player.leftHandTool = null;
            }
            
            // Remove ShelfSlotInteractable from shelf item
            var shelfInteractable = shelfItemObj.GetComponent<ShelfSlotInteractable>();
            if (shelfInteractable != null) Destroy(shelfInteractable);
            
            // Equip shelf item
            player.EquipTool(shelfTool);
            
            Debug.Log($"[ShelfSlotInteractable] Swapped tools at [{row},{col}]");
        }
        
        /// <summary>
        /// Return laser to LaserBody (find via BedController)
        /// </summary>
        private void ReturnLaserToBody(InteractionController player, ToolBase laserTool)
        {
            if (laserTool?.itemData == null) return;
            
            // Find LaserBody - look for BedController that has this shelf
            var bed = FindBedWithShelf();
            if (bed?.laserBody == null)
            {
                Debug.LogWarning("[ShelfSlotInteractable] Cannot find LaserBody to return laser");
                return;
            }
            
            // Unequip laser
            player.UnequipCurrentTool();
            
            // Place in matching slot
            bed.laserBody.PlaceItem(laserTool.itemData.targetArea, laserTool.itemData, laserTool.gameObject);
            Debug.Log($"[ShelfSlotInteractable] Returned {laserTool.itemData.name} to LaserBody");
        }
        
        /// <summary>
        /// Find BedController that has this shelf installed
        /// </summary>
        private BedController FindBedWithShelf()
        {
            if (shelf == null) return null;
            
            var beds = FindObjectsOfType<BedController>();
            foreach (var bed in beds)
            {
                if (bed.installedShelves != null)
                {
                    foreach (var s in bed.installedShelves)
                    {
                        if (s == shelf) return bed;
                    }
                }
            }
            return null;
        }
        
        private void PickupItem(InteractionController player)
        {
            GameObject itemObj = GetSlotItem();
            if (itemObj == null) return;
            
            ToolBase tool = itemObj.GetComponent<ToolBase>();
            if (tool == null) return;
            
            // Remove ShelfSlotInteractable
            var interactable = itemObj.GetComponent<ShelfSlotInteractable>();
            if (interactable != null) Destroy(interactable);
            
            // Remove from shelf data
            shelf.RemoveItemDirect(row, col, itemObj);
            
            // Equip
            player.EquipTool(tool);
            
            Debug.Log($"[ShelfSlotInteractable] Picked up from [{row},{col}]");
        }
        
        private void PlaceItem(InteractionController player, ToolBase tool)
        {
            // Block lasers from being placed on TreatmentShelf - they go on LaserBody only
            if (tool.itemData != null && tool.itemData.toolType == Core.TreatmentToolType.Laser)
            {
                Debug.Log($"[ShelfSlotInteractable] Cannot place laser on shelf - use LaserBody");
                return;
            }
            
            // Check if slot already has items of different hand type
            if (!IsSlotEmpty())
            {
                var slotHandType = GetSlotHandType();
                var toolHandType = GetToolHandType(tool);
                if (slotHandType != toolHandType)
                {
                    Debug.LogWarning($"[ShelfSlotInteractable] Cannot place {toolHandType} in slot with {slotHandType}");
                    return;
                }
            }
            
            string itemId = !string.IsNullOrEmpty(tool.itemId) ? tool.itemId : tool.toolName.Replace(" ", "");
            
            // Save for recovery
            Transform originalParent = tool.transform.parent;
            Vector3 originalPosition = tool.transform.position;
            Quaternion originalRotation = tool.transform.rotation;
            
            // Unparent first (required for PlaceItemDirect)
            tool.transform.SetParent(null);
            
            // Place on shelf
            if (!shelf.PlaceItemDirect(row, col, itemId, tool.gameObject))
            {
                Debug.LogWarning("[ShelfSlotInteractable] Failed to place item - recovering");
                tool.transform.SetParent(originalParent);
                tool.transform.position = originalPosition;
                tool.transform.rotation = originalRotation;
                return;
            }
            
            // Unequip AFTER successful placement (no need to re-equip on failure)
            var rightHandTool = tool as RightHandTool;
            if (rightHandTool != null) rightHandTool.Unequip();
            
            var leftHandTool = tool as LeftHandTool;
            if (leftHandTool != null) leftHandTool.Unequip();
            
            // Clear player's reference
            if (tool == player.currentTool)
            {
                player.currentTool = null;
                if (EquippedToolUI.Instance != null)
                    EquippedToolUI.Instance.SetRightHandUI(null);
            }
            else if (tool == player.leftHandTool)
            {
                player.leftHandTool = null;
                if (EquippedToolUI.Instance != null)
                    EquippedToolUI.Instance.SetLeftHandUI(null);
            }
            
            Debug.Log($"[ShelfSlotInteractable] Placed {itemId} at [{row},{col}]");
        }
    }
}
