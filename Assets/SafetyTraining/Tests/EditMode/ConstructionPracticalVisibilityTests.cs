using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class ConstructionPracticalVisibilityTests
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [SetUp]
        public void OpenTrainingScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void FirstPpeStep_StartsInsideUsableEntryViewport()
        {
            // Given: the Construction entry camera and first required physical action.
            var camera = Camera.main;
            var portal = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Single(item => !item.ReturnsToHub && item.DestinationSite == TrainingSiteId.Construction);
            var ppe = Object.FindObjectsByType<ConstructionActionInteractable>(FindObjectsSortMode.None)
                .Single(item => item.StepIndex == 0);
            camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;
            Physics.SyncTransforms();

            // When: the learner sees the center of the PPE click surface on arrival.
            var model = ppe.transform.Find("RealAsset - metal_toolbox_1k");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var modelBounds = renderers.Skip(1).Aggregate(renderers[0].bounds, (bounds, item) =>
            {
                bounds.Encapsulate(item.bounds);
                return bounds;
            });
            var viewport = camera.WorldToViewportPoint(modelBounds.center);

            // Then: the required first step is in the clear work area above the mission HUD.
            Assert.That(viewport.z, Is.GreaterThan(0f), "PPE must be in front of the learner.");
            Assert.That(viewport.x, Is.InRange(0.12f, 0.78f),
                $"PPE viewport x is {viewport.x:F3}; it must not begin off-screen or under the coach lane.");
            Assert.That(viewport.y, Is.InRange(0.28f, 0.78f),
                $"PPE viewport y is {viewport.y:F3}; it must remain above the bottom mission HUD.");
        }

        [Test]
        public void ConstructionAsset_WhenCompleted_PreservesAuthoredPbrColors()
        {
            // Given: the imported PPE toolbox and its authored renderer colors.
            var ppe = Object.FindObjectsByType<ConstructionActionInteractable>(FindObjectsSortMode.None)
                .Single(item => item.StepIndex == 0);
            var renderers = ppe.GetComponentsInChildren<Renderer>(true);
            var originalColors = renderers.Select(item => item.sharedMaterial.color).ToArray();

            // When: the learner completes the PPE step.
            ppe.MarkComplete();

            // Then: completion does not repaint the real asset fluorescent green.
            for (var index = 0; index < renderers.Length; index++)
            {
                Assert.That(renderers[index].sharedMaterial.color, Is.EqualTo(originalColors[index]),
                    $"Completion must preserve authored PBR color on {renderers[index].name}.");
            }
        }

        [Test]
        public void SerializedConstructionPpe_RequiresReleaseInsidePlacementTarget()
        {
            // Given: the saved scene has been loaded exactly as the standalone player loads it.
            var controller = Object.FindFirstObjectByType<ConstructionHandsOnController>();
            var ppe = Object.FindObjectsByType<ConstructionActionInteractable>(FindObjectsSortMode.None)
                .Single(item => item.StepIndex == 0);
            typeof(ConstructionHandsOnController).GetMethod("Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            typeof(ConstructionActionInteractable).GetMethod("Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ppe, null);
            controller.Begin();

            // When: a click is attempted without moving the PPE kit.
            ppe.TryPerform();

            // Then: clicking cannot complete the hands-on task.
            Assert.That(controller.CompletedSteps, Is.Zero);
            Assert.That(ppe.Completed, Is.False);

            // When: the learner releases the kit inside its marked destination.
            ppe.transform.localPosition = ppe.TargetLocalPosition;
            ppe.TryPerform("DesktopDrag");

            // Then: the physical placement advances the mission.
            Assert.That(controller.CompletedSteps, Is.EqualTo(1));
            Assert.That(ppe.Completed, Is.True);
        }
    }
}
