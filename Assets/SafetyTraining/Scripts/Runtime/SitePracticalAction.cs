using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(XRGrabInteractable), typeof(HandsOnPlacementInteractable))]
    public sealed class SitePracticalAction : MonoBehaviour
    {
        [SerializeField] int stepIndex;
        [SerializeField] string actionName = "Site control";
        [SerializeField] string instruction = "Complete the next marked control.";
        SitePracticalController controller;
        HandsOnPlacementInteractable placement;
        Collider primaryCollider;
        Collider stepMarkerClickSurface;
        Renderer[] stepMarkerRenderers;
        bool completed;

        public int StepIndex => stepIndex;
        public string ActionName => actionName;
        public string Instruction => instruction;
        public bool Completed => completed;
        public bool CanManipulate => !completed && controller != null && controller.CanManipulate(this);
        public Vector3 TargetLocalPosition => placement.TargetLocalPosition;

        void Awake()
        {
            controller ??= GetComponentInParent<SitePracticalController>();
            placement = GetComponent<HandsOnPlacementInteractable>();
            primaryCollider = GetComponent<Collider>();
            var stepMarker = transform.Find("Practical Step Marker");
            stepMarkerClickSurface = stepMarker?.GetComponent<Collider>();
            stepMarkerRenderers = stepMarker?.GetComponentsInChildren<Renderer>(true);
            SetCurrentStep(false);
        }

        public void Configure(SitePracticalController owner, int index, string title,
            string hint, Vector3 destination, Vector3 destinationEulerAngles, float acceptanceRadius,
            GameObject targetVisual)
        {
            controller = owner;
            stepIndex = index;
            actionName = title;
            instruction = hint;
            placement = GetComponent<HandsOnPlacementInteractable>();
            placement.Configure(destination, destinationEulerAngles, acceptanceRadius, targetVisual);
        }

        public void TryPerform(string inputMode = "Automated")
        {
            if (!completed)
                controller?.TryPerform(this, placement.DistanceToTarget, placement.IsWithinTarget, inputMode);
        }

        public void SetCurrentStep(bool isCurrent)
        {
            primaryCollider ??= GetComponent<Collider>();
            var stepMarker = transform.Find("Practical Step Marker");
            stepMarkerClickSurface ??= stepMarker?.GetComponent<Collider>();
            stepMarkerRenderers ??= stepMarker?.GetComponentsInChildren<Renderer>(true);
            var shouldShowCurrentStep = isCurrent && !completed;
            primaryCollider.enabled = shouldShowCurrentStep;
            placement ??= GetComponent<HandsOnPlacementInteractable>();
            placement.SetAvailable(shouldShowCurrentStep);
            if (stepMarkerClickSurface != null)
                stepMarkerClickSurface.enabled = false;
            if (stepMarkerRenderers != null)
            {
                foreach (var markerRenderer in stepMarkerRenderers)
                    markerRenderer.enabled = shouldShowCurrentStep;
            }
        }

        public void MarkComplete()
        {
            if (completed)
                return;
            completed = true;
            SetCurrentStep(false);
            placement.CompletePlacement();
        }
    }
}
