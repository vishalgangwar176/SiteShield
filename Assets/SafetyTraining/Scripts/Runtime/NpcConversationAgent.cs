using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class NpcConversationAgent : MonoBehaviour
    {
        [SerializeField] SafetyTraining.Core.TrainingSiteId siteId;
        [SerializeField] string npcRole = "site safety coach";
        [SerializeField, TextArea] string verifiedSafetyFacts = "Inspect the marked hazards before continuing.";
        [SerializeField] LlmEndpointConfig endpointConfig;
        [SerializeField] bool useLanguageModel = true;

        CancellationTokenSource lifetime;
        IConversationService fallback;
        readonly List<string> transcript = new List<string>();

        public string LastReply { get; private set; } = "Ask me about this training site.";
        public string SiteName => siteId switch
        {
            SafetyTraining.Core.TrainingSiteId.FireResponse => "Fire Response",
            SafetyTraining.Core.TrainingSiteId.ChemicalProcessing => "Chemical Processing",
            SafetyTraining.Core.TrainingSiteId.ElectricalMaintenance => "Electrical Maintenance",
            _ => siteId.ToString()
        };

        void Awake()
        {
            if (lifetime == null)
                lifetime = new CancellationTokenSource();
            if (fallback == null)
                fallback = new ScriptedConversationService();
        }

        void OnDestroy()
        {
            if (lifetime != null)
            {
                lifetime.Cancel();
                lifetime.Dispose();
                lifetime = null;
            }
        }

        public async void Ask(string learnerMessage)
        {
            if (lifetime == null)
                lifetime = new CancellationTokenSource();
            if (fallback == null)
                fallback = new ScriptedConversationService();

            TrainingCoordinator.Instance?.RecordCoachTurn(siteId);
            var request = new ConversationRequest
            {
                siteName = siteId.ToString(),
                npcRole = npcRole,
                learnerMessage = learnerMessage,
                safetyFacts = verifiedSafetyFacts,
                progress = BuildProgressContext(),
                transcript = string.Join("\n", transcript)
            };

            var aiConfig = SafetyAiConfig.Load();
            if (Application.isPlaying && aiConfig != null && aiConfig.Enabled &&
                (!string.IsNullOrWhiteSpace(aiConfig.ApiKey) || aiConfig.Provider == AiProviderType.OllamaLocal))
            {
                try
                {
                    var systemPrompt = SafetyAiContextBuilder.BuildSystemInstructions(siteId);
                    var dynamicContext = SafetyAiContextBuilder.BuildDynamicContext(siteId, learnerMessage);
                    LastReply = await SafetyAiClient.SendPromptAsync(
                        aiConfig,
                        systemPrompt,
                        dynamicContext,
                        null,
                        lifetime.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    LastReply = (await fallback.ReplyAsync(request, lifetime.Token)).Text;
                }
            }
            else
            {
                try
                {
                    var service = useLanguageModel && endpointConfig != null
                        ? new OpenAiCompatibleConversationService(endpointConfig)
                        : fallback;
                    LastReply = (await service.ReplyAsync(request, lifetime.Token)).Text;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception) when (lifetime != null && !lifetime.IsCancellationRequested)
                {
                    LastReply = (await fallback.ReplyAsync(request, lifetime.Token)).Text;
                }
            }

            transcript.Add($"Learner: {learnerMessage}");
            transcript.Add($"Coach: {LastReply}");
            if (transcript.Count > 8)
                transcript.RemoveRange(0, transcript.Count - 8);
        }

        public void Configure(SafetyTraining.Core.TrainingSiteId trainingSite, string role, string facts)
        {
            siteId = trainingSite;
            npcRole = role;
            verifiedSafetyFacts = facts;
        }

        public void ConfigureEndpoint(LlmEndpointConfig config)
        {
            endpointConfig = config;
        }

        string BuildProgressContext()
        {
            var coordinator = TrainingCoordinator.Instance;
            if (coordinator == null)
                return "not started";
            return $"{coordinator.GetProgress(siteId)}. {coordinator.GetDebrief(siteId)}";
        }
    }
}
