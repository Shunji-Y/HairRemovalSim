using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;
using HairRemovalSim.Core; // For TreatmentToolType

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
        public Shader maxBlendShader; // Added for burn intensity pass
        public float brushSize = 0.05f;
        [Range(0f, 10f)]
        public float completionBuffer = 2f;

        private RenderTexture[] maskTextures; // Array of masks, one per material
        private Material paintMaterial;
        private Material maxBlendMaterial; // Added for burn intensity pass
        private Material glMaterial; // For direct GL drawing
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
        
        // Cached textures for CountWhitePixels to avoid per-frame allocations
        private const int PIXEL_COUNT_SIZE = 64;
        private Texture2D cachedMaskTex;
        private Texture2D cachedHairGrowthTex;
        private Color32[] cachedMaskPixels;
        private Color32[] cachedHairPixels;
        
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
                // Use ARGB32 and enable mipmaps for blurred edge effects
                maskTextures[i] = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.ARGB32);
                maskTextures[i].useMipMap = true;
                maskTextures[i].autoGenerateMips = false; // Generate manually after painting
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
            
            // Initialize MaxBlend Material
            if (maxBlendShader == null)
            {
                maxBlendShader = Shader.Find("Hidden/MaxBlendPaint");
            }
            if (maxBlendShader != null)
            {
                maxBlendMaterial = new Material(maxBlendShader);
            }
            else
            {
                Debug.LogError("MaxBlendShader not found!");
            }
            
            // Step 4: Setup GL material for direct drawing (Min Blend)
            if (glMaterial == null)
            {
                Shader minBlendShader = Shader.Find("Hidden/MinBlendPaint");
                if (minBlendShader != null)
                {
                    glMaterial = new Material(minBlendShader);
                }
                else 
                {
                    Debug.LogError("Hidden/MinBlendPaint shader not found!");
                }
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
            
            // Initialize UV bitmap cache for each part (performance optimization)
            foreach (var part in targetBodyParts)
            {
                part.InitializeCache();
            }
            
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
        /// Set target body parts by name for the new reception-based system.
        /// Called by CustomerController with confirmedParts mapped to detailed parts.
        /// </summary>
        public void SetTargetBodyPartNames(string[] partNames, HairRemovalSim.Core.BodyPartsDatabase bodyPartsDatabase = null)
        {
            if (partNames == null || partNames.Length == 0)
            {
                Debug.LogWarning("[HairTreatmentController] No part names provided");
                return;
            }
            
            // Clear per-part tracking
            perPartInitialPixels.Clear();
            perPartCompletion.Clear();
            completedParts.Clear();
            targetBodyParts.Clear();
            
            // Lookup BodyPartDefinition for each name if database is provided
            foreach (var partName in partNames)
            {
                HairRemovalSim.Core.BodyPartDefinition partDef = null;
                if (bodyPartsDatabase != null)
                {
                    partDef = bodyPartsDatabase.GetPartByName(partName);
                }
                
                if (partDef != null)
                {
                    // Add to targetBodyParts for proper per-part tracking
                    targetBodyParts.Add(partDef);
                    
                    // Initialize UV bitmap cache (performance optimization)
                    partDef.InitializeCache();
                    
                    // Calculate initial pixels for this part
                    int partPixels = CountWhitePixelsForPart(partDef);
                    perPartInitialPixels[partName] = partPixels;
                    perPartCompletion[partName] = 0f;
                    Debug.Log($"[HairTreatmentController] Added target part: {partName} with {partPixels} initial pixels");
                }
                else
                {
                    // Fallback: just add to tracking without UV region
                    perPartInitialPixels[partName] = 0;
                    perPartCompletion[partName] = 0f;
                    Debug.LogWarning($"[HairTreatmentController] Part '{partName}' not found in database, using overall tracking");
                }
            }
            
            // Recalculate total initial pixels
            initialWhitePixels = CountWhitePixels();
            Debug.Log($"[HairTreatmentController] Set {partNames.Length} target parts by name. Total initial pixels: {initialWhitePixels}");
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
            // Guard: masks not yet created
            if (maskTextures == null || maskTextures.Length == 0) return;
            
            // Step 1: Clear all masks to WHITE (hair everywhere by default)
            // This ensures that if UV layout rendering fails or doesn't cover everything, we still have hair.
            for (int i = 0; i < maskTextures.Length; i++)
            {
                if (maskTextures[i] == null) continue;
                
                RenderTexture.active = maskTextures[i];
                // R=1 (Hair present), G=0 (No burn), B=0, A=0
                GL.Clear(false, true, new Color(1f, 0f, 0f, 0f));
                RenderTexture.active = null;
                
                // Initialize mipmaps to white to preventing shader from seeing "removed hair" everywhere
                maskTextures[i].GenerateMips();
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
        public void UpdateDecal(Vector2 uvPosition, float angle, Vector2 size, Color color, int subMeshIndex, int decalShape = 0)
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
            mat.SetFloat("_DecalShape", decalShape); // 0 = Rectangle, 1 = Circle
        }

        public void HideDecal()
        {
            if (targetMaterials == null) return;
            
            foreach (var mat in targetMaterials)
            {
                if (mat != null) mat.SetFloat("_DecalEnabled", 0.0f);
            }
        }

        // Apply treatment (remove hair) at UV position
        // Apply treatment (remove hair) at UV position
        // toolType: Shaver = shortens to 0.2, Laser = removes to 0.0
        public void ApplyTreatment(Vector2 uvPosition, Vector2 size, float angle, int subMeshIndex, int decalShape = 0, TreatmentToolType toolType = TreatmentToolType.Shaver, float burnIntensity = 0.0f)
        {
            if (paintMaterial == null) return;
            if (subMeshIndex < 0 || subMeshIndex >= maskTextures.Length) return;

            // Track which submesh we're treating (set on first paint)
            if (!hasActiveSubmesh)
            {
                activeSubmeshIndex = subMeshIndex;
                hasActiveSubmesh = true;
                initialWhitePixels = CountWhitePixels();
            }

            // Setup paint material based on shape - use GLOBAL shader properties (used by both shaders)
            if (decalShape == 0)
            {
                Shader.SetGlobalFloat("_BrushMode", 1.0f);
                Shader.SetGlobalVector("_BrushRect", new Vector4(uvPosition.x, uvPosition.y, size.x, size.y));
                Shader.SetGlobalFloat("_BrushAngle", angle);
            }
            else
            {
                Shader.SetGlobalFloat("_BrushMode", 0.0f);
                Shader.SetGlobalVector("_BrushPos", new Vector4(uvPosition.x, uvPosition.y, 0, 0));
                Shader.SetGlobalFloat("_BrushSize", size.x * 0.5f);
            }
            
            // For Shaver: Check if area is already laser-removed
            if (toolType == TreatmentToolType.Shaver)
            {
                float avgMask = GetAverageMaskValue(uvPosition, size, subMeshIndex);
                if (avgMask < 0.1f) return;
            }
            
            RenderTexture.active = maskTextures[subMeshIndex];
            GL.PushMatrix();
            GL.LoadOrtho();
            
            // --- Pass 1: Hair Removal (Min Blend) ---
            // R: Min(current.r, brush.r) -> Remove hair or create stubble
            // G: Min(current.g, brush.g) -> Must be 1.0 to preserve existing burns (Min(x, 1) = x)
            
            Color brushColor;
            if (toolType == TreatmentToolType.Laser)
            {
                brushColor = new Color(0.0f, 1.0f, 1.0f, 1.0f); // R=0 (Remove hair), G=1 (Preserve burn)
            }
            else
            {
                brushColor = new Color(0.3f, 1.0f, 1.0f, 1.0f); // R=0.3 (Stubble), G=1 (Preserve burn)
            }
            
            if (glMaterial == null)
            {
                glMaterial = new Material(Shader.Find("Hidden/MinBlendPaint"));
                glMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            
            glMaterial.SetPass(0);
            
            // Draw Quad
            GL.Begin(GL.QUADS);
            GL.Color(brushColor);
            DrawBrushQuad(uvPosition, size, angle, decalShape);
            GL.End();
            
            // --- Pass 2: Burn Intensity (Max Blend) ---
            // Only if laser and intensity > 0
            if (toolType == TreatmentToolType.Laser && burnIntensity > 0 && maxBlendMaterial != null)
            {
                maxBlendMaterial.SetPass(0);
                
                // R: Max(current.r, 0) -> Preserve removed hair status (Max(0, 0) = 0)
                // G: Max(current.g, intensity) -> Register stronger burn if applicable
                // Note: If hair is present (R=1), Max(1, 0) = 1. So this pass is safe for hair too.
                
                GL.Begin(GL.QUADS);
                GL.Color(new Color(0f, burnIntensity, 0f, 0f));
                DrawBrushQuad(uvPosition, size, angle, decalShape);
                GL.End();
            }
            
            GL.PopMatrix();
            RenderTexture.active = null;
            
            maskTextures[subMeshIndex].GenerateMips();
            
            UpdateCompletion();
        }

        private void DrawBrushQuad(Vector2 uvPosition, Vector2 size, float angle, int decalShape)
        {
            if (decalShape == 0)
            {
                float halfW = size.x * 0.5f;
                float halfH = size.y * 0.5f;
                
                float cos = Mathf.Cos(-angle);
                float sin = Mathf.Sin(-angle);
                
                Vector2[] corners = new Vector2[4];
                corners[0] = new Vector2(-halfW * cos - -halfH * sin, -halfW * sin + -halfH * cos);
                corners[1] = new Vector2(halfW * cos - -halfH * sin, halfW * sin + -halfH * cos);
                corners[2] = new Vector2(halfW * cos - halfH * sin, halfW * sin + halfH * cos);
                corners[3] = new Vector2(-halfW * cos - halfH * sin, -halfW * sin + halfH * cos);
                
                for (int i = 0; i < 4; i++)
                {
                    GL.Vertex3(uvPosition.x + corners[i].x, uvPosition.y + corners[i].y, 0);
                }
            }
            else
            {
                float r = size.x * 0.5f;
                // Draw as a simple quad, shader handles circle clipping if needed, 
                // but for simple GL drawing without UV check shader, we assume shader isn't clipping in vertex mode.
                // Actually MinBlendPaint uses vertex/fragment shader. The fragment shader uses vertex color.
                // But it assumes _BrushRect etc if we were using PaintShader logic.
                // Wait, MinBlendPaint as written (viewed earlier) just outputs vertex color.
                // It does NOT do circular clipping based on _BrushSize.
                // So drawing a Quad here will result in a Square paint.
                // If we need circle paint, we need to pass UVs to the shader and use the global props.
                // For now, let's just stick to the Quad logic used before.
                
                GL.Vertex3(uvPosition.x - r, uvPosition.y - r, 0);
                GL.Vertex3(uvPosition.x + r, uvPosition.y - r, 0);
                GL.Vertex3(uvPosition.x + r, uvPosition.y + r, 0);
                GL.Vertex3(uvPosition.x - r, uvPosition.y + r, 0);
            }
        }
        
        /// <summary>
        /// Calculate the average mask value in a given UV rectangle.
        /// Used to determine pain multiplier based on hair length.
        /// Returns: 1.0 = all long hair, 0.3 = all stubble, 0.0 = no hair
        /// </summary>
        public float GetAverageMaskValue(Vector2 uvCenter, Vector2 uvSize, int subMeshIndex = 0)
        {
            if (maskTextures == null || subMeshIndex >= maskTextures.Length || maskTextures[subMeshIndex] == null)
                return 1.0f; // Default to full hair if no mask
            
            RenderTexture mask = maskTextures[subMeshIndex];
            
            // Sample a grid of points in the UV rectangle
            int sampleCount = 9; // 3x3 grid
            float totalValue = 0f;
            int validSamples = 0;
            
            RenderTexture.active = mask;
            Texture2D readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    // Sample closer to center to avoid edges (especially when rotated)
                    // 0.25f offset means we sample a grid covering 50% of the size
                    float offsetX = (x - 1) * uvSize.x * 0.25f;
                    float offsetY = (y - 1) * uvSize.y * 0.25f;
                    float sampleU = uvCenter.x + offsetX;
                    float sampleV = uvCenter.y + offsetY;
                    
                    // Clamp to texture bounds
                    int px = Mathf.Clamp((int)(sampleU * mask.width), 0, mask.width - 1);
                    int py = Mathf.Clamp((int)(sampleV * mask.height), 0, mask.height - 1);
                    
                    readTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
                    readTex.Apply();
                    float value = readTex.GetPixel(0, 0).r;
                    
                    totalValue += value;
                    validSamples++;
                }
            }
            
            DestroyImmediate(readTex);
            RenderTexture.active = null;
            
            return validSamples > 0 ? totalValue / validSamples : 1.0f;
        }
        
        /// <summary>
        /// Calculate pain multiplier based on average hair length.
        /// longHairMultiplier: multiplier when all hair is long (mask ~1.0)
        /// shortHairMultiplier: multiplier when all hair is short/stubble (mask ~0.3)
        /// Returns 0 if no hair (mask ~0.0)
        /// </summary>
        public float CalculatePainMultiplier(float averageMaskValue, float longHairMultiplier = 4.5f, float shortHairMultiplier = 0.6f)
        {
            // No hair = no pain
            if (averageMaskValue < 0.05f)
                return 0f;
            
            // Stubble threshold (adjusted to 0.65 to account for sampling averages)
            float stubbleThreshold = 0.65f;
            
            if (averageMaskValue >= stubbleThreshold)
            {
                // Interpolate between short hair and long hair multipliers
                // mask 0.65 -> shortHairMultiplier, mask 1.0 -> longHairMultiplier
                float t = (averageMaskValue - stubbleThreshold) / (1.0f - stubbleThreshold);
                return Mathf.Lerp(shortHairMultiplier, longHairMultiplier, t);
            }
            else
            {
                // Very short hair (0.05 to 0.65) - interpolate from 0 to shortHairMultiplier
                float t = (averageMaskValue - 0.05f) / (stubbleThreshold - 0.05f);
                return Mathf.Lerp(0f, shortHairMultiplier, t);
            }
        }

        private void UpdateCompletion()
        {
            if (isCompleted || bodyPart == null) return;

            int currentWhitePixels = CountWhitePixels();
            
            // Calculate overall percentage based on initial white pixels
            float percentage = 0f;
            //if (initialWhitePixels > 0)
            //{
            //    int removedPixels = initialWhitePixels - currentWhitePixels;
            //    percentage = (float)removedPixels / initialWhitePixels * 100f;
                
            //    Debug.Log($"[HairTreatmentController] {name}: Removed {removedPixels}/{initialWhitePixels} pixels = {percentage:F1}%");
            //}
            
            // Update per-part completion
            // First, try old BodyPartDefinition system
            if (targetBodyParts.Count > 0)
            {
                foreach (var part in targetBodyParts)
                {
                    if (completedParts.Contains(part.partName)) continue; // Already completed
                    
                    int initialPartPixels = perPartInitialPixels.ContainsKey(part.partName) ? perPartInitialPixels[part.partName] : 0;
                    if (initialPartPixels <= 0) continue;
                    
                    int currentPartPixels = CountWhitePixelsForPart(part);
                    int removedPartPixels = initialPartPixels - currentPartPixels;
                    float partPercentage = Mathf.Clamp((float)removedPartPixels / initialPartPixels * 100f + completionBuffer, 0f, 100f);
                    
                    var threshold = 99f;
                    if(part.partName.Contains("Armpit"))
                    {
                        Debug.Log(partPercentage);
                        threshold = 91.4f;
                    }
                    // Round up to 100% when reaching 99.5% or higher
                    if (partPercentage >= threshold)
                    {
                        partPercentage = 100f;
                    }
                    
                    perPartCompletion[part.partName] = partPercentage;
                    
                    // Check if this part is now completed
                    if (partPercentage >= 100f && !completedParts.Contains(part.partName))
                    {
                        completedParts.Add(part.partName);
                        OnPartCompleted?.Invoke(part.partName);
                        Debug.Log($"[HairTreatmentController] Part {part.partName} completed!");
                    }
                }
            }
            else if (perPartCompletion.Count > 0)
            {
                // New reception-based system: use overall percentage for all parts
                // Since we don't have UV regions, use overall progress for all
                percentage = Mathf.Clamp(percentage + completionBuffer, 0f, 100f);
                
                foreach (var partName in new List<string>(perPartCompletion.Keys))
                {
                    if (completedParts.Contains(partName)) continue;
                    
                    perPartCompletion[partName] = percentage;
                    
                    if (percentage >= 100f && !completedParts.Contains(partName))
                    {
                        completedParts.Add(partName);
                        OnPartCompleted?.Invoke(partName);
                        Debug.Log($"[HairTreatmentController] Part {partName} completed!");
                    }
                }
            }
            
            // Clamp and add buffer for overall progress
            percentage = Mathf.Clamp(percentage + completionBuffer, 0f, 100f);
            
            bodyPart.UpdateCompletion(percentage);
            
            // Note: Completion is now tracked per-part via completedParts HashSet
            // TreatmentSession.AreAllPartsComplete() checks if all parts are done
        }

        /// <summary>
        /// Count white pixels in MaskMap, but only where HairGrowthMask indicates hair exists.
        /// Counts ALL submeshes with hair (not filtered by activeSubmesh) to support multi-part treatments.
        /// </summary>
        private int CountWhitePixels()
        {
            int totalHairPixels = 0;
            
            // Ensure cached textures exist
            if (cachedMaskTex == null)
            {
                cachedMaskTex = new Texture2D(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, TextureFormat.ARGB32, false);
            }
            if (cachedHairGrowthTex == null)
            {
                cachedHairGrowthTex = new Texture2D(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, TextureFormat.ARGB32, false);
            }
            
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
                
                // Use temporary RenderTextures (pooled by Unity)
                RenderTexture smallMaskRT = RenderTexture.GetTemporary(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(mask, smallMaskRT);
                RenderTexture.active = smallMaskRT;
                cachedMaskTex.ReadPixels(new Rect(0, 0, PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE), 0, 0);
                cachedMaskTex.Apply();
                
                RenderTexture smallHairRT = RenderTexture.GetTemporary(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(hairGrowthTex, smallHairRT);
                RenderTexture.active = smallHairRT;
                cachedHairGrowthTex.ReadPixels(new Rect(0, 0, PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE), 0, 0);
                cachedHairGrowthTex.Apply();
                RenderTexture.active = null;
                
                // Get pixels into cached arrays - GetPixels32 is faster than GetPixels
                cachedMaskPixels = cachedMaskTex.GetPixels32();
                cachedHairPixels = cachedHairGrowthTex.GetPixels32();
                
                int pixelCount = cachedMaskPixels.Length;
                int partCount = targetBodyParts.Count;
                
                for (int i = 0; i < pixelCount; i++)
                {
                    // Using byte comparison (0-255) instead of float (0-1)
                    // 0.1f * 255 ≈ 25, 0.5f * 255 ≈ 127
                    byte hairR = cachedHairPixels[i].r;
                    byte maskR = cachedMaskPixels[i].r;
                    
                    if (hairR <= 25 || maskR <= 127) continue;
                    
                    // Calculate pixel coordinates from index
                    int x = i % PIXEL_COUNT_SIZE;
                    int y = i / PIXEL_COUNT_SIZE;
                    
                    // Check if UV is in any target body part's region
                    bool isInTargetArea = false;
                    
                    if (partCount == 0)
                    {
                        // No target parts specified = count all (backwards compatibility)
                        isInTargetArea = true;
                    }
                    else
                    {
                        // Use for loop instead of foreach to avoid enumerator allocation
                        for (int p = 0; p < partCount; p++)
                        {
                            var part = targetBodyParts[p];
                            // Only check parts that match this material/submesh
                            if (part.materialIndex == matIndex)
                            {
                                // Use cached bitmap lookup for performance
                                if (part.ContainsPixel(x, y, PIXEL_COUNT_SIZE))
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
            
            // Ensure cached textures exist
            if (cachedMaskTex == null)
            {
                cachedMaskTex = new Texture2D(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, TextureFormat.ARGB32, false);
            }
            if (cachedHairGrowthTex == null)
            {
                cachedHairGrowthTex = new Texture2D(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, TextureFormat.ARGB32, false);
            }
            
            RenderTexture smallMaskRT = RenderTexture.GetTemporary(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(mask, smallMaskRT);
            RenderTexture.active = smallMaskRT;
            cachedMaskTex.ReadPixels(new Rect(0, 0, PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE), 0, 0);
            cachedMaskTex.Apply();
            
            RenderTexture smallHairRT = RenderTexture.GetTemporary(PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(hairGrowthTex, smallHairRT);
            RenderTexture.active = smallHairRT;
            cachedHairGrowthTex.ReadPixels(new Rect(0, 0, PIXEL_COUNT_SIZE, PIXEL_COUNT_SIZE), 0, 0);
            cachedHairGrowthTex.Apply();
            RenderTexture.active = null;
            
            cachedMaskPixels = cachedMaskTex.GetPixels32();
            cachedHairPixels = cachedHairGrowthTex.GetPixels32();
            
            int pixelCount = cachedMaskPixels.Length;
            
            for (int i = 0; i < pixelCount; i++)
            {
                // Using byte comparison (0-255) instead of float (0-1)
                byte hairR = cachedHairPixels[i].r;
                byte maskR = cachedMaskPixels[i].r;
                
                if (hairR <= 25 || maskR <= 127) continue;
                
                int x = i % PIXEL_COUNT_SIZE;
                int y = i / PIXEL_COUNT_SIZE;
                
                // Use cached bitmap lookup for performance
                if (part.ContainsPixel(x, y, PIXEL_COUNT_SIZE))
                {
                    partPixels++;
                }
            }
            
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
        
        /// <summary>
        /// Force mark a part as completed (for 98% threshold edge case)
        /// </summary>
        public void ForcePartComplete(string partName)
        {
            if (!completedParts.Contains(partName))
            {
                completedParts.Add(partName);
                Debug.Log($"[HairTreatmentController] Force completed: {partName}");
            }
        }
        
        /// <summary>
        /// Force mark all target parts as completed (for staff treatment automation)
        /// </summary>
        public void ForceCompleteAllParts()
        {
            foreach (var part in targetBodyParts)
            {
                if (!completedParts.Contains(part.partName))
                {
                    completedParts.Add(part.partName);
                    perPartCompletion[part.partName] = 100f;
                    OnPartCompleted?.Invoke(part.partName);
                }
            }
            
            // Also mark any parts tracked only by name
            foreach (var partName in new List<string>(perPartCompletion.Keys))
            {
                if (!completedParts.Contains(partName))
                {
                    completedParts.Add(partName);
                    perPartCompletion[partName] = 100f;
                    OnPartCompleted?.Invoke(partName);
                }
            }
            
            Debug.Log($"[HairTreatmentController] Force completed ALL {completedParts.Count} parts");
        }
        
        /// <summary>
        /// Get the completion percentage for each body part
        /// </summary>
        public Dictionary<string, float> GetPerPartCompletion()
        {
            return new Dictionary<string, float>(perPartCompletion);
        }
        
        /// <summary>
        /// Get list of all completed parts
        /// </summary>
        public HashSet<string> GetCompletedParts()
        {
            return new HashSet<string>(completedParts);
        }
        
        /// <summary>
        /// Get total number of target body parts
        /// </summary>
        public int GetTargetPartCount()
        {
            return targetBodyParts.Count;
        }
        
        /// <summary>
        /// Get number of completed body parts
        /// </summary>
        public int GetCompletedPartCount()
        {
            return completedParts.Count;
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
            
            // Cleanup cached textures
            if (cachedMaskTex != null)
            {
                Destroy(cachedMaskTex);
                cachedMaskTex = null;
            }
            if (cachedHairGrowthTex != null)
            {
                Destroy(cachedHairGrowthTex);
                cachedHairGrowthTex = null;
            }
        }
    }
}
