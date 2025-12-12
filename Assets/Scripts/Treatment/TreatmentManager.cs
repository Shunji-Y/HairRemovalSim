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
        
        // Highlight fade settings (Q key toggle)
        [Header("Highlight Settings")]
        public float highlightFadeDuration = 1.0f;
        public float maxHighlightIntensity = 2.0f;
        
        private bool isHighlighting = false;
        private float highlightTimer = 0f;
        private enum HighlightState { Off, FadingIn, FadingOut }
        private HighlightState highlightState = HighlightState.Off;

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
            Debug.Log($"Treatment Session Ended for {CurrentSession.Customer.data.customerName}.");
            
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
            // Called automatically when all target body parts reach 100%
            
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
            // Update all active sessions and check for completion
            foreach (var session in activeSessions.Values)
            {
                if (session != null && session.IsActive)
                {
                    // Check if all target body parts are complete
                    if (session.AreAllPartsComplete())
                    {
                        Debug.Log($"[TreatmentManager] Treatment complete for {session.Customer.data.customerName}! All parts done. Customer will now go to reception.");
                        
                        // Trigger customer to leave bed and go to reception
                        if (session.Customer != null)
                        {
                            // Submit review to ShopManager
                            if (Core.ShopManager.Instance != null)
                            {
                                Core.ShopManager.Instance.AddReview(
                                    session.Customer.GetBaseReviewValue(),
                                    session.Customer.GetPainMaxCount()
                                );
                            }
                            
                            session.Customer.CompleteTreatment();
                        }
                        
                        // End this specific session
                        CurrentSession = session;
                        EndSession();
                        return; // Exit to avoid modifying collection during iteration
                    }
                }
            }
            
            // Find if player is inside any bed with an active customer
            Environment.BedController activeBed = null;
            CustomerController nearestCustomer = null;
            
            foreach (var kvp in activeSessions)
            {
                if (kvp.Key != null && kvp.Key.IsReadyForTreatment)
                {
                    // Get the bed this customer is assigned to
                    var bed = kvp.Key.assignedBed;
                    if (bed != null && bed.IsPlayerInside)
                    {
                        activeBed = bed;
                        nearestCustomer = kvp.Key;
                        break;
                    }
                }
            }
            
            // Switch to customer's session if player is in their bed area
            if (nearestCustomer != null && activeSessions.ContainsKey(nearestCustomer))
            {
                TreatmentSession nearestSession = activeSessions[nearestCustomer];
                
                // Switch session if different
                if (CurrentSession != nearestSession)
                {
                    CurrentSession = nearestSession;
                    UI.HUDManager.Instance.ShowTreatmentPanel(CurrentSession);
                    Debug.Log($"Switched to {nearestCustomer.name}'s session (trigger-based)");
                }
                
                // Show UI
                if (UI.HUDManager.Instance != null && UI.HUDManager.Instance.treatmentPanel != null)
                {
                    GameObject panelRoot = UI.HUDManager.Instance.treatmentPanel.panelRoot;
                    if (panelRoot != null)
                    {
                        panelRoot.SetActive(true);
                        
                        // Enable treatment mode for zoom control
                        if (playerController != null)
                        {
                            playerController.SetTreatmentMode(true);
                        }
                    }
                    UI.HUDManager.Instance.UpdateTreatmentPanel();
                }
            }
            else
            {
                // Player not in any bed area - hide UI
                if (UI.HUDManager.Instance != null && UI.HUDManager.Instance.treatmentPanel != null)
                {
                    GameObject panelRoot = UI.HUDManager.Instance.treatmentPanel.panelRoot;
                    if (panelRoot != null)
                    {
                        panelRoot.SetActive(false);
                        
                        // Disable treatment mode for zoom control
                        if (playerController != null)
                        {
                            playerController.SetTreatmentMode(false);
                        }
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
            
            // Q key to show highlight with fade
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                if (highlightState == HighlightState.Off)
                {
                    highlightState = HighlightState.FadingIn;
                    highlightTimer = 0f;
                    Debug.Log("[TreatmentManager] Q pressed - starting highlight fade in");
                }
            }
            
            // Update highlight fade animation
            UpdateHighlightFade();
        }
        
        /// <summary>
        /// Update highlight intensity with fade animation
        /// </summary>
        private void UpdateHighlightFade()
        {
            if (highlightState == HighlightState.Off) return;
            
            highlightTimer += Time.deltaTime;
            float intensity = 0f;
            
            if (highlightState == HighlightState.FadingIn)
            {
                float t = Mathf.Clamp01(highlightTimer / highlightFadeDuration);
                intensity = Mathf.Lerp(0f, maxHighlightIntensity, t);
                
                // When fade-in completes, start fade-out
                if (highlightTimer >= highlightFadeDuration)
                {
                    highlightState = HighlightState.FadingOut;
                    highlightTimer = 0f;
                }
            }
            else if (highlightState == HighlightState.FadingOut)
            {
                float t = Mathf.Clamp01(highlightTimer / highlightFadeDuration);
                intensity = Mathf.Lerp(maxHighlightIntensity, 0f, t);
                
                // When fade-out completes, turn off
                if (highlightTimer >= highlightFadeDuration)
                {
                    highlightState = HighlightState.Off;
                    intensity = 0f;
                }
            }
            
            // Apply intensity to nearest customer's materials
            SetHighlightIntensityForNearestCustomer(intensity);
        }
        
        /// <summary>
        /// Set _HighlightIntensity on nearest customer's materials
        /// </summary>
        private void SetHighlightIntensityForNearestCustomer(float intensity)
        {
            if (playerTransform == null) return;
            
            CustomerController nearestCustomer = null;
            float nearestDistance = proximityDistance;
            
            foreach (var kvp in activeSessions)
            {
                CustomerController customer = kvp.Key;
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
                var renderers = nearestCustomer.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat.HasProperty("_HighlightIntensity"))
                        {
                            mat.SetFloat("_HighlightIntensity", intensity);
                        }
                    }
                }
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
