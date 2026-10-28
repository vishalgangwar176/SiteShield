using System;
using System.IO;
using Newtonsoft.Json;
using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class TrainingEventLogger : MonoBehaviour
    {
        string logPath;
        string sessionId;

        void Awake()
        {
            EnsureLogPath();
        }

        void EnsureLogPath()
        {
            if (!string.IsNullOrEmpty(logPath))
                return;
            sessionId = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Application.persistentDataPath, "SafetyTrainingLogs");
            Directory.CreateDirectory(directory);
            logPath = Path.Combine(directory, $"session_{sessionId}.jsonl");
        }

        public void Record(InspectionTarget target, InspectionResult result, int overallScore)
        {
            EnsureLogPath();
            var entry = new TrainingEvent
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                sessionId = sessionId,
                site = target.SiteId.ToString(),
                targetId = target.TargetId,
                isHazard = target.IsHazard,
                outcome = result.Outcome.ToString(),
                scoreDelta = result.ScoreDelta,
                siteScore = result.Score,
                overallScore = overallScore,
                hazardsFound = result.HazardsFound,
                hazardsRequired = result.HazardsRequired,
                siteComplete = result.IsComplete
            };
            File.AppendAllText(logPath, JsonConvert.SerializeObject(entry) + Environment.NewLine);
        }

        public void RecordPlacement(TrainingSiteId siteId, int stepIndex, string actionName,
            float releaseDistance, bool success, string inputMode)
        {
            EnsureLogPath();
            var entry = new PlacementEvent
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                sessionId = sessionId,
                eventType = "placement_attempt",
                site = siteId.ToString(),
                stepIndex = stepIndex,
                actionName = actionName,
                releaseDistance = releaseDistance,
                success = success,
                inputMode = inputMode
            };
            File.AppendAllText(logPath, JsonConvert.SerializeObject(entry) + Environment.NewLine);
        }

        [Serializable]
        sealed class TrainingEvent
        {
            public string timestampUtc = string.Empty;
            public string sessionId = string.Empty;
            public string site = string.Empty;
            public string targetId = string.Empty;
            public bool isHazard;
            public string outcome = string.Empty;
            public int scoreDelta;
            public int siteScore;
            public int overallScore;
            public int hazardsFound;
            public int hazardsRequired;
            public bool siteComplete;
        }

        [Serializable]
        sealed class PlacementEvent
        {
            public string timestampUtc = string.Empty;
            public string sessionId = string.Empty;
            public string eventType = string.Empty;
            public string site = string.Empty;
            public int stepIndex;
            public string actionName = string.Empty;
            public float releaseDistance;
            public bool success;
            public string inputMode = string.Empty;
        }
    }
}
