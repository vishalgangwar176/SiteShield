using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(XRSimpleInteractable), typeof(NpcConversationAgent))]
    public sealed class NpcTalkInteractable : MonoBehaviour
    {
        [SerializeField] NpcConversationAgent agent;
        [SerializeField] NpcSpeechBubbleView speechBubble;
        [SerializeField] TextMesh siteTitle;
        [SerializeField] string[] conversationPrompts =
        {
            "What should I inspect at this site?",
            "Give me a concise hint based on my current progress.",
            "Explain the safest control for the conditions I have found.",
            "Debrief my current performance without changing my score."
        };

        XRSimpleInteractable interactable;
        string displayedReply;
        int nextPrompt;
        bool conversationStarted;
        Coroutine dismissal;
        string[] replyPages;
        int pageIndex;
        float nextPageAt;
        NpcSpeakGlow speakGlow;

        public bool ConversationActive => conversationStarted;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (agent == null)
                agent = GetComponent<NpcConversationAgent>();
            speakGlow = GetComponent<NpcSpeakGlow>();
        }

        void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
        }

        void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelected);
            }
            speakGlow?.SetSpeaking(false);
        }

        void Update()
        {
            if (!conversationStarted || agent == null || speechBubble == null)
                return;

            if (displayedReply != agent.LastReply)
            {
                displayedReply = agent.LastReply;
                replyPages = Paginate(displayedReply, 34, 3);
                pageIndex = 0;
                ShowPage();
            }
            else if (replyPages != null && pageIndex + 1 < replyPages.Length &&
                     Time.unscaledTime >= nextPageAt)
            {
                pageIndex++;
                ShowPage();
            }
        }

        void OnMouseDown()
        {
            if (!DesktopPointerInputGate.CanUseWorldPointer)
                return;
            Ask();
        }

        public void Configure(
            NpcConversationAgent conversationAgent,
            NpcSpeechBubbleView bubble,
            TextMesh title)
        {
            agent = conversationAgent;
            speechBubble = bubble;
            siteTitle = title;
        }

        void OnSelected(SelectEnterEventArgs _)
        {
            Ask();
        }

        public void Ask()
        {
            if (agent == null || conversationPrompts == null || conversationPrompts.Length == 0)
                return;

            SafetyAudioManager.Instance?.PlayCoachChirp();
            speechBubble?.Show("Ask me about the condition, risk, or safest control.", 0, 1);
            if (siteTitle != null)
                siteTitle.gameObject.SetActive(false);
            TrainingHud.Instance?.SetVisible(false);
            conversationStarted = true;
            speakGlow?.SetSpeaking(true);
            var overlayChat = NpcChatPanel.Instance;
            if (overlayChat != null)
            {
                overlayChat.Open(agent, agent.name);
                return;
            }

            agent.Ask(conversationPrompts[nextPrompt]);
            nextPrompt = (nextPrompt + 1) % conversationPrompts.Length;
            if (dismissal != null)
                StopCoroutine(dismissal);
            dismissal = StartCoroutine(DismissAfterDelay());
        }

        IEnumerator DismissAfterDelay()
        {
            yield return new WaitForSecondsRealtime(25f);
            dismissal = null;
            EndConversation();
        }

        public void EndConversation()
        {
            if (dismissal != null)
            {
                StopCoroutine(dismissal);
                dismissal = null;
            }
            speechBubble?.Hide();
            if (siteTitle != null)
                siteTitle.gameObject.SetActive(true);
            conversationStarted = false;
            speakGlow?.SetSpeaking(false);
            replyPages = null;
            var ownsCurrentChat = NpcChatPanel.Instance?.IsOpenFor(agent) ?? false;
            if (ownsCurrentChat)
            {
                NpcChatPanel.Instance.CloseFor(agent);
                TrainingHud.Instance?.SetVisible(true);
            }
        }

        void ShowPage()
        {
            if (replyPages == null || replyPages.Length == 0)
                return;
            speechBubble.Show(replyPages[pageIndex], pageIndex, replyPages.Length);
            nextPageAt = Time.unscaledTime + 5f;
        }

        static string[] Paginate(string value, int width, int linesPerPage)
        {
            var words = value.Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            var line = new System.Text.StringBuilder();
            var lineLength = 0;
            foreach (var word in words)
            {
                if (lineLength > 0 && lineLength + word.Length + 1 > width)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    lineLength = 0;
                }
                if (lineLength > 0)
                {
                    line.Append(' ');
                    lineLength++;
                }
                line.Append(word);
                lineLength += word.Length;
            }
            if (line.Length > 0)
                lines.Add(line.ToString());

            var pages = new System.Collections.Generic.List<string>();
            for (var index = 0; index < lines.Count;)
            {
                var count = Mathf.Min(linesPerPage, lines.Count - index);
                if (index + count < lines.Count)
                {
                    for (var candidate = count - 1; candidate >= 2; candidate--)
                    {
                        var finalCharacter = lines[index + candidate - 1].TrimEnd();
                        if (finalCharacter.EndsWith(".") || finalCharacter.EndsWith("!") ||
                            finalCharacter.EndsWith("?"))
                        {
                            count = candidate;
                            break;
                        }
                    }
                }

                pages.Add(string.Join("\n", lines.GetRange(index, count)));
                index += count;
            }
            return pages.ToArray();
        }
    }
}
