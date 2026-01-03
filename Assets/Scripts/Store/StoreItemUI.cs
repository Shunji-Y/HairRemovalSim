using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// UI component for displaying a store item.
    /// Allows adding items to cart with quantity selection.
    /// </summary>
    public class StoreItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Data")]
        [SerializeField] private ItemData itemData;
        
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button addToCartButton;
        
        [Header("Lock State")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TextMeshProUGUI lockedText;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.6f, 0.6f, 0.6f);
        
        [Header("Category Icons & Colors")]
        [SerializeField] private Image categoryIcon;
        [Tooltip("Icon for items placeable on treatment shelf")]
        [SerializeField] private Sprite shelfIcon;
        [Tooltip("Icon for items placeable at reception desk")]
        [SerializeField] private Sprite receptionIcon;
        [Tooltip("Icon for items usable at checkout")]
        [SerializeField] private Sprite checkoutIcon;
        [Tooltip("Background color for shelf items")]
        [SerializeField] private Color shelfColor = new Color(0.3f, 0.7f, 0.3f); // Green
        [Tooltip("Background color for reception items")]
        [SerializeField] private Color receptionColor = new Color(0.3f, 0.5f, 0.8f); // Blue
        [Tooltip("Background color for checkout items")]
        [SerializeField] private Color checkoutColor = new Color(0.8f, 0.6f, 0.3f); // Orange
        
        private int selectedQuantity = 1;
        private StorePanel storePanel;
        private bool isLocked;
        
        private void Awake()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.AddListener(DecreaseQuantity);
            if (increaseButton != null)
                increaseButton.onClick.AddListener(IncreaseQuantity);
            if (addToCartButton != null)
                addToCartButton.onClick.AddListener(OnAddToCartClicked);
                
            storePanel = GetComponentInParent<StorePanel>();
        }
        
        private void OnDestroy()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.RemoveListener(DecreaseQuantity);
            if (increaseButton != null)
                increaseButton.onClick.RemoveListener(IncreaseQuantity);
            if (addToCartButton != null)
                addToCartButton.onClick.RemoveListener(OnAddToCartClicked);
        }
        
        private void Start()
        {
            RefreshUI();
        }
        
        /// <summary>
        /// Set item data and refresh UI
        /// </summary>
        public void SetItemData(ItemData data, bool locked = false)
        {
            itemData = data;
            isLocked = locked;
            selectedQuantity = 1;
            RefreshUI();
        }
        
        /// <summary>
        /// Set reference to parent store panel
        /// </summary>
        public void SetStorePanel(StorePanel panel)
        {
            storePanel = panel;
        }
        
        private void RefreshUI()
        {
            if (itemData == null) return;
            
            if (iconImage != null)
                iconImage.sprite = itemData.icon;
            if (nameText != null)
                nameText.text = itemData.GetLocalizedName();
            if (priceText != null)
                priceText.text = $"${itemData.price * selectedQuantity:N0}";
            if (quantityText != null)
                quantityText.text = selectedQuantity.ToString();
            
            // Category icon and background color based on placement type
            UpdateCategoryDisplay();
            
            // Lock state
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            if (lockedText != null && isLocked)
                lockedText.text = $"★{itemData.requiredStarLevel}";
            
            // Apply locked color override if locked
            if (cardBackground != null && isLocked)
                cardBackground.color = lockedColor;
            
            // Disable buttons when locked
            if (addToCartButton != null)
                addToCartButton.interactable = !isLocked;
            if (increaseButton != null)
                increaseButton.interactable = !isLocked;
            if (decreaseButton != null)
                decreaseButton.interactable = !isLocked;
        }
        
        /// <summary>
        /// Update category icon and background color based on ItemData properties
        /// Priority: Reception > Checkout > Shelf (default)
        /// </summary>
        private void UpdateCategoryDisplay()
        {
            if (itemData == null) return;
            
            Sprite selectedIcon = null;
            Color selectedColor = normalColor;
            
            // Determine category (priority order: Reception > Checkout > Shelf)
            if (itemData.CanPlaceAtReception)
            {
                selectedIcon = receptionIcon;
                selectedColor = receptionColor;
            }
            else if (itemData.CanUseAtCheckout)
            {
                selectedIcon = checkoutIcon;
                selectedColor = checkoutColor;
            }
            else if (itemData.CanPlaceOnShelf)
            {
                selectedIcon = shelfIcon;
                selectedColor = shelfColor;
            }
            
            // Apply icon
            if (categoryIcon != null)
            {
                if (selectedIcon != null)
                {
                    categoryIcon.sprite = selectedIcon;
                    categoryIcon.gameObject.SetActive(true);
                }
                else
                {
                    categoryIcon.gameObject.SetActive(false);
                }
            }
            
            // Apply background color (if not locked)
            if (cardBackground != null && !isLocked)
            {
                cardBackground.color = selectedColor;
            }
        }
        
        public void IncreaseQuantity()
        {
            if (itemData == null) return;
            if (selectedQuantity < itemData.maxPurchasePerOrder)
            {
                selectedQuantity++;
                RefreshUI();
            }
        }
        
        public void DecreaseQuantity()
        {
            if (selectedQuantity > 1)
            {
                selectedQuantity--;
                RefreshUI();
            }
        }
        
        private void OnAddToCartClicked()
        {
            if (storePanel == null)
                storePanel = GetComponentInParent<StorePanel>();
            
            if (storePanel != null && itemData != null)
            {
                storePanel.AddToCart(itemData, selectedQuantity);
                
                // Reset quantity after adding to cart
                selectedQuantity = 1;
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("[StoreItemUI] Cannot add to cart - missing storePanel or itemData");
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (storePanel == null)
                storePanel = GetComponentInParent<StorePanel>();
                
            if (storePanel != null && itemData != null)
            {
                storePanel.ShowItemDetail(itemData);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (storePanel != null)
            {
                storePanel.HideItemDetail();
            }
        }
        
        public int GetSelectedQuantity() => selectedQuantity;
        public ItemData GetItemData() => itemData;
    }
}
