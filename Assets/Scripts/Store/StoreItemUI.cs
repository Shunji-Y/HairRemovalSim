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
                nameText.text = itemData.displayName;
            if (priceText != null)
                priceText.text = $"${itemData.price * selectedQuantity:N0}";
            if (quantityText != null)
                quantityText.text = selectedQuantity.ToString();
            
            // Lock state
            if (lockedOverlay != null)
                lockedOverlay.SetActive(isLocked);
            if (lockedText != null && isLocked)
                lockedText.text = $"Grade {itemData.requiredShopGrade}";
            if (cardBackground != null)
                cardBackground.color = isLocked ? lockedColor : normalColor;
            
            // Disable buttons when locked
            if (addToCartButton != null)
                addToCartButton.interactable = !isLocked;
            if (increaseButton != null)
                increaseButton.interactable = !isLocked;
            if (decreaseButton != null)
                decreaseButton.interactable = !isLocked;
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
                string tooltipText = !string.IsNullOrEmpty(itemData.storeDescription) 
                    ? itemData.storeDescription 
                    : itemData.description;
                storePanel.ShowTooltip(tooltipText, GetComponent<RectTransform>());
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (storePanel != null)
            {
                storePanel.HideTooltip();
            }
        }
        
        public int GetSelectedQuantity() => selectedQuantity;
        public ItemData GetItemData() => itemData;
    }
}
