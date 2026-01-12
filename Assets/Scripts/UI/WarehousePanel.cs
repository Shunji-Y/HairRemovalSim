using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Main warehouse panel that manages all warehouse and shelf slot UIs.
    /// </summary>
    public class WarehousePanel : MonoBehaviour
    {
        [Header("Warehouse Grid")]
        [SerializeField] private Transform warehouseGridParent;
        [SerializeField] private GameObject warehouseSlotPrefab;
        
        [Header("Treatment Shelf Section")]
        [SerializeField] private Transform shelfListParent;
        [SerializeField] private GameObject shelfCartPrefab;
        
        [Header("Reception Stock Section")]
        [SerializeField] private Transform receptionStockParent;
        [SerializeField] private GameObject receptionStockGroupPrefab;
        
        [Header("Checkout Stock Section")]
        [SerializeField] private Transform checkoutStockParent;
        [SerializeField] private GameObject checkoutStockGroupPrefab;

        [Header("Category Icons")]
        [SerializeField] private Sprite faceLaserIcon;
        [SerializeField] private Sprite bodyLaserIcon;
        [SerializeField] private Sprite shaverIcon;
        [SerializeField] private Sprite shelfItemIcon;
        [SerializeField] private Sprite receptionItemIcon;
        [SerializeField] private Sprite checkoutItemIcon;
        [SerializeField] private Sprite placementIcon;
        
        [Header("Item Detail Display (Bottom Right)")]
        [SerializeField] private GameObject detailPanelRoot;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private Image categoryIconImage;
        [SerializeField] private TMP_Text sellingPriceText;
        [SerializeField] private TMP_Text reviewBonusText;
        
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        
        // Slot UI instances
        private List<WarehouseSlotUI> warehouseSlots = new List<WarehouseSlotUI>();
        private List<ShelfCartUI> shelfCarts = new List<ShelfCartUI>();
        
        // Dynamic stock slot groups (one group per station)
        private List<List<ReceptionStockSlotUI>> receptionStockGroups = new List<List<ReceptionStockSlotUI>>();
        private List<List<CheckoutStockSlotUI>> checkoutStockGroups = new List<List<CheckoutStockSlotUI>>();
        private List<GameObject> receptionStockGroupObjects = new List<GameObject>();
        private List<GameObject> checkoutStockGroupObjects = new List<GameObject>();
        
        public static WarehousePanel Instance { get; private set; }
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            // Play click sound on click
            //if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame ||
            //    UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            //{
            //    SoundManager.Instance?.PlaySFX("sfx_click");
            //}
            
            // Close on ESC or right-click (Input System)
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                Hide();
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to warehouse updates
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.OnSlotChanged += OnWarehouseSlotChanged;
                WarehouseManager.Instance.OnWarehouseUpdated += RefreshWarehouseGrid;
            }
            // Note: Tutorial trigger is in Show() method to avoid firing on initial scene load
        }
        
        private void OnDisable()
        {
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.OnSlotChanged -= OnWarehouseSlotChanged;
                WarehouseManager.Instance.OnWarehouseUpdated -= RefreshWarehouseGrid;
            }
        }
        
        private void Start()
        {
            InitializeWarehouseGrid();
            InitializeShelfCarts();
            InitializeReceptionStockGroups();
            InitializeCheckoutStockGroups();
            Hide();
        }
        
        #region Dynamic Stock Group Generation
        
        /// <summary>
        /// Initialize stock slot groups for each registered reception
        /// </summary>
        private void InitializeReceptionStockGroups()
        {
            // Clear existing groups
            foreach (var obj in receptionStockGroupObjects)
            {
                if (obj != null) Destroy(obj);
            }
            receptionStockGroups.Clear();
            receptionStockGroupObjects.Clear();
            
            if (receptionStockParent == null)
            {
                Debug.LogWarning("[WarehousePanel] receptionStockParent not assigned");
                return;
            }
            
            if (receptionStockGroupPrefab == null)
            {
                Debug.LogWarning("[WarehousePanel] receptionStockGroupPrefab not assigned - using existing slots in scene");
                // Fallback: use existing slots in scene (for backward compatibility)
                var existingSlots = receptionStockParent.GetComponentsInChildren<ReceptionStockSlotUI>(true);
                if (existingSlots.Length > 0)
                {
                    var group = new List<ReceptionStockSlotUI>(existingSlots);
                    for (int i = 0; i < existingSlots.Length; i++)
                    {
                        existingSlots[i].Initialize(0, i);
                    }
                    receptionStockGroups.Add(group);
                }
                return;
            }
            
            int receptionCount = ReceptionCounterManager.Instance?.ReceptionCount ?? 0;
            if (receptionCount == 0)
            {
                Debug.Log("[WarehousePanel] No receptions registered yet");
                return;
            }
            
            for (int stationIdx = 0; stationIdx < receptionCount; stationIdx++)
            {
                var groupObj = Instantiate(receptionStockGroupPrefab, receptionStockParent);
                groupObj.name = $"ReceptionStock_{stationIdx}";
                receptionStockGroupObjects.Add(groupObj);
                
                var slots = groupObj.GetComponentsInChildren<ReceptionStockSlotUI>(true);
                var groupList = new List<ReceptionStockSlotUI>();
                
                for (int slotIdx = 0; slotIdx < slots.Length; slotIdx++)
                {
                    slots[slotIdx].Initialize(stationIdx, slotIdx);
                    groupList.Add(slots[slotIdx]);
                }
                
                receptionStockGroups.Add(groupList);
                Debug.Log($"[WarehousePanel] Created reception stock group {stationIdx} with {slots.Length} slots");
            }
        }
        
        /// <summary>
        /// Initialize stock slot groups for each registered cash register
        /// </summary>
        private void InitializeCheckoutStockGroups()
        {
            // Clear existing groups
            foreach (var obj in checkoutStockGroupObjects)
            {
                if (obj != null) Destroy(obj);
            }
            checkoutStockGroups.Clear();
            checkoutStockGroupObjects.Clear();
            
            if (checkoutStockParent == null)
            {
                Debug.LogWarning("[WarehousePanel] checkoutStockParent not assigned");
                return;
            }
            
            if (checkoutStockGroupPrefab == null)
            {
                Debug.LogWarning("[WarehousePanel] checkoutStockGroupPrefab not assigned - using existing slots in scene");
                // Fallback: use existing slots in scene (for backward compatibility)
                var existingSlots = checkoutStockParent.GetComponentsInChildren<CheckoutStockSlotUI>(true);
                if (existingSlots.Length > 0)
                {
                    var group = new List<CheckoutStockSlotUI>(existingSlots);
                    for (int i = 0; i < existingSlots.Length; i++)
                    {
                        existingSlots[i].Initialize(0, i);
                    }
                    checkoutStockGroups.Add(group);
                }
                return;
            }
            
            int registerCount = CashRegisterManager.Instance?.RegisterCount ?? 0;
            if (registerCount == 0)
            {
                Debug.Log("[WarehousePanel] No registers registered yet");
                return;
            }
            
            for (int stationIdx = 0; stationIdx < registerCount; stationIdx++)
            {
                var groupObj = Instantiate(checkoutStockGroupPrefab, checkoutStockParent);
                groupObj.name = $"CheckoutStock_{stationIdx}";
                checkoutStockGroupObjects.Add(groupObj);
                
                var slots = groupObj.GetComponentsInChildren<CheckoutStockSlotUI>(true);
                var groupList = new List<CheckoutStockSlotUI>();
                
                for (int slotIdx = 0; slotIdx < slots.Length; slotIdx++)
                {
                    slots[slotIdx].Initialize(stationIdx, slotIdx);
                    groupList.Add(slots[slotIdx]);
                }
                
                checkoutStockGroups.Add(groupList);
                Debug.Log($"[WarehousePanel] Created checkout stock group {stationIdx} with {slots.Length} slots");
            }
        }
        
        /// <summary>
        /// Refresh stock groups (re-initialize if station count changed)
        /// </summary>
        private void RefreshStockGroups()
        {
            int receptionCount = ReceptionCounterManager.Instance?.ReceptionCount ?? 0;
            int registerCount = CashRegisterManager.Instance?.RegisterCount ?? 0;
            
            // Re-initialize if counts don't match
            if (receptionStockGroups.Count != receptionCount)
            {
                InitializeReceptionStockGroups();
            }
            else
            {
                // Just refresh existing slots from manager
                foreach (var group in receptionStockGroups)
                {
                    foreach (var slot in group)
                    {
                        slot.LoadFromManager();
                    }
                }
            }
            
            if (checkoutStockGroups.Count != registerCount)
            {
                InitializeCheckoutStockGroups();
            }
            else
            {
                // Just refresh existing slots from manager
                foreach (var group in checkoutStockGroups)
                {
                    foreach (var slot in group)
                    {
                        slot.LoadFromManager();
                    }
                }
            }
        }
        
        #endregion
        
        /// <summary>
        /// Show the warehouse panel
        /// </summary>
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            
            // Play sound if not conflicting with tutorial
            if (Core.TutorialManager.Instance == null || !Core.TutorialManager.Instance.IsShowingTutorial)
            {
                Core.SoundManager.Instance?.PlaySFX("sfx_drop");
            }
            
            // Hide "New!!" indicator when player opens warehouse
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.HideNewIndicator();
            }
            
            // Re-initialize shelf carts to sync with current TreatmentShelf state
            InitializeShelfCarts();
            
            // Refresh stock groups (re-initialize if station count changed)
            RefreshStockGroups();
            
            RefreshAll();
            
            // Clear item detail to default empty state
            ClearItemDetail();
            
            // Pause game / show cursor
            Cursor.lockState = CursorLockMode.None;
            
            // Tutorial trigger for item movement (moved from OnEnable to avoid initial load issue)
            Core.TutorialManager.Instance?.TryShowTutorial("tut_item_move");
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// Hide the warehouse panel
        /// </summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            
            // Resume game / hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
        
        public void Toggle()
        {
            if (panel != null && panel.activeSelf)
                Hide();
            else
                Show();
        }
        
        private void InitializeWarehouseGrid()
        {
            if (WarehouseManager.Instance == null || warehouseSlotPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in warehouseGridParent)
            {
                Destroy(child.gameObject);
            }
            warehouseSlots.Clear();
            
            // Create slot UIs
            int maxSlots = WarehouseManager.Instance.MaxSlots;
            for (int i = 0; i < maxSlots; i++)
            {
                var slotObj = Instantiate(warehouseSlotPrefab, warehouseGridParent);
                var slotUI = slotObj.GetComponent<WarehouseSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    warehouseSlots.Add(slotUI);
                }
            }
            
            // Adjust grid height for ScrollView (use actual slot count, not childCount)
          //  AdjustGridHeight(warehouseGridParent, WarehouseManager.Instance.Columns, maxSlots);
            
            RefreshWarehouseGrid();
        }
        
        /// <summary>
        /// Adjust RectTransform height based on GridLayoutGroup settings and row count
        /// </summary>
        /// <param name="itemCount">Explicit item count (-1 to use childCount)</param>

        
        /// <summary>
        /// Initialize/refresh shelf carts from all beds
        /// </summary>
        public void InitializeShelfCarts()
        {
            if (shelfCartPrefab == null || shelfListParent == null) return;
            
            // Clear existing
            foreach (Transform child in shelfListParent)
            {
                Destroy(child.gameObject);
            }
            shelfCarts.Clear();

            // Find all beds and their installed shelves
            var beds = ShopManager.Instance.Beds;
            
            foreach (var bed in beds)
            {
                var installedShelves = bed.GetInstalledShelves();
                for (int i = 0; i < installedShelves.Length; i++)
                {
                    var shelf = installedShelves[i];
                    if (shelf == null) continue;
                    
                    var cartObj = Instantiate(shelfCartPrefab, shelfListParent);
                    var cartUI = cartObj.GetComponent<ShelfCartUI>();
                    if (cartUI != null)
                    {
                        string cartName = $"{bed.name}";// : Cart{i + 1}";
                        cartUI.Initialize(shelf, bed, cartName); // Pass bed for laser slots
                        shelfCarts.Add(cartUI);
                    }
                }
            }
            
            // Adjust cart list height for ScrollView
            //var cartGridLayout = shelfListParent.GetComponent<GridLayoutGroup>();
            //if (cartGridLayout != null)
            //{
            //    int cartColumns = Mathf.Max(1, Mathf.FloorToInt(
            //        (shelfListParent.GetComponent<RectTransform>().rect.width - cartGridLayout.padding.left - cartGridLayout.padding.right + cartGridLayout.spacing.x) 
            //        / (cartGridLayout.cellSize.x + cartGridLayout.spacing.x)));
            //    AdjustGridHeight(shelfListParent, cartColumns, shelfCarts.Count);
            //}
            
            Debug.Log($"[WarehousePanel] Initialized {shelfCarts.Count} shelf carts");
        }
        
        /// <summary>
        /// Refresh shelf carts (public alias for external calls)
        /// </summary>
        public void RefreshShelfCarts()
        {
            InitializeShelfCarts();
        }
        
        private void OnWarehouseSlotChanged(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < warehouseSlots.Count)
            {
                warehouseSlots[slotIndex].RefreshFromWarehouse();
            }
        }
        
        private void RefreshWarehouseGrid()
        {
            foreach (var slot in warehouseSlots)
            {
                slot.RefreshFromWarehouse();
            }
        }
        
        public void RefreshAll()
        {
            RefreshWarehouseGrid();
            foreach (var cart in shelfCarts)
            {
                cart.RefreshFromShelf();
            }
            
            // Load checkout stock slots FROM Manager (station-specific data)
            var checkoutStockSlots = GetComponentsInChildren<CheckoutStockSlotUI>(true);
            foreach (var slot in checkoutStockSlots)
            {
                slot.LoadFromManager();
            }
            
            // Load reception stock slots FROM Manager (station-specific data)
            var receptionStockSlots = GetComponentsInChildren<ReceptionStockSlotUI>(true);
            foreach (var slot in receptionStockSlots)
            {
                slot.LoadFromManager();
            }
        }
        
        #region Item Detail Display
        
        /// <summary>
        /// Show item details in the bottom panel (called on slot hover)
        /// </summary>
        public void ShowItemDetail(ItemData item)
        {
            if (item == null) return;
            
            // Show detail panel
            if (detailPanelRoot != null)
                detailPanelRoot.SetActive(true);
            
            // Item name
            if (itemNameText != null)
                itemNameText.text = item.GetLocalizedName();
            
            // Description
            if (itemDescriptionText != null)
                itemDescriptionText.text = item.GetLocalizedDescription();
            
            // Category name and icon
            string categoryName = GetCategoryName(item);
            Sprite categorySprite = GetCategoryIcon(item);
            
            if (categoryText != null)
                categoryText.text = categoryName;
            
            if (categoryIconImage != null)
            {
                if (categorySprite != null)
                {
                    categoryIconImage.sprite = categorySprite;
                    categoryIconImage.gameObject.SetActive(true);
                }
                else
                {
                    categoryIconImage.gameObject.SetActive(false);
                }
            }
            
            // Selling price (upsellPrice)
            if (sellingPriceText != null)
            {
                if (item.upsellPrice > 0)
                    sellingPriceText.text = $"${item.upsellPrice}";
                else
                    sellingPriceText.text = "-";
            }
            
            // Review bonus
            if (reviewBonusText != null)
            {
                if (item.reviewBonus != 0)
                    reviewBonusText.text = item.reviewBonus > 0 ? $"+{item.reviewBonus}" : $"{item.reviewBonus}";
                else
                    reviewBonusText.text = "-";
            }
        }
        
        /// <summary>
        /// Hide item detail panel (called on slot hover exit)
        /// Clears content but keeps panel visible
        /// </summary>
        public void HideItemDetail()
        {
            ClearItemDetail();
        }
        
        /// <summary>
        /// Clear item detail content to default empty state
        /// </summary>
        public void ClearItemDetail()
        {
            if (itemNameText != null)
                itemNameText.text = "-";
            if (itemDescriptionText != null)
                itemDescriptionText.text = "-";
            if (categoryText != null)
                categoryText.text = "-";
            if (categoryIconImage != null)
                categoryIconImage.gameObject.SetActive(false);
            if (sellingPriceText != null)
                sellingPriceText.text = "-";
            if (reviewBonusText != null)
                reviewBonusText.text = "-";
        }
        
        /// <summary>
        /// Get category name for an item
        /// </summary>
        public string GetCategoryName(ItemData itemData)
        {
            if (itemData == null) return "";
            
            if (itemData.toolType == TreatmentToolType.Laser)
            {
                if (itemData.targetArea == ToolTargetArea.Face)
                    return "Face Laser";
                else if (itemData.targetArea == ToolTargetArea.Body)
                    return "Body Laser";
            }
            
            if (itemData.toolType == TreatmentToolType.Shaver)
                return "Shaver";
            
            if (itemData.category == ItemCategory.PlacementItem)
                return "Placement";
            
            if (itemData.CanPlaceAtReception)
                return "Reception";
            
            if (itemData.CanUseAtCheckout)
                return "Checkout";
            
            if (itemData.CanPlaceOnShelf)
                return "Shelf";
            
            return "";
        }
        
        #endregion
        
        /// <summary>
        /// Get category icon for an item based on its properties
        /// </summary>
        public Sprite GetCategoryIcon(ItemData itemData)
        {
            if (itemData == null) return null;
            
            // Priority order: Laser > Shaver > Placement > Reception > Checkout > Shelf
            if (itemData.toolType == TreatmentToolType.Laser)
            {
                if (itemData.targetArea == ToolTargetArea.Face)
                    return faceLaserIcon;
                else if (itemData.targetArea == ToolTargetArea.Body)
                    return bodyLaserIcon;
            }
            
            if (itemData.toolType == TreatmentToolType.Shaver)
                return shaverIcon;
            
            if (itemData.category == ItemCategory.PlacementItem)
                return placementIcon;
            
            if (itemData.CanPlaceAtReception)
                return receptionItemIcon;
            
            if (itemData.CanUseAtCheckout)
                return checkoutItemIcon;
            
            if (itemData.CanPlaceOnShelf)
                return shelfItemIcon;
            
            return null;
        }
    }
}
