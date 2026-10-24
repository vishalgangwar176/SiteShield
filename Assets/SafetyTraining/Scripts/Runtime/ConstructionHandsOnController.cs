using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class ConstructionHandsOnController : MonoBehaviour
    {
        readonly List<ConstructionActionInteractable> actions = new();
        int nextStep;
        bool complete;
        bool active;

        public int CompletedSteps => nextStep;
        public int TotalSteps => actions.Count;
        public bool IsComplete => complete;

        void Awake()
        {
            actions.AddRange(GetComponentsInChildren<ConstructionActionInteractable>(true)
                .OrderBy(item => item.StepIndex));
        }

        public void Begin()
        {
            active = true;
            RefreshCurrentStep();
            Publish("Pick up the marked PPE kit to begin.");
        }

        public bool CanManipulate(ConstructionActionInteractable action)
        {
            return active && !complete && action.StepIndex == nextStep;
        }

        public void TryPerform(ConstructionActionInteractable action, float releaseDistance,
            bool insideTarget, string inputMode)
        {
            if (!active || complete)
                return;
            if (action.StepIndex != nextStep)
            {
                var expected = actions.ElementAtOrDefault(nextStep);
                Publish($"Sequence check: complete {expected?.ActionName ?? "the remaining control"} first.");
                return;
            }
            if (!insideTarget)
            {
                TrainingCoordinator.Instance?.RecordPlacementAttempt(
                    SafetyTraining.Core.TrainingSiteId.Construction, action.StepIndex, action.ActionName,
                    releaseDistance, false, inputMode);
                Publish($"Placement check: move {action.ActionName} into the highlighted DROP HERE zone.");
                return;
            }

            action.MarkComplete();
            nextStep++;
            RefreshCurrentStep();
            TrainingCoordinator.Instance?.RecordPlacementAttempt(
                SafetyTraining.Core.TrainingSiteId.Construction, action.StepIndex, action.ActionName,
                releaseDistance, true, inputMode);
            TrainingCoordinator.Instance?.AddHandsOnBonus(20);
            SafetyTaskManager.Instance?.NotifyPracticalStepCompleted(
                SafetyTraining.Core.TrainingSiteId.Construction, action.StepIndex);
            var coach = FindObjectsByType<NpcSiteCompanion>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.SiteId == SafetyTraining.Core.TrainingSiteId.Construction);
            coach?.Encourage(nextStep >= actions.Count);
            if (nextStep >= actions.Count)
            {
                complete = true;
                Publish("Construction practical complete: controls installed and verified. Debrief with the coach.");
                return;
            }

            var next = actions[nextStep];
            Publish($"Step {nextStep}/{actions.Count} complete. Next: {next.ActionName}. {next.Instruction}");
        }

        void Publish(string message)
        {
            TrainingCoordinator.Instance?.SetHandsOnFeedback(message);
        }

        void RefreshCurrentStep()
        {
            foreach (var action in actions)
                action.SetCurrentStep(active && !complete && action.StepIndex == nextStep);
        }
    }
}
