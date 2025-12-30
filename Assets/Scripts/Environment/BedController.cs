using UnityEngine;
using HairRemovalSim.Customer;
using System.Collections;
using HairRemovalSim.Core;

namespace HairRemovalSim.Environment
{
    public class BedController : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }
        public CustomerController CurrentCustomer { get; private set; }
        
        // Transform where the customer should lie down
        public Transform lieDownPoint;
        
        [Tooltip("Position and rotation where customer stands up after treatment. If null, uses bed transform.")]
        public Transform standUpPoint;
        
        [Header("Navigation")]
        [Tooltip("Point where customer walks to (should be bed center). If null, uses bed transform.")]
        public Transform arrivalPoint;
        
        [Header("Staff")]
        [Tooltip("Position where staff stands when assigned to this bed")]
        public Transform staffPoint;
        
        [Tooltip("If true, staff cannot be assigned to this bed (player only)")]
        public bool isVipOnly = false;
        
        [Header("Curtain Doors")]
        [Tooltip("Left curtain door")]
        public CurtainDoor leftDoor;
        [Tooltip("Right curtain door")]
        public CurtainDoor rightDoor;
        
        [Header("Laser Body")]
        [Tooltip("The laser body unit for this bed")]
        public LaserBody laserBody;
        
        [Header("Treatment Shelves")]
        [Tooltip("Transform positions where shelves can be placed (max 2)")]
        public Transform[] shelfSlots = new Transform[2];
        [Tooltip("Currently installed shelves")]
        public TreatmentShelf[] installedShelves = new TreatmentShelf[2];
        
        [Header("Staff Points")]
        [Tooltip("Position where restock staff stands to refill shelf items")]
        public Transform restockPoint;
        
        [Header("Player Detection")]
        [Tooltip("Collider used for player detection (Box Collider)")]
        [SerializeField] private Collider playerDetectionCollider;
        
        [SerializeField] private bool staffTreatmentInProgress = false;
        
        // Events
        public event System.Action OnDoorsOpened;
        public event System.Action OnDoorsClosed;
        
        private int doorsClosed = 0;
        private int doorsOpened = 0;
        private int doorsToCloseTarget = 0;
        private int doorsToOpenTarget = 0;
        
        [Header("Staff Treatment UI")]
        [Tooltip("Prefab for staff treatment progress UI (spawned at runtime if not set)")]
        public UI.StaffTreatmentProgressUI treatmentProgressUI;
        [Tooltip("Position offset for treatment progress UI relative to bed")]
        public Vector3 treatmentUIOffset = new Vector3(0, 2f, 0);
        
        /// <summary>
        /// Get the linked StaffTreatmentHandler (if any staff is assigned to this bed)
        /// </summary>
        public Staff.StaffTreatmentHandler LinkedStaffHandler { get; private set; }

        private void Awake()
        {
            // If no lie down point set, use own transform
            if (lieDownPoint == null)
            {
                lieDownPoint = transform;
            }
        }
        
        private void Start()
        {
            // Subscribe to door events
            if (leftDoor != null)
            {
                leftDoor.OnDoorClosed += OnSingleDoorClosed;
                leftDoor.OnDoorOpened += OnSingleDoorOpened;
            }
            if (rightDoor != null)
            {
                rightDoor.OnDoorClosed += OnSingleDoorClosed;
                rightDoor.OnDoorOpened += OnSingleDoorOpened;
            }
            
            // Link treatment progress UI to this bed
            if (treatmentProgressUI != null)
            {
                treatmentProgressUI.SetLinkedBed(this);
            }
            
            // Default state: doors open if no customer
            if (!IsOccupied)
            {
             //   OpenDoors();
            }

            ShopManager.Instance.Beds.Add(this);
            // Sort beds by name so Bed1 comes before Bed2, etc.
            ShopManager.Instance.Beds.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }
        
        private void OnDestroy()
        {
            // Unsubscribe
            if (leftDoor != null)
            {
                leftDoor.OnDoorClosed -= OnSingleDoorClosed;
                leftDoor.OnDoorOpened -= OnSingleDoorOpened;
            }
            if (rightDoor != null)
            {
                rightDoor.OnDoorClosed -= OnSingleDoorClosed;
                rightDoor.OnDoorOpened -= OnSingleDoorOpened;
            }
            
            // Remove from ShopManager list
            ShopManager.Instance?.Beds?.Remove(this);
        }
        
        private void OnSingleDoorClosed()
        {
            doorsClosed++;
            if (doorsToCloseTarget > 0 && doorsClosed >= doorsToCloseTarget)
            {
                doorsClosed = 0;
                doorsToCloseTarget = 0;
                OnDoorsClosed?.Invoke();
             
            }
        }
        
        private void OnSingleDoorOpened()
        {
            doorsOpened++;
            if (doorsToOpenTarget > 0 && doorsOpened >= doorsToOpenTarget)
            {
                doorsOpened = 0;
                doorsToOpenTarget = 0;
                OnDoorsOpened?.Invoke();
            }
        }
        
        /// <summary>
        /// Check if all doors are currently closed
        /// </summary>
        public bool AreAllDoorsClosed
        {
            get
            {
                bool leftClosed = leftDoor == null || !leftDoor.IsOpen;
                bool rightClosed = rightDoor == null || !rightDoor.IsOpen;
                return leftClosed && rightClosed;
            }
        }
        
        /// <summary>
        /// Check if all doors are currently open
        /// </summary>
        public bool AreAllDoorsOpen
        {
            get
            {
                bool leftOpen = leftDoor == null || leftDoor.IsOpen;
                bool rightOpen = rightDoor == null || rightDoor.IsOpen;
                return leftOpen && rightOpen;
            }
        }
        
        /// <summary>
        /// Check if any door is animating
        /// </summary>
        public bool AreDoorsAnimating
        {
            get
            {
                bool leftAnimating = leftDoor != null && leftDoor.IsAnimating;
                bool rightAnimating = rightDoor != null && rightDoor.IsAnimating;
                return leftAnimating || rightAnimating;
            }
        }
        
        /// <summary>
        /// Open both curtain doors
        /// </summary>
        public void OpenDoors()
        {
            doorsOpened = 0;
            int doorsToOpen = 0;
            
            // Count doors that need opening and open them
            if (leftDoor != null)
            {
                if (!leftDoor.IsOpen && !leftDoor.IsAnimating)
                {
                    leftDoor.Open();
                    doorsToOpen++;
                }
            }
            if (rightDoor != null)
            {
                if (!rightDoor.IsOpen && !rightDoor.IsAnimating)
                {
                    rightDoor.Open();
                    doorsToOpen++;
                }
            }
            
            // Set target for event handler
            doorsToOpenTarget = doorsToOpen;
            
            // If no doors needed opening, fire event immediately
            if (doorsToOpen == 0)
            {
                OnDoorsOpened?.Invoke();
            }
        }
        
        /// <summary>
        /// Close both curtain doors
        /// </summary>
        public void CloseDoors()
        {
            doorsClosed = 0;
            int doorsToClose = 0;
            
            // Count doors that need closing and close them
            if (leftDoor != null)
            {
                if (leftDoor.IsOpen && !leftDoor.IsAnimating)
                {
                    leftDoor.Close();
                    doorsToClose++;
                }
            }
            if (rightDoor != null)
            {
                if (rightDoor.IsOpen && !rightDoor.IsAnimating)
                {
                    rightDoor.Close();
                    doorsToClose++;
                }
            }
            
            // Set target for event handler
            doorsToCloseTarget = doorsToClose;
            
            // If no doors needed closing, fire event immediately
            if (doorsToClose == 0)
            {
                OnDoorsClosed?.Invoke();
            }
        }
        
        /// <summary>
        /// Open doors if any are closed, returns true if doors needed opening
        /// </summary>
        public bool OpenDoorsIfClosed()
        {
            if (!AreAllDoorsOpen)
            {
                OpenDoors();
                return true;
            }
            return false;
        }

        public void AssignCustomer(CustomerController customer)
        {
            CurrentCustomer = customer;
            IsOccupied = true;
        }

        public void ClearCustomer()
        {
            CurrentCustomer = null;
            IsOccupied = false;
        }
        
        /// <summary>
        /// Check if bed has an available shelf slot
        /// </summary>
        public bool HasAvailableShelfSlot()
        {
            for (int i = 0; i < shelfSlots.Length; i++)
            {
                if (shelfSlots[i] != null && installedShelves[i] == null)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get the first available shelf slot index (-1 if none)
        /// </summary>
        public int GetAvailableShelfSlotIndex()
        {
            for (int i = 0; i < shelfSlots.Length; i++)
            {
                if (shelfSlots[i] != null && installedShelves[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// Install a shelf at the specified slot
        /// </summary>
        public bool InstallShelf(TreatmentShelf shelfPrefab, int slotIndex = -1)
        {
            // Auto-select slot if not specified
            if (slotIndex < 0)
            {
                slotIndex = GetAvailableShelfSlotIndex();
            }
            
            if (slotIndex < 0 || slotIndex >= shelfSlots.Length)
            {
                Debug.LogWarning("[BedController] No available shelf slot");
                return false;
            }
            
            if (shelfSlots[slotIndex] == null)
            {
                Debug.LogWarning($"[BedController] Shelf slot {slotIndex} has no transform");
                return false;
            }
            
            if (installedShelves[slotIndex] != null)
            {
                Debug.LogWarning($"[BedController] Shelf slot {slotIndex} already occupied");
                return false;
            }
            
            // Instantiate shelf at slot position
            TreatmentShelf shelf = Instantiate(shelfPrefab, shelfSlots[slotIndex].position, shelfSlots[slotIndex].rotation);
            shelf.transform.SetParent(shelfSlots[slotIndex]);
            installedShelves[slotIndex] = shelf;
            
            Debug.Log($"[BedController] Installed shelf at slot {slotIndex}");
            return true;
        }
        
        /// <summary>
        /// Get all installed shelves
        /// </summary>
        public TreatmentShelf[] GetInstalledShelves()
        {
            return installedShelves;
        }
        
        // Player detection
        public event System.Action<BedController> OnPlayerEntered;
        public event System.Action<BedController> OnPlayerExited;
        
        public bool IsPlayerInside { get; private set; }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player (has CharacterController or PlayerController tag)
            if (other.GetComponent<CharacterController>() != null || 
                other.CompareTag("Player"))
            {
                IsPlayerInside = true;
                Debug.Log($"[BedController] Player entered bed area: {name}");
                
                // Auto-close doors when player enters (if occupied)
                if (IsOccupied)
                {
                    CloseDoors();
                    
                    // Pause customer waiting timer - gauge stays visible while player is at bed
                    if (CurrentCustomer != null)
                    {
                        CurrentCustomer.PauseWaiting();
                    }
                }
                
                OnPlayerEntered?.Invoke(this);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            // Check if it's the player
            if (other.GetComponent<CharacterController>() != null || 
                other.CompareTag("Player"))
            {
                IsPlayerInside = false;
                Debug.Log($"[BedController] Player exited bed area: {name}");
                
                // Resume customer waiting timer from current value if treatment not complete
                if (IsOccupied && CurrentCustomer != null && !CurrentCustomer.IsCompleted)
                {
                    CurrentCustomer.ResumeWaiting();
                }
                
                // Return equipped laser to LaserBody if player has one
                ReturnEquippedLaserToBody(other.gameObject);
                
                OnPlayerExited?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Return player's equipped laser back to LaserBody when leaving bed area
        /// </summary>
        private void ReturnEquippedLaserToBody(GameObject playerObject)
        {
            if (laserBody == null) return;
            
            // Find InteractionController on player
            var interactionController = playerObject.GetComponent<Player.InteractionController>();
            if (interactionController == null)
            {
                interactionController = playerObject.GetComponentInChildren<Player.InteractionController>();
            }
            if (interactionController == null)
            {
                interactionController = FindObjectOfType<Player.InteractionController>();
            }
            if (interactionController == null) return;
            
            var currentTool = interactionController.CurrentTool;
            if (currentTool == null) return;
            
            // Check if it's a laser (has targetArea set)
            var itemData = currentTool.itemData;
            if (itemData == null) return;
            
            // Skip vacuum cleaner - it should not be auto-returned
            if (itemData.toolType == Core.TreatmentToolType.Vacuum)
            {
                return;
            }
            
            // Only return Face/Body lasers
            if (itemData.targetArea != Core.ToolTargetArea.Face && 
                itemData.targetArea != Core.ToolTargetArea.Body)
            {
                return;
            }
            
            // Unequip from player
            interactionController.UnequipCurrentTool();
            
            // Place in appropriate slot
            laserBody.PlaceItem(itemData.targetArea, itemData, currentTool.gameObject);
            
            Debug.Log($"[BedController] Returned {itemData.name} to LaserBody");
        }
        
        // ========== STAFF TREATMENT CONTROL ==========
        
        /// <summary>
        /// Start staff treatment - disable player detection
        /// </summary>
        public void StartStaffTreatment()
        {
            staffTreatmentInProgress = true;
            if (playerDetectionCollider != null)
            {
                playerDetectionCollider.enabled = false;
                Debug.Log($"[BedController] Staff treatment started, player detection disabled");
            }
        }
        
        /// <summary>
        /// End staff treatment - re-enable player detection
        /// </summary>
        public void EndStaffTreatment()
        {
            staffTreatmentInProgress = false;
            if (playerDetectionCollider != null)
            {
                playerDetectionCollider.enabled = true;
                Debug.Log($"[BedController] Staff treatment ended, player detection re-enabled");
            }
        }
        
        /// <summary>
        /// Check if staff treatment is in progress
        /// </summary>
        public bool IsStaffTreatmentInProgress => staffTreatmentInProgress;
        
        /// <summary>
        /// Get an item from installed shelves (consumes one)
        /// Returns item ID and data, or null if no items
        /// </summary>
        public (string itemId, Core.ItemData itemData)? ConsumeShelfItem()
        {
            foreach (var shelf in installedShelves)
            {
                if (shelf == null) continue;
                
                var result = shelf.ConsumeRandomItem();
                if (result.HasValue)
                {
                    return result;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Check if any shelf has items
        /// </summary>
        public bool HasShelfItems()
        {
            foreach (var shelf in installedShelves)
            {
                if (shelf != null && shelf.HasItems())
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Register a staff handler to this bed and initialize treatment UI
        /// </summary>
        public void RegisterStaffHandler(Staff.StaffTreatmentHandler handler)
        {
            LinkedStaffHandler = handler;
            
            // Initialize treatment UI if available
            if (treatmentProgressUI != null && handler != null)
            {
                treatmentProgressUI.Initialize(handler, this);
                Debug.Log($"[BedController] Registered staff handler and initialized treatment UI");
            }
        }
        
        /// <summary>
        /// Unregister staff handler from this bed
        /// </summary>
        public void UnregisterStaffHandler()
        {
            LinkedStaffHandler = null;
        }
    }
}
