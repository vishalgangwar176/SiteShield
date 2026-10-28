using System.Collections;
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
        }

        public static NpcChatPanel Instance { get; private set; }

        [SerializeField] GameObject panel;
        [SerializeField] Text title;
        [SerializeField] Text transcript;
        [SerializeField] InputField input;
        [SerializeField] Text sendLabel;
        [SerializeField] Button sendButton;
        [SerializeField] Button closeButton;
        NpcConversationAgent activeAgent;
        string lastReply;
        Coroutine refresh;
        bool controlsBound;

        public bool IsVisible => panel != null && panel.activeSelf;
        public bool IsOpenFor(NpcConversationAgent agent) => activeAgent == agent && IsVisible;

        void Awake()
        {
            Instance = this;
            BindControls();
            SetVisible(false);
        }

        void Update()
        {
            if (IsVisible && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        void OnDestroy()
        {
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
        }

        public void Open(NpcConversationAgent agent, string npcName)
        {
            activeAgent = agent;
            var conciseSiteName = agent.SiteName.Split(' ')[0].ToUpperInvariant();
            title.text = $"{conciseSiteName} SAFETY COACH";
            transcript.text = $"{agent.SiteName} coach ready.\nAsk about the visible condition, risk, or required control.";
            lastReply = agent.LastReply;
            SetVisible(true);
            input.Select();
            input.ActivateInputField();
            if (refresh != null)
                StopCoroutine(refresh);
            refresh = StartCoroutine(RefreshReply());
        }

        public void Close()
        {
            DesktopPointerInputGate.BlockFor(0.15f);
            if (refresh != null)
                StopCoroutine(refresh);
            refresh = null;
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
            OnSubmit(input != null ? input.text : string.Empty);
        }

        void OnSubmit(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || activeAgent == null)
                return;
            var message = value.Trim();
            input.text = string.Empty;
            transcript.text = $"You: {message}\n\nCoach is responding...";
            activeAgent.Ask(message);
        }

        IEnumerator RefreshReply()
        {
            while (activeAgent != null)
            {
                if (activeAgent.LastReply != lastReply)
                {
                    lastReply = activeAgent.LastReply;
                    transcript.text = "Coach replied in the speech bubble.\nAsk a follow-up or request a control explanation.";
                    input.Select();
                    input.ActivateInputField();
                }
                yield return null;
            }
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
            input.onEndEdit.AddListener(OnSubmit);
            sendButton.onClick.AddListener(SubmitCurrent);
            closeButton.onClick.AddListener(Close);
            controlsBound = true;
        }

        void UnbindControls()
        {
            if (!controlsBound)
                return;
            input.onEndEdit.RemoveListener(OnSubmit);
            sendButton.onClick.RemoveListener(SubmitCurrent);
            closeButton.onClick.RemoveListener(Close);
            controlsBound = false;
        }
    }
}
