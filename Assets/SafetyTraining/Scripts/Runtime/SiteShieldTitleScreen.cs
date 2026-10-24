using System;
using SafetyTraining.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    /// <summary>
    /// Professional Main Menu & Title Screen for SiteShield.
    /// Manages the opening brand experience, module quick navigation,
    /// accessibility/audio settings, and gameplay pause/resume flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SiteShieldTitleScreen : MonoBehaviour
    {
        public static SiteShieldTitleScreen Instance { get; private set; }

        [Header("Canvases & Groups")]
        [SerializeField] Canvas rootCanvas;
        [SerializeField] CanvasGroup mainCanvasGroup;
        [SerializeField] GameObject screenBackdrop;
        [SerializeField] GameObject titleScreenView;
        [SerializeField] GameObject moduleSelectView;
        [SerializeField] GameObject settingsView;
        [SerializeField] GameObject pauseBanner;

        [Header("Primary Buttons")]
        [SerializeField] Button btnEnterCampus;
        [SerializeField] Button btnModuleAccess;
        [SerializeField] Button btnSettings;
        [SerializeField] Button btnExit;
        [SerializeField] Button btnModuleBack;
        [SerializeField] Button btnSettingsBack;
        [SerializeField] Button[] moduleButtons;

        [Header("Settings Controls")]
        [SerializeField] Slider volumeSlider;
        [SerializeField] Toggle muteToggle;
        [SerializeField] Toggle reducedMotionToggle;
        [SerializeField] Slider lookSensitivitySlider;

        bool isOpen = true;
        bool isPausedMode = false;
        float fadeSpeed = 5.0f;
        float targetAlpha = 1f;

        public bool IsOpen => isOpen;
        public bool ReducedMotion { get; private set; } = false;
        public float LookSensitivity { get; private set; } = 1.0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (rootCanvas == null)
                rootCanvas = GetComponent<Canvas>();

            if (mainCanvasGroup == null)
                mainCanvasGroup = GetComponent<CanvasGroup>();

            BindControls();
            LoadSettings();
        }

        void Start()
        {
            // By default, open on launch as Title Screen
            ShowTitleMenu();
        }

        void OnDestroy()
        {
            UnbindControls();
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            // Smooth fade transition
            if (mainCanvasGroup != null && Mathf.Abs(mainCanvasGroup.alpha - targetAlpha) > 0.001f)
            {
                if (ReducedMotion)
                    mainCanvasGroup.alpha = targetAlpha;
                else
                    mainCanvasGroup.alpha = Mathf.MoveTowards(mainCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);

                mainCanvasGroup.blocksRaycasts = mainCanvasGroup.alpha > 0.05f;
                mainCanvasGroup.interactable = mainCanvasGroup.alpha > 0.8f;

                if (targetAlpha == 0f && mainCanvasGroup.alpha <= 0.01f)
                {
                    ApplyClosedVisuals();
                }
            }

            // Keyboard shortcuts: Esc or M toggles menu during gameplay
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            {
                // Only toggle if chat is not open or if menu is open
                var chatOpen = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;
                if (!chatOpen)
                {
                    TogglePauseMenu();
                }
            }
        }

        public void BindControls()
        {
            if (btnEnterCampus != null)
            {
                btnEnterCampus.onClick.RemoveListener(StartCampusTraining);
                btnEnterCampus.onClick.AddListener(StartCampusTraining);
            }
            if (btnModuleAccess != null)
            {
                btnModuleAccess.onClick.RemoveListener(ShowModuleSelect);
                btnModuleAccess.onClick.AddListener(ShowModuleSelect);
            }
            if (btnSettings != null)
            {
                btnSettings.onClick.RemoveListener(ShowSettings);
                btnSettings.onClick.AddListener(ShowSettings);
            }
            if (btnExit != null)
            {
                btnExit.onClick.RemoveListener(ExitSimulator);
                btnExit.onClick.AddListener(ExitSimulator);
            }
            if (btnModuleBack != null)
            {
                btnModuleBack.onClick.RemoveListener(ShowTitleMenu);
                btnModuleBack.onClick.AddListener(ShowTitleMenu);
            }
            if (btnSettingsBack != null)
            {
                btnSettingsBack.onClick.RemoveListener(ShowTitleMenu);
                btnSettingsBack.onClick.AddListener(ShowTitleMenu);
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(OnMuteToggled);
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            }
            if (reducedMotionToggle != null)
            {
                reducedMotionToggle.onValueChanged.RemoveListener(OnReducedMotionToggled);
                reducedMotionToggle.onValueChanged.AddListener(OnReducedMotionToggled);
            }
            if (lookSensitivitySlider != null)
            {
                lookSensitivitySlider.onValueChanged.RemoveListener(OnLookSensitivityChanged);
                lookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivityChanged);
            }

            if (moduleButtons != null)
            {
                for (int i = 0; i < moduleButtons.Length; i++)
                {
                    var btn = moduleButtons[i];
                    if (btn != null)
                    {
                        var siteIndex = i;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => LaunchModule(siteIndex));
                    }
                }
            }
        }

        public void UnbindControls()
        {
            if (btnEnterCampus != null) btnEnterCampus.onClick.RemoveListener(StartCampusTraining);
            if (btnModuleAccess != null) btnModuleAccess.onClick.RemoveListener(ShowModuleSelect);
            if (btnSettings != null) btnSettings.onClick.RemoveListener(ShowSettings);
            if (btnExit != null) btnExit.onClick.RemoveListener(ExitSimulator);
            if (btnModuleBack != null) btnModuleBack.onClick.RemoveListener(ShowTitleMenu);
            if (btnSettingsBack != null) btnSettingsBack.onClick.RemoveListener(ShowTitleMenu);
        }

        public void ShowTitleMenu()
        {
            isOpen = true;
            targetAlpha = 1f;

            if (rootCanvas != null) rootCanvas.enabled = true;
            if (screenBackdrop != null) screenBackdrop.SetActive(true);
            if (titleScreenView != null) titleScreenView.SetActive(true);
            if (moduleSelectView != null) moduleSelectView.SetActive(false);
            if (settingsView != null) settingsView.SetActive(false);
            if (pauseBanner != null) pauseBanner.SetActive(isPausedMode);

            if (mainCanvasGroup != null)
            {
                if (ReducedMotion)
                    mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            SafetyAudioManager.Instance?.PlayHover();
        }

        public void ShowModuleSelect()
        {
            if (rootCanvas != null) rootCanvas.enabled = true;
            if (screenBackdrop != null) screenBackdrop.SetActive(true);
            if (titleScreenView != null) titleScreenView.SetActive(false);
            if (moduleSelectView != null) moduleSelectView.SetActive(true);
            if (settingsView != null) settingsView.SetActive(false);

            SafetyAudioManager.Instance?.PlayClick();
        }

        public void ShowSettings()
        {
            if (rootCanvas != null) rootCanvas.enabled = true;
            if (screenBackdrop != null) screenBackdrop.SetActive(true);
            if (titleScreenView != null) titleScreenView.SetActive(false);
            if (moduleSelectView != null) moduleSelectView.SetActive(false);
            if (settingsView != null) settingsView.SetActive(true);

            SafetyAudioManager.Instance?.PlayClick();
        }

        public void StartCampusTraining()
        {
            SafetyAudioManager.Instance?.PlayClick();
            CloseMenu();
            TrainingCoordinator.Instance?.LeaveSite(TrainingSiteId.Construction);
        }

        public void LaunchModule(int siteIdInt)
        {
            var siteId = (TrainingSiteId)siteIdInt;
            SafetyAudioManager.Instance?.PlayPortal();
            CloseMenu();

            var portals = FindObjectsByType<SitePortal>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                if (!portal.ReturnsToHub && portal.DestinationSite == siteId)
                {
                    portal.Activate();
                    break;
                }
            }
        }

        public void CloseMenu()
        {
            isOpen = false;
            targetAlpha = 0f;
            isPausedMode = true; // next time opened, it acts as pause menu
            SafetyAudioManager.Instance?.PlayClick();

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }

            if (ReducedMotion)
            {
                ApplyClosedVisuals();
            }
        }

        public void ApplyClosedVisuals()
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }
            if (titleScreenView != null) titleScreenView.SetActive(false);
            if (moduleSelectView != null) moduleSelectView.SetActive(false);
            if (settingsView != null) settingsView.SetActive(false);
            if (screenBackdrop != null) screenBackdrop.SetActive(false);
            if (rootCanvas != null) rootCanvas.enabled = false;
        }

        public void TogglePauseMenu()
        {
            if (isOpen)
                CloseMenu();
            else
            {
                isPausedMode = true;
                ShowTitleMenu();
            }
        }

        public void ExitSimulator()
        {
            SafetyAudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ==========================================
        // Settings Handlers
        // ==========================================

        public void OnVolumeChanged(float value)
        {
            if (SafetyAudioManager.Instance != null)
                SafetyAudioManager.Instance.masterVolume = value;
            PlayerPrefs.SetFloat("SS_MasterVolume", value);
        }

        public void OnMuteToggled(bool muted)
        {
            if (SafetyAudioManager.Instance != null)
                SafetyAudioManager.Instance.isMuted = muted;
            PlayerPrefs.SetInt("SS_Mute", muted ? 1 : 0);
        }

        public void OnReducedMotionToggled(bool value)
        {
            ReducedMotion = value;
            PlayerPrefs.SetInt("SS_ReducedMotion", value ? 1 : 0);
        }

        public void OnLookSensitivityChanged(float value)
        {
            LookSensitivity = value;
            PlayerPrefs.SetFloat("SS_LookSensitivity", value);
        }

        void LoadSettings()
        {
            var vol = PlayerPrefs.GetFloat("SS_MasterVolume", 0.85f);
            var mute = PlayerPrefs.GetInt("SS_Mute", 0) == 1;
            var redMo = PlayerPrefs.GetInt("SS_ReducedMotion", 0) == 1;
            var sens = PlayerPrefs.GetFloat("SS_LookSensitivity", 1.0f);

            ReducedMotion = redMo;
            LookSensitivity = sens;

            if (volumeSlider != null) volumeSlider.value = vol;
            if (muteToggle != null) muteToggle.isOn = mute;
            if (reducedMotionToggle != null) reducedMotionToggle.isOn = redMo;
            if (lookSensitivitySlider != null) lookSensitivitySlider.value = sens;

            if (SafetyAudioManager.Instance != null)
            {
                SafetyAudioManager.Instance.masterVolume = vol;
                SafetyAudioManager.Instance.isMuted = mute;
            }
        }
    }
}
