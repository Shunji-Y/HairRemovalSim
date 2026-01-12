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
        
        [Header("Immediate Delivery Toggle")]
        [SerializeField] private Toggle immediateDeliveryToggle;
        [SerializeField] private TextMeshProUGUI immediateDeliveryFeeText;
        
        // Delivery plan item IDs
        private const string PREMIUM_DELIVERY_PLAN_ID = "premium_delivery_plan";
        private const string EXECUTIVE_DELIVERY_PLAN_ID = "executive_delivery_plan";

        private bool isImmediateDelivery = false;
        
        [Header("Item Detail Display (Bottom Panels)")]
        [SerializeField] private GameObject detailPanelRoot;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Image categoryIconImage;
        [SerializeField] private TextMeshProUGUI sellingPriceText;
        [SerializeField] private TextMeshProUGUI reviewBonusText;
        
        [Header("Category Icons (same as StoreItemUI)")]
        [SerializeField] private Sprite shelfIcon;
        [SerializeField] private Sprite receptionIcon;
        [SerializeField] private Sprite checkoutIcon;
        
        private List<StoreItemUI> itemUIs = new List<StoreItemUI>();
        private Dictionary<string, CartEntry> cart = new Dictionary<string, CartEntry>();
        
        private void Awake()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
                
            if (immediateDeliveryToggle != null)
                immediateDeliveryToggle.onValueChanged.AddListener(OnImmediateDeliveryToggled);
        }
        
        private void OnDestroy()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }
        
        private void OnEnable()
        {
            // Reset immediate delivery
            isImmediateDelivery = false;
            if (immediateDeliveryToggle != null)
                immediateDeliveryToggle.isOn = false;
            
            // Refresh store items each time panel is shown
            PopulateStore();
            HideTooltip();
            RefreshCartDisplay();
            
            // Complete tut_store_open when panel is opened
            Core.TutorialManager.Instance?.CompleteByAction("store_panel_opened");
            
            // Tutorial trigger for item shop
            Core.TutorialManager.Instance?.TryShowTutorial("tut_item_about");
        }
        
        private void OnDisable()
        {
            // Complete tutorial when panel is closed
            Core.TutorialManager.Instance?.CompleteByAction("store_panel_closed");
            
            // Day 1 specific: trigger review check tutorial
            if (Core.GameManager.Instance?.DayCount == 1)
            {
                Core.TutorialManager.Instance?.TryShowTutorial("tut_review_check");
            }
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
            
            int currentStarLevel = ShopManager.Instance?.StarRating ?? 1;
            var storeItems = ItemDataRegistry.Instance.GetStoreItems();
            
            // Create item UIs (filtered by star level)
            foreach (var itemData in storeItems)
            {
                // Star level filter: hide if requiredStarLevel > currentStarLevel + 5
                int starDiff = itemData.requiredStarLevel - currentStarLevel;
                if (starDiff > 5)
                    continue; // Hide completely
                
                bool isLocked = starDiff > 0;
                
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
        }
        
        /// <summary>
        /// Adjust RectTransform height based on GridLayoutGroup settings and row count

        #region Item Detail Display
        
        /// <summary>
        /// Show item details in the bottom panels (called on hover)
        /// </summary>
        public void ShowItemDetail(ItemData item)
        {
            if (item == null) return;
            
            // Show detail panel root
            if (detailPanelRoot != null)
                detailPanelRoot.SetActive(true);
            
            // Left panel: Description
            if (descriptionText != null)
                descriptionText.text = item.GetLocalizedDescription();
            
            // Right panel: Category
            string categoryName = "";
            Sprite categorySprite = null;
            
            if (item.CanPlaceAtReception)
            {
                categoryName = LocalizationManager.Instance.Get("ui.reception");
                categorySprite = receptionIcon;
            }
            else if (item.CanUseAtCheckout)
            {
                categoryName = LocalizationManager.Instance.Get("ui.checkout");
                categorySprite = checkoutIcon;
            }
            else if (item.CanPlaceOnShelf)
            {
                categoryName = LocalizationManager.Instance.Get("ui.shelf");
                categorySprite = shelfIcon;
            }
            
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
            
            // Selling Price (upsellPrice)
            if (sellingPriceText != null)
            {
                if (item.upsellPrice > 0)
                    sellingPriceText.text = $"${item.upsellPrice}";
                else
                    sellingPriceText.text = "-";
            }
            
            // Review Bonus
            if (reviewBonusText != null)
            {
                if (item.reviewBonus != 0)
                    reviewBonusText.text = item.reviewBonus > 0 ? $"+{item.reviewBonus}" : $"{item.reviewBonus}";
                else
                    reviewBonusText.text = "-";
            }
        }
        
        /// <summary>
        /// Hide item detail panels (called on hover exit)
        /// </summary>
        public void HideItemDetail()
        {
            if (detailPanelRoot != null)
                detailPanelRoot.SetActive(false);
        }
        
        // Backward compatibility aliases
        public void ShowTooltip(string text, RectTransform itemRect) { }
        public void HideTooltip() => HideItemDetail();
        
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
        /// Calculate total price without fee
        /// </summary>
        private int CalculateBaseTotal()
        {
            int total = 0;
            foreach (var entry in cart.Values)
            {
                total += entry.itemData.price * entry.quantity;
            }
            return total;
        }

        /// <summary>
        /// Calculate total price including delivery fee
        /// </summary>
        private int CalculateTotalWithFee()
        {
            int baseTotal = CalculateBaseTotal();
            if (!isImmediateDelivery) return baseTotal;

            int fee = CalculateImmediateDeliveryFee(baseTotal);
            return baseTotal + fee;
        }
        
        /// <summary>
        /// Update total display
        /// </summary>
        private void UpdateTotalDisplay()
        {
            if (totalText != null)
            {
                int total = CalculateTotalWithFee();
                int baseTotal = CalculateBaseTotal();
                
                if (isImmediateDelivery && baseTotal > 0)
                {
                    float feePercent = GetImmediateDeliveryFeePercent() * 100f;
                    totalText.text = $"${total:N0} (+{feePercent:F0}%)";
                }
                else
                {
                    totalText.text = $"${total:N0}";
                }
            }

            // Update delivery fee text
            if (immediateDeliveryFeeText != null)
            {
                if (isImmediateDelivery)
                {
                    int baseTotal = CalculateBaseTotal();
                    int fee = CalculateImmediateDeliveryFee(baseTotal);
                    float feePercent = GetImmediateDeliveryFeePercent() * 100f;
                    immediateDeliveryFeeText.text = baseTotal > 0 ? $"+${fee:N0}" : "$0";
                }
                else
                {
                    immediateDeliveryFeeText.text = "";
                }
            }
        }

        private void OnImmediateDeliveryToggled(bool isOn)
        {
            isImmediateDelivery = isOn;
            UpdateTotalDisplay();
        }
        
        /// <summary>
        /// Get immediate delivery fee percentage based on owned delivery plans
        /// </summary>
        private float GetImmediateDeliveryFeePercent()
        {
            // Check for Executive plan (free delivery)
            if (IsUsefulItemOwned(EXECUTIVE_DELIVERY_PLAN_ID))
                return 0f;
            
            // Check for Premium plan (10% fee)
            if (IsUsefulItemOwned(PREMIUM_DELIVERY_PLAN_ID))
                return 0.10f;
            
            // Default: 20% fee
            return 0.20f;
        }
        
        /// <summary>
        /// Calculate immediate delivery fee for a given price
        /// </summary>
        private int CalculateImmediateDeliveryFee(int basePrice)
        {
            float percent = GetImmediateDeliveryFeePercent();
            return Mathf.RoundToInt(basePrice * percent);
        }

        private bool IsUsefulItemOwned(string itemId)
        {
            // Check WarehouseManager for useful items
            if (HairRemovalSim.Core.WarehouseManager.Instance != null)
            {
                return HairRemovalSim.Core.WarehouseManager.Instance.GetTotalItemCount(itemId) > 0;
            }
            return false;
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
            
            int totalCost = CalculateTotalWithFee();
            
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
            
            // Process purchase
            if (EconomyManager.Instance.SpendMoney(totalCost))
            {
                if (isImmediateDelivery)
                {
                    // Immediate delivery: add directly to warehouse
                    if (HairRemovalSim.Core.WarehouseManager.Instance != null)
                    {
                        foreach (var entry in cart.Values)
                        {
                            HairRemovalSim.Core.WarehouseManager.Instance.AddItem(entry.itemData.itemId, entry.quantity);
                            Debug.Log($"[StorePanel] {entry.quantity}x {entry.itemData.name} delivered immediately");
                        }
                        HairRemovalSim.Core.WarehouseManager.Instance.ShowNewIndicator();
                        Debug.Log($"[StorePanel] Order placed! Total: ${totalCost} - Immediate delivery");
                    }
                }
                else
                {
                    // Standard delivery: add to pending orders (delivered next day)
                    if (InventoryManager.Instance != null)
                    {
                        foreach (var entry in cart.Values)
                        {
                            InventoryManager.Instance.AddPendingOrder(entry.itemData, entry.quantity);
                            Debug.Log($"[StorePanel] Ordered {entry.quantity}x {entry.itemData.name} (delivered tomorrow)");
                        }
                        Debug.Log($"[StorePanel] Order placed! Total: ${totalCost} - Items will be delivered next day");
                    }
                }

                SoundManager.Instance.PlaySFX("money_plus");

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
