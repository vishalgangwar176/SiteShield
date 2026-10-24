using System;
using System.Collections.Generic;

namespace SafetyTraining.Core
{
    public enum SafetyTaskType
    {
        HazardIdentification,
        PhysicalControl,
        PpeVerification,
        VerificationSignOff
    }

    [Serializable]
    public sealed class SafetyTaskDefinition
    {
        public string TaskId;
        public string Title;
        public string Description;
        public TrainingSiteId SiteId;
        public int SequenceOrder;
        public SafetyTaskType Type;
        public string TargetId;
        public string HintText;
        public int Points;
        public int Penalty;
        public bool IsCompleted;
        public float CompletedTimestamp;

        public SafetyTaskDefinition(
            string taskId,
            string title,
            string description,
            TrainingSiteId siteId,
            int sequenceOrder,
            SafetyTaskType type,
            string targetId,
            string hintText,
            int points = 100,
            int penalty = 25)
        {
            TaskId = taskId;
            Title = title;
            Description = description;
            SiteId = siteId;
            SequenceOrder = sequenceOrder;
            Type = type;
            TargetId = targetId;
            HintText = hintText;
            Points = points;
            Penalty = penalty;
            IsCompleted = false;
            CompletedTimestamp = 0f;
        }
    }

    [Serializable]
    public sealed class ModuleAssessmentSummary
    {
        public TrainingSiteId SiteId;
        public string SiteName;
        public int TotalTasks;
        public int CompletedTasks;
        public int HazardsFound;
        public int TotalHazards;
        public int Score;
        public int FalsePositives;
        public float ElapsedTimeSeconds;
        public string PerformanceRating;
        public string KeyTakeaway;

        public bool IsPassed => CompletedTasks >= TotalTasks && FalsePositives <= 2;
        public float CompletionPercentage => TotalTasks > 0 ? (float)CompletedTasks / TotalTasks : 0f;
    }
}
