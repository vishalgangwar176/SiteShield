using System.Collections.Generic;
using NUnit.Framework;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class SiteShield2UpgradeTests
    {
        [Test]
        public void SafetyTaskManager_InitializesAllFiveSitesWithSequentialTasks()
        {
            var go = new GameObject("Test Safety Task Manager");
            var manager = go.AddComponent<SafetyTaskManager>();
            manager.InitializeTaskCatalog();

            var sites = new[]
            {
                TrainingSiteId.Construction,
                TrainingSiteId.Warehouse,
                TrainingSiteId.FireResponse,
                TrainingSiteId.ChemicalProcessing,
                TrainingSiteId.ElectricalMaintenance
            };

            foreach (var site in sites)
            {
                var tasks = manager.GetTasksForSite(site);
                Assert.That(tasks, Is.Not.Null, $"Tasks for {site} must not be null.");
                Assert.That(tasks.Count, Is.GreaterThanOrEqualTo(4), $"Site {site} must have at least 4 practical tasks.");

                for (int i = 0; i < tasks.Count; i++)
                {
                    Assert.That(tasks[i].SequenceOrder, Is.EqualTo(i), $"Task {tasks[i].TaskId} sequence order must match index.");
                    Assert.That(tasks[i].Title, Is.Not.Null.And.Not.Empty, $"Task title must not be empty.");
                    Assert.That(tasks[i].Description, Is.Not.Null.And.Not.Empty, $"Task description must not be empty.");
                    Assert.That(tasks[i].HintText, Is.Not.Null.And.Not.Empty, $"Task hint must not be empty.");
                }
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafetyTaskManager_CompletingTask_UpdatesProgressAndFiresEvent()
        {
            var go = new GameObject("Test Manager");
            var manager = go.AddComponent<SafetyTaskManager>();
            manager.InitializeTaskCatalog();

            SafetyTaskDefinition receivedTask = null;
            manager.TaskCompleted += t => receivedTask = t;

            var initialTask = manager.GetCurrentTask(TrainingSiteId.Construction);
            Assert.That(initialTask, Is.Not.Null);
            Assert.That(initialTask.IsCompleted, Is.False);

            manager.CompleteTask(TrainingSiteId.Construction, initialTask.TargetId);

            Assert.That(initialTask.IsCompleted, Is.True);
            Assert.That(receivedTask, Is.EqualTo(initialTask));
            Assert.That(manager.GetCompletedCount(TrainingSiteId.Construction), Is.EqualTo(1));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafetyTaskManager_GeneratesAccurateModuleAssessment()
        {
            var go = new GameObject("Test Manager");
            var manager = go.AddComponent<SafetyTaskManager>();
            manager.InitializeTaskCatalog();

            var summary = manager.GenerateModuleAssessment(TrainingSiteId.Warehouse);
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.SiteId, Is.EqualTo(TrainingSiteId.Warehouse));
            Assert.That(summary.TotalTasks, Is.EqualTo(manager.GetTotalCount(TrainingSiteId.Warehouse)));
            Assert.That(summary.KeyTakeaway, Is.Not.Null.And.Not.Empty);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafetyAiConfig_EncryptsAndDecryptsApiKey()
        {
            var config = new SafetyAiConfig
            {
                Provider = AiProviderType.GoogleGemini,
                Endpoint = "https://generativelanguage.googleapis.com",
                Model = "gemini-1.5-flash",
                ApiKey = "AIzaSyTestKey_1234567890",
                Enabled = true
            };

            config.Save();

            // Verify raw PlayerPrefs value is not plaintext
            var rawEncKey = PlayerPrefs.GetString(SafetyAiConfig.PrefKeyApiKey, "");
            Assert.That(rawEncKey, Is.Not.EqualTo("AIzaSyTestKey_1234567890"), "Raw PlayerPrefs key must be obfuscated/encrypted.");

            var loaded = SafetyAiConfig.Load();
            Assert.That(loaded.ApiKey, Is.EqualTo("AIzaSyTestKey_1234567890"), "Decrypted key must match original.");
            Assert.That(loaded.Model, Is.EqualTo("gemini-1.5-flash"));
            Assert.That(loaded.Enabled, Is.True);
        }

        [Test]
        public void SafetyAiConfig_ProvidesSensibleDefaultsPerProvider()
        {
            Assert.That(SafetyAiConfig.GetDefaultEndpoint(AiProviderType.OpenAiCompatible), Does.Contain("api.openai.com"));
            Assert.That(SafetyAiConfig.GetDefaultEndpoint(AiProviderType.GoogleGemini), Does.Contain("generativelanguage.googleapis.com"));
            Assert.That(SafetyAiConfig.GetDefaultEndpoint(AiProviderType.OllamaLocal), Does.Contain("localhost:11434"));

            Assert.That(SafetyAiConfig.GetDefaultModel(AiProviderType.OpenAiCompatible), Is.EqualTo("gpt-4o-mini"));
            Assert.That(SafetyAiConfig.GetDefaultModel(AiProviderType.GoogleGemini), Is.EqualTo("gemini-1.5-flash"));
            Assert.That(SafetyAiConfig.GetDefaultModel(AiProviderType.OllamaLocal), Is.EqualTo("llama3.2"));
        }

        [Test]
        public void SafetyAiClient_BuildsCorrectProviderUrls()
        {
            var openAiConfig = new SafetyAiConfig
            {
                Provider = AiProviderType.OpenAiCompatible,
                Endpoint = "https://api.openai.com/v1",
                Model = "gpt-4o-mini"
            };
            Assert.That(SafetyAiClient.BuildRequestUrl(openAiConfig), Is.EqualTo("https://api.openai.com/v1/chat/completions"));

            var geminiConfig = new SafetyAiConfig
            {
                Provider = AiProviderType.GoogleGemini,
                Endpoint = "https://generativelanguage.googleapis.com",
                Model = "gemini-1.5-flash",
                ApiKey = "test-gemini-key"
            };
            Assert.That(SafetyAiClient.BuildRequestUrl(geminiConfig), Is.EqualTo("https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=test-gemini-key"));

            var ollamaConfig = new SafetyAiConfig
            {
                Provider = AiProviderType.OllamaLocal,
                Endpoint = "http://localhost:11434",
                Model = "llama3.2"
            };
            Assert.That(SafetyAiClient.BuildRequestUrl(ollamaConfig), Is.EqualTo("http://localhost:11434/api/chat"));
        }

        [Test]
        public void SafetyAiClient_SerializesProviderPayloads()
        {
            var config = new SafetyAiConfig
            {
                Provider = AiProviderType.GoogleGemini,
                Model = "gemini-1.5-flash"
            };

            var payload = SafetyAiClient.BuildRequestPayload(config, "System instructions", "User message", null);
            Assert.That(payload, Does.Contain("contents"));
            Assert.That(payload, Does.Contain("System instructions"));
            Assert.That(payload, Does.Contain("User message"));

            config.Provider = AiProviderType.OpenAiCompatible;
            config.Model = "gpt-4o-mini";
            var openAiPayload = SafetyAiClient.BuildRequestPayload(config, "System instructions", "User message", null);
            Assert.That(openAiPayload, Does.Contain("messages"));
            Assert.That(openAiPayload, Does.Contain("gpt-4o-mini"));
        }

        [Test]
        public void SafetyAiClient_ParsesResponsesAccurately()
        {
            // OpenAI parse test
            var openAiJson = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Install top and mid guardrails.\"}}]}";
            var openAiResult = SafetyAiClient.ParseResponse(AiProviderType.OpenAiCompatible, openAiJson);
            Assert.That(openAiResult, Is.EqualTo("Install top and mid guardrails."));

            // Gemini parse test
            var geminiJson = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Wear personal fall arrest equipment.\"}]}}]}";
            var geminiResult = SafetyAiClient.ParseResponse(AiProviderType.GoogleGemini, geminiJson);
            Assert.That(geminiResult, Is.EqualTo("Wear personal fall arrest equipment."));

            // Ollama parse test
            var ollamaJson = "{\"message\":{\"role\":\"assistant\",\"content\":\"Verify zero energy state with voltmeter.\"}}";
            var ollamaResult = SafetyAiClient.ParseResponse(AiProviderType.OllamaLocal, ollamaJson);
            Assert.That(ollamaResult, Is.EqualTo("Verify zero energy state with voltmeter."));
        }

        [Test]
        public void SafetyAiClient_MapsErrorCodesToFriendlyMessages()
        {
            var msg401 = SafetyAiClient.MapErrorMessage(401, "Unauthorized", AiProviderType.GoogleGemini);
            Assert.That(msg401, Does.Contain("401 Unauthorized"));
            Assert.That(msg401, Does.Contain("API Key"));

            var msg429 = SafetyAiClient.MapErrorMessage(429, "Rate limit", AiProviderType.OpenAiCompatible);
            Assert.That(msg429, Does.Contain("429"));
            Assert.That(msg429, Does.Contain("Rate limit"));

            var msg404 = SafetyAiClient.MapErrorMessage(404, "Not found", AiProviderType.OllamaLocal);
            Assert.That(msg404, Does.Contain("404"));
        }

        [Test]
        public void SafetyAiContextBuilder_ProvidesVerifiedKnowledgeForAllFiveModules()
        {
            var sites = new[]
            {
                TrainingSiteId.Construction,
                TrainingSiteId.Warehouse,
                TrainingSiteId.FireResponse,
                TrainingSiteId.ChemicalProcessing,
                TrainingSiteId.ElectricalMaintenance
            };

            foreach (var site in sites)
            {
                var kb = SafetyAiContextBuilder.GetSiteKnowledgeBase(site);
                Assert.That(kb, Is.Not.Null.And.Not.Empty, $"Knowledge base for {site} must be populated.");
                Assert.That(kb.Length, Is.GreaterThan(50), $"Knowledge base for {site} must be detailed.");
            }

            var prompt = SafetyAiContextBuilder.BuildSystemInstructions(TrainingSiteId.Construction);
            Assert.That(prompt, Does.Contain("OSHA 1926.501"));
            Assert.That(prompt, Does.Contain("SiteShield Senior Safety Training Coach"));
        }
    }
}
