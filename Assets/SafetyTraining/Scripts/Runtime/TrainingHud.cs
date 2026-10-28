using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    public sealed class TrainingHud : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Bindings
        {
            public CanvasGroup CanvasGroup;
            public Text SiteDisplay;
            public Text ScoreDisplay;
            public Text StateDisplay;
            public Text FeedbackDisplay;
            public Text ProgressDisplay;
            public RectTransform ProgressFill;
            public Image StateIcon;
        }

        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Text siteDisplay;
        [SerializeField] Text scoreDisplay;
        [SerializeField] Text stateDisplay;
        [SerializeField] Text feedbackDisplay;
        [SerializeField] Text progressDisplay;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Image stateIcon;
        string shownFeedback;
        static readonly Color Accent = new Color(0.133f, 0.827f, 0.773f); // #22D3C5
        static readonly Color Warning = new Color(1.0f, 0.761f, 0.102f); // #FFC21A
        static readonly Color Danger = new Color(1.0f, 0.302f, 0.302f); // #FF4D4D
        static readonly Color Success = new Color(0.180f, 0.800f, 0.443f); // #2ECC71
        static readonly Color MandatoryActionBlue = new Color(0.435f, 0.690f, 1.0f); // #6FB0FF

        public static TrainingHud Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            var coordinator = TrainingCoordinator.Instance;
            if (coordinator == null || feedbackDisplay == null)
                return;

            var elapsedSeconds = Mathf.FloorToInt(coordinator.SessionElapsedSeconds);
            var stateKey = $"{coordinator.OverallScore}|{coordinator.HudFeedback}|" +
                           $"{coordinator.ActiveSiteName}|{coordinator.CertificationComplete}|" +
                           $"{coordinator.ReviewedConditionCount}|{elapsedSeconds}";
            if (shownFeedback == stateKey)
                return;

            shownFeedback = stateKey;
            feedbackDisplay.text = coordinator.HudFeedback;
            if (siteDisplay != null)
                siteDisplay.text = coordinator.CertificationComplete ? "MISSION COMPLETE" : coordinator.ActiveSiteName;
            if (scoreDisplay != null)
                scoreDisplay.text = coordinator.OverallScore.ToString("0000");
            if (progressDisplay != null)
                progressDisplay.text = $"SESSION {elapsedSeconds / 60:00}:{elapsedSeconds % 60:00} / 20:00";
            if (progressFill != null)
            {
                progressFill.localScale = new Vector3(Mathf.Max(0.02f, coordinator.GuidedProgress01), 1f, 1f);
            }
            ApplyState(coordinator.HudFeedback, coordinator.CertificationComplete);
        }

        public void Configure(Bindings bindings)
        {
            canvasGroup = bindings.CanvasGroup;
            siteDisplay = bindings.SiteDisplay;
            scoreDisplay = bindings.ScoreDisplay;
            stateDisplay = bindings.StateDisplay;
            feedbackDisplay = bindings.FeedbackDisplay;
            progressDisplay = bindings.ProgressDisplay;
            progressFill = bindings.ProgressFill;
            stateIcon = bindings.StateIcon;
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
        }

        void ApplyState(string feedback, bool complete)
        {
            var label = "Mission Brief";
            var color = Accent;
            if (complete)
            {
                label = "Review Complete";
                color = Success;
            }
            else if (feedback.Contains("False positive", System.StringComparison.OrdinalIgnoreCase))
            {
                label = "Review Note";
                color = Warning;
            }
            else if (feedback.Contains("PPE", System.StringComparison.OrdinalIgnoreCase))
            {
                label = "Mandatory Action";
                color = MandatoryActionBlue;
            }
            else if (feedback.Contains("Correct") || feedback.Contains("complete", System.StringComparison.OrdinalIgnoreCase))
            {
                label = "Control Verified";
                color = Success;
            }
            else if (feedback.Contains("hazard", System.StringComparison.OrdinalIgnoreCase))
            {
                label = "Hazard Review";
                color = Danger;
            }
            if (stateDisplay != null)
            {
                stateDisplay.text = label;
                stateDisplay.color = color;
                stateDisplay.fontStyle = FontStyle.Bold;
                stateDisplay.fontSize = 13; // 60% of instruction size (21px)
            }
            if (stateIcon != null)
                stateIcon.color = color;
        }
    }
}
