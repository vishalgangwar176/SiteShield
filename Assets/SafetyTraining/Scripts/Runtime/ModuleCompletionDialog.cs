using System;
using SafetyTraining.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    public sealed class ModuleCompletionDialog : MonoBehaviour
    {
        public static ModuleCompletionDialog Instance { get; private set; }

        [System.Serializable]
        public sealed class Bindings
        {
            public GameObject ModalPanel;
            public Text ModuleTitle;
            public Text RatingLabel;
            public Text ScoreLabel;
            public Text HazardsLabel;
            public Text TasksLabel;
            public Text SummaryText;
            public Button ReturnCampusButton;
            public Button ContinueButton;
        }

        [SerializeField] GameObject modalPanel;
        [SerializeField] Text moduleTitle;
        [SerializeField] Text ratingLabel;
        [SerializeField] Text scoreLabel;
        [SerializeField] Text hazardsLabel;
        [SerializeField] Text tasksLabel;
        [SerializeField] Text summaryText;
        [SerializeField] Button returnCampusButton;
        [SerializeField] Button continueButton;

        TrainingSiteId currentSite;

        void Awake()
        {
            Instance = this;
            if (modalPanel != null)
                modalPanel.SetActive(false);
            BindEvents();
        }

        void Start()
        {
            if (SafetyTaskManager.Instance != null)
            {
                SafetyTaskManager.Instance.ModuleAssessmentCompleted += OnAssessmentCompleted;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Bindings bindings)
        {
            modalPanel = bindings.ModalPanel;
            moduleTitle = bindings.ModuleTitle;
            ratingLabel = bindings.RatingLabel;
            scoreLabel = bindings.ScoreLabel;
            hazardsLabel = bindings.HazardsLabel;
            tasksLabel = bindings.TasksLabel;
            summaryText = bindings.SummaryText;
            returnCampusButton = bindings.ReturnCampusButton;
            continueButton = bindings.ContinueButton;

            BindEvents();
        }

        void BindEvents()
        {
            if (returnCampusButton != null)
            {
                returnCampusButton.onClick.RemoveAllListeners();
                returnCampusButton.onClick.AddListener(OnReturnCampusClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(Close);
            }
        }

        void OnAssessmentCompleted(TrainingSiteId siteId, ModuleAssessmentSummary summary)
        {
            DisplaySummary(summary);
        }

        public void DisplaySummary(ModuleAssessmentSummary summary)
        {
            currentSite = summary.SiteId;

            if (moduleTitle != null)
                moduleTitle.text = summary.SiteName.ToUpperInvariant();

            if (ratingLabel != null)
            {
                ratingLabel.text = summary.PerformanceRating;
                ratingLabel.color = summary.IsPassed
                    ? new Color(0.180f, 0.800f, 0.443f)
                    : new Color(1.0f, 0.761f, 0.102f);
            }

            if (scoreLabel != null)
                scoreLabel.text = $"SCORE: {summary.Score:0000} PTS";

            if (hazardsLabel != null)
                hazardsLabel.text = $"HAZARDS: {summary.HazardsFound}/{summary.TotalHazards} FOUND";

            if (tasksLabel != null)
                tasksLabel.text = $"CONTROLS: {summary.CompletedTasks}/{summary.TotalTasks} VERIFIED";

            if (summaryText != null)
                summaryText.text = summary.KeyTakeaway;

            if (modalPanel != null)
                modalPanel.SetActive(true);

            SafetyAudioManager.Instance?.PlayObjectiveComplete();
        }

        public void Close()
        {
            if (modalPanel != null)
                modalPanel.SetActive(false);

            SafetyAudioManager.Instance?.PlayClick();
        }

        void OnReturnCampusClicked()
        {
            Close();
            TrainingCoordinator.Instance?.ReturnToCampus();
            SafetyAudioManager.Instance?.PlayClick();
        }
    }
}
