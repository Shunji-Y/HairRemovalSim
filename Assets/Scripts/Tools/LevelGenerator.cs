using UnityEngine;
using HairRemovalSim.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HairRemovalSim.Tools
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Settings")]
        public int roomWidth = 15;
        public int roomDepth = 20;
        public int roomHeight = 4;
        public int bedCount = 4;

        [Header("Materials (Optional)")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material bedMaterial;

        [ContextMenu("Generate Level")]
        public void GenerateLevel()
        {
            // Clear existing level
            var existingLevel = GameObject.Find("LevelRoot");
            if (existingLevel != null)
            {
                DestroyImmediate(existingLevel);
            }

            GameObject root = new GameObject("LevelRoot");

            // Floor
            CreatePrimitive(PrimitiveType.Cube, "Floor", new Vector3(roomWidth, 0.1f, roomDepth), new Vector3(0, -0.05f, 0), root, floorMaterial);

            // Ceiling
            CreatePrimitive(PrimitiveType.Cube, "Ceiling", new Vector3(roomWidth, 0.1f, roomDepth), new Vector3(0, roomHeight, 0), root, wallMaterial);

            // Walls
            CreateWall(new Vector3(roomWidth, roomHeight, 0.1f), new Vector3(0, roomHeight / 2f, roomDepth / 2f), "Wall_Back", root);
            CreateWall(new Vector3(roomWidth, roomHeight, 0.1f), new Vector3(0, roomHeight / 2f, -roomDepth / 2f), "Wall_Front", root);
            CreateWall(new Vector3(0.1f, roomHeight, roomDepth), new Vector3(roomWidth / 2f, roomHeight / 2f, 0), "Wall_Right", root);
            CreateWall(new Vector3(0.1f, roomHeight, roomDepth), new Vector3(-roomWidth / 2f, roomHeight / 2f, 0), "Wall_Left", root);

            // Reception
            GameObject reception = CreatePrimitive(PrimitiveType.Cube, "ReceptionDesk", new Vector3(3, 1, 1), new Vector3(0, 0.5f, -roomDepth / 2f + 3), root);
            reception.tag = GameConstants.Tags.Interactable;
            reception.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);

            // Beds
            float startZ = -roomDepth / 2f + 6;
            float spacing = 3.0f;
            
            for (int i = 0; i < bedCount; i++)
            {
                float xPos = (i % 2 == 0) ? -roomWidth / 4f : roomWidth / 4f;
                float zPos = startZ + (i / 2) * spacing;

                GameObject bed = CreatePrimitive(PrimitiveType.Cube, $"Bed_{i}", new Vector3(2, 0.8f, 1), new Vector3(xPos, 0.4f, zPos), root, bedMaterial);
                bed.tag = GameConstants.Tags.Bed;
                bed.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
                
                // Add a simple pillow to indicate head direction
                CreatePrimitive(PrimitiveType.Cube, "Pillow", new Vector3(1.8f, 0.2f, 0.4f), new Vector3(0, 0.5f, 0.3f), bed);
            }

            // Warehouse
            GameObject warehouse = CreatePrimitive(PrimitiveType.Cube, "WarehouseShelf", new Vector3(4, 2, 1), new Vector3(-roomWidth / 2f + 2.5f, 1, roomDepth / 2f - 1), root);
            warehouse.tag = GameConstants.Tags.Interactable;
            warehouse.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);

            Debug.Log("Level Generated!");
        }

        private void CreateWall(Vector3 size, Vector3 position, string name, GameObject parent)
        {
            CreatePrimitive(PrimitiveType.Cube, name, size, position, parent, wallMaterial);
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 size, Vector3 localPosition, GameObject parent, Material mat = null)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.localScale = size;
            obj.transform.localPosition = localPosition;
            
            if (mat != null)
            {
                obj.GetComponent<Renderer>().material = mat;
            }

            return obj;
        }
    }
}
