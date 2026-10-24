using System.Linq;
using NUnit.Framework;
using SafetyTraining.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class EnterpriseSystemTests
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [SetUp]
        public void OpenTrainingScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void TitleScreen_IsConfiguredWithSiteShieldBrandElements()
        {
            var titleScreen = Object.FindFirstObjectByType<SiteShieldTitleScreen>();
            Assert.That(titleScreen, Is.Not.Null, "SiteShieldTitleScreen must be present in the scene.");

            var texts = titleScreen.GetComponentsInChildren<Text>(true);
            var titleText = texts.FirstOrDefault(t => t.text == "SITESHIELD");
            Assert.That(titleText, Is.Not.Null, "Brand title 'SITESHIELD' must be present.");

            var taglineText = texts.FirstOrDefault(t => t.text.Contains("Practise safer decisions"));
            Assert.That(taglineText, Is.Not.Null, "Brand tagline 'Practise safer decisions before the real worksite.' must be present.");

            var descText = texts.FirstOrDefault(t => t.text.Contains("OpenXR workplace-safety"));
            Assert.That(descText, Is.Not.Null, "Supporting description must be present.");
        }

        [Test]
        public void TitleScreen_ProvidesAccessibleSettingsControls()
        {
            var titleScreen = Object.FindFirstObjectByType<SiteShieldTitleScreen>();
            Assert.That(titleScreen, Is.Not.Null);

            var sliders = titleScreen.GetComponentsInChildren<Slider>(true);
            Assert.That(sliders.Length, Is.GreaterThanOrEqualTo(2), "Volume and Look Sensitivity sliders must exist.");

            var toggles = titleScreen.GetComponentsInChildren<Toggle>(true);
            Assert.That(toggles.Length, Is.GreaterThanOrEqualTo(2), "Mute and Reduced Motion toggles must exist.");
        }

        [Test]
        public void InteractionPromptHud_IsConfiguredOnHud()
        {
            var promptHud = Object.FindFirstObjectByType<InteractionPromptHud>();
            Assert.That(promptHud, Is.Not.Null, "InteractionPromptHud must be active on the HUD canvas.");

            var reticle = GameObject.Find("Interaction Reticle");
            Assert.That(reticle, Is.Not.Null, "Center reticle dot must be present for precision desktop interaction.");
        }

        [Test]
        public void SafetyAudioManager_IsActiveAndGeneratesSynthesizedClips()
        {
            var audioMgr = Object.FindFirstObjectByType<SafetyAudioManager>();
            Assert.That(audioMgr, Is.Not.Null, "SafetyAudioManager must be configured in the scene.");
            Assert.That(audioMgr.GetComponent<AudioSource>(), Is.Not.Null, "AudioSource must be present.");
        }
    }
}
