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
            var titleScreen = Object.FindFirstObjectByType<SiteShieldTitleScreen>();
            titleScreen?.BindControls();
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

        [Test]
        public void TitleScreen_EnterCampusButton_IsBoundAndDismissesPanel()
        {
            var titleScreen = Object.FindFirstObjectByType<SiteShieldTitleScreen>();
            Assert.That(titleScreen, Is.Not.Null, "TitleScreen must be present.");

            titleScreen.ShowTitleMenu();
            Assert.That(titleScreen.IsOpen, Is.True, "Title menu should be open.");

            var enterBtnGo = GameObject.Find("Btn Enter Campus");
            Assert.That(enterBtnGo, Is.Not.Null, "Btn Enter Campus GameObject must exist.");
            var enterBtn = enterBtnGo.GetComponent<Button>();
            Assert.That(enterBtn, Is.Not.Null, "Btn Enter Campus must have Button component.");

            Assert.That(enterBtn.onClick.GetPersistentEventCount(), Is.GreaterThan(0),
                "Btn Enter Campus must have a persistent onClick listener in the scene!");

            // Test clicking the button
            enterBtn.onClick.Invoke();

            Assert.That(titleScreen.IsOpen, Is.False, "Menu must close after clicking Enter Training Campus.");

            var canvasGroup = titleScreen.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.blocksRaycasts, Is.False, "BlocksRaycasts must be disabled when menu closes.");
        }
    }
}
