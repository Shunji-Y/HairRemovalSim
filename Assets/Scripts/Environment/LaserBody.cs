using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Interaction;

namespace HairRemovalSim.Environment
{
    /// <summary>
    /// Laser body unit that holds Face and Body laser slots.
    /// Players interact with slots to equip/swap lasers.
    /// </summary>
    public class LaserBody : MonoBehaviour
    {
        [Header("Slot Transforms")]
        [Tooltip("Transform for Face laser slot position")]
        public Transform faceSlotTransform;
        
        [Tooltip("Transform for Body laser slot position")]
        public Transform bodySlotTransform;
        
        [Header("Initial Items")]
        [Tooltip("Initial Face laser ItemData")]
        [SerializeField] private ItemData initialFaceItem;
        
        [Tooltip("Initial Body laser ItemData")]
        [SerializeField] private ItemData initialBodyItem;
        
        [Tooltip("Enable to spawn initial items on this LaserBody (only first bed should be true)")]
        [SerializeField] private bool spawnInitialItems = true;
        
        // Current items in slots
        private ItemData faceItem;
        private ItemData bodyItem;
        private GameObject faceItemInstance;
        private GameObject bodyItemInstance;
        
        // Public accessors
        public ItemData FaceItem => faceItem;
        public ItemData BodyItem => bodyItem;
        public GameObject FaceItemInstance => faceItemInstance;
        public GameObject BodyItemInstance => bodyItemInstance;
        
        public bool HasFaceLaser => faceItem != null;
        public bool HasBodyLaser => bodyItem != null;
        
        private void Start()
        {
            SpawnInitialItems();
            CreateSlotColliders();
        }
        
        private void SpawnInitialItems()
        {
            // Only spawn on first bed (check via BedController index or flag)
            if (!spawnInitialItems) return;
            
            if (initialFaceItem != null)
            {
                PlaceItem(ToolTargetArea.Face, initialFaceItem, null);
            }
            
            if (initialBodyItem != null)
            {
                PlaceItem(ToolTargetArea.Body, initialBodyItem, null);
            }
        }
        
        private void CreateSlotColliders()
        {
            // Create Face slot interactable
            if (faceSlotTransform != null)
            {
                var faceInteractable = faceSlotTransform.gameObject.GetComponent<LaserSlotInteractable>();
                if (faceInteractable == null)
                {
                    faceInteractable = faceSlotTransform.gameObject.AddComponent<LaserSlotInteractable>();
                }
                faceInteractable.Initialize(this, ToolTargetArea.Face);
                
                // Ensure collider exists
                if (faceSlotTransform.GetComponent<Collider>() == null)
                {
                    var col = faceSlotTransform.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.15f, 0.1f, 0.15f);
                    col.isTrigger = false;
                }
                faceSlotTransform.gameObject.layer = LayerMask.NameToLayer("Interactable");
            }
            
            // Create Body slot interactable
            if (bodySlotTransform != null)
            {
                var bodyInteractable = bodySlotTransform.gameObject.GetComponent<LaserSlotInteractable>();
                if (bodyInteractable == null)
                {
                    bodyInteractable = bodySlotTransform.gameObject.AddComponent<LaserSlotInteractable>();
                }
                bodyInteractable.Initialize(this, ToolTargetArea.Body);
                
                // Ensure collider exists
                if (bodySlotTransform.GetComponent<Collider>() == null)
                {
                    var col = bodySlotTransform.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.15f, 0.1f, 0.15f);
                    col.isTrigger = false;
                }
                bodySlotTransform.gameObject.layer = LayerMask.NameToLayer("Interactable");
            }
        }
        
        /// <summary>
        /// Place an item in the specified slot
        /// </summary>
        public bool PlaceItem(ToolTargetArea slotType, ItemData item, GameObject existingInstance)
        {
            if (item == null) return false;
            
            // Only allow Laser type tools on LaserBody
            if (item.toolType != TreatmentToolType.Laser)
            {
                Debug.Log($"[LaserBody] Only Laser type tools can be placed here. {item.displayName} is {item.toolType}");
                return false;
            }
            
            // Verify item matches slot type
            if (item.targetArea != slotType && item.targetArea != ToolTargetArea.All)
            {
                Debug.LogWarning($"[LaserBody] Cannot place {item.targetArea} item in {slotType} slot");
                return false;
            }
            
            Transform slotTransform = slotType == ToolTargetArea.Face ? faceSlotTransform : bodySlotTransform;
            if (slotTransform == null) return false;
            
            // Check if slot is already occupied
            if (slotType == ToolTargetArea.Face && faceItem != null)
            {
                Debug.LogWarning("[LaserBody] Face slot already occupied");
                return false;
            }
            if (slotType == ToolTargetArea.Body && bodyItem != null)
            {
                Debug.LogWarning("[LaserBody] Body slot already occupied");
                return false;
            }
            
            // Create or use existing instance
            GameObject instance = existingInstance;
            if (instance == null && item.prefab != null)
            {
                instance = Instantiate(item.prefab, slotTransform.position, slotTransform.rotation);
            }
            
            if (instance != null)
            {
                // Position and parent to slot
                instance.transform.position = slotTransform.position + item.shelfPosition;
                instance.transform.rotation = slotTransform.rotation * Quaternion.Euler(item.shelfRotation);
                instance.transform.SetParent(slotTransform);
                
                // Disable tool functionality while on shelf
                var tool = instance.GetComponent<Tools.ToolBase>();
                if (tool != null)
                {
                    tool.enabled = false;
                }
            }
            
            // Store reference
            if (slotType == ToolTargetArea.Face)
            {
                faceItem = item;
                faceItemInstance = instance;
            }
            else
            {
                bodyItem = item;
                bodyItemInstance = instance;
            }
            
            Debug.Log($"[LaserBody] Placed {item.displayName} in {slotType} slot");
            return true;
        }
        
        /// <summary>
        /// Remove item from specified slot
        /// </summary>
        public (ItemData item, GameObject instance) RemoveItem(ToolTargetArea slotType)
        {
            ItemData item;
            GameObject instance;
            
            if (slotType == ToolTargetArea.Face)
            {
                item = faceItem;
                instance = faceItemInstance;
                faceItem = null;
                faceItemInstance = null;
            }
            else
            {
                item = bodyItem;
                instance = bodyItemInstance;
                bodyItem = null;
                bodyItemInstance = null;
            }
            
            if (instance != null)
            {
                instance.transform.SetParent(null);
                
                // Re-enable tool functionality
                var tool = instance.GetComponent<Tools.ToolBase>();
                if (tool != null)
                {
                    tool.enabled = true;
                }
            }
            
            if (item != null)
            {
                Debug.Log($"[LaserBody] Removed {item.displayName} from {slotType} slot");
            }
            
            return (item, instance);
        }
        
        /// <summary>
        /// Get item in specified slot
        /// </summary>
        public ItemData GetItem(ToolTargetArea slotType)
        {
            return slotType == ToolTargetArea.Face ? faceItem : bodyItem;
        }
        
        /// <summary>
        /// Get instance in specified slot
        /// </summary>
        public GameObject GetInstance(ToolTargetArea slotType)
        {
            return slotType == ToolTargetArea.Face ? faceItemInstance : bodyItemInstance;
        }
        
        /// <summary>
        /// Check if required lasers are available for a treatment plan
        /// </summary>
        public bool HasRequiredLasers(Customer.TreatmentBodyPart confirmedParts)
        {
            bool needsFace = Customer.CustomerPlanHelper.ContainsFaceParts(confirmedParts);
            bool needsBody = Customer.CustomerPlanHelper.ContainsBodyParts(confirmedParts);
            
            if (needsFace && !HasFaceLaser)
            {
                Debug.Log("[LaserBody] Missing Face laser for treatment");
                return false;
            }
            if (needsBody && !HasBodyLaser)
            {
                Debug.Log("[LaserBody] Missing Body laser for treatment");
                return false;
            }
            
            return true;
        }
    }
}
