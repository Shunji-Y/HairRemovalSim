using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace HairRemovalSim.Treatment
{
    public class HairTreatmentController : MonoBehaviour
    {
        [Header("Completion Settings")]
        // public int minWhitePixels = 0; // Moved to BodyPart

        [Header("Settings")]
        public bool useUVMasking = true; // Toggle to enable/disable UV-based masking
        public int maskResolution = 1024;
        public Shader paintShader;
        public float brushSize = 0.05f;
        [Range(0f, 10f)]
        public float completionBuffer = 2f;

        private RenderTexture[] maskTextures; // Array of masks, one per material
        private Material paintMaterial;
        private Material[] targetMaterials;
        private Renderer meshRenderer;
        private HairRemovalSim.Core.BodyPart bodyPart;
        private int initialWhitePixels = 0;
        private bool isCompleted = false;
        
        // Track which submesh is being treated (set from the first ApplyTreatment call)
        // -1 means not yet determined, will be set on first paint
        private int activeSubmeshIndex = -1;
        private bool hasActiveSubmesh = false;
        
        // Target body parts for UV region-based counting
        private List<HairRemovalSim.Core.BodyPartDefinition> targetBodyParts = new List<HairRemovalSim.Core.BodyPartDefinition>();
        
        // Per-body-part tracking
        private Dictionary<string, int> perPartInitialPixels = new Dictionary<string, int>();
        private Dictionary<string, float> perPartCompletion = new Dictionary<string, float>();
        private HashSet<string> completedParts = new HashSet<string>();
        
        // Event fired when an individual body part completes (partName)
        public event Action<string> OnPartCompleted;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer == null) return;
            
            bodyPart = GetComponent<HairRemovalSim.Core.BodyPart>();
            targetMaterials = meshRenderer.materials;
            
            // Initialize mask array
            maskTextures = new RenderTexture[targetMaterials.Length];

            for (int i = 0; i < targetMaterials.Length; i++)
            {
                // Use ARGB32 instead of R8 for broader platform support
                maskTextures[i] = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.ARGB32);
                maskTextures[i].wrapMode = TextureWrapMode.Repeat; // Repeat for tiled UVs (0-1, 1-2, 2-3, etc.)
                maskTextures[i].Create();
                
                if (targetMaterials[i] != null)
                {
                    targetMaterials[i].SetTexture("_MaskMap", maskTextures[i]);
                }
            }
            
            ClearMask();

            if (paintShader == null)
            {
                paintShader = Shader.Find("Custom/PaintShader");
            }
            if (paintShader != null)
            {
                paintMaterial = new Material(paintShader);
            }
            else
            {
                Debug.LogError("PaintShader not found!");
            }
            
            // Delay initial pixel count until after first render
            StartCoroutine(InitializePixelCount());
            
            // Initial update
            UpdateCompletion();
        }
        
        private System.Collections.IEnumerator InitializePixelCount()
        {
            // Wait for mask to be painted with mesh UVs
            yield return null;
            
            // Count actual white pixels (only where mesh exists)
            initialWhitePixels = CountWhitePixels();
            Debug.Log($"[HairTreatmentController] Initial white pixels (mesh-covered area): {initialWhitePixels}");
            
            UpdateCompletion();
        }
        
        /// <summary>
        /// Set target body parts for UV region-based completion tracking.
        /// Called by CustomerController after treatment plan is assigned.
        /// </summary>
        public void SetTargetBodyParts(List<HairRemovalSim.Core.BodyPartDefinition> parts)
        {
            targetBodyParts = parts ?? new List<HairRemovalSim.Core.BodyPartDefinition>();
            
            // Clear per-part tracking
            perPartInitialPixels.Clear();
            perPartCompletion.Clear();
            completedParts.Clear();
            
            // Calculate initial pixels for each target part
            foreach (var part in targetBodyParts)
            {
                int partPixels = CountWhitePixelsForPart(part);
                perPartInitialPixels[part.partName] = partPixels;
                perPartCompletion[part.partName] = 0f;
                Debug.Log($"[HairTreatmentController] Part {part.partName}: Initial pixels = {partPixels}");
            }
            
            // Recalculate total initial pixels
            initialWhitePixels = CountWhitePixels();
            Debug.Log($"[HairTreatmentController] Set {targetBodyParts.Count} target parts. Total initial pixels: {initialWhitePixels}");
        }
        
        /// <summary>
        /// Reset controller for reuse from object pool.
        /// Called when customer is reused from pool.
        /// </summary>
        public void ResetForReuse()
        {
            // Reset completion state
            isCompleted = false;
            initialWhitePixels = 0;
            activeSubmeshIndex = -1;
            hasActiveSubmesh = false;
            targetBodyParts.Clear();
            
            // Clear per-part tracking
            perPartInitialPixels.Clear();
            perPartCompletion.Clear();
            completedParts.Clear();
            
            // Clear masks back to white (hair visible everywhere)
            ClearMask();
            
            // Hide any decals
            HideDecal();
            
            Debug.Log($"[HairTreatmentController] Reset for reuse: {name}");
        }

        public void ClearMask()
        {
            // Step 1: Clear all masks to WHITE (hair everywhere by default)
            // This ensures that if UV layout rendering fails or doesn't cover everything, we still have hair.
            for (int i = 0; i < maskTextures.Length; i++)
            {
                RenderTexture.active = maskTextures[i];
                GL.Clear(false, true, Color.white);
                RenderTexture.active = null;
            }
            
            // Step 2: Paint white only where mesh exists (using GPU rendering)
            // Actually, since we want hair everywhere initially, we might not need UVLayout for initialization
            // unless we want to restrict hair ONLY to the mesh UV islands (to avoid texture bleeding).
            // But if the background is black, hair won't show.
            // If we clear to white, hair shows everywhere on the texture.
            // Let's try clearing to white first. If that causes bleeding, we can revisit UVLayout.
            
            /*
            Mesh meshToRender = null;
            
            if (useUVMasking)
            {
                // Check for MeshFilter first
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshToRender = meshFilter.sharedMesh;
                }
                // If no MeshFilter, check for SkinnedMeshRenderer
                else
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        meshToRender = skinnedMeshRenderer.sharedMesh;
                    }
                }
            }

            if (meshToRender != null)
            {
                Material uvLayoutMat = new Material(Shader.Find("Hidden/UVLayout"));
                if (uvLayoutMat != null)
                {
                    // Render mesh into mask texture using UV layout shader
                    CommandBuffer cmd = new CommandBuffer();
                    
                    // Draw each submesh to its corresponding mask texture
                    // Assuming submesh index matches material index
                    for (int i = 0; i < Mathf.Min(maskTextures.Length, meshToRender.subMeshCount); i++)
                    {
                        // Clear to black first if we are strictly using UV layout
                        cmd.SetRenderTarget(maskTextures[i]);
                        cmd.ClearRenderTarget(false, true, Color.black);
                        
                        cmd.DrawMesh(meshToRender, Matrix4x4.identity, uvLayoutMat, i);
                    }
                    
                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    Destroy(uvLayoutMat);
                }
            }
            */
        }

        // Track which submesh has the decal enabled (for proper cleanup)
        private int previousDecalSubMeshIndex = -1;

        // Update decal position on the mask
        public void UpdateDecal(Vector2 uvPosition, float angle, Vector2 size, Color color, int subMeshIndex)
        {
            if (subMeshIndex < 0 || subMeshIndex >= targetMaterials.Length) return;
            if (targetMaterials[subMeshIndex] == null) return;
            
            // Hide decal on previous submesh if we switched to a different one
            if (previousDecalSubMeshIndex >= 0 && previousDecalSubMeshIndex != subMeshIndex)
            {
                if (previousDecalSubMeshIndex < targetMaterials.Length && targetMaterials[previousDecalSubMeshIndex] != null)
                {
                    targetMaterials[previousDecalSubMeshIndex].SetFloat("_DecalEnabled", 0.0f);
                }
            }
            previousDecalSubMeshIndex = subMeshIndex;
            
            // Only update the material corresponding to the hit submesh
            Material mat = targetMaterials[subMeshIndex];
            mat.SetFloat("_DecalEnabled", 1.0f);
            mat.SetVector("_DecalUVCenter", new Vector4(uvPosition.x, uvPosition.y, 0, 0));
            mat.SetVector("_DecalUVSize", new Vector4(size.x, size.y, 0, 0));
            mat.SetColor("_DecalColor", color);
            mat.SetFloat("_DecalUVAngle", angle);
        }

        public void HideDecal()
        {
            foreach (var mat in targetMaterials)
            {
                if (mat != null) mat.SetFloat("_DecalEnabled", 0.0f);
            }
        }

        // Apply treatment (remove hair) at UV position
        public void ApplyTreatment(Vector2 uvPosition, Vector2 size, float angle, int subMeshIndex)
        {
            if (paintMaterial == null)
            {
                Debug.LogError("[HairTreatmentController] PaintMaterial is null!");
                return;
            }
            if (subMeshIndex < 0 || subMeshIndex >= maskTextures.Length)
            {
                Debug.LogError($"[HairTreatmentController] SubMeshIndex {subMeshIndex} out of range (Length: {maskTextures.Length})");
                return;
            }

            Debug.Log($"[HairTreatmentController] Applying treatment at {uvPosition}, Size: {size}, Angle: {angle}, SubMesh: {subMeshIndex}");

            // Track which submesh we're treating (set on first paint)
            if (!hasActiveSubmesh)
            {
                activeSubmeshIndex = subMeshIndex;
                hasActiveSubmesh = true;
                // Recalculate initial pixels for this specific submesh only
                initialWhitePixels = CountWhitePixels();
                Debug.Log($"[HairTreatmentController] Active submesh set to {activeSubmeshIndex}, initial pixels: {initialWhitePixels}");
            }

            // Setup paint material to draw a rectangle (tape shape)
            // Shader properties: _BrushMode (1=Rect), _BrushRect (x,y,w,h), _BrushAngle, _BrushColor
            paintMaterial.SetFloat("_BrushMode", 1.0f); // Rect mode
            paintMaterial.SetVector("_BrushRect", new Vector4(uvPosition.x, uvPosition.y, size.x, size.y));
            paintMaterial.SetFloat("_BrushAngle", angle);
            paintMaterial.SetColor("_BrushColor", Color.black); // Paint black to remove hair
            
            // Draw black shape on the specific mask texture
            RenderTexture.active = maskTextures[subMeshIndex];
            GL.PushMatrix();
            GL.LoadOrtho();
            
            // Use Graphics.Blit with shader to handle brush shape
            RenderTexture temp = RenderTexture.GetTemporary(maskTextures[subMeshIndex].width, maskTextures[subMeshIndex].height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(maskTextures[subMeshIndex], temp);
            Graphics.Blit(temp, maskTextures[subMeshIndex], paintMaterial);
            RenderTexture.ReleaseTemporary(temp);
            
            GL.PopMatrix();
            RenderTexture.active = null;
            
            UpdateCompletion();
        }

        private void UpdateCompletion()
        {
            if (isCompleted || bodyPart == null) return;

            int currentWhitePixels = CountWhitePixels();
            
            // Calculate overall percentage based on initial white pixels
            float percentage = 0f;
            if (initialWhitePixels > 0)
            {
                int removedPixels = initialWhitePixels - currentWhitePixels;
                percentage = (float)removedPixels / initialWhitePixels * 100f;
                
                Debug.Log($"[HairTreatmentController] {name}: Removed {removedPixels}/{initialWhitePixels} pixels = {percentage:F1}%");
            }
            
            // Update per-part completion
            foreach (var part in targetBodyParts)
            {
                if (completedParts.Contains(part.partName)) continue; // Already completed
                
                int initialPartPixels = perPartInitialPixels.ContainsKey(part.partName) ? perPartInitialPixels[part.partName] : 0;
                if (initialPartPixels <= 0) continue;
                
                int currentPartPixels = CountWhitePixelsForPart(part);
                int removedPartPixels = initialPartPixels - currentPartPixels;
                float partPercentage = Mathf.Clamp((float)removedPartPixels / initialPartPixels * 100f + completionBuffer, 0f, 100f);
                
                perPartCompletion[part.partName] = partPercentage;
                
                // Check if this part is now completed
                if (partPercentage >= 100f && !completedParts.Contains(part.partName))
                {
                    completedParts.Add(part.partName);
                    OnPartCompleted?.Invoke(part.partName);
                    Debug.Log($"[HairTreatmentController] Part {part.partName} completed!");
                }
            }
            
            // Clamp and add buffer for overall progress
            percentage = Mathf.Clamp(percentage + completionBuffer, 0f, 100f);
            
            bodyPart.UpdateCompletion(percentage);

            if (percentage >= 100f)
            {
                isCompleted = true;
                HideDecal(); // Ensure decal is hidden
                Debug.Log($"[HairTreatmentController] Treatment completed for {name}!");
            }
        }

        /// <summary>
        /// Count white pixels in MaskMap, but only where HairGrowthMask indicates hair exists.
        /// Counts ALL submeshes with hair (not filtered by activeSubmesh) to support multi-part treatments.
        /// </summary>
        private int CountWhitePixels()
        {
            int totalHairPixels = 0;
            
            for (int matIndex = 0; matIndex < maskTextures.Length; matIndex++)
            {
                var mask = maskTextures[matIndex];
                if (mask == null) continue;
                
                // Skip materials that don't support hair (e.g., Nails, Eyelash)
                if (targetMaterials[matIndex] == null) continue;
                if (!targetMaterials[matIndex].HasProperty("_HairGrowthMask")) continue;
                
                // Get HairGrowthMask from the material
                Texture hairGrowthTex = targetMaterials[matIndex].GetTexture("_HairGrowthMask");
                if (hairGrowthTex == null) continue;
                
                // Create small temporary textures for reading
                int smallSize = 64;
                
                RenderTexture smallMaskRT = RenderTexture.GetTemporary(smallSize, smallSize, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(mask, smallMaskRT);
                Texture2D maskTex = new Texture2D(smallSize, smallSize, TextureFormat.ARGB32, false);
                RenderTexture.active = smallMaskRT;
                maskTex.ReadPixels(new Rect(0, 0, smallSize, smallSize), 0, 0);
                maskTex.Apply();
                
                RenderTexture smallHairRT = RenderTexture.GetTemporary(smallSize, smallSize, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(hairGrowthTex, smallHairRT);
                Texture2D hairGrowthTex2D = new Texture2D(smallSize, smallSize, TextureFormat.ARGB32, false);
                RenderTexture.active = smallHairRT;
                hairGrowthTex2D.ReadPixels(new Rect(0, 0, smallSize, smallSize), 0, 0);
                hairGrowthTex2D.Apply();
                RenderTexture.active = null;
                
                Color[] maskPixels = maskTex.GetPixels();
                Color[] hairPixels = hairGrowthTex2D.GetPixels();
                
                for (int i = 0; i < maskPixels.Length; i++)
                {
                    bool hasHair = hairPixels[i].r > 0.1f;
                    bool notRemoved = maskPixels[i].r > 0.5f;
                    
                    if (!hasHair || !notRemoved) continue;
                    
                    // Calculate UV from pixel index
                    int x = i % smallSize;
                    int y = i / smallSize;
                    Vector2 uv = new Vector2((float)x / smallSize, (float)y / smallSize);
                    
                    // Check if UV is in any target body part's region
                    bool isInTargetArea = false;
                    
                    if (targetBodyParts.Count == 0)
                    {
                        // No target parts specified = count all (backwards compatibility)
                        isInTargetArea = true;
                    }
                    else
                    {
                        foreach (var part in targetBodyParts)
                        {
                            // Only check parts that match this material/submesh
                            if (part.materialIndex == matIndex)
                            {
                                if (part.ContainsUV(uv))
                                {
                                    isInTargetArea = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (isInTargetArea)
                    {
                        totalHairPixels++;
                    }
                }
                
                Destroy(maskTex);
                Destroy(hairGrowthTex2D);
                RenderTexture.ReleaseTemporary(smallMaskRT);
                RenderTexture.ReleaseTemporary(smallHairRT);
            }
            
            return totalHairPixels;
        }
        
        /// <summary>
        /// Count white pixels for a specific body part only
        /// </summary>
        private int CountWhitePixelsForPart(HairRemovalSim.Core.BodyPartDefinition part)
        {
            int partPixels = 0;
            int matIndex = part.materialIndex;
            
            if (matIndex < 0 || matIndex >= maskTextures.Length) return 0;
            
            var mask = maskTextures[matIndex];
            if (mask == null) return 0;
            if (targetMaterials[matIndex] == null) return 0;
            if (!targetMaterials[matIndex].HasProperty("_HairGrowthMask")) return 0;
            
            Texture hairGrowthTex = targetMaterials[matIndex].GetTexture("_HairGrowthMask");
            if (hairGrowthTex == null) return 0;
            
            int smallSize = 64;
            
            RenderTexture smallMaskRT = RenderTexture.GetTemporary(smallSize, smallSize, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(mask, smallMaskRT);
            Texture2D maskTex = new Texture2D(smallSize, smallSize, TextureFormat.ARGB32, false);
            RenderTexture.active = smallMaskRT;
            maskTex.ReadPixels(new Rect(0, 0, smallSize, smallSize), 0, 0);
            maskTex.Apply();
            
            RenderTexture smallHairRT = RenderTexture.GetTemporary(smallSize, smallSize, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(hairGrowthTex, smallHairRT);
            Texture2D hairGrowthTex2D = new Texture2D(smallSize, smallSize, TextureFormat.ARGB32, false);
            RenderTexture.active = smallHairRT;
            hairGrowthTex2D.ReadPixels(new Rect(0, 0, smallSize, smallSize), 0, 0);
            hairGrowthTex2D.Apply();
            RenderTexture.active = null;
            
            Color[] maskPixels = maskTex.GetPixels();
            Color[] hairPixels = hairGrowthTex2D.GetPixels();
            
            for (int i = 0; i < maskPixels.Length; i++)
            {
                bool hasHair = hairPixels[i].r > 0.1f;
                bool notRemoved = maskPixels[i].r > 0.5f;
                
                if (!hasHair || !notRemoved) continue;
                
                int x = i % smallSize;
                int y = i / smallSize;
                Vector2 uv = new Vector2((float)x / smallSize, (float)y / smallSize);
                
                if (part.ContainsUV(uv))
                {
                    partPixels++;
                }
            }
            
            Destroy(maskTex);
            Destroy(hairGrowthTex2D);
            RenderTexture.ReleaseTemporary(smallMaskRT);
            RenderTexture.ReleaseTemporary(smallHairRT);
            
            return partPixels;
        }
        
        /// <summary>
        /// Get completion percentage for a specific body part
        /// </summary>
        public float GetPartCompletion(string partName)
        {
            if (perPartCompletion.TryGetValue(partName, out float completion))
            {
                return completion;
            }
            return 0f;
        }
        
        /// <summary>
        /// Get list of target body part names
        /// </summary>
        public List<string> GetTargetPartNames()
        {
            var names = new List<string>();
            foreach (var part in targetBodyParts)
            {
                names.Add(part.partName);
            }
            return names;
        }
        
        /// <summary>
        /// Check if a specific part is completed
        /// </summary>
        public bool IsPartCompleted(string partName)
        {
            return completedParts.Contains(partName);
        }

        private void OnDestroy()
        {
            if (maskTextures != null)
            {
                foreach (var mask in maskTextures)
                {
                    if (mask != null) mask.Release();
                }
            }
        }
    }
}
