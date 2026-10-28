using SafetyTraining.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(XRSimpleInteractable))]
    public sealed class SitePortal : MonoBehaviour
    {
        [SerializeField] Vector3 arrivalPoint;
        [SerializeField] TrainingSiteId destinationSite;
        [SerializeField] bool returnToHub;
        XRSimpleInteractable interactable;
        Light accentLight;
        Vector3 restingScale;
        Vector3 targetScale;
        bool hovered;
        bool activating;
        string activationSource;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            accentLight = GetComponentInChildren<Light>(true);
            restingScale = transform.localScale;
            targetScale = restingScale;
        }

        void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
        }

        void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelected);
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.hoverExited.RemoveListener(OnHoverExited);
            }
        }

        void OnMouseDown()
        {
            if (!DesktopPointerInputGate.CanUseWorldPointer)
                return;
            BeginActivation("desktop collider click");
        }

        void OnMouseEnter()
        {
            SetHovered(true);
        }

        void OnMouseExit()
        {
            SetHovered(false);
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            if (accentLight != null && !activating)
                accentLight.intensity = Mathf.Lerp(accentLight.intensity, hovered ? 2.4f : 0.9f,
                    1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        public void Configure(Vector3 destination, TrainingSiteId site)
        {
            arrivalPoint = destination;
            destinationSite = site;
            returnToHub = false;
        }

        public void ConfigureReturn(Vector3 destination, TrainingSiteId site)
        {
            arrivalPoint = destination;
            destinationSite = site;
            returnToHub = true;
        }

        public bool ReturnsToHub => returnToHub;
        public Vector3 ArrivalPoint => arrivalPoint;
        public TrainingSiteId DestinationSite => destinationSite;

        void OnSelected(SelectEnterEventArgs _)
        {
            BeginActivation("XR select");
        }

        void OnHoverEntered(HoverEnterEventArgs _)
        {
            SetHovered(true);
        }

        void OnHoverExited(HoverExitEventArgs _)
        {
            SetHovered(false);
        }

        void SetHovered(bool value)
        {
            if (activating)
                return;
            hovered = value;
            targetScale = restingScale * (hovered ? 1.045f : 1f);
        }

        public void Activate()
        {
            BeginActivation("desktop camera raycast");
        }

        void BeginActivation(string source)
        {
            if (activating)
                return;
            activationSource = source;
            StartCoroutine(PlayActivation());
        }

        IEnumerator PlayActivation()
        {
            activating = true;
            targetScale = restingScale * 0.94f;
            if (accentLight != null)
                accentLight.intensity = 4.5f;
            yield return new WaitForSecondsRealtime(0.08f);
            targetScale = restingScale * 1.08f;
            yield return new WaitForSecondsRealtime(0.1f);
            var travelCompleted = Travel();
            targetScale = restingScale;
            activating = false;
            if (!travelCompleted)
                yield break;
            if (returnToHub)
                SiteIsolationController.Instance?.ShowHub();
            else
                SiteIsolationController.Instance?.ShowSite(destinationSite);
        }

        bool Travel()
        {
            var viewer = Camera.main;
            if (viewer == null)
                return false;

            var rig = viewer.transform.root;
            var horizontalDelta = arrivalPoint - viewer.transform.position;
            horizontalDelta.y = 0f;
            var destination = rig.position + horizontalDelta;
            var characterController = rig.GetComponent<CharacterController>();
            var controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled)
                characterController.enabled = false;
            rig.position = destination;
            Physics.SyncTransforms();
            if (controllerWasEnabled)
                characterController.enabled = true;
            rig.GetComponent<StartupGroundingGuard>()?.SetSafeSpawn(rig.position);
            if (returnToHub)
            {
                TrainingCoordinator.Instance?.LeaveSite(destinationSite);
                NpcChatPanel.Instance?.Close();
            }
            else
            {
                TrainingCoordinator.Instance?.EnterSite(destinationSite);
            }
            Debug.Log($"Portal travel completed: {destinationSite}, return={returnToHub}, source={activationSource}, " +
                      $"rig={rig.position}, viewer={viewer.transform.position}.");
            return true;
        }
    }
}
