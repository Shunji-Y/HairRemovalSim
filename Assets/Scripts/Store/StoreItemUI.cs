using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// UI component for displaying a store item.
    /// Includes icon, name, quantity selector, and purchase button.
    /// Tooltip is managed by StorePanel (shared).
    /// </summary>
    public class StoreItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Data")]
        [SerializeField] private StoreItemData itemData;
        
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button purchaseButton;
        
        private int selectedQuantity = 1;
        private StorePanel storePanel;
        
        private void Awake()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.AddListener(DecreaseQuantity);
            if (increaseButton != null)
                increaseButton.onClick.AddListener(IncreaseQuantity);
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
                
            storePanel = GetComponentInParent<StorePanel>();
        }
        
        private void OnDestroy()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.RemoveListener(DecreaseQuantity);
            if (increaseButton != null)
                increaseButton.onClick.RemoveListener(IncreaseQuantity);
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }
        
        private void Start()
        {
            RefreshUI();
        }
        
        /// <summary>
        /// Set item data and refresh UI
        /// </summary>
        public void SetItemData(StoreItemData data)
        {
            itemData = data;
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
                nameText.text = itemData.itemName;
            if (priceText != null)
                priceText.text = $"${itemData.price * selectedQuantity}";
            if (quantityText != null)
                quantityText.text = selectedQuantity.ToString();
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
        
        private void OnPurchaseClicked()
        {
            Debug.Log($"[StoreItemUI] Purchase clicked. storePanel: {storePanel}, itemData: {itemData}");
            
            if (storePanel == null)
            {
                // Try to find StorePanel in parents again
                storePanel = GetComponentInParent<StorePanel>();
            }
            
            if (storePanel != null && itemData != null)
            {
                storePanel.ShowPurchaseDialog(itemData, selectedQuantity);
            }
            else
            {
                Debug.LogWarning("[StoreItemUI] Cannot show purchase dialog - missing storePanel or itemData");
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (storePanel == null)
                storePanel = GetComponentInParent<StorePanel>();
                
            if (storePanel != null && itemData != null)
            {
                // Pass the RectTransform for proper positioning
                storePanel.ShowTooltip(itemData.description, GetComponent<RectTransform>());
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
        public StoreItemData GetItemData() => itemData;
    }
}
