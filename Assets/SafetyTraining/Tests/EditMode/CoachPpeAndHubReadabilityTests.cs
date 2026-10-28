using System.Linq;
using NUnit.Framework;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class CoachPpeAndHubReadabilityTests
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [SetUp]
        public void OpenTrainingScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [TestCase(TrainingSiteId.ChemicalProcessing, "Chemical Splash Goggles", "Half Mask Respirator")]
        [TestCase(TrainingSiteId.ElectricalMaintenance, "Arc Flash Face Shield", "Voltage Rated Gloves")]
        public void SpecializedCoaches_WearScenarioSpecificProtectiveEquipment(
            TrainingSiteId siteId,
            string primaryEquipment,
            string secondaryEquipment)
        {
            // Given: the coach assigned to a specialist work zone.
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == siteId);
            var coach = zone.GetComponentInChildren<NpcConversationAgent>(true).transform;

            // When: authored PPE attachments are inspected.
            var equipment = coach.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == primaryEquipment || item.name == secondaryEquipment)
                .ToArray();

            // Then: both scenario-specific pieces are real rendered meshes on the coach.
            Assert.That(equipment.Select(item => item.name),
                Is.EquivalentTo(new[] { primaryEquipment, secondaryEquipment }));
            Assert.That(equipment.All(item => item.GetComponentInChildren<MeshRenderer>(true) != null), Is.True);
        }

        [Test]
        public void HubModuleCards_KeepPrimarySelectionCopyReadableAtVrDistance()
        {
            // Given: the five module-selection portals viewed from the hub arrival point.
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub).ToArray();

            // When: subtitle and action labels are measured.
            Assert.That(portals, Has.Length.EqualTo(5));
            foreach (var portal in portals)
            {
                var labels = portal.GetComponentsInChildren<TextMesh>(true);
                var action = labels.Single(label => label.text == "CLICK / SELECT TO ENTER");
                var subtitle = labels.Single(label => label != action &&
                    label.anchor == TextAnchor.MiddleCenter && label.transform.localPosition.y < 1.3f &&
                    label.transform.localPosition.y > 1f);

                // Then: interaction copy uses a larger, high-contrast type treatment.
                Assert.That(action.characterSize, Is.GreaterThanOrEqualTo(0.058f), portal.name);
                Assert.That(subtitle.characterSize, Is.GreaterThanOrEqualTo(0.064f), portal.name);
                Assert.That(action.color.maxColorComponent, Is.GreaterThanOrEqualTo(0.72f), portal.name);
            }
        }
    }
}
