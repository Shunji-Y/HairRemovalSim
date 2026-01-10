using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Save data structure - easily extendable by adding new fields
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Meta info
        public int saveSlot;
        public string saveDate;
        public string saveVersion = "1.0";
        
        // Core game state
        public int day = 1;
        public int money = 1000;
        public int shopGrade = 1;
        public int loanRemaining = 0;
        
        // Tutorial completed flags (reset on new game)
        public List<string> completedTutorials = new List<string>();
        
        // Statistics (for future use)
        public int totalRevenue = 0;
        public int totalExpenses = 0;
        public int totalCustomers = 0;
        
        // Placeholder for future expansion
        public List<string> ownedItemIds = new List<string>();
    }
    
    /// <summary>
    /// Manages save/load functionality with 3 slots
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        public const int MAX_SLOTS = 3;
        private const string SAVE_FOLDER = "Saves";
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string SAVE_EXTENSION = ".json";
        
        // Current loaded save data (null if new game)
        public SaveData CurrentSaveData { get; private set; }
        public int CurrentSlot { get; private set; } = -1;
        
        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureSaveFolderExists();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void EnsureSaveFolderExists()
        {
            string path = GetSaveFolderPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        private string GetSaveFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        }
        
        private string GetSaveFilePath(int slot)
        {
            return Path.Combine(GetSaveFolderPath(), $"{SAVE_FILE_PREFIX}{slot}{SAVE_EXTENSION}");
        }
        
        /// <summary>
        /// Save current game state to specified slot
        /// </summary>
        public bool Save(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS)
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }
            
            try
            {
                // Create save data from current game state
                SaveData data = CreateSaveDataFromCurrentState();
                data.saveSlot = slot;
                data.saveDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                
                // Serialize to JSON
                string json = JsonUtility.ToJson(data, true);
                
                // Write to file
                string path = GetSaveFilePath(slot);
                File.WriteAllText(path, json);
                
                CurrentSaveData = data;
                CurrentSlot = slot;
                
                Debug.Log($"[SaveManager] Saved to slot {slot}: {path}");
                OnSaveCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load game state from specified slot
        /// </summary>
        public bool Load(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS)
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }
            
            string path = GetSaveFilePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] No save file at slot {slot}");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                
                if (data == null)
                {
                    Debug.LogError("[SaveManager] Failed to parse save data");
                    return false;
                }
                
                CurrentSaveData = data;
                CurrentSlot = slot;
                
                // Apply save data to game state
                ApplySaveDataToGame(data);
                
                Debug.Log($"[SaveManager] Loaded from slot {slot}");
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if a save exists in the specified slot
        /// </summary>
        public bool HasSaveData(int slot)
        {
            return File.Exists(GetSaveFilePath(slot));
        }
        
        /// <summary>
        /// Get save info for display (without loading full data)
        /// </summary>
        public (bool exists, int day, string date) GetSlotInfo(int slot)
        {
            if (!HasSaveData(slot))
            {
                return (false, 0, null);
            }
            
            try
            {
                string json = File.ReadAllText(GetSaveFilePath(slot));
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return (true, data.day, data.saveDate);
            }
            catch
            {
                return (false, 0, null);
            }
        }
        
        /// <summary>
        /// Delete save data in specified slot
        /// </summary>
        public bool DeleteSlot(int slot)
        {
            string path = GetSaveFilePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] Deleted slot {slot}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Start a new game (clears current save data)
        /// </summary>
        public void StartNewGame()
        {
            CurrentSaveData = new SaveData();
            CurrentSlot = -1;
            ApplySaveDataToGame(CurrentSaveData);
            Debug.Log("[SaveManager] Started new game");
        }
        
        /// <summary>
        /// Create SaveData from current game state
        /// </summary>
        private SaveData CreateSaveDataFromCurrentState()
        {
            SaveData data = CurrentSaveData ?? new SaveData();
            
            // Get data from managers
            if (GameManager.Instance != null)
            {
                data.day = GameManager.Instance.CurrentDay;
            }
            
            if (EconomyManager.Instance != null)
            {
                data.money = EconomyManager.Instance.CurrentMoney;
                data.loanRemaining = EconomyManager.Instance.LoanRemaining;
            }
            
            if (ShopManager.Instance != null)
            {
                data.shopGrade = ShopManager.Instance.CurrentGrade;
            }
            
            // Tutorial flags are managed separately by TutorialManager
            if (TutorialManager.Instance != null)
            {
                data.completedTutorials = TutorialManager.Instance.GetCompletedTutorials();
            }
            
            return data;
        }
        
        /// <summary>
        /// Apply SaveData to game state
        /// </summary>
        private void ApplySaveDataToGame(SaveData data)
        {
            if (data == null) return;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetDay(data.day);
            }
            
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetMoney(data.money);
                EconomyManager.Instance.SetLoan(data.loanRemaining);
            }
            
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.SetGrade(data.shopGrade);
            }
            
            // Restore tutorial flags
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SetCompletedTutorials(data.completedTutorials);
            }
        }
    }
}
