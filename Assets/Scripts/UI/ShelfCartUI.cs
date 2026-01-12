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
        
        [Header("Laser Slots")]
        [SerializeField] private ShelfSlotUI faceLaserSlotUI;
        [SerializeField] private ShelfSlotUI bodyLaserSlotUI;
        
        [Header("Shaver Slot")]
        [SerializeField] private GameObject shaverSlotPrefab;
        private ShelfSlotUI generatedShaverSlot;
        
        // Linked shelf and bed
        private TreatmentShelf linkedShelf;
        private BedController linkedBed;
        private List<ShelfSlotUI> slotUIs = new List<ShelfSlotUI>();
        
        public TreatmentShelf LinkedShelf => linkedShelf;
        public BedController LinkedBed => linkedBed;

        public TMP_Text bedText;
        
        public void Initialize(TreatmentShelf shelf, string cartName)
        {
            linkedShelf = shelf;
            
            if (titleText != null)
            {
                titleText.text = cartName;
            }


            
            CreateSlotUIs();
        }
        
        /// <summary>
        /// Initialize with bed reference for laser slots
        /// </summary>
        public void Initialize(TreatmentShelf shelf, BedController bed, string cartName)
        {
            linkedShelf = shelf;
            linkedBed = bed;
            
            if (titleText != null)
            {
                titleText.text = cartName;
            }

            if (bedText != null)
            {
                bedText.text = LocalizationManager.Instance.Get("ui.bed") + $"{bed.bedNum}";
            }

            CreateSlotUIs();
            InitializeLaserSlots();
            InitializeShaverSlot();
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
            generatedShaverSlot = null;
            
            // Create slot UIs for each shelf slot (rowCount x columnCount)
            for (int row = 0; row < linkedShelf.rowCount; row++)
            {
                for (int col = 0; col < linkedShelf.columnCount; col++)
                {
                    // At [0,0], create shaver slot if prefab is assigned
                    if (row == 0 && col == 0 && shaverSlotPrefab != null)
                    {
                        var shaverObj = Instantiate(shaverSlotPrefab, slotGridParent);
                        generatedShaverSlot = shaverObj.GetComponent<ShelfSlotUI>();
                        if (generatedShaverSlot != null)
                        {
                            slotUIs.Add(generatedShaverSlot);
                        }
                        continue;
                    }
                    
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
        
        private void InitializeLaserSlots()
        {
            if (linkedBed == null) return;
            
            if (faceLaserSlotUI != null)
            {
                faceLaserSlotUI.InitializeAsLaserSlot(linkedBed, ShelfSlotUI.SlotMode.FaceLaser);
            }
            
            if (bodyLaserSlotUI != null)
            {
                bodyLaserSlotUI.InitializeAsLaserSlot(linkedBed, ShelfSlotUI.SlotMode.BodyLaser);
            }
        }
        
        private void InitializeShaverSlot()
        {
            if (generatedShaverSlot != null && linkedShelf != null)
            {
                // Shaver slot uses row 0, col 0 of the shelf
                generatedShaverSlot.InitializeAsShaverSlot(linkedShelf);
            }
        }
        
        public void RefreshFromShelf()
        {
            foreach (var slot in slotUIs)
            {
                slot.RefreshFromShelf();
            }
        }
        
        public void RefreshLaserSlots()
        {
            faceLaserSlotUI?.RefreshFromLaserBody();
            bodyLaserSlotUI?.RefreshFromLaserBody();
        }
        
        public void RefreshShaverSlot()
        {
            generatedShaverSlot?.RefreshFromShelf();
        }
    }
}
