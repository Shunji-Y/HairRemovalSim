using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// UI component for displaying a cart item.
    /// </summary>
    public class CartItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button removeButton;
        
        private string itemId;
        private StorePanel storePanel;
        
        private void Awake()
        {
            if (removeButton != null)
                removeButton.onClick.AddListener(OnRemoveClicked);
        }
        
        private void OnDestroy()
        {
            if (removeButton != null)
                removeButton.onClick.RemoveListener(OnRemoveClicked);
        }
        
        /// <summary>
        /// Set cart item data
        /// </summary>
        public void SetData(string id, string displayName, int quantity, int unitPrice, StorePanel panel)
        {
            itemId = id;
            storePanel = panel;
            
            if (nameText != null)
                nameText.text = displayName;
            if (quantityText != null)
                quantityText.text = $"×{quantity}";
            if (priceText != null)
                priceText.text = $"${unitPrice * quantity:N0}";
        }
        
        private void OnRemoveClicked()
        {
            if (storePanel != null && !string.IsNullOrEmpty(itemId))
            {
                storePanel.RemoveFromCart(itemId);
            }
        }
        
        public string ItemId => itemId;
    }
}
