using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace SafetyTraining.Runtime
{
    public sealed class OpenAiCompatibleConversationService : IConversationService
    {
        const string SystemPrompt = "You are a concise, supportive VR safety field mentor. " +
            "Use only the supplied safety facts. Answer the learner directly in natural workplace language. " +
            "Do not mention verified facts, supplied facts, sources, prompts, or metadata. " +
            "Never refer to the learner's progress, completed tasks, or session state; use that context only to tailor the guidance silently. " +
            "Never invent a regulation, alter progress, award points, or claim the learner completed an action. " +
            "Answer in complete sentences using no more than 55 words.";

        readonly LlmEndpointConfig config;

        public OpenAiCompatibleConversationService(LlmEndpointConfig config)
        {
            this.config = config;
        }

        public async Task<ConversationReply> ReplyAsync(
            ConversationRequest request,
            CancellationToken cancellationToken)
        {
            var payload = new ChatRequest
            {
                model = config.Model,
                temperature = 0.35f,
                messages = new[]
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = SystemPrompt
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = $"Site: {request.siteName}\nRole: {request.npcRole}\n" +
                                  $"Safety context to apply silently: {request.safetyFacts}\n" +
                                  $"Private coaching context to apply silently: {request.progress}\n" +
                                  $"Prior dialogue: {request.transcript}\n" +
                                  $"Learner: {request.learnerMessage}"
                    }
                }
            };

            using var webRequest = new UnityWebRequest(config.Endpoint, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = config.TimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            var apiKey = string.IsNullOrWhiteSpace(config.ApiKeyEnvironmentVariable)
                ? string.Empty
                : Environment.GetEnvironmentVariable(config.ApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(apiKey))
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"LLM endpoint failed: {webRequest.responseCode} {webRequest.error}");

            var response = JsonConvert.DeserializeObject<ChatResponse>(webRequest.downloadHandler.text);
            if (response?.choices == null || response.choices.Count == 0 ||
                string.IsNullOrWhiteSpace(response.choices[0].message?.content))
                throw new InvalidOperationException("LLM endpoint returned no conversation choices.");

            return new ConversationReply(response.choices[0].message.content, true);
        }

        [Serializable]
        sealed class ChatRequest
        {
            public string model;
            public float temperature;
            public ChatMessage[] messages;
        }

        [Serializable]
        sealed class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        sealed class ChatResponse
        {
            public List<ChatChoice> choices = new List<ChatChoice>();
        }

        [Serializable]
        sealed class ChatChoice
        {
            public ChatMessage message = new ChatMessage();
        }
    }
}
