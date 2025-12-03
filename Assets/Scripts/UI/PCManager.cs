using UnityEngine;
using HairRemovalSim.Interaction;
using HairRemovalSim.Player;

namespace HairRemovalSim.UI
{
    public class PCManager : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        public GameObject pcCanvas;
        public GameObject desktopPanel;
        public GameObject shopPanel;
        public GameObject emailPanel;

        private bool isUsingPC = false;

        private void Start()
        {
            if (pcCanvas != null) pcCanvas.SetActive(false);
        }

        private void Update()
        {
            if (isUsingPC && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePC();
            }
        }

        // IInteractable
        public void OnInteract(InteractionController interactor)
        {
            OpenPC();
        }

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public string GetInteractionPrompt() => "Use PC";

        private void OpenPC()
        {
            isUsingPC = true;
            if (pcCanvas != null) pcCanvas.SetActive(true);
            ShowDesktop();
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ClosePC()
        {
            isUsingPC = false;
            if (pcCanvas != null) pcCanvas.SetActive(false);
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ShowDesktop()
        {
            desktopPanel.SetActive(true);
            shopPanel.SetActive(false);
            emailPanel.SetActive(false);
        }

        public void OpenShop()
        {
            desktopPanel.SetActive(false);
            shopPanel.SetActive(true);
        }

        public void OpenEmail()
        {
            desktopPanel.SetActive(false);
            emailPanel.SetActive(true);
        }
    }
}
