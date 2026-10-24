using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SafetyTraining.Runtime
{
    [Serializable]
    public sealed class ChatMessageItem
    {
        public string Role; // "user", "assistant", "system"
        public string Text;
        public string Timestamp;

        public ChatMessageItem(string role, string text)
        {
            Role = role;
            Text = text;
            Timestamp = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    public sealed class ConnectionTestResult
    {
        public bool Success;
        public int StatusCode;
        public long LatencyMs;
        public string Message;
    }

    public static class SafetyAiClient
    {
        public static async Task<ConnectionTestResult> TestConnectionAsync(
            SafetyAiConfig config,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ConnectionTestResult();

            if (config == null)
            {
                result.Success = false;
                result.Message = "Configuration is missing.";
                return result;
            }

            if (config.Provider != AiProviderType.OllamaLocal && string.IsNullOrWhiteSpace(config.ApiKey))
            {
                result.Success = false;
                result.Message = "API Key is required for this provider. Configure it in AI Settings.";
                return result;
            }

            try
            {
                var reply = await SendPromptAsync(
                    config,
                    "You are a test ping agent. Answer with 'OK' and nothing else.",
                    "Ping test",
                    null,
                    cancellationToken);

                stopwatch.Stop();
                result.Success = !string.IsNullOrWhiteSpace(reply);
                result.StatusCode = 200;
                result.LatencyMs = stopwatch.ElapsedMilliseconds;
                result.Message = $"Connected to {config.Provider} ({config.Model}) in {result.LatencyMs}ms.";
                return result;
            }
            catch (UnityWebRequestException uwe)
            {
                stopwatch.Stop();
                result.Success = false;
                result.StatusCode = uwe.ResponseCode;
                result.LatencyMs = stopwatch.ElapsedMilliseconds;
                result.Message = MapErrorMessage(uwe.ResponseCode, uwe.Message, config.Provider);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                result.Success = false;
                result.StatusCode = 408;
                result.LatencyMs = stopwatch.ElapsedMilliseconds;
                result.Message = $"Request timed out after {config.TimeoutSeconds}s. Check network or server address.";
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.StatusCode = 0;
                result.LatencyMs = stopwatch.ElapsedMilliseconds;
                result.Message = $"Connection failed: {ex.Message}";
                return result;
            }
        }

        public static async Task<string> SendPromptAsync(
            SafetyAiConfig config,
            string systemInstructions,
            string userPrompt,
            List<ChatMessageItem> history,
            CancellationToken cancellationToken)
        {
            if (config == null || !config.Enabled)
                throw new InvalidOperationException("AI Coach is currently disabled in settings.");

            var url = BuildRequestUrl(config);
            var jsonPayload = BuildRequestPayload(config, systemInstructions, userPrompt, history);

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var bodyBytes = Encoding.UTF8.GetBytes(jsonPayload);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = Mathf.Clamp(config.TimeoutSeconds, 5, 120);
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // Attach Provider-specific auth headers
            ApplyAuthHeaders(webRequest, config);

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                var errorText = webRequest.downloadHandler?.text ?? "";
                throw new UnityWebRequestException(
                    (int)webRequest.responseCode,
                    $"{webRequest.error}: {errorText}");
            }

            var responseJson = webRequest.downloadHandler.text;
            return ParseResponse(config.Provider, responseJson);
        }

        public static string BuildRequestUrl(SafetyAiConfig config)
        {
            var baseEndpoint = (config.Endpoint ?? "").TrimEnd('/');

            return config.Provider switch
            {
                AiProviderType.OpenAiCompatible =>
                    baseEndpoint.EndsWith("/chat/completions") ? baseEndpoint : $"{baseEndpoint}/chat/completions",

                AiProviderType.GoogleGemini =>
                    $"{baseEndpoint}/v1beta/models/{config.Model}:generateContent?key={config.ApiKey}",

                AiProviderType.OllamaLocal =>
                    baseEndpoint.EndsWith("/api/chat") ? baseEndpoint : $"{baseEndpoint}/api/chat",

                AiProviderType.CustomProxy =>
                    baseEndpoint,

                _ => $"{baseEndpoint}/chat/completions"
            };
        }

        public static string BuildRequestPayload(
            SafetyAiConfig config,
            string systemInstructions,
            string userPrompt,
            List<ChatMessageItem> history)
        {
            switch (config.Provider)
            {
                case AiProviderType.GoogleGemini:
                {
                    var contents = new List<object>();

                    // Inject system prompt combined with context in the first turn
                    var combinedPrompt = $"{systemInstructions}\n\n{userPrompt}";
                    contents.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = combinedPrompt } }
                    });

                    var payload = new
                    {
                        contents = contents,
                        generationConfig = new
                        {
                            temperature = 0.4f,
                            maxOutputTokens = 220
                        }
                    };
                    return JsonConvert.SerializeObject(payload);
                }

                case AiProviderType.OllamaLocal:
                {
                    var messages = new List<object>
                    {
                        new { role = "system", content = systemInstructions }
                    };

                    if (history != null)
                    {
                        foreach (var item in history)
                        {
                            messages.Add(new { role = item.Role, content = item.Text });
                        }
                    }

                    messages.Add(new { role = "user", content = userPrompt });

                    var payload = new
                    {
                        model = config.Model,
                        messages = messages,
                        stream = false,
                        options = new
                        {
                            temperature = 0.35f,
                            num_predict = 180
                        }
                    };
                    return JsonConvert.SerializeObject(payload);
                }

                case AiProviderType.OpenAiCompatible:
                case AiProviderType.CustomProxy:
                default:
                {
                    var messages = new List<object>
                    {
                        new { role = "system", content = systemInstructions }
                    };

                    if (history != null)
                    {
                        foreach (var item in history)
                        {
                            messages.Add(new { role = item.Role, content = item.Text });
                        }
                    }

                    messages.Add(new { role = "user", content = userPrompt });

                    var payload = new
                    {
                        model = config.Model,
                        messages = messages,
                        temperature = 0.35f,
                        max_tokens = 200
                    };
                    return JsonConvert.SerializeObject(payload);
                }
            }
        }

        static void ApplyAuthHeaders(UnityWebRequest webRequest, SafetyAiConfig config)
        {
            if (config.Provider == AiProviderType.OpenAiCompatible || config.Provider == AiProviderType.CustomProxy)
            {
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {config.ApiKey}");
                }
            }
        }

        public static string ParseResponse(AiProviderType provider, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("Empty response received from AI provider.");

            var root = JObject.Parse(json);

            switch (provider)
            {
                case AiProviderType.GoogleGemini:
                {
                    var text = root.SelectToken("candidates[0].content.parts[0].text")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();

                    // Check for blocked content or error
                    var blockReason = root.SelectToken("promptFeedback.blockReason")?.ToString();
                    if (!string.IsNullOrWhiteSpace(blockReason))
                        throw new InvalidOperationException($"Gemini blocked response: {blockReason}");

                    var errorMsg = root.SelectToken("error.message")?.ToString();
                    if (!string.IsNullOrWhiteSpace(errorMsg))
                        throw new InvalidOperationException($"Gemini error: {errorMsg}");

                    throw new InvalidOperationException("Unrecognized Gemini response format.");
                }

                case AiProviderType.OllamaLocal:
                {
                    var text = root.SelectToken("message.content")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();

                    var error = root.SelectToken("error")?.ToString();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new InvalidOperationException($"Ollama error: {error}");

                    throw new InvalidOperationException("Unrecognized Ollama response format.");
                }

                case AiProviderType.OpenAiCompatible:
                case AiProviderType.CustomProxy:
                default:
                {
                    var text = root.SelectToken("choices[0].message.content")?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();

                    var errorMsg = root.SelectToken("error.message")?.ToString();
                    if (!string.IsNullOrWhiteSpace(errorMsg))
                        throw new InvalidOperationException($"Provider error: {errorMsg}");

                    throw new InvalidOperationException("Unrecognized OpenAI response format.");
                }
            }
        }

        public static string MapErrorMessage(int statusCode, string rawError, AiProviderType provider) => statusCode switch
        {
            401 => "Authentication failed (401 Unauthorized): API Key is invalid or expired.",
            403 => "Access forbidden (403): Check your provider quotas and model access rights.",
            404 => $"Endpoint or model not found (404): Check model name and URL for {provider}.",
            429 => "Rate limit exceeded (429): Quota reached or too many requests. Please wait.",
            500 => "Provider server internal error (500). Please try again shortly.",
            502 or 503 => "Provider server unavailable (502/503). Service might be undergoing maintenance.",
            _ => $"HTTP {statusCode} Error: {rawError}"
        };
    }

    public sealed class UnityWebRequestException : Exception
    {
        public int ResponseCode { get; }

        public UnityWebRequestException(int responseCode, string message) : base(message)
        {
            ResponseCode = responseCode;
        }
    }
}
