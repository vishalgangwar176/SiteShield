using System;
using System.Collections.Generic;

namespace SafetyTraining.Core
{
    public sealed class GuidedSessionPlan
    {
        public const float MinimumSessionSeconds = 20f * 60f;
        public const float MinimumSiteSeconds = 4f * 60f;
        public const int RequiredCoachTurnsPerSite = 2;

        readonly Dictionary<TrainingSiteId, float> siteSeconds = new();
        readonly Dictionary<TrainingSiteId, int> coachTurns = new();

        public GuidedSessionPlan()
        {
            foreach (TrainingSiteId site in Enum.GetValues(typeof(TrainingSiteId)))
            {
                siteSeconds.Add(site, 0f);
                coachTurns.Add(site, 0);
            }
        }

        public float ElapsedSeconds { get; private set; }
        public int CompletedCoachTurns
        {
            get
            {
                var total = 0;
                foreach (var turns in coachTurns.Values)
                    total += Math.Min(turns, RequiredCoachTurnsPerSite);
                return total;
            }
        }

        public bool IsComplete
        {
            get
            {
                if (ElapsedSeconds < MinimumSessionSeconds)
                    return false;
                foreach (TrainingSiteId site in Enum.GetValues(typeof(TrainingSiteId)))
                    if (siteSeconds[site] < MinimumSiteSeconds ||
                        coachTurns[site] < RequiredCoachTurnsPerSite)
                        return false;
                return true;
            }
        }

        public void Advance(TrainingSiteId? activeSite, float deltaSeconds)
        {
            if (activeSite == null || deltaSeconds <= 0f)
                return;
            siteSeconds[activeSite.Value] += deltaSeconds;
            ElapsedSeconds += deltaSeconds;
        }

        public void RecordCoachTurn(TrainingSiteId site)
        {
            coachTurns[site]++;
        }

        public float GetSiteSeconds(TrainingSiteId site) => siteSeconds[site];

        public int GetCoachTurns(TrainingSiteId site) => coachTurns[site];
    }
}
