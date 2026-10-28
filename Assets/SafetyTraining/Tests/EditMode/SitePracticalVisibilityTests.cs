using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class SitePracticalVisibilityTests
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [SetUp]
        public void OpenTrainingScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void EverySitePractical_StartsInsideCentralLearnerVisibilityBand()
        {
            // Given: all real-asset practical props in the generated non-construction sites.
            var actions = Object.FindObjectsByType<SitePracticalAction>(FindObjectsSortMode.None);

            // When: their learner-entry positions are evaluated in site-local coordinates.
            Assert.That(actions, Has.Length.EqualTo(12));
            foreach (var action in actions)
            {
                var zone = action.GetComponentInParent<SiteExperienceZone>();
                var position = zone.transform.InverseTransformPoint(action.transform.position);

                // Then: no task starts behind the coach or perimeter environment at either edge.
                Assert.That(position.x, Is.InRange(-2f, 2f), action.name);
                Assert.That(position.z, Is.InRange(0f, 2.1f), action.name);
            }
        }

        [Test]
        public void OutOfOrderSelection_DoesNotAdvancePractical()
        {
            // Given: an active warehouse practical with its three ordered actions.
            var controller = GameObject.Find("Warehouse").GetComponent<SitePracticalController>();
            var actions = controller.GetComponentsInChildren<SitePracticalAction>(true)
                .OrderBy(item => item.StepIndex).ToArray();
            typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            foreach (var action in actions)
                typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(action, null);
            controller.Begin();

            // When: the learner selects step two before step one.
            actions[1].TryPerform();

            // Then: neither progress nor the selected object changes state.
            Assert.That(controller.CompletedSteps, Is.Zero);
            Assert.That(actions[1].Completed, Is.False);
        }

        [Test]
        public void PracticalMarkers_AreVisuallySeparatedFromEntryCoach()
        {
            // Given: each site entry camera, its staged coach, and practical step markers.
            var camera = Camera.main;
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub && portal.DestinationSite != TrainingSiteId.Construction)
                .ToArray();
            var safeTargetField = typeof(NpcSiteCompanion).GetField("safeTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var viewerField = typeof(NpcSiteCompanion).GetField("viewer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // When: the learner's entry view is projected into viewport coordinates.
            foreach (var portal in portals)
            {
                var zone = zones.Single(item => item.SiteId == portal.DestinationSite);
                var companion = zone.GetComponentInChildren<NpcSiteCompanion>(true);
                camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;
                viewerField.SetValue(companion, camera);
                companion.SetAccompanying(true);
                var coachTarget = (Vector3)safeTargetField.GetValue(companion);
                var coachViewport = camera.WorldToViewportPoint(coachTarget + Vector3.up);

                // Then: every marker remains inside the view and outside the coach silhouette lane.
                foreach (var action in zone.GetComponentsInChildren<SitePracticalAction>(true))
                {
                    var marker = action.transform.Find("Practical Step Marker");
                    var markerViewport = camera.WorldToViewportPoint(marker.position);
                    Assert.That(markerViewport.z, Is.GreaterThan(0f), action.name);
                    Assert.That(markerViewport.x, Is.InRange(0.08f, 0.92f), action.name);
                    Assert.That(Mathf.Abs(markerViewport.x - coachViewport.x), Is.GreaterThanOrEqualTo(0.12f),
                        $"{portal.DestinationSite}: {action.name}");
                }
            }
        }

        [Test]
        public void PracticalProps_AreFirstInteractiveTargetsFromEntryCamera()
        {
            // Given: each non-construction learner-entry camera and its practical props.
            var camera = Camera.main;
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub && portal.DestinationSite != TrainingSiteId.Construction)
                .ToArray();
            var viewerField = typeof(NpcSiteCompanion).GetField("viewer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // When: a desktop interaction ray is aimed at each prop collider center.
            foreach (var portal in portals)
            {
                var zone = zones.Single(item => item.SiteId == portal.DestinationSite);
                var companion = zone.GetComponentInChildren<NpcSiteCompanion>(true);
                camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;
                viewerField.SetValue(companion, camera);
                companion.SetAccompanying(true);
                Physics.SyncTransforms();

                // Then: no coach or environment collider owns the ray before the intended action.
                foreach (var action in zone.GetComponentsInChildren<SitePracticalAction>(true))
                {
                    var target = action.GetComponent<Collider>().bounds.center;
                    var hits = Physics.RaycastAll(camera.transform.position,
                            (target - camera.transform.position).normalized, 100f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                        .OrderBy(hit => hit.distance).ToArray();
                    var firstInteractive = hits.Select(hit => hit.collider.transform)
                        .FirstOrDefault(transform => transform.GetComponentInParent<SitePortal>() != null ||
                                                     transform.GetComponentInParent<InspectionTarget>() != null ||
                                                     transform.GetComponentInParent<SitePracticalAction>() != null ||
                                                     transform.GetComponentInParent<NpcTalkInteractable>() != null);
                    Assert.That(firstInteractive, Is.Not.Null, action.name);
                    Assert.That(firstInteractive.GetComponentInParent<SitePracticalAction>(), Is.SameAs(action),
                        $"{portal.DestinationSite}: {action.name} first hit {firstInteractive.name}");
                }
            }
        }

        [Test]
        public void WarehouseAisleLoad_EntryViewProvidesRecognizableRealAssetFootprint()
        {
            // Given: the learner entry camera and real aisle-load cart.
            var camera = Camera.main;
            var portal = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Single(item => !item.ReturnsToHub && item.DestinationSite == TrainingSiteId.Warehouse);
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.Warehouse);
            var action = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 2);
            var model = action.transform.Find("RealAsset - industrial_storage_cart_1k");
            camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;
            Physics.SyncTransforms();

            // When: the cart's physical renderer bounds are projected into the entry viewport.
            var viewportRect = ProjectBounds(camera, CombinedBounds(model));

            // Then: the cart occupies a recognizable physical footprint instead of a sub-pixel point.
            Assert.That(viewportRect.width, Is.GreaterThanOrEqualTo(0.08f),
                $"Warehouse aisle-load cart viewport width is {viewportRect.width:F4}.");
            Assert.That(viewportRect.height, Is.GreaterThanOrEqualTo(0.08f),
                $"Warehouse aisle-load cart viewport height is {viewportRect.height:F4}.");
        }

        [Test]
        public void CurrentPracticalProp_IsTheFirstInteractiveTargetForEachSequentialStep()
        {
            // Given: each practical begins with only its current physical asset marker selectable.
            var camera = Camera.main;
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Where(item => item.SiteId != TrainingSiteId.Construction).ToArray();
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(item => !item.ReturnsToHub && item.DestinationSite != TrainingSiteId.Construction)
                .ToArray();

            foreach (var zone in zones)
            {
                var portal = portals.Single(item => item.DestinationSite == zone.SiteId);
                var controller = zone.GetComponent<SitePracticalController>();
                var actions = controller.GetComponentsInChildren<SitePracticalAction>(true)
                    .OrderBy(item => item.StepIndex).ToArray();
                typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);
                foreach (var action in actions)
                    typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(action, null);
                controller.Begin();
                camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;

                // When: the learner aims at each physical prop as that step becomes current.
                foreach (var action in actions)
                {
                    Physics.SyncTransforms();
                    var propCollider = action.GetComponent<Collider>();
                    Assert.That(propCollider.enabled, Is.True,
                        $"{zone.SiteId}: current prop {action.ActionName} must be grabbable.");

                    var target = propCollider.bounds.center;
                    var firstInteractive = Physics.RaycastAll(camera.transform.position,
                            (target - camera.transform.position).normalized, 100f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                        .OrderBy(hit => hit.distance)
                        .Select(hit => hit.collider.transform)
                        .FirstOrDefault(transform => transform.GetComponentInParent<SitePortal>() != null ||
                                                     transform.GetComponentInParent<InspectionTarget>() != null ||
                                                     transform.GetComponentInParent<SitePracticalAction>() != null ||
                                                     transform.GetComponentInParent<NpcTalkInteractable>() != null);

                    // Then: the visible prop selects its own real asset, never a portal or another step.
                    Assert.That(firstInteractive, Is.Not.Null, action.ActionName);
                    Assert.That(firstInteractive.GetComponentInParent<SitePracticalAction>(), Is.SameAs(action),
                        $"{zone.SiteId}: {action.ActionName} prop first hit {firstInteractive.name}");
                    action.transform.localPosition = action.TargetLocalPosition;
                    action.TryPerform();
                }
            }
        }

        [Test]
        public void PracticalMarkers_ShowOnlyTheCurrentStepDuringSequentialProgress()
        {
            var controller = GameObject.Find("Warehouse").GetComponent<SitePracticalController>();
            var actions = controller.GetComponentsInChildren<SitePracticalAction>(true)
                .OrderBy(item => item.StepIndex).ToArray();
            typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            foreach (var action in actions)
                typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(action, null);
            controller.Begin();

            foreach (var current in actions)
            {
                foreach (var candidate in actions)
                {
                    var markerRenderers = candidate.transform.Find("Practical Step Marker")
                        .GetComponentsInChildren<Renderer>(true);
                    Assert.That(markerRenderers, Is.Not.Empty, candidate.ActionName);
                    Assert.That(markerRenderers.All(item => item.enabled), Is.EqualTo(candidate == current),
                        $"Only current STEP marker should be visible: {candidate.ActionName}");
                }
                current.transform.localPosition = current.TargetLocalPosition;
                current.TryPerform();
            }

            Assert.That(actions.SelectMany(item => item.transform.Find("Practical Step Marker")
                .GetComponentsInChildren<Renderer>(true)).All(item => !item.enabled), Is.True,
                "Completed practical must not leave marker labels in the scene.");
        }

        [Test]
        public void PracticalMarkerClick_WhenActionAlsoHasInspectionTarget_CannotCompletePlacement()
        {
            // Given: Fire step one is both an inspectable hazard and the current practical action.
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.FireResponse);
            var controller = zone.GetComponent<SitePracticalController>();
            var action = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(action, null);
            controller.Begin();
            var marker = action.transform.Find("Practical Step Marker");
            Assert.That(marker.GetComponentInParent<InspectionTarget>(), Is.Not.Null);
            Assert.That(marker.GetComponent<Collider>().enabled, Is.False,
                "STEP labels guide the learner but must not act as completion buttons.");

            // When: the learner attempts completion without moving the physical obstruction.
            action.TryPerform();

            // Then: neither the practical nor the shared inspection state is consumed by a label click.
            Assert.That(controller.CompletedSteps, Is.Zero);
            Assert.That(action.Completed, Is.False);
        }

        [Test]
        public void FireObstruction_WhenCompleted_StagesClearOfLearnerEntryView()
        {
            // Given: the fire-response learner entry and the physical crate obstruction.
            var portal = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Single(item => !item.ReturnsToHub && item.DestinationSite == TrainingSiteId.FireResponse);
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.FireResponse);
            var obstruction = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            var learnerEye = portal.ArrivalPoint + Vector3.up * 1.36f;

            // When: the learner clears the obstruction from the fire point.
            obstruction.MarkComplete();
            Physics.SyncTransforms();

            // Then: the staged crate stack is not placed in the learner's immediate foreground.
            var closestPoint = obstruction.GetComponent<Collider>().bounds.ClosestPoint(learnerEye);
            Assert.That(Vector3.Distance(learnerEye, closestPoint), Is.GreaterThanOrEqualTo(3f),
                "Completed fire obstruction must remain at least three metres from the learner entry eye point.");
        }

        [Test]
        public void PracticalAsset_WhenCompleted_PreservesAuthoredPbrColors()
        {
            // Given: a real textured fire crate and its existing per-renderer property state.
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.FireResponse);
            var obstruction = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            var renderers = obstruction.GetComponentsInChildren<Renderer>(true);
            var originalColorOverrides = renderers.Select(ReadColorOverrides).ToArray();

            // When: the learner completes the control and the prop moves to safe staging.
            obstruction.MarkComplete();

            // Then: completion feedback must not repaint the physical asset fluorescent green.
            for (var index = 0; index < renderers.Length; index++)
            {
                Assert.That(ReadColorOverrides(renderers[index]), Is.EqualTo(originalColorOverrides[index]),
                    $"Completion must preserve authored PBR color properties on {renderers[index].name}.");
            }
        }

        [Test]
        public void FireObstruction_WhenCompleted_RemainsOutsideEmergencyEgressLane()
        {
            // Given: the fire-response obstruction and marked emergency egress lane.
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.FireResponse);
            var obstruction = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            var egressLane = zone.transform.Find("Emergency Egress Lane");

            // When: the obstruction is moved to its completed staging position.
            obstruction.MarkComplete();
            Physics.SyncTransforms();

            // Then: clearing the extinguisher never creates a second hazard in the exit route.
            Assert.That(obstruction.GetComponent<Collider>().bounds.Intersects(CombinedBounds(egressLane)), Is.False);
        }

        [Test]
        public void FireResponseKit_WhenCompleted_RemainsOutsideEmergencyEgressLane()
        {
            // Given: the fire response kit and the marked emergency egress lane.
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.FireResponse);
            var responseKit = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 1);
            var egressLane = zone.transform.Find("Emergency Egress Lane");

            // When: the response kit is staged beside the accessible fire point.
            responseKit.MarkComplete();
            Physics.SyncTransforms();

            // Then: readiness equipment does not become a trip hazard in the escape route.
            Assert.That(responseKit.GetComponent<Collider>().bounds.Intersects(CombinedBounds(egressLane)), Is.False,
                "Completed fire response kit must remain outside the emergency egress lane.");
        }

        [Test]
        public void ChemicalContainmentKit_WhenCompleted_RemainsOutsideLeakingDrumSplashZone()
        {
            var zone = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None)
                .Single(item => item.SiteId == TrainingSiteId.ChemicalProcessing);
            var containmentKit = zone.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            var leakingDrum = zone.transform.Find("Leaking Solvent Drum");
            var splashZone = CombinedBounds(leakingDrum);
            splashZone.Expand(new Vector3(1.2f, 0f, 1.2f));

            containmentKit.MarkComplete();
            Physics.SyncTransforms();

            Assert.That(containmentKit.GetComponent<Collider>().bounds.Intersects(splashZone), Is.False,
                "Completed containment equipment must be staged outside the leaking drum splash zone.");
        }

        static Bounds CombinedBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var item in renderers.Skip(1))
                bounds.Encapsulate(item.bounds);
            return bounds;
        }

        static (Color color, Color baseColor) ReadColorOverrides(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return (block.GetColor("_Color"), block.GetColor("_BaseColor"));
        }

        static Rect ProjectBounds(Camera camera, Bounds bounds)
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
            {
                var corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                    y == 0 ? bounds.min.y : bounds.max.y,
                    z == 0 ? bounds.min.z : bounds.max.z);
                var viewport = camera.WorldToViewportPoint(corner);
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
