using System;
using System.Text;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public enum AiProviderType
    {
        OpenAiCompatible,
        GoogleGemini,
        OllamaLocal,
        CustomProxy
    }

    [Serializable]
    public sealed class SafetyAiConfig
    {
        public const string PrefKeyProvider = "SiteShield_AI_Provider";
        public const string PrefKeyEndpoint = "SiteShield_AI_Endpoint";
        public const string PrefKeyModel = "SiteShield_AI_Model";
        public const string PrefKeyApiKey = "SiteShield_AI_Key_Enc";
        public const string PrefKeyEnabled = "SiteShield_AI_Enabled";
        public const string PrefKeyTimeout = "SiteShield_AI_Timeout";

        public AiProviderType Provider = AiProviderType.GoogleGemini;
        public string Endpoint = "https://generativelanguage.googleapis.com";
        public string Model = "gemini-1.5-flash";
        public string ApiKey = "";
        public bool Enabled = true;
        public int TimeoutSeconds = 25;

        const string ObfuscationKey = "SiteShieldSafety2026";

        public static SafetyAiConfig Load()
        {
            var config = new SafetyAiConfig();
            config.Provider = (AiProviderType)PlayerPrefs.GetInt(PrefKeyProvider, (int)AiProviderType.GoogleGemini);

            var defaultEndpoint = GetDefaultEndpoint(config.Provider);
            var defaultModel = GetDefaultModel(config.Provider);

            config.Endpoint = PlayerPrefs.GetString(PrefKeyEndpoint, defaultEndpoint);
            if (string.IsNullOrWhiteSpace(config.Endpoint))
                config.Endpoint = defaultEndpoint;

            config.Model = PlayerPrefs.GetString(PrefKeyModel, defaultModel);
            if (string.IsNullOrWhiteSpace(config.Model))
                config.Model = defaultModel;

            var encKey = PlayerPrefs.GetString(PrefKeyApiKey, "");
            config.ApiKey = DecryptKey(encKey);

            // Also check environment variable as a fallback for secure automated or enterprise setups
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("SITESHIELD_AI_KEY") ??
                             Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
                             Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                    config.ApiKey = envKey.Trim();
            }

            config.Enabled = PlayerPrefs.GetInt(PrefKeyEnabled, 1) == 1;
            config.TimeoutSeconds = PlayerPrefs.GetInt(PrefKeyTimeout, 25);
            return config;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(PrefKeyProvider, (int)Provider);
            PlayerPrefs.SetString(PrefKeyEndpoint, Endpoint ?? "");
            PlayerPrefs.SetString(PrefKeyModel, Model ?? "");
            PlayerPrefs.SetString(PrefKeyApiKey, EncryptKey(ApiKey ?? ""));
            PlayerPrefs.SetInt(PrefKeyEnabled, Enabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeyTimeout, Mathf.Clamp(TimeoutSeconds, 5, 120));
            PlayerPrefs.Save();
        }

        public static string GetDefaultEndpoint(AiProviderType provider) => provider switch
        {
            AiProviderType.OpenAiCompatible => "https://api.openai.com/v1",
            AiProviderType.GoogleGemini => "https://generativelanguage.googleapis.com",
            AiProviderType.OllamaLocal => "http://localhost:11434",
            AiProviderType.CustomProxy => "http://127.0.0.1:8000/v1/chat/completions",
            _ => "https://generativelanguage.googleapis.com"
        };

        public static string GetDefaultModel(AiProviderType provider) => provider switch
        {
            AiProviderType.OpenAiCompatible => "gpt-4o-mini",
            AiProviderType.GoogleGemini => "gemini-1.5-flash",
            AiProviderType.OllamaLocal => "llama3.2",
            AiProviderType.CustomProxy => "siteshield-safety-coach",
            _ => "gemini-1.5-flash"
        };

        static string EncryptKey(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var keyBytes = Encoding.UTF8.GetBytes(ObfuscationKey);
            for (int i = 0; i < plainBytes.Length; i++)
            {
                plainBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return Convert.ToBase64String(plainBytes);
        }

        static string DecryptKey(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                var keyBytes = Encoding.UTF8.GetBytes(ObfuscationKey);
                for (int i = 0; i < cipherBytes.Length; i++)
                {
                    cipherBytes[i] = (byte)(cipherBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }
                return Encoding.UTF8.GetString(cipherBytes);
            }
            catch
            {
                return "";
            }
        }
    }
}
