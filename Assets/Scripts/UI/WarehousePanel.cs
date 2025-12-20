using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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
        [Tooltip("Slot for anesthesia cream stock at reception")]

        [Header("Category Icons")]
        [SerializeField] private Sprite faceLaserIcon;
        [SerializeField] private Sprite bodyLaserIcon;
        [SerializeField] private Sprite shaverIcon;
        [SerializeField] private Sprite shelfItemIcon;
        [SerializeField] private Sprite receptionItemIcon;
        [SerializeField] private Sprite checkoutItemIcon;
        
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        
        // Slot UI instances
        private List<WarehouseSlotUI> warehouseSlots = new List<WarehouseSlotUI>();
        private List<ShelfCartUI> shelfCarts = new List<ShelfCartUI>();
        
        public static WarehousePanel Instance { get; private set; }
        public bool IsOpen => panel != null && panel.activeSelf;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
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
            Hide();
        }
        
        /// <summary>
        /// Show the warehouse panel
        /// </summary>
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            
            // Re-initialize shelf carts to sync with current TreatmentShelf state
            InitializeShelfCarts();
            RefreshAll();
            
            // Pause game / show cursor
            Cursor.lockState = CursorLockMode.None;
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
        private void AdjustGridHeight(Transform gridParent, int columns, int itemCount = -1)
        {
            var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            var rectTransform = gridParent.GetComponent<RectTransform>();
            
            if (gridLayout == null || rectTransform == null) return;
            
            // Use explicit itemCount if provided, otherwise fallback to childCount
            int count = itemCount >= 0 ? itemCount : gridParent.childCount;
            int rows = Mathf.CeilToInt((float)count / columns);
            
            float cellHeight = gridLayout.cellSize.y;
            float spacingY = gridLayout.spacing.y;
            float paddingTop = gridLayout.padding.top;
            float paddingBottom = gridLayout.padding.bottom;
            
            float totalHeight = paddingTop + paddingBottom + (rows * cellHeight) + (Mathf.Max(0, rows - 1) * spacingY);
            
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, totalHeight);
        }
        
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
            
            // Sync checkout stock slots FROM checkout panel (to reflect changes made at register)
            var checkoutStockSlots = GetComponentsInChildren<CheckoutStockSlotUI>(true);
            foreach (var slot in checkoutStockSlots)
            {
                slot.SyncFromCheckoutPanel();
            }
            
            // Sync reception stock slots FROM reception panel (to reflect changes made at reception)
            var receptionStockSlots = GetComponentsInChildren<ReceptionStockSlotUI>(true);
            foreach (var slot in receptionStockSlots)
            {
                slot.SyncFromReceptionPanel();
            }
        }
        
        /// <summary>
        /// Get category icon for an item based on its properties
        /// </summary>
        public Sprite GetCategoryIcon(ItemData itemData)
        {
            if (itemData == null) return null;
            
            // Priority order: Laser > Shaver > Reception > Checkout > Shelf
            if (itemData.toolType == TreatmentToolType.Laser)
            {
                if (itemData.targetArea == ToolTargetArea.Face)
                    return faceLaserIcon;
                else if (itemData.targetArea == ToolTargetArea.Body)
                    return bodyLaserIcon;
            }
            
            if (itemData.toolType == TreatmentToolType.Shaver)
                return shaverIcon;
            
            if (itemData.canPlaceAtReception)
                return receptionItemIcon;
            
            if (itemData.canUseAtCheckout)
                return checkoutItemIcon;
            
            if (itemData.canPlaceOnShelf)
                return shelfItemIcon;
            
            return null;
        }
    }
}
