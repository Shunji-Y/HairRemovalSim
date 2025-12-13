using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// UI representation of a single treatment shelf (cart) in the warehouse panel.
    /// </summary>
    public class ShelfCartUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform slotGridParent;
        [SerializeField] private GameObject shelfSlotPrefab;
        
        // Linked shelf
        private TreatmentShelf linkedShelf;
        private List<ShelfSlotUI> slotUIs = new List<ShelfSlotUI>();
        
        public TreatmentShelf LinkedShelf => linkedShelf;
        
        public void Initialize(TreatmentShelf shelf, string cartName)
        {
            linkedShelf = shelf;
            
            if (titleText != null)
            {
                titleText.text = cartName;
            }
            
            CreateSlotUIs();
        }
        
        private void CreateSlotUIs()
        {
            if (linkedShelf == null || shelfSlotPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in slotGridParent)
            {
                Destroy(child.gameObject);
            }
            slotUIs.Clear();
            
            // Create slot UIs for each shelf slot (rowCount x columnCount)
            for (int row = 0; row < linkedShelf.rowCount; row++)
            {
                for (int col = 0; col < linkedShelf.columnCount; col++)
                {
                    var slotObj = Instantiate(shelfSlotPrefab, slotGridParent);
                    var slotUI = slotObj.GetComponent<ShelfSlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.Initialize(linkedShelf, row, col);
                        slotUIs.Add(slotUI);
                    }
                }
            }
        }
        
        public void RefreshFromShelf()
        {
            foreach (var slot in slotUIs)
            {
                slot.RefreshFromShelf();
            }
        }
    }
}
