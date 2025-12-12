using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HairRemovalSim.Core;
using HairRemovalSim.UI;
using HairRemovalSim.Player;
using UnityEngine.InputSystem;
using HairRemovalSim.Treatment;
using HairRemovalSim.Customer;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HairRemovalSim.Tools
{
    public class SceneSetupTool : MonoBehaviour
    {
        [ContextMenu("Setup Basic Scene")]
        public void SetupBasicScene()
        {
            SetupManagers();
            SetupUI();
            SetupPlayer();
            
            Debug.Log("Basic Scene Setup Complete!");
        }

        private void SetupManagers()
        {
            GameObject managers = GameObject.Find("Managers");
            if (managers == null)
            {
                managers = new GameObject("Managers");
            }

            EnsureComponent<GameManager>(managers);
            EnsureComponent<EconomyManager>(managers);
            
            // HUDManager is usually on the UI Canvas or Managers, let's put it on Managers for now
            // and link references later.
            EnsureComponent<HUDManager>(managers);
            
            var treatment = EnsureComponent<TreatmentManager>(managers);
            // We need to link player controller to treatment manager later or find it dynamically
            
            EnsureComponent<CustomerSpawner>(managers);
        }

        private void SetupUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create HUD Panel
            // Create HUD Panel
            Transform hudTrans = canvasObj.transform.Find("HUD_Panel");
            GameObject hudPanel;
            if (hudTrans == null)
            {
                hudPanel = new GameObject("HUD_Panel", typeof(RectTransform));
                hudPanel.transform.SetParent(canvasObj.transform, false);
            }
            else
            {
                hudPanel = hudTrans.gameObject;
            }

            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(1, 1);
            hudRect.pivot = new Vector2(0.5f, 1);
            hudRect.anchoredPosition = new Vector2(0, -50);
            hudRect.sizeDelta = new Vector2(0, 100);

            // Create Texts
            TextMeshProUGUI dayText = CreateText(hudPanel, "DayText", "Day 1", new Vector2(-300, 0));
            TextMeshProUGUI timeText = CreateText(hudPanel, "TimeText", "09:00", new Vector2(0, 0));
            TextMeshProUGUI moneyText = CreateText(hudPanel, "MoneyText", "$10,000", new Vector2(300, 0));
            
            // Create Interaction Prompt Text (Center Screen, slightly below)
            TextMeshProUGUI promptText = CreateText(hudPanel, "InteractionPrompt", "", new Vector2(0, -100));
            promptText.fontSize = 24;
            promptText.gameObject.SetActive(false);

            // Create Crosshair (Center)
            GameObject crosshair = FindOrCreateChild(hudPanel, "Crosshair");
            Image crosshairImg = EnsureComponent<Image>(crosshair);
            crosshairImg.color = new Color(1, 1, 1, 0.5f); // Semi-transparent white
            RectTransform crosshairRect = crosshair.GetComponent<RectTransform>();
            crosshairRect.anchoredPosition = Vector2.zero;
            crosshairRect.sizeDelta = new Vector2(10, 10); // Small dot

            // Link to HUDManager
            HUDManager hudManager = FindObjectOfType<HUDManager>();
            if (hudManager != null)
            {
                hudManager.dayText = dayText;
                hudManager.timeText = timeText;
                hudManager.moneyText = moneyText;
                hudManager.interactionPromptText = promptText;
            }

            // EventSystem
            if (GameObject.Find("EventSystem") == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            SetupTreatmentUI(canvasObj, hudManager);
        }

        private void SetupTreatmentUI(GameObject canvasObj, HUDManager hudManager)
        {
            // 1. Create BodyPartEntry Prefab (as a disabled object in scene for now)
            GameObject prefabRoot = FindOrCreateChild(canvasObj, "BodyPartEntry_Template");
            prefabRoot.SetActive(false);
            RectTransform prefabRect = EnsureComponent<RectTransform>(prefabRoot);
            prefabRect.sizeDelta = new Vector2(200, 40);
            
            // Name Text
            TextMeshProUGUI nameText = CreateText(prefabRoot, "NameText", "Body Part", new Vector2(-50, 0));
            nameText.fontSize = 18;
            nameText.rectTransform.sizeDelta = new Vector2(100, 30);
            nameText.alignment = TextAlignmentOptions.Left;

            // Progress Slider
            GameObject sliderObj = FindOrCreateChild(prefabRoot, "ProgressSlider");
            RectTransform sliderRect = EnsureComponent<RectTransform>(sliderObj);
            sliderRect.anchoredPosition = new Vector2(50, 0);
            sliderRect.sizeDelta = new Vector2(100, 20);
            Slider slider = EnsureComponent<Slider>(sliderObj);
            
            // Slider Background
            GameObject bgObj = FindOrCreateChild(sliderObj, "Background");
            Image bgImg = EnsureComponent<Image>(bgObj);
            bgImg.color = Color.gray;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Slider Fill Area
            GameObject fillArea = FindOrCreateChild(sliderObj, "Fill Area");
            RectTransform fillAreaRect = EnsureComponent<RectTransform>(fillArea);
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-10, 0); // Padding

            // Slider Fill
            GameObject fill = FindOrCreateChild(fillArea, "Fill");
            Image fillImg = EnsureComponent<Image>(fill);
            fillImg.color = Color.green;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.targetGraphic = bgImg;
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;


            // 2. Create TreatmentPanel
            GameObject panelObj = FindOrCreateChild(canvasObj, "TreatmentPanel");
            TreatmentPanel panel = EnsureComponent<TreatmentPanel>(panelObj);
            RectTransform panelRect = EnsureComponent<RectTransform>(panelObj);
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);
            panelRect.anchoredPosition = new Vector2(-20, 20);
            panelRect.sizeDelta = new Vector2(250, 400);

            // Panel Root (Visuals)
            GameObject panelRoot = FindOrCreateChild(panelObj, "PanelRoot");
            Image panelBg = EnsureComponent<Image>(panelRoot);
            panelBg.color = new Color(0, 0, 0, 0.8f);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // Container
            GameObject container = FindOrCreateChild(panelRoot, "BodyPartListContainer");
            RectTransform containerRect = EnsureComponent<RectTransform>(container);
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -50);
            containerRect.sizeDelta = new Vector2(0, 300);
            
            VerticalLayoutGroup vlg = EnsureComponent<VerticalLayoutGroup>(container);
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Overall Progress
            TextMeshProUGUI overallText = CreateText(panelRoot, "OverallProgressText", "0%", new Vector2(0, 170)); // Relative to center? No, let's fix pos
            overallText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            overallText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            overallText.rectTransform.pivot = new Vector2(0.5f, 0);
            overallText.rectTransform.anchoredPosition = new Vector2(0, 10);

            // Assign References
            panel.bodyPartListContainer = container.transform;
            panel.bodyPartEntryPrefab = prefabRoot; // Use the template object as prefab
            panel.overallProgressText = overallText;
            panel.panelRoot = panelRoot;

            // Link to HUDManager
            if (hudManager != null)
            {
                hudManager.treatmentPanel = panel;
            }
        }

        private void SetupPlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.tag = GameConstants.Tags.Player;
                player.transform.position = new Vector3(0, 1, 0);
            }

            // Remove default collider if we want to use CharacterController exclusively, 
            // but CharacterController needs a collider-like shape. 
            // Usually we remove the CapsuleCollider and let CharacterController handle it, 
            // or keep it for triggers. Let's keep it simple.
            
            EnsureComponent<CharacterController>(player);
            EnsureComponent<PlayerInput>(player);
            PlayerController controller = EnsureComponent<PlayerController>(player);
            EnsureComponent<InteractionController>(player);

            // Setup Camera
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0, 0.6f, 0); // Eye level

            // Setup RightHandPoint (Child of Camera so it follows look)
            GameObject rightHandPoint = FindOrCreateChild(camObj, "RightHandPoint");
            rightHandPoint.transform.localPosition = new Vector3(0.5f, -0.4f, 1.0f); // Right hand position
            
            // Setup LeftHandPoint
            GameObject leftHandPoint = FindOrCreateChild(camObj, "LeftHandPoint");
            leftHandPoint.transform.localPosition = new Vector3(-0.3f, -0.4f, 0.8f); // Left hand position

            // Link references
            controller.cameraTransform = camObj.transform;
            var interaction = player.GetComponent<InteractionController>();
            interaction.cameraTransform = camObj.transform;
            interaction.rightHandPoint = rightHandPoint.transform;
            interaction.leftHandPoint = leftHandPoint.transform;
            
            // Set Interaction Layer
            interaction.interactionLayer = LayerMask.GetMask(GameConstants.Layers.Interactable);
            
            // Link TreatmentManager
            var tm = FindObjectOfType<TreatmentManager>();
            if (tm != null)
            {
                tm.playerController = controller;
                tm.interactionController = interaction;
                tm.mainCamera = camObj.GetComponent<Camera>();
            }
        }

        private T EnsureComponent<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null)
            {
                comp = obj.AddComponent<T>();
            }
            return comp;
        }

        private GameObject FindOrCreateChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child != null) return child.gameObject;
            
            GameObject newChild = new GameObject(name);
            newChild.transform.SetParent(parent.transform, false);
            return newChild;
        }

        private TextMeshProUGUI CreateText(GameObject parent, string name, string defaultText, Vector2 position)
        {
            GameObject textObj = FindOrCreateChild(parent, name);
            TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(textObj);
            tmp.text = defaultText;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 50);
            
            return tmp;
        }
    }
}
