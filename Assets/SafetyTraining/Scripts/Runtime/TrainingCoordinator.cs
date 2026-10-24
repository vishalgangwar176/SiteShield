using System;
using System.Collections.Generic;
using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    [DefaultExecutionOrder(-1000)]
    public sealed class TrainingCoordinator : MonoBehaviour
    {
        readonly Dictionary<TrainingSiteId, TrainingSession> sessions =
            new Dictionary<TrainingSiteId, TrainingSession>();
        readonly GuidedSessionPlan guidedPlan = new GuidedSessionPlan();

        TrainingEventLogger eventLogger;
        PracticalProgressRegistry practicals;

        public static TrainingCoordinator Instance { get; private set; }
        public string LastFeedback { get; private set; } =
            "Inspect each site. Select only conditions you believe are hazardous.";
        public string HudFeedback { get; private set; } =
            "Inspect each site. Select only conditions you believe are hazardous.";
        public string ActiveSiteName { get; private set; } = "TRAINING CAMPUS";
        public int OverallScore { get; private set; }
        public int ReviewedConditionCount
        {
            get
            {
                var count = 0;
                foreach (var session in sessions.Values)
                    count += session.InspectedCount;
                return count;
            }
        }
        public int TotalConditionCount { get; private set; }
        public int IdentifiedHazardCount
        {
            get
            {
                var count = 0;
                foreach (var session in sessions.Values)
                    count += session.HazardsFound;
                return count;
            }
        }
        public int TotalHazardCount
        {
            get
            {
                var count = 0;
                foreach (var session in sessions.Values)
                    count += session.HazardsRequired;
                return count;
            }
        }
        public TrainingSiteId? ActiveSite { get; private set; }
        public float SessionElapsedSeconds => guidedPlan.ElapsedSeconds;
        public int CompletedCoachTurns => guidedPlan.CompletedCoachTurns;
        public bool CertificationComplete => AllSitesComplete && guidedPlan.IsComplete &&
            (practicals?.IsComplete ?? false);
        public float GuidedProgress01
        {
            get
            {
                var time = Mathf.Clamp01(SessionElapsedSeconds / GuidedSessionPlan.MinimumSessionSeconds);
                var checks = (float)IdentifiedHazardCount / Mathf.Max(1, TotalHazardCount);
                var coach = (float)CompletedCoachTurns / 10f;
                var practical = practicals?.Progress01 ?? 0f;
                return Mathf.Clamp01(time * 0.4f + checks * 0.3f + coach * 0.2f + practical * 0.1f);
            }
        }
        int handsOnBonus;
        public bool AllSitesComplete
        {
            get
            {
                if (sessions.Count == 0)
                    return false;
                foreach (var session in sessions.Values)
                    if (!session.IsComplete)
                        return false;
                return true;
            }
        }

        public event Action<TrainingSiteId, InspectionResult> InspectionCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            eventLogger = GetComponent<TrainingEventLogger>() ?? gameObject.AddComponent<TrainingEventLogger>();
            BuildSessions();
            practicals = new PracticalProgressRegistry();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            guidedPlan.Advance(ActiveSite, Time.unscaledDeltaTime);
        }

        public InspectionResult Inspect(InspectionTarget target)
        {
            if (!sessions.TryGetValue(target.SiteId, out var session))
                return null;

            var result = session.Inspect(target.TargetId);
            OverallScore = CalculateOverallScore();
            LastFeedback = FormatFeedback(target, result);
            HudFeedback = FormatHudFeedback(target, result);
            eventLogger.Record(target, result, OverallScore);
            InspectionCompleted?.Invoke(target.SiteId, result);

            if (result.Outcome == InspectionOutcome.CorrectHazard)
            {
                if (result.IsComplete)
                    SafetyAudioManager.Instance?.PlayObjectiveComplete();
                else
                    SafetyAudioManager.Instance?.PlayScoreGain();
            }
            else if (result.Outcome == InspectionOutcome.SafeObjectSelected)
            {
                SafetyAudioManager.Instance?.PlayPenalty();
            }

            return result;
        }

        public void AddHandsOnBonus(int points)
        {
            handsOnBonus += Mathf.Max(0, points);
            OverallScore = CalculateOverallScore();
            SafetyAudioManager.Instance?.PlayPlacementSnap();
        }

        public void RecordPlacementAttempt(TrainingSiteId siteId, int stepIndex, string actionName,
            float releaseDistance, bool success, string inputMode)
        {
            eventLogger?.RecordPlacement(siteId, stepIndex, actionName, releaseDistance, success, inputMode);
        }

        public void SetHandsOnFeedback(string message)
        {
            ActiveSiteName = "CONSTRUCTION SITE";
            HudFeedback = message;
            LastFeedback = message;
            TrainingHud.Instance?.SetVisible(true);
        }

        public void SetContextFeedback(string message)
        {
            HudFeedback = message;
            LastFeedback = message;
            TrainingHud.Instance?.SetVisible(true);
        }

        public void RecordCoachTurn(TrainingSiteId siteId)
        {
            guidedPlan.RecordCoachTurn(siteId);
        }

        public string GetProgress(TrainingSiteId siteId)
        {
            if (!sessions.TryGetValue(siteId, out var session))
                return "not started";

            return $"{session.HazardsFound}/{session.HazardsRequired} hazards | " +
                   $"score {session.Score} | false positives {session.FalsePositives}";
        }

        public string GetDebrief(TrainingSiteId siteId)
        {
            if (!sessions.TryGetValue(siteId, out var session))
                return "This site has not started.";
            if (!session.IsComplete)
                return $"Continue inspecting. {session.HazardsRequired - session.HazardsFound} hazard(s) remain.";

            return $"Site complete with {session.Score} points and {session.FalsePositives} false positive(s).";
        }

        public void EnterSite(TrainingSiteId siteId)
        {
            ActiveSite = siteId;
            var siteName = siteId switch
            {
                TrainingSiteId.FireResponse => "Fire Response",
                TrainingSiteId.ChemicalProcessing => "Chemical Processing",
                TrainingSiteId.ElectricalMaintenance => "Electrical Maintenance",
                _ => siteId.ToString()
            };
            ActiveSiteName = siteName.ToUpperInvariant();
            LastFeedback = "4 checks | 2 coach questions | 4-minute field review.";
            HudFeedback = LastFeedback;
            practicals?.Begin(siteId);
            SiteIsolationController.Instance?.ShowSite(siteId);
            TrainingHud.Instance?.SetVisible(true);
        }

        public void LeaveSite(TrainingSiteId siteId)
        {
            if (ActiveSite != siteId)
                return;
            ActiveSite = null;
            ActiveSiteName = "TRAINING CAMPUS";
            HudFeedback = "Approach a work zone to begin a supervised safety review.";
            LastFeedback = HudFeedback;
            SiteIsolationController.Instance?.ShowHub();
            TrainingHud.Instance?.SetVisible(false);
        }

        void BuildSessions()
        {
            var targets = FindObjectsByType<InspectionTarget>(FindObjectsSortMode.None);
            foreach (TrainingSiteId siteId in Enum.GetValues(typeof(TrainingSiteId)))
            {
                var specs = new List<InspectionTargetSpec>();
                foreach (var target in targets)
                    if (target.SiteId == siteId)
                        specs.Add(new InspectionTargetSpec(target.TargetId, target.IsHazard));

                if (specs.Count > 0)
                {
                    sessions.Add(siteId, new TrainingSession(siteId, specs));
                    TotalConditionCount += specs.Count;
                }
            }
        }

        int CalculateOverallScore()
        {
            var total = 0;
            foreach (var session in sessions.Values)
                total += session.Score;
            return total + handsOnBonus;
        }

        string FormatFeedback(InspectionTarget target, InspectionResult result)
        {
            switch (result.Outcome)
            {
                case InspectionOutcome.CorrectHazard when result.IsComplete:
                    return $"Correct +100: {target.DisplayName}. Site complete ({result.Score} points).\n" +
                           $"{target.Rationale}\nControl: {target.CorrectiveAction}";
                case InspectionOutcome.CorrectHazard:
                    return $"Correct +100: {target.DisplayName} ({result.HazardsFound}/{result.HazardsRequired}).\n" +
                           $"{target.Rationale}\nControl: {target.CorrectiveAction}";
                case InspectionOutcome.SafeObjectSelected:
                    return $"False positive -25: {target.DisplayName} is controlled.\n" +
                           $"{target.Rationale}\nSite score: {result.Score}.";
                case InspectionOutcome.AlreadyInspected:
                    return "That condition has already been recorded.";
                case InspectionOutcome.UnknownTarget:
                    return "That object is not part of the current inspection.";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static string FormatHudFeedback(InspectionTarget target, InspectionResult result)
        {
            return result.Outcome switch
            {
                InspectionOutcome.CorrectHazard when result.IsComplete =>
                    $"Correct +100 | {target.DisplayName}\nSite complete: {result.Score} points.",
                InspectionOutcome.CorrectHazard =>
                    $"Correct +100 | {target.DisplayName} " +
                    $"({result.HazardsFound}/{result.HazardsRequired})\nControl: {target.CorrectiveAction}",
                InspectionOutcome.SafeObjectSelected =>
                    $"False positive -25 | {target.DisplayName} is controlled.\nSite score: {result.Score}.",
                InspectionOutcome.AlreadyInspected => "That condition is already recorded.",
                InspectionOutcome.UnknownTarget => "That object is outside the current inspection.",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
