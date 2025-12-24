using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using HairRemovalSim.Core;
using UnityEngine.Localization.Settings;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// Cart entry data
    /// </summary>
    [System.Serializable]
    public class CartEntry
    {
        public ItemData itemData;
        public int quantity;
        public CartItemUI uiInstance;
    }
    
    /// <summary>
    /// Manages the store panel UI with cart functionality.
    /// </summary>
    public class StorePanel : MonoBehaviour
    {
        [Header("Store UI References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemPrefab;
        
        [Header("Cart UI References")]
        [SerializeField] private Transform cartContainer;
        [SerializeField] private GameObject cartItemPrefab;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] private Button purchaseButton;
        
        [Header("Shared Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        private List<StoreItemUI> itemUIs = new List<StoreItemUI>();
        private Dictionary<string, CartEntry> cart = new Dictionary<string, CartEntry>();
        
        private void Awake()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
        
        private void OnDestroy()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }
        
        private void OnEnable()
        {
            // Refresh store items each time panel is shown
            PopulateStore();
            HideTooltip();
            RefreshCartDisplay();
        }
        
        /// <summary>
        /// Populate store with items from ItemDataRegistry
        /// </summary>
        private void PopulateStore()
        {
            if (itemContainer == null || itemPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }
            itemUIs.Clear();
            
            // Get store items from registry
            if (ItemDataRegistry.Instance == null)
            {
                Debug.LogWarning("[StorePanel] ItemDataRegistry not found");
                return;
            }
            
            int currentGrade = ShopManager.Instance?.ShopGrade ?? 1;
            var storeItems = ItemDataRegistry.Instance.GetStoreItems();
            
            // Create item UIs (filtered by grade)
            foreach (var itemData in storeItems)
            {
                // Grade filter: hide if requiredGrade > currentGrade + 1
                int gradeDiff = itemData.requiredShopGrade - currentGrade;
                if (gradeDiff >= 2)
                    continue; // Hide completely
                
                bool isLocked = gradeDiff == 1;
                
                GameObject itemObj = Instantiate(itemPrefab, itemContainer);
                StoreItemUI itemUI = itemObj.GetComponent<StoreItemUI>();
                if (itemUI != null)
                {
                    itemUI.SetItemData(itemData, isLocked);
                    itemUI.SetStorePanel(this);
                    itemUIs.Add(itemUI);
                }
            }
            
            // Adjust grid height for ScrollView
            AdjustGridHeight(itemContainer);
        }
        
        /// <summary>
        /// Adjust RectTransform height based on GridLayoutGroup settings and row count
        /// </summary>
        private void AdjustGridHeight(Transform gridParent)
        {
            var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            var rectTransform = gridParent.GetComponent<RectTransform>();
            
            if (gridLayout == null || rectTransform == null) return;
            
            int childCount = gridParent.childCount;
            if (childCount == 0) return;
            
            // Calculate columns based on container width
            float containerWidth = rectTransform.rect.width;
            float cellWidth = gridLayout.cellSize.x;
            float spacingX = gridLayout.spacing.x;
            float paddingLR = gridLayout.padding.left + gridLayout.padding.right;
            
            int columns = Mathf.Max(1, Mathf.FloorToInt((containerWidth - paddingLR + spacingX) / (cellWidth + spacingX)));
            int rows = Mathf.CeilToInt((float)childCount / columns);
            
            float cellHeight = gridLayout.cellSize.y;
            float spacingY = gridLayout.spacing.y;
            float paddingTop = gridLayout.padding.top;
            float paddingBottom = gridLayout.padding.bottom;
            
            float totalHeight = paddingTop + paddingBottom + (rows * cellHeight) + (Mathf.Max(0, rows - 1) * spacingY);
            
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, totalHeight);
        }
        
        #region Tooltip
        
        /// <summary>
        /// Show shared tooltip at item position
        /// </summary>
        public void ShowTooltip(string text, RectTransform itemRect)
        {
            if (tooltipPanel == null || itemRect == null) return;
            
            tooltipPanel.SetActive(true);
            
            Transform tooltipTransform = tooltipPanel.transform;
            tooltipTransform.position = itemRect.position;
            tooltipTransform.rotation = itemRect.rotation;

            if (tooltipText != null)
            {
                var t = LocalizationSettings.StringDatabase.GetLocalizedString("DescriptionTable", text);
                tooltipText.text = t;
            }
        }
        
        /// <summary>
        /// Hide the shared tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
        
        #endregion
        
        #region Cart Management
        
        /// <summary>
        /// Add item to cart
        /// </summary>
        public void AddToCart(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            
            if (cart.TryGetValue(item.itemId, out CartEntry entry))
            {
                // Already in cart, add quantity
                entry.quantity += quantity;
            }
            else
            {
                // New item
                cart[item.itemId] = new CartEntry
                {
                    itemData = item,
                    quantity = quantity,
                    uiInstance = null
                };
            }
            
            RefreshCartDisplay();
            Debug.Log($"[StorePanel] Added {quantity}x {item.name} to cart");
        }
        
        /// <summary>
        /// Remove item from cart
        /// </summary>
        public void RemoveFromCart(string itemId)
        {
            if (cart.TryGetValue(itemId, out CartEntry entry))
            {
                if (entry.uiInstance != null)
                {
                    Destroy(entry.uiInstance.gameObject);
                }
                cart.Remove(itemId);
                RefreshCartDisplay();
                Debug.Log($"[StorePanel] Removed {itemId} from cart");
            }
        }
        
        /// <summary>
        /// Clear the entire cart
        /// </summary>
        public void ClearCart()
        {
            foreach (var entry in cart.Values)
            {
                if (entry.uiInstance != null)
                {
                    Destroy(entry.uiInstance.gameObject);
                }
            }
            cart.Clear();
            RefreshCartDisplay();
        }
        
        /// <summary>
        /// Refresh cart display
        /// </summary>
        private void RefreshCartDisplay()
        {
            if (cartContainer == null || cartItemPrefab == null) return;
            
            // Clear existing cart UI
            foreach (Transform child in cartContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Recreate cart items
            foreach (var kvp in cart)
            {
                var entry = kvp.Value;
                GameObject cartObj = Instantiate(cartItemPrefab, cartContainer);
                CartItemUI cartUI = cartObj.GetComponent<CartItemUI>();
                if (cartUI != null)
                {
                    cartUI.SetData(
                        entry.itemData.itemId,
                        entry.itemData.GetLocalizedName(),
                        entry.quantity,
                        entry.itemData.price,
                        this
                    );
                    entry.uiInstance = cartUI;
                }
            }
            
            // Update total
            UpdateTotalDisplay();
        }
        
        /// <summary>
        /// Calculate total price
        /// </summary>
        private int CalculateTotal()
        {
            int total = 0;
            foreach (var entry in cart.Values)
            {
                total += entry.itemData.price * entry.quantity;
            }
            return total;
        }
        
        /// <summary>
        /// Update total display
        /// </summary>
        private void UpdateTotalDisplay()
        {
            if (totalText != null)
            {
                totalText.text = $"${CalculateTotal():N0}";
            }
        }
        
        /// <summary>
        /// Purchase all items in cart
        /// </summary>
        private void OnPurchaseClicked()
        {
            if (cart.Count == 0)
            {
                Debug.Log("[StorePanel] Cart is empty");
                return;
            }
            
            int totalCost = CalculateTotal();
            
            // Check if player has enough money
            if (EconomyManager.Instance == null)
            {
                Debug.LogWarning("[StorePanel] EconomyManager not found");
                return;
            }
            
            if (EconomyManager.Instance.CurrentMoney < totalCost)
            {
                Debug.Log($"[StorePanel] Not enough money! Need ${totalCost}");
                return;
            }
            
            // Note: No warehouse space check for pending orders
            // Items will be delivered next day and space will be checked then
            // Process purchase
            if (EconomyManager.Instance.SpendMoney(totalCost))
            {
                // Add all items as pending orders (delivered next day)
                if (InventoryManager.Instance != null)
                {
                    foreach (var entry in cart.Values)
                    {
                        InventoryManager.Instance.AddPendingOrder(entry.itemData, entry.quantity);
                        Debug.Log($"[StorePanel] Ordered {entry.quantity}x {entry.itemData.name} (delivered tomorrow)");
                    }
                }
                
                Debug.Log($"[StorePanel] Order placed! Total: ${totalCost} - Items will be delivered next day");
                ClearCart();
            }
        }
        
        #endregion
        
        /// <summary>
        /// Refresh store display
        /// </summary>
        public void RefreshStore()
        {
            PopulateStore();
        }
    }
}
