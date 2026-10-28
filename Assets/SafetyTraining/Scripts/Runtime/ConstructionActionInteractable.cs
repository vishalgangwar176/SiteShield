using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(XRGrabInteractable), typeof(HandsOnPlacementInteractable))]
    public sealed class ConstructionActionInteractable : MonoBehaviour
    {
        [SerializeField] int stepIndex;
        [SerializeField] string actionName = "Hands-on action";
        [SerializeField] string instruction = "Complete the next control.";
        ConstructionHandsOnController controller;
        HandsOnPlacementInteractable placement;
        Collider primaryCollider;
        bool completed;

        public int StepIndex => stepIndex;
        public string ActionName => actionName;
        public string Instruction => instruction;
        public bool Completed => completed;
        public bool CanManipulate => !completed && controller != null && controller.CanManipulate(this);
        public Vector3 TargetLocalPosition => placement.TargetLocalPosition;

        void Awake()
        {
            controller ??= GetComponentInParent<ConstructionHandsOnController>();
            placement = GetComponent<HandsOnPlacementInteractable>();
            primaryCollider = GetComponent<Collider>();
        }

        public void Configure(ConstructionHandsOnController owner, int index,
            string title, string hint, Vector3 destination, Vector3 destinationEulerAngles,
            float acceptanceRadius, GameObject targetVisual)
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
            placement ??= GetComponent<HandsOnPlacementInteractable>();
            var available = isCurrent && !completed;
            primaryCollider.enabled = available;
            placement.SetAvailable(available);
        }

        public void MarkComplete()
        {
            completed = true;
            SetCurrentStep(false);
            placement.CompletePlacement();
        }
    }
}
