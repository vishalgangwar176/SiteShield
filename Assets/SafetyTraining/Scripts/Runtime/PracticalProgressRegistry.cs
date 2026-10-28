using System.Linq;
using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    internal sealed class PracticalProgressRegistry
    {
        readonly ConstructionHandsOnController construction;
        readonly SitePracticalController[] sitePracticals;

        public PracticalProgressRegistry()
        {
            construction = Object.FindFirstObjectByType<ConstructionHandsOnController>();
            sitePracticals = Object.FindObjectsByType<SitePracticalController>(FindObjectsSortMode.None);
        }

        public int CompletedSteps =>
            (construction?.CompletedSteps ?? 0) + sitePracticals.Sum(item => item.CompletedSteps);

        public int TotalSteps =>
            (construction?.TotalSteps ?? 0) + sitePracticals.Sum(item => item.TotalSteps);

        public bool IsComplete => construction != null && construction.IsComplete &&
                                  sitePracticals.Length == 4 && sitePracticals.All(item => item.IsComplete);

        public float Progress01 => (float)CompletedSteps / Mathf.Max(1, TotalSteps);

        public void Begin(TrainingSiteId siteId)
        {
            if (siteId == TrainingSiteId.Construction)
            {
                construction?.Begin();
                return;
            }

            sitePracticals.FirstOrDefault(item => item.SiteId == siteId)?.Begin();
        }
    }
}
