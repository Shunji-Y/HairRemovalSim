using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.Store
{
    /// <summary>
    /// Manages the store panel UI.
    /// Displays items, handles purchase flow and shared tooltip.
    /// </summary>
    public class StorePanel : MonoBehaviour
    {
        [Header("Item Catalog")]
        [SerializeField] private List<StoreItemData> storeItems = new List<StoreItemData>();
        
        [Header("UI References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private PurchaseDialog purchaseDialog;
        
        [Header("Shared Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private Vector3 tooltipOffset = new Vector3(0.1f, 0, 0);
        
        private List<StoreItemUI> itemUIs = new List<StoreItemUI>();
        
        private void Start()
        {
            PopulateStore();
            HideTooltip();
        }
        
        /// <summary>
        /// Populate store with items from catalog
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
            
            // Create item UIs
            foreach (var itemData in storeItems)
            {
                GameObject itemObj = Instantiate(itemPrefab, itemContainer);
                StoreItemUI itemUI = itemObj.GetComponent<StoreItemUI>();
                if (itemUI != null)
                {
                    itemUI.SetItemData(itemData);
                    itemUI.SetStorePanel(this);
                    itemUIs.Add(itemUI);
                }
            }
        }
        
        /// <summary>
        /// Show shared tooltip at item position
        /// </summary>
        public void ShowTooltip(string text, RectTransform itemRect)
        {
            if (tooltipPanel == null || itemRect == null) return;
            
            tooltipPanel.SetActive(true);
            
            // Position tooltip relative to the item
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                // Get item's world position and offset to the right
                Vector3 itemPos = itemRect.position;
                tooltipRect.position = itemPos + tooltipOffset;
            }
            
            if (tooltipText != null)
                tooltipText.text = text;
        }
        
        /// <summary>
        /// Hide the shared tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
        
        /// <summary>
        /// Show purchase confirmation dialog
        /// </summary>
        public void ShowPurchaseDialog(StoreItemData item, int quantity)
        {
            if (purchaseDialog == null) return;
            
            purchaseDialog.Show(item, quantity, (confirmed) =>
            {
                if (confirmed)
                {
                    ProcessPurchase(item, quantity);
                }
            });
        }
        
        /// <summary>
        /// Process the actual purchase
        /// </summary>
        private void ProcessPurchase(StoreItemData item, int quantity)
        {
            int totalCost = item.price * quantity;
            
            // Check if player has enough money
            if (EconomyManager.Instance != null)
            {
                if (EconomyManager.Instance.SpendMoney(totalCost))
                {
                    // Add to inventory
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.AddItem(item, quantity);
                    }
                    
                    Debug.Log($"[StorePanel] Purchased {quantity}x {item.itemName} for ${totalCost}");
                }
                else
                {
                    Debug.Log($"[StorePanel] Not enough money! Need ${totalCost}");
                    // TODO: Show error message to player
                }
            }
        }
        
        /// <summary>
        /// Refresh store display (call when items change)
        /// </summary>
        public void RefreshStore()
        {
            PopulateStore();
        }
    }
}
