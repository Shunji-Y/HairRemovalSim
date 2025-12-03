using UnityEngine;
using HairRemovalSim.Interaction;

namespace HairRemovalSim.Effects
{
    // Simple highlighter that changes material color or enables an outline component
    // Since we don't have a shader, we'll use a color tint or emission.
    public class OutlineHighlighter : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _originalColor;
        private Material _material;
        
        [ColorUsage(false, true)]
        public Color highlightColor = new Color(0.5f, 0.5f, 0.2f); // Yellowish tint

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
                _originalColor = _material.color;
            }
        }

        public void Highlight()
        {
            if (_material != null)
            {
                _material.color = _originalColor + highlightColor;
            }
        }

        public void Unhighlight()
        {
            if (_material != null)
            {
                _material.color = _originalColor;
            }
        }
    }
}
