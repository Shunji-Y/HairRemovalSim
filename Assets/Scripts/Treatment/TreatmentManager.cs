using UnityEngine;
using UnityEngine.InputSystem;
using HairRemovalSim.Core;
using HairRemovalSim.Player;
using HairRemovalSim.Customer;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace HairRemovalSim.Treatment
{
    public class TreatmentManager : Singleton<TreatmentManager>
    {
        [Header("References")]
        public PlayerController playerController;
        public InteractionController interactionController;
        public Camera mainCamera;

        [Header("Settings")]
        public float zoomDuration = 0.5f;
        public Vector3 zoomOffset = new Vector3(0, 0.5f, -0.5f); // Offset from body part
        public float proximityDistance = 3.0f; // Distance to show/hide UI

        private bool isTreating = false;
        private Core.BodyPart currentTarget;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Transform playerTransform;

        public TreatmentSession CurrentSession { get; private set; } // Currently displayed session
        private Dictionary<CustomerController, TreatmentSession> activeSessions = new Dictionary<CustomerController, TreatmentSession>();

        public void StartSession(CustomerController customer)
        {
            // Check if session already exists for this customer
            if (activeSessions.ContainsKey(customer))
            {
                Debug.LogWarning($"Session already exists for {customer.name}");
                return;
            }

            TreatmentSession newSession = new TreatmentSession(customer);
            activeSessions.Add(customer, newSession);
            Debug.Log($"Treatment Session Started for {customer.name}. Total active sessions: {activeSessions.Count}");
            
            // Get player transform if not cached
            if (playerTransform == null && playerController != null)
            {
                playerTransform = playerController.transform;
            }
            
            // Set as current session if this is the first one or no current session
            if (CurrentSession == null || !CurrentSession.IsActive)
            {
                CurrentSession = newSession;
                // Setup UI (creates sliders), but proximity will control visibility
                UI.HUDManager.Instance.ShowTreatmentPanel(CurrentSession);
            }
        }

        public TreatmentSession GetSessionForCustomer(CustomerController customer)
        {
            if (activeSessions.ContainsKey(customer))
            {
                return activeSessions[customer];
            }
            return null;
        }

        public void EndSession()
        {
            if (CurrentSession == null || !CurrentSession.IsActive) return;

            CurrentSession.EndSession();
            Debug.Log($"Treatment Session Ended. Final Progress: {CurrentSession.OverallProgress:F1}%");
            
            // Remove from active sessions
            CustomerController customerToRemove = null;
            foreach (var kvp in activeSessions)
            {
                if (kvp.Value == CurrentSession)
                {
                    customerToRemove = kvp.Key;
                    break;
                }
            }
            if (customerToRemove != null)
            {
                activeSessions.Remove(customerToRemove);
            }
            
            // Notify UI
            UI.HUDManager.Instance.HideTreatmentPanel();
            
            // Handle Rewards (To be implemented)
            // EconomyManager.Instance.AddMoney(...);
            
            // Note: Customer.CompleteTreatment() handles the payment flow
            // Called automatically when OverallProgress reaches 100%
            
            CurrentSession = null;
            StopTreatment(); // Ensure we exit zoom mode
        }

        public void StartTreatment(Core.BodyPart target)
        {
            if (isTreating) return;
            
            // Check if customer is ready (lying on bed)
            var customer = target.GetComponentInParent<CustomerController>();
            if (customer != null && !customer.IsReadyForTreatment)
            {
                Debug.LogWarning("Customer is not ready for treatment (must be on bed).");
                return;
            }

            currentTarget = target;
            isTreating = true;

            // Save original player state
            if (mainCamera != null)
            {
                originalPosition = mainCamera.transform.position;
                originalRotation = mainCamera.transform.rotation;
            }

            // Lock Player Movement
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
            }

            // Move Camera to optimal position
            // For prototype, simple lerp or instant move
            // We want to look at the body part
            if (mainCamera != null)
            {
                Vector3 targetPos = target.transform.position + target.transform.TransformDirection(zoomOffset);
                
                // Ideally we coroutine this, but for now instant
                mainCamera.transform.position = targetPos;
                mainCamera.transform.LookAt(target.transform.position);
            }
            
            Debug.Log($"Started treatment on {target.partName}");
        }

        public void StopTreatment()
        {
            if (!isTreating) return;

            isTreating = false;
            currentTarget = null;

            // Restore Camera
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
                // Reset camera local pos to eye level
                if (mainCamera != null)
                {
                    mainCamera.transform.localPosition = new Vector3(0, 0.6f, 0);
                }
            }

            Debug.Log("Stopped treatment");
        }

        private void Update()
        {
            // Update all active sessions
            foreach (var session in activeSessions.Values)
            {
                if (session != null && session.IsActive)
                {
                    session.UpdateProgress();
                    
                    // Check for completion and auto-complete
                    if (session.OverallProgress >= 100f)
                    {
                        Debug.Log($"[TreatmentManager] Treatment complete for {session.Customer.data.customerName}! Customer will now go to reception.");
                        
                        // Trigger customer to leave bed and go to reception
                        if (session.Customer != null)
                        {
                            session.Customer.CompleteTreatment();
                        }
                        
                        // End this specific session
                        CurrentSession = session;
                        EndSession();
                        return; // Exit to avoid modifying collection during iteration
                    }
                }
            }
            
            // Find nearest customer to player
            CustomerController nearestCustomer = null;
            float nearestDistance = float.MaxValue;
            
            if (playerTransform != null)
            {
                foreach (var kvp in activeSessions)
                {
                    if (kvp.Key != null && kvp.Key.IsReadyForTreatment)
                    {
                        float distance = Vector3.Distance(playerTransform.position, kvp.Key.transform.position);
                        if (distance < nearestDistance && distance <= proximityDistance)
                        {
                            nearestDistance = distance;
                            nearestCustomer = kvp.Key;
                        }
                    }
                }
            }
            
            // Switch to nearest customer's session if different
            if (nearestCustomer != null && activeSessions.ContainsKey(nearestCustomer))
            {
                TreatmentSession nearestSession = activeSessions[nearestCustomer];
                
                // Switch session if different
                if (CurrentSession != nearestSession)
                {
                    CurrentSession = nearestSession;
                    UI.HUDManager.Instance.ShowTreatmentPanel(CurrentSession);
                    Debug.Log($"Switched to {nearestCustomer.name}'s session");
                }
                
                // Show UI
                if (UI.HUDManager.Instance != null && UI.HUDManager.Instance.treatmentPanel != null)
                {
                    GameObject panelRoot = UI.HUDManager.Instance.treatmentPanel.panelRoot;
                    if (panelRoot != null)
                    {
                        panelRoot.SetActive(true);
                    }
                    UI.HUDManager.Instance.UpdateTreatmentPanel();
                }
            }
            else
            {
                // No customer nearby - hide UI
                if (UI.HUDManager.Instance != null && UI.HUDManager.Instance.treatmentPanel != null)
                {
                    GameObject panelRoot = UI.HUDManager.Instance.treatmentPanel.panelRoot;
                    if (panelRoot != null)
                    {
                        panelRoot.SetActive(false);
                    }
                }
            }

            if (isTreating)
            {
                // Right Click to Exit (Input System)
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    StopTreatment();
                }
            }
            
            // R key to rotate customer
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotateNearestCustomer();
            }
        }
        
        public void RotateNearestCustomer()
        {
            if (playerTransform == null) return;
            
            Customer.CustomerController nearestCustomer = null;
            float nearestDistance = proximityDistance;
            
            foreach (var kvp in activeSessions)
            {
                Customer.CustomerController customer = kvp.Key;
                if (customer != null && customer.IsReadyForTreatment)
                {
                    float distance = Vector3.Distance(playerTransform.position, customer.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestCustomer = customer;
                    }
                }
            }
            
            if (nearestCustomer != null)
            {
                nearestCustomer.RotateCustomer();
            }
        }
    }
}
