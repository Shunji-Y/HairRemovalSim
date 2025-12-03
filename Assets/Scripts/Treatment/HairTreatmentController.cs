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

        private RenderTexture maskTexture;
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
            
            maskTexture = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.R8);
            maskTexture.wrapMode = TextureWrapMode.Clamp; // Prevent tiling/ghost hair at edges
            maskTexture.Create();
            ClearMask();
            
            foreach (var mat in targetMaterials)
            {
                if (mat != null)
                {
                    mat.SetTexture("_MaskMap", maskTexture);
                }
            }

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
            // Step 1: Clear to black (no hair anywhere)
            RenderTexture.active = maskTexture;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = null;
            
            // Step 2: Paint white only where mesh exists (using GPU rendering)
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
                    // This maps UVs to screen space and outputs white
                    CommandBuffer cmd = new CommandBuffer();
                    cmd.SetRenderTarget(maskTexture);
                    
                    // Draw all submeshes
                    for (int i = 0; i < meshToRender.subMeshCount; i++)
                    {
                        cmd.DrawMesh(meshToRender, Matrix4x4.identity, uvLayoutMat, i);
                    }
                    
                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    Destroy(uvLayoutMat);
                }
                else
                {
                    Debug.LogError("Hidden/UVLayout shader not found! Falling back to full white.");
                    RenderTexture.active = maskTexture;
                    GL.Clear(false, true, Color.white);
                    RenderTexture.active = null;
                }
            }
            else
            {
                if (useUVMasking)
                {
                    Debug.LogWarning("[HairTreatmentController] No MeshFilter or SkinnedMeshRenderer found. Cannot generate UV mask.");
                }
                
                // Fallback to full white if no mesh found or masking disabled
                RenderTexture.active = maskTexture;
                GL.Clear(false, true, Color.white);
                RenderTexture.active = null;
            }
            
            isCompleted = false;
        }

        public void RemoveHairAt(Vector2 uv)
        {
            if (maskTexture == null || paintMaterial == null) return;

            paintMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
            paintMaterial.SetFloat("_BrushSize", brushSize);
            paintMaterial.SetColor("_BrushColor", Color.black);
            
            RenderTexture temp = RenderTexture.GetTemporary(maskTexture.width, maskTexture.height, 0, RenderTextureFormat.R8);
            Graphics.Blit(maskTexture, temp);
            Graphics.Blit(temp, maskTexture, paintMaterial);
            RenderTexture.ReleaseTemporary(temp);
            
            // Note: We do NOT call UpdateCompletion here to avoid lag during drag.
            // It should be called by the tool on UseUp or periodically.
       }

        public void RemoveHairInRect(Vector2 uvCenter, Vector2 uvSize, float angleRadians = 0f)
        {
            if (maskTexture == null || paintMaterial == null) return;

            paintMaterial.SetVector("_BrushRect", new Vector4(uvCenter.x, uvCenter.y, uvSize.x, uvSize.y));
            paintMaterial.SetFloat("_BrushAngle", angleRadians);
            paintMaterial.SetFloat("_BrushMode", 1);
            paintMaterial.SetColor("_BrushColor", Color.black);
            
            RenderTexture temp = RenderTexture.GetTemporary(maskTexture.width, maskTexture.height, 0, RenderTextureFormat.R8);
            Graphics.Blit(maskTexture, temp);
            Graphics.Blit(temp, maskTexture, paintMaterial);
            RenderTexture.ReleaseTemporary(temp);
        }
        
        private int CountWhitePixels()
        {
            if (maskTexture == null) return 0;
            
            RenderTexture.active = maskTexture;
            Texture2D tempTexture = new Texture2D(maskTexture.width, maskTexture.height, TextureFormat.R8, false);
            tempTexture.ReadPixels(new Rect(0, 0, maskTexture.width, maskTexture.height), 0, 0);
            tempTexture.Apply();
            RenderTexture.active = null;
            
            Color[] pixels = tempTexture.GetPixels();
            int whitePixels = 0;
            
            foreach (Color pixel in pixels)
            {
                if (pixel.r > 0.5f) whitePixels++;
            }
            
            Destroy(tempTexture);
            return whitePixels;
        }
        
        [ContextMenu("Set Current Pixels as Target (100%)")]
        public void SetCurrentAsTarget()
        {
            if (bodyPart != null)
            {
                bodyPart.targetWhitePixelCount = CountWhitePixels();
                Debug.Log($"[HairTreatmentController] Set targetWhitePixelCount to {bodyPart.targetWhitePixelCount} for {bodyPart.partName}");
                UpdateCompletion();
            }
        }

        public void UpdateCompletion()
        {
            if (bodyPart == null || initialWhitePixels == 0) return;
            
            int currentWhite = CountWhitePixels();
            
            // Calculate percentage based on range [Initial, Target]
            // Progress = (Initial - Current) / (Initial - Target)
            
            float range = initialWhitePixels - bodyPart.targetWhitePixelCount;
            if (range <= 0) range = 1; // Avoid division by zero
            
            float removed = initialWhitePixels - currentWhite;
            float rawPercentage = (removed / range) * 100f;
            
            float threshold = 100f - completionBuffer;
            float finalPercentage = rawPercentage >= threshold ? 100f : Mathf.Clamp(rawPercentage, 0f, 100f);
            
            Debug.Log($"[Completion] {bodyPart.partName}: White {currentWhite} (Target: {bodyPart.targetWhitePixelCount}) -> {rawPercentage:F1}%");
            
            bodyPart.SetCompletion(finalPercentage);

            // If 100% reached, remove all remaining hair visually
            if (finalPercentage >= 100f && !isCompleted)
            {
                RemoveAllHair();
                isCompleted = true;
                Debug.Log($"[Completion] {bodyPart.partName}: Reached 100%, removing all remaining hair.");
            }
            else if (finalPercentage < 100f)
            {
                isCompleted = false;
            }
        }

        public void RemoveAllHair()
        {
            if (maskTexture == null) return;
            RenderTexture.active = maskTexture;
            GL.Clear(false, true, Color.black); // Black removes hair
            RenderTexture.active = null;
        }

        private void OnDestroy()
        {
            if (maskTexture != null)
            {
                maskTexture.Release();
            }
        }
    }
}
