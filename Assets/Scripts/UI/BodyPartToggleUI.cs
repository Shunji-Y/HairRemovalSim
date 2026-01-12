using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace HairRemovalSim.UI
{
    /// <summary>
    /// Individual body part toggle for reception UI
    /// Handles visual states: Normal, Requested (pulse), Selected (blue), Matched (green)
    /// </summary>
    public class BodyPartToggleUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Settings")]
        [Tooltip("Part name matching BodyPartDefinition.partName")]
        [SerializeField] private string partName;
        
        [Header("References")]
        [SerializeField] private Image partImage;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color requestedColor = new Color(1f, 0.7f, 0.2f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.9f);
        [SerializeField] private Color matchedColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
        
        [Header("Pulse Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.4f;
        [SerializeField] private float pulseMaxAlpha = 1f;
        
        // State
        private bool isRequested = false;
        private bool isSelected = false;
        private Coroutine pulseCoroutine;
        
        /// <summary>
        /// Part name for identification
        /// </summary>
        public string PartName => partName;
        
        /// <summary>
        /// Is this part selected by player?
        /// </summary>
        public bool IsSelected => isSelected;
        
        /// <summary>
        /// Is this part requested by customer?
        /// </summary>
        public bool IsRequested => isRequested;
        
        /// <summary>
        /// Event when selection state changes
        /// </summary>
        public event System.Action<BodyPartToggleUI, bool> OnSelectionChanged;
        
        private void Awake()
        {
            if (partImage == null)
                partImage = GetComponent<Image>();
        }
        
        private void OnEnable()
        {
            UpdateVisual();
        }
        
        private void OnDisable()
        {
            StopPulse();
        }
        
        /// <summary>
        /// Set the part name (if not set in inspector)
        /// </summary>
        public void SetPartName(string name)
        {
            partName = name;
        }
        
        /// <summary>
        /// Set whether this part is requested by customer
        /// </summary>
        public void SetRequested(bool requested)
        {
            isRequested = requested;
            UpdateVisual();
            
            if (isRequested && !isSelected)
            {
                StartPulse();
            }
            else
            {
                StopPulse();
            }
        }
        
        /// <summary>
        /// Set whether this part is selected by player
        /// </summary>
        public void SetSelected(bool selected, bool notify = true)
        {
            if (isSelected == selected) return;
            
            isSelected = selected;
            UpdateVisual();
            
            // Handle pulse animation
            if (isSelected)
            {
                // Stop pulse when selected
                StopPulse();
            }
            else if (isRequested)
            {
                // Resume pulse if deselected but still requested
                StartPulse();
            }
            
            if (notify)
                OnSelectionChanged?.Invoke(this, isSelected);
        }
        
        /// <summary>
        /// Reset to initial state
        /// </summary>
        public void Reset()
        {
            isRequested = false;
            isSelected = false;
            StopPulse();
            UpdateVisual();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isRequested) return;
            
                
            
            // Toggle selection
            SetSelected(!isSelected);
        }
        
        private void UpdateVisual()
        {
            if (partImage == null) return;
            
            Color targetColor;
            
            if (isSelected && isRequested)
            {
                // Matched - both selected and requested
                targetColor = matchedColor;
            }
            else if (isSelected)
            {
                // Selected but not requested
                targetColor = selectedColor;
            }
            else if (isRequested)
            {
                // Requested but not yet selected - pulse will handle this
                targetColor = requestedColor;
            }
            else
            {
                // Normal state
                targetColor = normalColor;
            }
            
            partImage.color = targetColor;
        }
        
        private void StartPulse()
        {
            if (pulseCoroutine != null) return;
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
        
        private void StopPulse()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }
        
        private IEnumerator PulseAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * pulseSpeed;
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f);
                
                if (partImage != null)
                {
                    Color c = requestedColor;
                    c.a = alpha;
                    partImage.color = c;
                }
                
                yield return null;
            }
        }
    }
}
