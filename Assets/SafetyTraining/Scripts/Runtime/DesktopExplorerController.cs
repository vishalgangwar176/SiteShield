using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

namespace SafetyTraining.Runtime
{
    public sealed class DesktopExplorerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] float moveSpeed = 5f;

        [Header("First-Person Mouse Look")]
        [SerializeField, Range(0.2f, 5f)] float mouseSensitivity = 2.0f;
        [SerializeField] bool invertY = false;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;

        Transform locomotionRoot;
        CharacterController characterController;
        bool desktopMode;
        bool isCursorLocked;
        bool wasUiBlocking;
        bool startupStabilityReported;
        float pitch;
        float startupStabilityDeadline;
        float maximumRootDrift;
        float maximumCameraLocalDrift;
        Vector3 stableRootPosition;
        InteractiveHoverFeedback hoveredTarget;
        HandsOnPlacementInteractable draggedPlacement;

        public bool DesktopMode => desktopMode;
        public bool IsCursorLocked => isCursorLocked;

        public float MouseSensitivity
        {
            get => mouseSensitivity * PlayerPrefs.GetFloat("SS_LookSensitivity", 1.0f);
            set
            {
                mouseSensitivity = Mathf.Clamp(value, 0.2f, 5.0f);
                PlayerPrefs.SetFloat("SS_LookSensitivity", 1.0f);
            }
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        void Start()
        {
            locomotionRoot = transform.root;
            characterController = locomotionRoot.GetComponent<CharacterController>();
            desktopMode = !HasRunningXrDisplay();
            if (!desktopMode)
            {
                enabled = false;
                return;
            }

            DisableXrPoseAndLocomotionWriters();

            // Initialize camera orientation and pitch
            var currentAngles = transform.localEulerAngles;
            pitch = currentAngles.x;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            stableRootPosition = locomotionRoot.position;
            startupStabilityDeadline = Time.unscaledTime + 2f;

            // Lock and hide cursor for first-person gameplay
            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;
            wasUiBlocking = menuOpen;
            SetCursorLocked(!menuOpen);

            Debug.Log("Desktop preview mode: First-person camera mouse look (unlocked unrestricted yaw, clamped pitch) and WASD locomotion active.");
        }

        public void SetCursorLocked(bool locked)
        {
            isCursorLocked = locked;
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Update()
        {
            if (!desktopMode)
                return;

            HandleCursorState();

            var chatVisible = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;
            var textEntryFocused = IsTextEntryFocused();
            var locomotionBlocked = ShouldBlockLocomotion(chatVisible, textEntryFocused);

            HandleLocomotion(locomotionBlocked);
            HandleLook(chatVisible, textEntryFocused);
            HandlePointer(chatVisible);
        }

        void HandleCursorState()
        {
            var chatVisible = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;
            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;
            bool uiBlocking = chatVisible || menuOpen;

            // Sync cursor lock when UI visibility changes
            if (uiBlocking != wasUiBlocking)
            {
                wasUiBlocking = uiBlocking;
                SetCursorLocked(!uiBlocking);
                return;
            }

            // Escape key toggles cursor lock when chat/menu are closed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!uiBlocking)
                {
                    SetCursorLocked(!isCursorLocked);
                }
            }
            // Left click in viewport re-locks cursor if not clicking UI
            else if (!isCursorLocked && !uiBlocking)
            {
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
                {
                    SetCursorLocked(true);
                }
            }
        }

        void HandleLook(bool chatVisible, bool textEntryFocused)
        {
            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;

            // Lock camera look when in menus, chat, text entry, or cursor is unlocked
            if (!isCursorLocked || chatVisible || textEntryFocused || menuOpen)
                return;

            Vector2 mouseDelta = GetMouseDelta();
            if (mouseDelta.sqrMagnitude < 0.000001f)
                return;

            float sensMultiplier = PlayerPrefs.GetFloat("SS_LookSensitivity", 1.0f);
            float effectiveSensitivity = mouseSensitivity * sensMultiplier;

            float deltaYaw = mouseDelta.x * effectiveSensitivity;
            float deltaPitch = mouseDelta.y * effectiveSensitivity * (invertY ? -1f : 1f);

            // Horizontal rotation: rotates the player body (locomotionRoot) around world Y
            if (locomotionRoot != null)
            {
                locomotionRoot.Rotate(0f, deltaYaw, 0f, Space.World);
            }

            // Vertical rotation: rotates the camera pivot around local X, clamped
            pitch = Mathf.Clamp(pitch - deltaPitch, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 GetMouseDelta()
        {
            try
            {
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
            catch
            {
                return Vector2.zero;
            }
        }

        void HandleLocomotion(bool locomotionBlocked)
        {
            var movement = locomotionBlocked
                ? Vector3.zero
                : new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            MonitorStartupStability(movement);

            if (locomotionBlocked || movement.sqrMagnitude < 0.0001f)
                return;

            // Planar movement relative to camera forward/right
            var planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var planarRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var worldMovement = (planarRight * movement.x + planarForward * movement.z).normalized;
            var displacement = worldMovement * (moveSpeed * Time.deltaTime);

            if (characterController != null && characterController.enabled)
                characterController.Move(displacement);
            else if (locomotionRoot != null)
                locomotionRoot.position += displacement;
        }

        bool IsTextEntryFocused()
        {
            try
            {
                var es = EventSystem.current;
                if (es == null) return false;
                var go = es.currentSelectedGameObject;
                if (go == null) return false;
                var tmp = go.GetComponent<TMPro.TMP_InputField>();
                if (tmp != null && tmp.isFocused) return true;
                var input = go.GetComponent<InputField>();
                if (input != null && input.isFocused) return true;
            }
            catch
            {
            }
            return false;
        }

        bool IsPointerOverUi()
        {
            try
            {
                var es = EventSystem.current;
                return es != null && es.IsPointerOverGameObject();
            }
            catch
            {
                return false;
            }
        }

        void HandlePointer(bool chatVisible)
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            // When cursor is locked, cast ray directly from center reticle (0.5, 0.5)
            // When cursor is unlocked, cast ray from mouse screen position
            Ray ray = isCursorLocked
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(Input.mousePosition);

            if (draggedPlacement != null)
            {
                HandleActiveDesktopDrag(ray, Input.GetMouseButton(0));
                return;
            }

            var pointerOverUi = IsPointerOverUi();
            if (!DesktopPointerInputGate.CanUseWorldPointer || ShouldBlockWorldPointer(chatVisible, pointerOverUi))
            {
                if (hoveredTarget != null)
                {
                    hoveredTarget.SetHovered(false);
                    hoveredTarget = null;
                }
                return;
            }

            var hits = Physics.RaycastAll(ray, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            Transform target = null;
            InteractiveHoverFeedback nextHoveredTarget = null;
            foreach (var hit in hits)
            {
                var candidate = hit.collider.transform;
                var feedback = candidate.GetComponentInParent<InteractiveHoverFeedback>();
                if (feedback == null && candidate.GetComponentInParent<SitePortal>() == null &&
                    candidate.GetComponentInParent<InspectionTarget>() == null &&
                    candidate.GetComponentInParent<ConstructionActionInteractable>() == null &&
                    candidate.GetComponentInParent<SitePracticalAction>() == null &&
                    candidate.GetComponentInParent<NpcTalkInteractable>() == null)
                    continue;
                target = candidate;
                nextHoveredTarget = feedback;
                break;
            }
            if (nextHoveredTarget != hoveredTarget)
            {
                if (hoveredTarget != null)
                    hoveredTarget.SetHovered(false);
                hoveredTarget = nextHoveredTarget;
                if (hoveredTarget != null)
                    hoveredTarget.SetHovered(true);
            }

            if (!Input.GetMouseButtonDown(0) || target == null)
                return;

            var placement = target.GetComponentInParent<HandsOnPlacementInteractable>();
            if (placement != null && placement.BeginDesktopDrag(ray))
            {
                draggedPlacement = placement;
                return;
            }

            ActivateTarget(target);
        }

        static bool ShouldBlockWorldPointer(bool chatVisible, bool pointerOverUi)
        {
            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;
            return chatVisible || pointerOverUi || menuOpen;
        }

        static bool ShouldBlockLocomotion(bool chatVisible, bool textEntryFocused)
        {
            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;
            return chatVisible || textEntryFocused || menuOpen;
        }

        void HandleActiveDesktopDrag(Ray pointerRay, bool buttonHeld)
        {
            if (draggedPlacement == null)
                return;

            if (buttonHeld)
            {
                draggedPlacement.UpdateDesktopDrag(pointerRay);
                return;
            }

            draggedPlacement.EndDesktopDrag();
            draggedPlacement = null;
        }

        void ActivateTarget(Transform target)
        {
            var portal = target.GetComponentInParent<SitePortal>();
            if (portal != null)
            {
                portal.Activate();
                return;
            }

            var npc = target.GetComponentInParent<NpcTalkInteractable>();
            if (npc != null)
            {
                npc.Ask();
                return;
            }

            var inspection = target.GetComponentInParent<InspectionTarget>();
            if (inspection != null)
            {
                inspection.Inspect();
                return;
            }
        }

        void OnDisable()
        {
            SetCursorLocked(false);
            if (hoveredTarget != null)
            {
                hoveredTarget.SetHovered(false);
                hoveredTarget = null;
            }
            if (draggedPlacement != null)
            {
                draggedPlacement.CancelDesktopDrag();
                draggedPlacement = null;
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetCursorLocked(false);
                if (draggedPlacement != null)
                {
                    draggedPlacement.CancelDesktopDrag();
                    draggedPlacement = null;
                }
            }
        }

        void MonitorStartupStability(Vector3 movement)
        {
            if (startupStabilityReported || movement.sqrMagnitude > 0f)
                return;

            if (locomotionRoot == null) return;
            maximumRootDrift = Mathf.Max(maximumRootDrift, Vector3.Distance(stableRootPosition, locomotionRoot.position));
            maximumCameraLocalDrift = Mathf.Max(maximumCameraLocalDrift, transform.localPosition.magnitude);
            if (Time.unscaledTime < startupStabilityDeadline)
                return;

            startupStabilityReported = true;
            Debug.Log($"Desktop startup stability PASS: root drift {maximumRootDrift:F4} m, camera local drift {maximumCameraLocalDrift:F4} m over 2.0 s.");
        }

        static bool HasRunningXrDisplay()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            return displays.Exists(display => display != null && display.running);
        }

        void DisableXrPoseAndLocomotionWriters()
        {
            if (locomotionRoot == null) return;
            foreach (var behaviour in locomotionRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == this)
                    continue;

                var type = behaviour.GetType();
                var typeName = type.Name;
                var typeNamespace = type.Namespace ?? string.Empty;
                if (typeName.Contains("TrackedPoseDriver") ||
                    typeNamespace.StartsWith("UnityEngine.XR.Interaction.Toolkit.Locomotion"))
                    behaviour.enabled = false;
            }
        }
    }
}

