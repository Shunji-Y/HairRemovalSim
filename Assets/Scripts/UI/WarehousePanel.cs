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
        [Tooltip("Slot for anesthesia cream stock at reception")]

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
            
            // Hide "New!!" indicator when player opens warehouse
            if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.HideNewIndicator();
            }
            
            // Re-initialize shelf carts to sync with current TreatmentShelf state
            InitializeShelfCarts();
            RefreshAll();
            
            // Clear item detail to default empty state
            ClearItemDetail();
            
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
