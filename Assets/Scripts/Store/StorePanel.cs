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
        
        private void OnEnable()
        {
            // Refresh store items each time panel is shown
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
            
            // Check if warehouse has space
            if (WarehouseManager.Instance != null)
            {
                if (!WarehouseManager.Instance.CanAddItem(item.itemId, quantity))
                {
                    Debug.Log($"[StorePanel] 倉庫がいっぱいです！");
                    // TODO: Show "warehouse full" message to user
                    return;
                }
            }
            
            // Check if player has enough money
            if (EconomyManager.Instance != null)
            {
                if (EconomyManager.Instance.SpendMoney(totalCost))
                {
                    // Add directly to warehouse (instant delivery for now)
                    if (WarehouseManager.Instance != null)
                    {
                        int added = WarehouseManager.Instance.AddItem(item.itemId, quantity);
                        Debug.Log($"[StorePanel] Purchased {added}x {item.displayName} for ${totalCost}");
                    }
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
