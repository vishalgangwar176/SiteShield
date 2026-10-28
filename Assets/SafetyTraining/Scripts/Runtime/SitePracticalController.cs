using System.Collections.Generic;
using System.Linq;
using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class SitePracticalController : MonoBehaviour
    {
        readonly List<SitePracticalAction> actions = new();
        [SerializeField] TrainingSiteId siteId;
        [SerializeField] string introduction = "Complete the marked site controls in order.";
        [SerializeField] string completion = "Practical complete. Debrief with the coach.";
        int nextStep;
        bool active;

        public TrainingSiteId SiteId => siteId;
        public int CompletedSteps => nextStep;
        public int TotalSteps => actions.Count;
        public bool IsComplete => actions.Count > 0 && nextStep >= actions.Count;

        void Awake()
        {
            CacheActions();
        }

        public void Configure(TrainingSiteId id, string intro, string completedMessage)
        {
            siteId = id;
            introduction = intro;
            completion = completedMessage;
        }

        public void Begin()
        {
            active = true;
            CacheActions();
            RefreshCurrentStep();
            if (IsComplete)
                Publish(completion);
            else
                Publish($"{introduction} STEP 1/{actions.Count}: {actions[0].ActionName.ToUpperInvariant()}.");
        }

        public bool CanManipulate(SitePracticalAction action)
        {
            return active && !IsComplete && action.StepIndex == nextStep;
        }

        public void TryPerform(SitePracticalAction action, float releaseDistance, bool insideTarget,
            string inputMode)
        {
            if (!active || IsComplete)
                return;
            if (action.StepIndex != nextStep)
            {
                var expected = actions.ElementAtOrDefault(nextStep);
                Publish($"Sequence check: complete {expected?.ActionName ?? "the current control"} first.");
                return;
            }
            if (!insideTarget)
            {
                TrainingCoordinator.Instance?.RecordPlacementAttempt(siteId, action.StepIndex,
                    action.ActionName, releaseDistance, false, inputMode);
                Publish($"Placement check: move {action.ActionName} into the highlighted DROP HERE zone.");
                return;
            }

            action.MarkComplete();
            TrainingCoordinator.Instance?.RecordPlacementAttempt(siteId, action.StepIndex,
                action.ActionName, releaseDistance, true, inputMode);
            nextStep++;
            RefreshCurrentStep();
            TrainingCoordinator.Instance?.AddHandsOnBonus(25);
            FindObjectsByType<NpcSiteCompanion>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.SiteId == siteId)?.Encourage(IsComplete);
            if (IsComplete)
            {
                Publish(completion);
                return;
            }

            var next = actions[nextStep];
            Publish($"Step {nextStep}/{actions.Count} complete. Next: {next.ActionName}. {next.Instruction}");
        }

        void CacheActions()
        {
            actions.Clear();
            actions.AddRange(GetComponentsInChildren<SitePracticalAction>(true)
                .OrderBy(item => item.StepIndex));
        }

        void RefreshCurrentStep()
        {
            foreach (var action in actions)
                action.SetCurrentStep(active && !IsComplete && action.StepIndex == nextStep);
        }

        static void Publish(string message)
        {
            TrainingCoordinator.Instance?.SetContextFeedback(message);
        }
    }
}
