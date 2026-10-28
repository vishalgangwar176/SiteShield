using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class SiteIsolationController : MonoBehaviour
    {
        [SerializeField] Transform[] hubRoots;
        [SerializeField] SiteExperienceZone[] sites;

        public static SiteIsolationController Instance { get; private set; }
        public int SiteCount => sites?.Length ?? 0;
        public int HubRootCount => hubRoots?.Length ?? 0;
        public bool HubVisible
        {
            get
            {
                if (hubRoots == null || hubRoots.Length == 0)
                    return false;
                foreach (var root in hubRoots)
                    if (root == null || !root.gameObject.activeSelf)
                        return false;
                return true;
            }
        }

        public int VisibleSiteCount
        {
            get
            {
                var count = 0;
                if (sites == null)
                    return count;
                foreach (var site in sites)
                    if (site != null && site.gameObject.activeSelf)
                        count++;
                return count;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            ShowHub();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Transform[] trainingHubRoots, SiteExperienceZone[] trainingSites)
        {
            hubRoots = trainingHubRoots;
            sites = trainingSites;
        }

        public void ShowHub()
        {
            SetHubActive(true);
            SetOnlySiteActive(null);
            Debug.Log("Site isolation state: training hub only.");
        }

        public void ShowSite(TrainingSiteId siteId)
        {
            SetHubActive(false);
            SiteExperienceZone selected = null;
            if (sites != null)
            {
                foreach (var site in sites)
                    if (site != null && site.SiteId == siteId)
                        selected = site;
            }
            SetOnlySiteActive(selected);
            Debug.Log($"Site isolation state: {siteId} only.");
        }

        public bool IsSiteVisible(TrainingSiteId siteId)
        {
            if (sites == null)
                return false;
            foreach (var site in sites)
                if (site != null && site.SiteId == siteId)
                    return site.gameObject.activeSelf;
            return false;
        }

        void SetHubActive(bool active)
        {
            if (hubRoots == null)
                return;
            foreach (var root in hubRoots)
                if (root != null)
                    root.gameObject.SetActive(active);
        }

        void SetOnlySiteActive(SiteExperienceZone selected)
        {
            if (sites == null)
                return;
            foreach (var site in sites)
                if (site != null)
                    site.gameObject.SetActive(site == selected);
        }
    }
}
