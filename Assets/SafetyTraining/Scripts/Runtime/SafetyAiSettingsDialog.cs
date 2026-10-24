using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    public sealed class SafetyAiSettingsDialog : MonoBehaviour
    {
        public static SafetyAiSettingsDialog Instance { get; private set; }

        [System.Serializable]
        public sealed class Bindings
        {
            public GameObject ModalPanel;
            public Dropdown ProviderDropdown;
            public InputField EndpointInput;
            public InputField ModelInput;
            public InputField ApiKeyInput;
            public Toggle EnabledToggle;
            public Text StatusLabel;
            public Image StatusIndicatorDot;
            public Button TestButton;
            public Text TestButtonLabel;
            public Button SaveButton;
            public Button CloseButton;
        }

        [SerializeField] GameObject modalPanel;
        [SerializeField] Dropdown providerDropdown;
        [SerializeField] InputField endpointInput;
        [SerializeField] InputField modelInput;
        [SerializeField] InputField apiKeyInput;
        [SerializeField] Toggle enabledToggle;
        [SerializeField] Text statusLabel;
        [SerializeField] Image statusIndicatorDot;
        [SerializeField] Button testButton;
        [SerializeField] Text testButtonLabel;
        [SerializeField] Button saveButton;
        [SerializeField] Button closeButton;

        SafetyAiConfig currentConfig;
        CancellationTokenSource testCancellation;
        bool isTesting = false;

        static readonly Color DotNeutral = new Color(0.5f, 0.55f, 0.6f);
        static readonly Color DotTesting = new Color(0.184f, 0.490f, 1.0f);
        static readonly Color DotSuccess = new Color(0.180f, 0.800f, 0.443f);
        static readonly Color DotError = new Color(0.95f, 0.28f, 0.22f);

        public bool IsOpen => modalPanel != null && modalPanel.activeSelf;

        void Awake()
        {
            Instance = this;
            if (modalPanel != null)
                modalPanel.SetActive(false);
        }

        void OnDestroy()
        {
            CancelTest();
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Bindings bindings)
        {
            modalPanel = bindings.ModalPanel;
            providerDropdown = bindings.ProviderDropdown;
            endpointInput = bindings.EndpointInput;
            modelInput = bindings.ModelInput;
            apiKeyInput = bindings.ApiKeyInput;
            enabledToggle = bindings.EnabledToggle;
            statusLabel = bindings.StatusLabel;
            statusIndicatorDot = bindings.StatusIndicatorDot;
            testButton = bindings.TestButton;
            testButtonLabel = bindings.TestButtonLabel;
            saveButton = bindings.SaveButton;
            closeButton = bindings.CloseButton;

            BindEvents();
        }

        void BindEvents()
        {
            if (providerDropdown != null)
            {
                providerDropdown.onValueChanged.RemoveAllListeners();
                providerDropdown.onValueChanged.AddListener(OnProviderChanged);
            }

            if (testButton != null)
            {
                testButton.onClick.RemoveAllListeners();
                testButton.onClick.AddListener(OnTestClicked);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Open()
        {
            currentConfig = SafetyAiConfig.Load();
            PopulateUiFromConfig(currentConfig);
            SetStatus("Ready to test or configure AI provider.", DotNeutral);

            if (modalPanel != null)
                modalPanel.SetActive(true);

            SafetyAudioManager.Instance?.PlayClick();
        }

        public void Close()
        {
            CancelTest();
            if (modalPanel != null)
                modalPanel.SetActive(false);

            SafetyAudioManager.Instance?.PlayClick();
        }

        void PopulateUiFromConfig(SafetyAiConfig config)
        {
            if (providerDropdown != null)
                providerDropdown.value = (int)config.Provider;

            if (endpointInput != null)
                endpointInput.text = config.Endpoint;

            if (modelInput != null)
                modelInput.text = config.Model;

            if (apiKeyInput != null)
                apiKeyInput.text = config.ApiKey;

            if (enabledToggle != null)
                enabledToggle.isOn = config.Enabled;
        }

        void OnProviderChanged(int index)
        {
            var provider = (AiProviderType)index;
            if (endpointInput != null)
                endpointInput.text = SafetyAiConfig.GetDefaultEndpoint(provider);

            if (modelInput != null)
                modelInput.text = SafetyAiConfig.GetDefaultModel(provider);

            SetStatus($"Switched provider to {provider}. Update credentials if required.", DotNeutral);
        }

        public async void OnTestClicked()
        {
            if (isTesting)
                return;

            isTesting = true;
            CancelTest();
            testCancellation = new CancellationTokenSource();

            var configToTest = ReadConfigFromUi();
            SetStatus($"Pinging {configToTest.Provider} ({configToTest.Model})...", DotTesting);
            if (testButtonLabel != null)
                testButtonLabel.text = "TESTING...";

            try
            {
                var result = await SafetyAiClient.TestConnectionAsync(configToTest, testCancellation.Token);

                if (result.Success)
                {
                    SetStatus(result.Message, DotSuccess);
                    SafetyAudioManager.Instance?.PlayObjectiveComplete();
                }
                else
                {
                    SetStatus(result.Message, DotError);
                    SafetyAudioManager.Instance?.PlayPenalty();
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Test error: {ex.Message}", DotError);
                SafetyAudioManager.Instance?.PlayPenalty();
            }
            finally
            {
                isTesting = false;
                if (testButtonLabel != null)
                    testButtonLabel.text = "TEST CONNECTION";
            }
        }

        public void OnSaveClicked()
        {
            currentConfig = ReadConfigFromUi();
            currentConfig.Save();

            SetStatus("Configuration saved successfully to local settings.", DotSuccess);
            SafetyAudioManager.Instance?.PlayScoreGain();
        }

        SafetyAiConfig ReadConfigFromUi()
        {
            var config = new SafetyAiConfig();
            if (providerDropdown != null)
                config.Provider = (AiProviderType)providerDropdown.value;

            if (endpointInput != null)
                config.Endpoint = endpointInput.text.Trim();

            if (modelInput != null)
                config.Model = modelInput.text.Trim();

            if (apiKeyInput != null)
                config.ApiKey = apiKeyInput.text.Trim();

            if (enabledToggle != null)
                config.Enabled = enabledToggle.isOn;

            config.TimeoutSeconds = 25;
            return config;
        }

        void SetStatus(string message, Color dotColor)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
                statusLabel.color = dotColor == DotError ? dotColor : Color.white;
            }

            if (statusIndicatorDot != null)
                statusIndicatorDot.color = dotColor;
        }

        void CancelTest()
        {
            if (testCancellation != null)
            {
                testCancellation.Cancel();
                testCancellation.Dispose();
                testCancellation = null;
            }
        }
    }
}
