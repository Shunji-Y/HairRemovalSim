using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Treatment
{
    public class HairRenderer : MonoBehaviour
    {
        [Header("Settings")]
        public Mesh hairMesh; // Assign a small cylinder/capsule in Inspector
        public Material hairMaterial;
        public int hairCount = 1000;
        public float surfaceRadius = 0.5f; // Radius of the leg (Capsule)
        public float surfaceHeight = 2.0f;

        private List<Matrix4x4> hairMatrices = new List<Matrix4x4>();
        private List<bool> hairActiveState = new List<bool>(); // True = Hair exists, False = Removed

        public void ResetHairs()
        {
            GenerateHair();
        }

        private void Start()
        {
            GenerateHair();
        }

        private void Update()
        {
            RenderHair();
        }

        private void GenerateHair()
        {
            hairMatrices.Clear();
            hairActiveState.Clear();

            for (int i = 0; i < hairCount; i++)
            {
                // Random position on a cylinder surface
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float height = Random.Range(-surfaceHeight / 2f, surfaceHeight / 2f);
                
                Vector3 position = new Vector3(Mathf.Cos(angle) * surfaceRadius, height, Mathf.Sin(angle) * surfaceRadius);
                Quaternion rotation = Quaternion.LookRotation(position.normalized); // Point outward
                
                // Add some randomness to rotation
                rotation *= Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(-15f, 15f), 0f);

                Vector3 scale = Vector3.one * Random.Range(0.05f, 0.1f); // Thin hair

                Matrix4x4 matrix = Matrix4x4.TRS(transform.position + position, rotation, scale);
                
                hairMatrices.Add(matrix);
                hairActiveState.Add(true);
            }
        }

        private void RenderHair()
        {
            if (hairMesh == null || hairMaterial == null) return;

            // Batch matrices (Graphics.DrawMeshInstanced supports max 1023 per batch)
            List<Matrix4x4> batch = new List<Matrix4x4>();
            
            for (int i = 0; i < hairMatrices.Count; i++)
            {
                if (hairActiveState[i])
                {
                    batch.Add(hairMatrices[i]);
                    if (batch.Count >= 1023)
                    {
                        Graphics.DrawMeshInstanced(hairMesh, 0, hairMaterial, batch);
                        batch.Clear();
                    }
                }
            }
            
            if (batch.Count > 0)
            {
                Graphics.DrawMeshInstanced(hairMesh, 0, hairMaterial, batch);
            }
        }

        // Called by tools to remove hair in an area
        public int RemoveHairInArea(Vector3 center, float radius)
        {
            int removedCount = 0;
            for (int i = 0; i < hairMatrices.Count; i++)
            {
                if (!hairActiveState[i]) continue;

                Vector3 hairPos = hairMatrices[i].GetColumn(3); // Extract position
                if (Vector3.Distance(center, hairPos) <= radius)
                {
                    hairActiveState[i] = false;
                    removedCount++;
                }
            }
            return removedCount;
        }
        
        public float GetCleanlinessPercentage()
        {
            int activeCount = 0;
            foreach (bool active in hairActiveState)
            {
                if (active) activeCount++;
            }
            return 1.0f - ((float)activeCount / hairCount);
        }
    }
}
