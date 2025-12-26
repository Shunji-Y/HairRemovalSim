using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Effects
{
    /// <summary>
    /// Highlighter that changes material color for all renderers including children.
    /// Supports multiple materials per renderer.
    /// </summary>
    public class OutlineHighlighter : MonoBehaviour
    {
        [ColorUsage(false, true)]
        public Color highlightColor = new Color(0.5f, 0.5f, 0.2f);
        
        [Tooltip("Include renderers on child objects")]
        public bool includeChildren = true;
        
        private struct MaterialData
        {
            public Material material;
            public Color originalColor;
        }
        
        private List<MaterialData> cachedMaterials = new List<MaterialData>();
        private bool isHighlighted = false;
        
        private void Awake()
        {
            CacheMaterials();
        }
        
        private void CacheMaterials()
        {
            cachedMaterials.Clear();
            
            Renderer[] renderers;
            if (includeChildren)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
            else
            {
                var rend = GetComponent<Renderer>();
                renderers = rend != null ? new Renderer[] { rend } : new Renderer[0];
            }
            
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                // Get all materials on this renderer
                foreach (var mat in renderer.materials)
                {
                    if (mat != null && mat.HasProperty("_Color"))
                    {
                        cachedMaterials.Add(new MaterialData
                        {
                            material = mat,
                            originalColor = mat.color
                        });
                    }
                }
            }
        }
        
        public void Highlight()
        {
            if (isHighlighted) return;
            isHighlighted = true;
            
            // Re-cache if empty (might have been instantiated late)
            if (cachedMaterials.Count == 0)
            {
                CacheMaterials();
            }
            
            foreach (var data in cachedMaterials)
            {
                if (data.material != null)
                {
                    data.material.color = data.originalColor + highlightColor;
                }
            }
        }
        
        public void Unhighlight()
        {
            if (!isHighlighted) return;
            isHighlighted = false;
            
            foreach (var data in cachedMaterials)
            {
                if (data.material != null)
                {
                    data.material.color = data.originalColor;
                }
            }
        }
        
        /// <summary>
        /// Refresh cached materials (call after object structure changes)
        /// </summary>
        public void RefreshCache()
        {
            bool wasHighlighted = isHighlighted;
            if (wasHighlighted) Unhighlight();
            
            CacheMaterials();
            
            if (wasHighlighted) Highlight();
        }
    }
}
