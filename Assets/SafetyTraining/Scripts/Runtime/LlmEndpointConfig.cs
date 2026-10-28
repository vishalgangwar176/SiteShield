using UnityEngine;

namespace SafetyTraining.Runtime
{
    [CreateAssetMenu(menuName = "Safety Training/LLM Endpoint Config")]
    public sealed class LlmEndpointConfig : ScriptableObject
    {
        [SerializeField] string endpoint = "http://localhost:11434/v1/chat/completions";
        [SerializeField] string model = "hermes3:8b";
        [SerializeField] string apiKeyEnvironmentVariable = "";
        [SerializeField, Min(5)] int timeoutSeconds = 20;

        public string Endpoint => endpoint;
        public string Model => model;
        public string ApiKeyEnvironmentVariable => apiKeyEnvironmentVariable;
        public int TimeoutSeconds => timeoutSeconds;
    }
}
