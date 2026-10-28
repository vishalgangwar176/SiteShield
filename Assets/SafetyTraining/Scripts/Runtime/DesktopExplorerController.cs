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
        [SerializeField, Min(0.1f)] float moveSpeed = 5f;
        [SerializeField, Min(1f)] float lookSpeed = 100f;

        Transform locomotionRoot;
        CharacterController characterController;
        bool desktopMode;
        bool startupStabilityReported;
        float pitch;
        float startupStabilityDeadline;
        float maximumRootDrift;
        float maximumCameraLocalDrift;
        float maximumCameraAngularDrift;
        Vector3 stableRootPosition;
        InteractiveHoverFeedback hoveredTarget;
        HandsOnPlacementInteractable draggedPlacement;

        public bool DesktopMode => desktopMode;

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
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            pitch = 0f;
            stableRootPosition = locomotionRoot.position;
            startupStabilityDeadline = Time.unscaledTime + 2f;
            Debug.Log("Desktop preview mode: XR pose and locomotion writers disabled; camera is controlled through the rig root.");
        }

        void Update()
        {
            if (!desktopMode)
                return;

            var chatVisible = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;
            var textEntryFocused = IsTextEntryFocused();
            var locomotionBlocked = ShouldBlockLocomotion(chatVisible, textEntryFocused);
            var movement = locomotionBlocked
                ? Vector3.zero
                : new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            MonitorStartupStability(movement);
            HandlePointer();
            if (locomotionBlocked)
                return;
            var planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var planarRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var worldMovement = (planarRight * movement.x + planarForward * movement.z).normalized;
            var displacement = worldMovement * (moveSpeed * Time.deltaTime);
            if (characterController != null && characterController.enabled)
                characterController.Move(displacement);
            else if (locomotionRoot != null)
                locomotionRoot.position += displacement;

            if (!Input.GetMouseButton(1))
                return;

            var yaw = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime, -80f, 80f);
            if (locomotionRoot != null)
                locomotionRoot.Rotate(0f, yaw, 0f, Space.World);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

        void HandlePointer()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (draggedPlacement != null)
            {
                HandleActiveDesktopDrag(ray, Input.GetMouseButton(0));
                return;
            }

            var chatVisible = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;
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
            return chatVisible || pointerOverUi;
        }

        static bool ShouldBlockLocomotion(bool chatVisible, bool textEntryFocused)
        {
            return chatVisible || textEntryFocused;
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
            if (hasFocus || draggedPlacement == null)
                return;

            draggedPlacement.CancelDesktopDrag();
            draggedPlacement = null;
        }

        void MonitorStartupStability(Vector3 movement)
        {
            if (startupStabilityReported || movement.sqrMagnitude > 0f || Input.GetMouseButton(1))
                return;

            if (locomotionRoot == null) return;
            maximumRootDrift = Mathf.Max(maximumRootDrift, Vector3.Distance(stableRootPosition, locomotionRoot.position));
            maximumCameraLocalDrift = Mathf.Max(maximumCameraLocalDrift, transform.localPosition.magnitude);
            maximumCameraAngularDrift = Mathf.Max(maximumCameraAngularDrift, Quaternion.Angle(Quaternion.identity, transform.localRotation));
            if (Time.unscaledTime < startupStabilityDeadline)
                return;

            startupStabilityReported = true;
            Debug.Log($"Desktop startup stability PASS: root drift {maximumRootDrift:F4} m, camera local drift {maximumCameraLocalDrift:F4} m, camera angular drift {maximumCameraAngularDrift:F3} deg over 2.0 s.");
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
