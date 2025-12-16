using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Localization manager - wrapper for Unity Localization
    /// Usage: Loc.Get("key") or Loc.Get("key", arg1, arg2)
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [Header("Settings")]
        [SerializeField] private string defaultTableName = "GameText";
        
        /// <summary>
        /// Event fired when locale changes
        /// </summary>
        public event Action OnLocaleChanged;
        
        /// <summary>
        /// Shorthand accessor
        /// </summary>
        public static LocalizationManager Loc => Instance;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Subscribe to locale changed event
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }
        
        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }
        
        private void HandleLocaleChanged(Locale newLocale)
        {
            Debug.Log($"[Localization] Locale changed to: {newLocale?.Identifier.Code}");
            OnLocaleChanged?.Invoke();
        }
        
        /// <summary>
        /// Get localized string by key
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            
            try
            {
                var localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(defaultTableName, key);
                return string.IsNullOrEmpty(localizedString) ? $"[{key}]" : localizedString;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] Failed to get key '{key}': {e.Message}");
                return $"[{key}]";
            }
        }
        
        /// <summary>
        /// Get localized string with formatting
        /// </summary>
        public string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] Format error for key '{key}': {e.Message}");
                return template;
            }
        }
        
        /// <summary>
        /// Get current locale code
        /// </summary>
        public string CurrentLocale => LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
        
        /// <summary>
        /// Set locale by code (e.g., "en", "ja", "zh-CN")
        /// </summary>
        public void SetLocale(string localeCode)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                if (locale.Identifier.Code == localeCode)
                {
                    LocalizationSettings.SelectedLocale = locale;
                    return;
                }
            }
            Debug.LogWarning($"[Localization] Locale not found: {localeCode}");
        }
    }
}
