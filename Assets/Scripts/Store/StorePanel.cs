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
    /// Now uses unified ItemData instead of StoreItemData.
    /// </summary>
    public class StorePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private PurchaseDialog purchaseDialog;
        
        [Header("Shared Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        private List<StoreItemUI> itemUIs = new List<StoreItemUI>();
        
        private void Start()
        {
            PopulateStore();
            HideTooltip();
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
            
            var storeItems = ItemDataRegistry.Instance.GetStoreItems();
            
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
            
            Transform tooltipTransform = tooltipPanel.transform;
            tooltipTransform.position = itemRect.position;
            tooltipTransform.rotation = itemRect.rotation;
            
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
        public void ShowPurchaseDialog(ItemData item, int quantity)
        {
            Debug.Log($"[StorePanel] ShowPurchaseDialog called. purchaseDialog: {purchaseDialog}");
            
            if (purchaseDialog == null)
            {
                Debug.LogWarning("[StorePanel] purchaseDialog is null!");
                return;
            }
            
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
        private void ProcessPurchase(ItemData item, int quantity)
        {
            int totalCost = item.price * quantity;
            
            // Check if player has enough money
            if (EconomyManager.Instance != null)
            {
                if (EconomyManager.Instance.SpendMoney(totalCost))
                {
                    // Add to pending orders (delivered next day)
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.AddPendingOrder(item, quantity);
                    }
                    
                    Debug.Log($"[StorePanel] Ordered {quantity}x {item.displayName} for ${totalCost} (delivered next day)");
                }
                else
                {
                    Debug.Log($"[StorePanel] Not enough money! Need ${totalCost}");
                }
            }
        }
        
        /// <summary>
        /// Refresh store display
        /// </summary>
        public void RefreshStore()
        {
            PopulateStore();
        }
    }
}
