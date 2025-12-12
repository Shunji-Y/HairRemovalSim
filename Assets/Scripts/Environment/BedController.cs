using UnityEngine;
using HairRemovalSim.Customer;
using System.Collections;

namespace HairRemovalSim.Environment
{
    public class BedController : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }
        public CustomerController CurrentCustomer { get; private set; }
        
        // Transform where the customer should lie down
        public Transform lieDownPoint;
        
        [Header("Navigation")]
        [Tooltip("Point where customer walks to (should be bed center). If null, uses bed transform.")]
        public Transform arrivalPoint;
        
        [Header("Curtain Doors")]
        [Tooltip("Left curtain door")]
        public CurtainDoor leftDoor;
        [Tooltip("Right curtain door")]
        public CurtainDoor rightDoor;
        
        [Header("Treatment Shelves")]
        [Tooltip("Transform positions where shelves can be placed (max 2)")]
        public Transform[] shelfSlots = new Transform[2];
        [Tooltip("Currently installed shelves")]
        public TreatmentShelf[] installedShelves = new TreatmentShelf[2];
        
        // Events
        public event System.Action OnDoorsOpened;
        public event System.Action OnDoorsClosed;
        
        private int doorsClosed = 0;
        private int doorsOpened = 0;
        private int doorsToCloseTarget = 0;
        private int doorsToOpenTarget = 0;

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
            
            // Default state: doors open if no customer
            if (!IsOccupied)
            {
                OpenDoors();
            }
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
        }
        
        private void OnSingleDoorClosed()
        {
            doorsClosed++;
            if (doorsToCloseTarget > 0 && doorsClosed >= doorsToCloseTarget)
            {
                doorsClosed = 0;
                doorsToCloseTarget = 0;
                OnDoorsClosed?.Invoke();
                Debug.Log("[BedController] All doors closed");
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
                Debug.Log("[BedController] All doors opened");
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
                Debug.Log("[BedController] All doors already open, event fired immediately");
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
                Debug.Log("[BedController] All doors already closed, event fired immediately");
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
                
                OnPlayerExited?.Invoke(this);
            }
        }
    }
}
