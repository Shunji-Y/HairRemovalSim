using UnityEngine;
using TMPro;
using HairRemovalSim.Core;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Auto-localizes TMP_Text with a static key
    /// Attach to any TMP_Text that needs localization
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization")]
        [Tooltip("Key in GameText table (e.g., 'menu.title')")]
        [SerializeField] private string localizationKey;
        
        private TMP_Text textComponent;
        private LocalizationManager L => LocalizationManager.Instance;
        
        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }
        
        private void OnEnable()
        {
            UpdateText();
            if (L != null)
                L.OnLocaleChanged += UpdateText;
        }
        
        private void OnDisable()
        {
            if (L != null)
                L.OnLocaleChanged -= UpdateText;
        }
        
        private void UpdateText()
        {
            if (textComponent == null || string.IsNullOrEmpty(localizationKey)) return;
            
            string localizedText = L?.Get(localizationKey);
            if (!string.IsNullOrEmpty(localizedText))
            {
                textComponent.text = localizedText;
            }
        }
        
        /// <summary>
        /// Force update (e.g., after changing key at runtime)
        /// </summary>
        public void Refresh()
        {
            UpdateText();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Show key in editor for reference
            if (textComponent == null)
                textComponent = GetComponent<TMP_Text>();
        }
#endif
    }
}
