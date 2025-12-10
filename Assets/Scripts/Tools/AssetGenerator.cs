using UnityEngine;
using HairRemovalSim.Core;
using HairRemovalSim.Environment;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HairRemovalSim.Tools
{
    public class AssetGenerator : MonoBehaviour
    {
        [ContextMenu("Create Door Placeholder")]
        public void CreateDoorPlaceholder()
        {
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door_Placeholder";
            door.transform.localScale = new Vector3(2, 3, 0.2f);
            door.tag = GameConstants.Tags.Interactable;
            door.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            var renderer = door.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.6f, 0.4f, 0.2f); // Brown
            }


            // Add DoorController
            if (door.GetComponent<DoorController>() == null)
            {
                door.AddComponent<DoorController>();
            }

            Debug.Log("Door Placeholder Created!");
        }

        [ContextMenu("Create Cash Register")]
        public void CreateCashRegister()
        {
            GameObject cashRegister = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cashRegister.name = "CashRegister";
            cashRegister.transform.localScale = new Vector3(0.5f, 0.8f, 0.4f);
            cashRegister.tag = GameConstants.Tags.Interactable;
            cashRegister.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            var renderer = cashRegister.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.7f, 0.7f, 0.7f); // Gray
            }
            
            // Add BoxCollider as trigger for customer detection
            var boxCollider = cashRegister.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(2f, 2f, 2f); // Larger trigger area
            }
            
            cashRegister.AddComponent<UI.CashRegister>();
            cashRegister.AddComponent<Effects.OutlineHighlighter>();
            
            Debug.Log("Cash Register created with CashRegister component!");
        }

        [ContextMenu("Create Reception Desk")]
        public void CreateReceptionDesk()
        {
            GameObject reception = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reception.name = "ReceptionDesk";
            reception.transform.localScale = new Vector3(2f, 1f, 1f);
            reception.tag = GameConstants.Tags.Interactable;
            reception.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            var renderer = reception.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.8f, 0.6f, 0.4f); // Wood color
            }
            
            // Add BoxCollider as trigger for customer detection
            var boxCollider = reception.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(2.5f, 2f, 2.5f); // Larger trigger area
            }
            
            var receptionManager = reception.AddComponent<UI.ReceptionManager>();
            reception.AddComponent<Effects.OutlineHighlighter>();
            
            // Find and assign beds
            var beds = FindObjectsOfType<BedController>();
            if (beds.Length > 0)
            {
                receptionManager.beds = beds;
                Debug.Log($"Reception Desk created with {beds.Length} beds assigned!");
            }
            else
            {
                Debug.LogWarning("Reception Desk created but no beds found. Assign beds manually in Inspector.");
            }
        }

        [ContextMenu("Create Bed Placeholder")]
        public void CreateBedPlaceholder()
        {
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Bed";
            bed.transform.localScale = new Vector3(1.5f, 0.6f, 2.5f);
            // Position slightly above ground
            bed.transform.position = new Vector3(0, 0.3f, 0); 
            
            var renderer = bed.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = Color.white;
            }
            
            // Add BedController
            bed.AddComponent<BedController>();
            
            // Set Tag
            bed.tag = GameConstants.Tags.Bed;
            bed.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            Debug.Log("Bed Placeholder Created with BedController!");
        }

        [ContextMenu("Create Hair Prefab")]
        public void CreateHairPrefab()
        {
            GameObject hairPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hairPrefab.name = "HairPrefab";
            hairPrefab.transform.localScale = new Vector3(0.02f, 0.5f, 0.02f);
            
            var renderer = hairPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.2f, 0.1f, 0.05f); // Dark brown
            }
            
            Debug.Log("Hair Prefab Created! Save this as a prefab and assign to BodyPart components.");
        }

        [ContextMenu("Create Customer Placeholder")]
        public void CreateCustomerPlaceholder()
        {
            GameObject customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            customer.name = "Customer_Placeholder";
            customer.tag = GameConstants.Tags.Customer;
            customer.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            // Add a "Head" sphere
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(customer.transform);
            head.transform.localPosition = new Vector3(0, 1, 0);
            head.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            
            // Create Limbs (Capsules)
            // Legs
            CreateBodyPartWithHair(customer, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.25f, -1.0f, 0), new Vector3(0.25f, 1.0f, 0.25f), 50);
            CreateBodyPartWithHair(customer, "RightLeg", PrimitiveType.Capsule, new Vector3(0.25f, -1.0f, 0), new Vector3(0.25f, 1.0f, 0.25f), 50);
            
            // Arms
            CreateBodyPartWithHair(customer, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.7f, 0.2f, 0), new Vector3(0.2f, 0.8f, 0.2f), 30);
            CreateBodyPartWithHair(customer, "RightArm", PrimitiveType.Capsule, new Vector3(0.7f, 0.2f, 0), new Vector3(0.2f, 0.8f, 0.2f), 30);

            Debug.Log("Customer Placeholder Created with Capsule BodyParts and HairShader!");
        }

        private GameObject CreateBodyPartWithHair(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, int hairCount)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent.transform);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            
            // Apply HairShader
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader hairShader = Shader.Find("Custom/HairShader");
                if (hairShader != null)
                {
                    renderer.sharedMaterial = new Material(hairShader);
                    renderer.sharedMaterial.SetColor("_BodyColor", new Color(1f, 0.8f, 0.6f)); // Skin tone
                }
                else
                {
                    Debug.LogError("Custom/HairShader not found!");
                    renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    renderer.sharedMaterial.color = new Color(1f, 0.8f, 0.6f);
                }
            }
            
            // Add BodyPart component
            var bodyPart = part.AddComponent<Core.BodyPart>();
            bodyPart.partName = name;
            bodyPart.hairCount = hairCount;
            
            // Set layer to Interactable
            part.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);

            // Replace primitive collider with MeshCollider for UV Raycast support
            Collider oldCollider = part.GetComponent<Collider>();
            if (oldCollider != null)
            {
                DestroyImmediate(oldCollider);
            }
            
            MeshFilter meshFilter = part.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                MeshCollider meshCollider = part.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                // Note: MeshCollider does not need to be Convex for raycasts, 
                // but if we want physics interactions later, it might need to be.
                // For simple raycasting, non-convex is fine and more accurate.
            }
            
            return part;
        }

        [ContextMenu("Create Tool Placeholders")]
        public void CreateToolPlaceholders()
        {
            GameObject toolsRoot = new GameObject("Tool_Placeholders");

            // Rectangle Laser (Cube)
            CreateTool(toolsRoot, "Tool_RectangleLaser", PrimitiveType.Cube, new Vector3(0.2f, 0.2f, 0.2f), Color.gray);

            // Wax (Cylinder)
            CreateTool(toolsRoot, "Tool_Wax", PrimitiveType.Cylinder, new Vector3(0.2f, 0.2f, 0.2f), Color.yellow);

            // Razor (Cube flattened)
            CreateTool(toolsRoot, "Tool_Razor", PrimitiveType.Cube, new Vector3(0.1f, 0.3f, 0.1f), Color.blue);

            // Laser (Capsule)
            CreateTool(toolsRoot, "Tool_Laser", PrimitiveType.Capsule, new Vector3(0.15f, 0.3f, 0.15f), Color.white);

            Debug.Log("Tool Placeholders Created!");
        }

        private void CreateTool(GameObject root, string name, PrimitiveType type, Vector3 scale, Color color)
        {
            GameObject tool = GameObject.CreatePrimitive(type);
            tool.name = name;
            tool.transform.SetParent(root.transform);
            tool.transform.localScale = scale;
            tool.transform.position = root.transform.position + new Vector3(root.transform.childCount * 0.5f, 0, 0); // Spacing
            
            var renderer = tool.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = color;
            }
            
            // Add appropriate tool script
            tool.tag = GameConstants.Tags.Interactable;
            tool.layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            
            ToolBase toolScript;
            if (name.Contains("RectangleLaser"))
            {
                toolScript = tool.AddComponent<RectangleLaser>();
            }
            else
            {
                toolScript = tool.AddComponent<SimpleTool>();
            }
            toolScript.toolName = name;
        }
    }
}
