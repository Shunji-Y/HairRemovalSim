using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using HairRemovalSim.Core;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// Purchase confirmation dialog with item details and Confirm/Cancel buttons.
    /// Now uses unified ItemData.
    /// </summary>
    public class PurchaseDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI totalPriceText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private ItemData currentItem;
        private int currentQuantity;
        private Action<bool> onResult;
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
                
            Hide();
        }
        
        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
        
        /// <summary>
        /// Show purchase confirmation dialog
        /// </summary>
        public void Show(ItemData item, int quantity, Action<bool> callback)
        {
            currentItem = item;
            currentQuantity = quantity;
            onResult = callback;
            
            int totalPrice = item.price * quantity;
            
            // Update UI elements
            if (itemIcon != null)
                itemIcon.sprite = item.icon;
            if (itemNameText != null)
                itemNameText.text = item.GetLocalizedName();
            if (quantityText != null)
                quantityText.text = $"x{quantity}";
            if (totalPriceText != null)
                totalPriceText.text = $"¥{totalPrice:N0}";
                
            if (dialogPanel != null)
                dialogPanel.SetActive(true);
        }
        
        /// <summary>
        /// Hide the dialog
        /// </summary>
        public void Hide()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
        }
        
        private void OnConfirmClicked()
        {
            Hide();
            onResult?.Invoke(true);
        }
        
        private void OnCancelClicked()
        {
            Hide();
            onResult?.Invoke(false);
        }
    }
}
