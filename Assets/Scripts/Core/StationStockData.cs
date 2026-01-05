using System;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Data for a single stock slot
    /// </summary>
    [Serializable]
    public struct StockSlotData
    {
        public string itemId;
        public int quantity;
        
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
        
        public void Clear()
        {
            itemId = null;
            quantity = 0;
        }
    }
    
    /// <summary>
    /// Stock data for one station (Reception or CashRegister)
    /// Contains 8 slots by default
    /// </summary>
    [Serializable]
    public class StationStockData
    {
        public const int SLOT_COUNT = 8;
        public StockSlotData[] slots = new StockSlotData[SLOT_COUNT];
        
        public StationStockData()
        {
            slots = new StockSlotData[SLOT_COUNT];
        }
        
        public StockSlotData GetSlot(int index)
        {
            if (index < 0 || index >= SLOT_COUNT) return default;
            return slots[index];
        }
        
        public void SetSlot(int index, string itemId, int quantity)
        {
            if (index < 0 || index >= SLOT_COUNT) return;
            slots[index].itemId = itemId;
            slots[index].quantity = quantity;
        }
        
        public void ClearSlot(int index)
        {
            if (index < 0 || index >= SLOT_COUNT) return;
            slots[index].Clear();
        }
        
        public void ClearAll()
        {
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                slots[i].Clear();
            }
        }
    }
}
