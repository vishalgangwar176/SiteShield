using SafetyTraining.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(XRSimpleInteractable))]
    public sealed class InspectionTarget : MonoBehaviour
    {
        [SerializeField] string targetId = "target";
        [SerializeField] TrainingSiteId siteId;
        [SerializeField] bool isHazard = true;
        [SerializeField] string displayName = "Inspection target";
        [SerializeField, TextArea] string rationale = "Explain the observed condition.";
        [SerializeField, TextArea] string correctiveAction = "Apply the site control procedure.";

        XRSimpleInteractable interactable;
        Renderer[] renderers;
        InteractiveHoverFeedback hoverFeedback;
        bool inspected;

        public string TargetId => targetId;
        public TrainingSiteId SiteId => siteId;
        public bool IsHazard => isHazard;
        public string DisplayName => displayName;
        public string Rationale => rationale;
        public string CorrectiveAction => correctiveAction;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            renderers = GetComponentsInChildren<Renderer>();
            hoverFeedback = GetComponent<InteractiveHoverFeedback>();
            if (hoverFeedback == null)
                hoverFeedback = gameObject.AddComponent<InteractiveHoverFeedback>();
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

        void OnMouseEnter() => hoverFeedback?.SetHovered(true);
        void OnMouseExit() => hoverFeedback?.SetHovered(false);

        void OnMouseDown()
        {
            if (DesktopPointerInputGate.CanUseWorldPointer)
                Inspect();
        }

        public void Configure(
            TrainingSiteId trainingSite,
            string id,
            bool hazard,
            string title,
            string why,
            string action)
        {
            siteId = trainingSite;
            targetId = id;
            isHazard = hazard;
            displayName = title;
            rationale = why;
            correctiveAction = action;
        }

        public void Inspect()
        {
            if (inspected)
                return;

            var result = TrainingCoordinator.Instance?.Inspect(this);
            if (result == null || result.Outcome == InspectionOutcome.UnknownTarget)
                return;

            inspected = true;
            SetFeedbackColor(result.Outcome == InspectionOutcome.CorrectHazard
                ? new Color(0.16f, 0.75f, 0.32f)
                : new Color(1f, 0.66f, 0.1f));
        }

        void OnSelected(SelectEnterEventArgs _)
        {
            Inspect();
        }

        void OnHoverEntered(HoverEnterEventArgs _) => hoverFeedback?.SetHovered(true);
        void OnHoverExited(HoverExitEventArgs _) => hoverFeedback?.SetHovered(false);

        void SetFeedbackColor(Color color)
        {
            if (renderers == null)
                renderers = GetComponentsInChildren<Renderer>();
            foreach (var itemRenderer in renderers)
                itemRenderer.material.color = color;
        }
    }
}
