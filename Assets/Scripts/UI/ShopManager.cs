using UnityEngine;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    public class ShopManager : MonoBehaviour
    {
        public void BuyItem(string itemName, int cost)
        {
            if (EconomyManager.Instance.SpendMoney(cost))
            {
                Debug.Log($"Bought {itemName} for {cost}");
                // Add item to inventory (TODO: Inventory System)
                if (itemName == "Duct Tape")
                {
                    // Add tape stock
                }
            }
            else
            {
                Debug.Log("Not enough money!");
            }
        }

        // Called by UI Buttons
        public void BuyDuctTape() => BuyItem("Duct Tape", 500);
        public void BuyRazor() => BuyItem("Razor", 1000);
        public void BuyGel() => BuyItem("Cooling Gel", 2000);
    }
}
