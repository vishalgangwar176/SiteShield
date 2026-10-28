using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class SiteExperienceDirector : MonoBehaviour
    {
        [SerializeField, Min(1f)] float enterRadius = 9.5f;
        [SerializeField, Min(1f)] float exitRadius = 11f;
        [SerializeField, Min(0.05f)] float sampleInterval = 0.12f;

        SiteExperienceZone[] zones;
        SiteExperienceZone activeZone;
        Camera viewer;
        float nextSample;

        void Start()
        {
            zones = FindObjectsByType<SiteExperienceZone>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            viewer = Camera.main;
            TrainingHud.Instance?.SetVisible(false);
        }

        void Update()
        {
            if (viewer == null)
                viewer = Camera.main;
            if (viewer == null || zones == null)
                return;

            if (Input.GetKeyDown(KeyCode.C) && activeZone != null)
                activeZone.Companion?.TryTalk();
            if (Time.unscaledTime < nextSample)
                return;

            nextSample = Time.unscaledTime + sampleInterval;
            UpdateActiveZone(viewer.transform.position);
        }

        void UpdateActiveZone(Vector3 playerPosition)
        {
            if (activeZone != null && activeZone.HorizontalDistance(playerPosition) <= exitRadius)
                return;

            var nearest = FindNearest(playerPosition, out var nearestDistance);
            if (nearestDistance <= enterRadius)
            {
                if (nearest != activeZone)
                    SwitchZone(nearest);
                return;
            }

            SwitchZone(null);
        }

        SiteExperienceZone FindNearest(Vector3 playerPosition, out float distance)
        {
            SiteExperienceZone nearest = null;
            distance = float.PositiveInfinity;
            foreach (var zone in zones)
            {
                var candidate = zone.HorizontalDistance(playerPosition);
                if (candidate >= distance)
                    continue;
                nearest = zone;
                distance = candidate;
            }
            return nearest;
        }

        void SwitchZone(SiteExperienceZone next)
        {
            if (activeZone == next)
                return;

            if (activeZone != null)
            {
                activeZone.Companion?.SetAccompanying(false);
                TrainingCoordinator.Instance?.LeaveSite(activeZone.SiteId);
                NpcChatPanel.Instance?.Close();
            }

            activeZone = next;
            if (activeZone == null)
                return;

            TrainingCoordinator.Instance?.EnterSite(activeZone.SiteId);
            activeZone.Companion?.SetAccompanying(true);
        }
    }
}
