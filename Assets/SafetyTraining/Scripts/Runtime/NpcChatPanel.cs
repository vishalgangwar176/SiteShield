using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SafetyTraining.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    public sealed class NpcChatPanel : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Bindings
        {
            public GameObject Panel;
            public Text Title;
            public Text Transcript;
            public InputField Input;
            public Text SendLabel;
            public Button SendButton;
            public Button CloseButton;
            public Button ClearButton;
            public Button SettingsButton;
            public GameObject LoadingIndicator;
        }

        public static NpcChatPanel Instance { get; private set; }

        [SerializeField] GameObject panel;
        [SerializeField] Text title;
        [SerializeField] Text transcript;
        [SerializeField] InputField input;
        [SerializeField] Text sendLabel;
        [SerializeField] Button sendButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button settingsButton;
        [SerializeField] GameObject loadingIndicator;

        NpcConversationAgent activeAgent;
        CancellationTokenSource requestCancellation;
        readonly List<ChatMessageItem> history = new List<ChatMessageItem>();
        bool controlsBound;
        bool isAwaitingResponse;

        public bool IsVisible => panel != null && panel.activeSelf;
        public bool IsOpenFor(NpcConversationAgent agent) => activeAgent == agent && IsVisible;
        public IReadOnlyList<ChatMessageItem> History => history;

        void Awake()
        {
            Instance = this;
            BindControls();
            SetVisible(false);
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }

        void Update()
        {
            if (IsVisible && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        void OnDestroy()
        {
            CancelPendingRequest();
            UnbindControls();
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Bindings bindings)
        {
            panel = bindings.Panel;
            title = bindings.Title;
            transcript = bindings.Transcript;
            input = bindings.Input;
            sendLabel = bindings.SendLabel;
            sendButton = bindings.SendButton;
            closeButton = bindings.CloseButton;
            clearButton = bindings.ClearButton;
            settingsButton = bindings.SettingsButton;
            loadingIndicator = bindings.LoadingIndicator;

            BindControls();
        }

        public void Open(NpcConversationAgent agent, string npcName)
        {
            activeAgent = agent;
            var siteName = agent != null ? agent.SiteName : "TRAINING SITE";
            var conciseSiteName = siteName.Split(' ')[0].ToUpperInvariant();
            if (title != null)
                title.text = $"{conciseSiteName} SAFETY COACH";

            if (history.Count == 0)
            {
                var initialWelcome = $"{siteName} Safety Coach ready.\n" +
                                     "Ask about visible hazards, required PPE, or control steps.";
                history.Add(new ChatMessageItem("assistant", initialWelcome));
            }

            RenderTranscript();
            SetVisible(true);

            if (input != null)
            {
                input.Select();
                input.ActivateInputField();
            }
        }

        public void Close()
        {
            CancelPendingRequest();
            DesktopPointerInputGate.BlockFor(0.15f);
            var previousAgent = activeAgent;
            var restoreHud = previousAgent != null && TrainingCoordinator.Instance?.ActiveSite != null;
            activeAgent = null;
            SetVisible(false);
            previousAgent?.GetComponent<NpcTalkInteractable>()?.EndConversation();
            if (restoreHud)
                TrainingHud.Instance?.SetVisible(true);
        }

        public void CloseFor(NpcConversationAgent agent)
        {
            if (activeAgent == agent)
                Close();
        }

        public void SubmitCurrent()
        {
            if (input != null)
                OnSubmit(input.text);
        }

        public void ClearHistory()
        {
            history.Clear();
            var siteName = activeAgent != null ? activeAgent.SiteName : "Site";
            history.Add(new ChatMessageItem("assistant", $"Conversation reset. {siteName} Safety Coach ready."));
            RenderTranscript();
            SafetyAudioManager.Instance?.PlayClick();
        }

        public void OpenSettings()
        {
            SafetyAiSettingsDialog.Instance?.Open();
        }

        public async void OnSubmit(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || isAwaitingResponse)
                return;

            var learnerMessage = value.Trim();
            if (input != null)
                input.text = string.Empty;

            history.Add(new ChatMessageItem("user", learnerMessage));
            if (history.Count > 16)
                history.RemoveAt(0);

            RenderTranscript();

            isAwaitingResponse = true;
            SetLoading(true);

            var siteId = TrainingCoordinator.Instance?.ActiveSite;
            TrainingCoordinator.Instance?.RecordCoachTurn(siteId ?? TrainingSiteId.Construction);

            var aiConfig = SafetyAiConfig.Load();
            string replyText;

            CancelPendingRequest();
            requestCancellation = new CancellationTokenSource();

            var systemPrompt = SafetyAiContextBuilder.BuildSystemInstructions(siteId);
            var dynamicContext = SafetyAiContextBuilder.BuildDynamicContext(siteId, learnerMessage);

            if (!Application.isPlaying)
            {
                replyText = "[EditMode]: Field guidance verified.";
            }
            else if (aiConfig.Enabled && (!string.IsNullOrWhiteSpace(aiConfig.ApiKey) || aiConfig.Provider == AiProviderType.OllamaLocal))
            {
                try
                {
                    replyText = await SafetyAiClient.SendPromptAsync(
                        aiConfig,
                        systemPrompt,
                        dynamicContext,
                        history,
                        requestCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    isAwaitingResponse = false;
                    SetLoading(false);
                    return;
                }
                catch (UnityWebRequestException uwe)
                {
                    var friendlyError = SafetyAiClient.MapErrorMessage(uwe.ResponseCode, uwe.Message, aiConfig.Provider);
                    replyText = $"[AI Provider Notice]: {friendlyError}\n\nFalling back to verified onsite procedures: Inspect the nearest hazard control.";
                }
                catch (Exception ex)
                {
                    replyText = $"[AI Connection Error]: {ex.Message}\nCheck network or AI Settings.";
                }
            }
            else
            {
                // Fallback deterministic coaching when unconfigured
                replyText = "[AI Offline]: Provider credentials unconfigured. Open [AI CONFIG] to enable live AI.\n" +
                            "Field Guidance: Verify all safety barriers and follow the active objective.";
            }

            isAwaitingResponse = false;
            SetLoading(false);

            history.Add(new ChatMessageItem("assistant", replyText));
            if (history.Count > 16)
                history.RemoveAt(0);

            RenderTranscript();

            // Also inform the active NPC agent and 3D speech bubble
            if (activeAgent != null)
            {
                var bubble = activeAgent.GetComponentInChildren<NpcSpeechBubbleView>();
                bubble?.Show(replyText, 0, 1);
            }

            SafetyAudioManager.Instance?.PlayScoreGain();

            if (input != null)
            {
                input.Select();
                input.ActivateInputField();
            }
        }

        void RenderTranscript()
        {
            if (transcript == null)
                return;

            var sb = new StringBuilder();
            foreach (var item in history)
            {
                if (item.Role == "user")
                {
                    sb.AppendLine($"<color=#22D3C5><b>TRAINEE [{item.Timestamp}]:</b></color> {item.Text}");
                }
                else
                {
                    sb.AppendLine($"<color=#F59E0B><b>SAFETY COACH:</b></color> {item.Text}");
                }
                sb.AppendLine();
            }

            if (isAwaitingResponse)
            {
                sb.AppendLine("<color=#6FB0FF><i>Coach is analyzing site conditions and regulations... ⏳</i></color>");
            }

            transcript.text = sb.ToString().TrimEnd();
        }

        void SetLoading(bool loading)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(loading);

            if (sendButton != null)
                sendButton.interactable = !loading;

            RenderTranscript();
        }

        void SetVisible(bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }

        void BindControls()
        {
            if (controlsBound)
                return;

            if (input != null)
                input.onEndEdit.AddListener(OnSubmit);

            if (sendButton != null)
                sendButton.onClick.AddListener(SubmitCurrent);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearHistory);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            controlsBound = true;
        }

        void UnbindControls()
        {
            if (!controlsBound)
                return;

            if (input != null)
                input.onEndEdit.RemoveListener(OnSubmit);

            if (sendButton != null)
                sendButton.onClick.RemoveListener(SubmitCurrent);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (clearButton != null)
                clearButton.onClick.RemoveListener(ClearHistory);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);

            controlsBound = false;
        }

        void CancelPendingRequest()
        {
            if (requestCancellation != null)
            {
                requestCancellation.Cancel();
                requestCancellation.Dispose();
                requestCancellation = null;
            }
            isAwaitingResponse = false;
        }
    }
}
