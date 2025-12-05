using UnityEngine;
using UnityEngine.Rendering;

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
        public float completionBuffer = 5f;

        private RenderTexture[] maskTextures; // Array of masks, one per material
        private Material paintMaterial;
        private Material[] targetMaterials;
        private Renderer meshRenderer;
        private HairRemovalSim.Core.BodyPart bodyPart;
        private int initialWhitePixels = 0;
        private bool isCompleted = false;

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

        // Update decal position on the mask
        public void UpdateDecal(Vector2 uvPosition, float angle, Vector2 size, Color color, int subMeshIndex)
        {
            if (subMeshIndex < 0 || subMeshIndex >= targetMaterials.Length) return;
            if (targetMaterials[subMeshIndex] == null) return;
            
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
            
            // Calculate percentage based on initial white pixels
            // If initial is 0 (error or no mesh), avoid divide by zero
            float percentage = 0f;
            if (initialWhitePixels > 0)
            {
                // Inverted: We want % removed (black), so (Initial - Current) / Initial
                percentage = (float)(initialWhitePixels - currentWhitePixels) / initialWhitePixels * 100f;
            }
            
            // Clamp and add buffer
            percentage = Mathf.Clamp(percentage + completionBuffer, 0f, 100f);
            
            bodyPart.UpdateCompletion(percentage);

            if (percentage >= 100f)
            {
                isCompleted = true;
                HideDecal(); // Ensure decal is hidden
                Debug.Log($"[HairTreatmentController] Treatment completed for {name}!");
            }
        }

        private int CountWhitePixels()
        {
            // This is slow, should be optimized for production (e.g. Compute Shader or async GPU readback)
            // For prototype, we'll sample a low-res version or just do it less frequently
            
            // Optimization: Only check every N frames or use a smaller texture for counting
            // For now, let's just sum up all masks
            
            int totalWhite = 0;
            
            foreach (var mask in maskTextures)
            {
                if (mask == null) continue;
                
                // Create a small temporary texture to read from (downsample for speed)
                int smallSize = 64; 
                RenderTexture smallRT = RenderTexture.GetTemporary(smallSize, smallSize, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(mask, smallRT);
                
                Texture2D tex = new Texture2D(smallSize, smallSize, TextureFormat.ARGB32, false);
                RenderTexture.active = smallRT;
                tex.ReadPixels(new Rect(0, 0, smallSize, smallSize), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                
                Color[] pixels = tex.GetPixels();
                foreach (Color p in pixels)
                {
                    if (p.r > 0.5f) totalWhite++;
                }
                
                Destroy(tex);
                RenderTexture.ReleaseTemporary(smallRT);
            }

            return totalWhite;
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
