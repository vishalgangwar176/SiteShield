using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class SiteExperienceZone : MonoBehaviour
    {
        [SerializeField] TrainingSiteId siteId;

        public TrainingSiteId SiteId => siteId;
        public NpcSiteCompanion Companion { get; private set; }

        public void Configure(TrainingSiteId value)
        {
            siteId = value;
        }

        public void BindCompanion(NpcSiteCompanion companion)
        {
            Companion = companion;
        }

        public float HorizontalDistance(Vector3 point)
        {
            var delta = point - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
