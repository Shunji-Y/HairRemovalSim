using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using HairRemovalSim.Core;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Editor tool for exporting/importing ItemData to/from CSV.
    /// Allows bulk editing of ItemData properties outside Unity.
    /// </summary>
    public static class ItemDataCSVManager
    {
        private const string CSV_PATH = "Assets/Data/ItemMaster.csv";
        private const string CSV_HEADER = "itemId,nameKey,category,price,upsellPrice,reviewBonus,requiredShopGrade,maxWarehouseStack,availableInStore,burnIntensity,toolType,targetArea";
        
        [MenuItem("Tools/HairRemovalSim/Export ItemData to CSV")]
        public static void ExportToCSV()
        {
            // Find all ItemData assets
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ItemDataCSVManager] No ItemData assets found.");
                return;
            }
            
            var items = new List<ItemData>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                    items.Add(item);
            }
            
            // Sort by itemId for consistency
            items = items.OrderBy(x => x.itemId).ToList();
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(CSV_PATH);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Build CSV
            var sb = new StringBuilder();
            sb.AppendLine(CSV_HEADER);
            
            foreach (var item in items)
            {
                string line = string.Join(",",
                    EscapeCSV(item.itemId),
                    EscapeCSV(item.nameKey),
                    item.category.ToString(),
                    item.price.ToString(),
                    item.upsellPrice.ToString(),
                    item.reviewBonus.ToString(),
                    item.requiredShopGrade.ToString(),
                    item.maxWarehouseStack.ToString(),
                    item.availableInStore ? "true" : "false",
                    item.burnIntensity.ToString("F2"),
                    item.toolType.ToString(),
                    item.targetArea.ToString()
                );
                sb.AppendLine(line);
            }
            
            File.WriteAllText(CSV_PATH, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            
            Debug.Log($"[ItemDataCSVManager] Exported {items.Count} items to {CSV_PATH}");
            EditorUtility.DisplayDialog("Export Complete", $"Exported {items.Count} ItemData assets to:\n{CSV_PATH}", "OK");
        }
        
        [MenuItem("Tools/HairRemovalSim/Import ItemData from CSV")]
        public static void ImportFromCSV()
        {
            if (!File.Exists(CSV_PATH))
            {
                Debug.LogError($"[ItemDataCSVManager] CSV file not found: {CSV_PATH}");
                EditorUtility.DisplayDialog("Import Error", $"CSV file not found:\n{CSV_PATH}\n\nRun Export first.", "OK");
                return;
            }
            
            // Build lookup of existing ItemData by itemId
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            var itemLookup = new Dictionary<string, ItemData>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    itemLookup[item.itemId] = item;
                }
            }
            
            // Parse CSV
            string[] lines = File.ReadAllLines(CSV_PATH, Encoding.UTF8);
            if (lines.Length < 2)
            {
                Debug.LogWarning("[ItemDataCSVManager] CSV file is empty or has no data rows.");
                return;
            }
            
            // Parse header
            string[] headers = ParseCSVLine(lines[0]);
            int idxItemId = System.Array.IndexOf(headers, "itemId");
            int idxCategory = System.Array.IndexOf(headers, "category");
            int idxPrice = System.Array.IndexOf(headers, "price");
            int idxUpsellPrice = System.Array.IndexOf(headers, "upsellPrice");
            int idxReviewBonus = System.Array.IndexOf(headers, "reviewBonus");
            int idxRequiredGrade = System.Array.IndexOf(headers, "requiredShopGrade");
            int idxMaxWarehouseStack = System.Array.IndexOf(headers, "maxWarehouseStack");
            int idxAvailableInStore = System.Array.IndexOf(headers, "availableInStore");
            int idxBurnIntensity = System.Array.IndexOf(headers, "burnIntensity");
            int idxToolType = System.Array.IndexOf(headers, "toolType");
            int idxTargetArea = System.Array.IndexOf(headers, "targetArea");
            
            if (idxItemId < 0)
            {
                Debug.LogError("[ItemDataCSVManager] CSV missing required 'itemId' column.");
                return;
            }
            
            int updatedCount = 0;
            int notFoundCount = 0;
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length <= idxItemId) continue;
                
                string itemId = values[idxItemId];
                if (!itemLookup.TryGetValue(itemId, out ItemData item))
                {
                    Debug.LogWarning($"[ItemDataCSVManager] ItemData not found for itemId: {itemId}");
                    notFoundCount++;
                    continue;
                }
                
                bool changed = false;
                
                // Update fields
                if (idxCategory >= 0 && idxCategory < values.Length)
                {
                    if (System.Enum.TryParse<ItemCategory>(values[idxCategory], out var cat) && item.category != cat)
                    {
                        item.category = cat;
                        changed = true;
                    }
                }
                
                if (idxPrice >= 0 && idxPrice < values.Length)
                {
                    if (int.TryParse(values[idxPrice], out int price) && item.price != price)
                    {
                        item.price = price;
                        changed = true;
                    }
                }
                
                if (idxUpsellPrice >= 0 && idxUpsellPrice < values.Length)
                {
                    if (int.TryParse(values[idxUpsellPrice], out int upsellPrice) && item.upsellPrice != upsellPrice)
                    {
                        item.upsellPrice = upsellPrice;
                        changed = true;
                    }
                }
                
                if (idxReviewBonus >= 0 && idxReviewBonus < values.Length)
                {
                    if (int.TryParse(values[idxReviewBonus], out int reviewBonus) && item.reviewBonus != reviewBonus)
                    {
                        item.reviewBonus = reviewBonus;
                        changed = true;
                    }
                }
                
                if (idxRequiredGrade >= 0 && idxRequiredGrade < values.Length)
                {
                    if (int.TryParse(values[idxRequiredGrade], out int grade) && item.requiredShopGrade != grade)
                    {
                        item.requiredShopGrade = grade;
                        changed = true;
                    }
                }
                
                if (idxMaxWarehouseStack >= 0 && idxMaxWarehouseStack < values.Length)
                {
                    if (int.TryParse(values[idxMaxWarehouseStack], out int stack) && item.maxWarehouseStack != stack)
                    {
                        item.maxWarehouseStack = stack;
                        changed = true;
                    }
                }
                
                if (idxAvailableInStore >= 0 && idxAvailableInStore < values.Length)
                {
                    bool available = values[idxAvailableInStore].ToLower() == "true";
                    if (item.availableInStore != available)
                    {
                        item.availableInStore = available;
                        changed = true;
                    }
                }
                
                if (idxBurnIntensity >= 0 && idxBurnIntensity < values.Length)
                {
                    if (float.TryParse(values[idxBurnIntensity], out float burn) && Mathf.Abs(item.burnIntensity - burn) > 0.001f)
                    {
                        item.burnIntensity = burn;
                        changed = true;
                    }
                }
                
                if (idxToolType >= 0 && idxToolType < values.Length)
                {
                    if (System.Enum.TryParse<TreatmentToolType>(values[idxToolType], out var toolType) && item.toolType != toolType)
                    {
                        item.toolType = toolType;
                        changed = true;
                    }
                }
                
                if (idxTargetArea >= 0 && idxTargetArea < values.Length)
                {
                    if (System.Enum.TryParse<ToolTargetArea>(values[idxTargetArea], out var area) && item.targetArea != area)
                    {
                        item.targetArea = area;
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    EditorUtility.SetDirty(item);
                    updatedCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[ItemDataCSVManager] Import complete. Updated: {updatedCount}, Not found: {notFoundCount}");
            EditorUtility.DisplayDialog("Import Complete", $"Updated {updatedCount} ItemData assets.\n{notFoundCount} items in CSV were not found.", "OK");
        }
        
        private static string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
        
        private static string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
