using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class SceneCompositionTests
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [SetUp]
        public void OpenTrainingScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void Scene_ContainsFiveWorkplaceScenarios()
        {
            var targets = Object.FindObjectsByType<InspectionTarget>(FindObjectsSortMode.None);

            Assert.That(targets, Has.Length.EqualTo(20));
            foreach (var site in (TrainingSiteId[])System.Enum.GetValues(typeof(TrainingSiteId)))
            {
                var siteTargets = targets.Where(target => target.SiteId == site).ToArray();
                Assert.That(siteTargets, Has.Length.EqualTo(4), site.ToString());
                Assert.That(siteTargets.Count(target => target.IsHazard), Is.EqualTo(2), site.ToString());
                Assert.That(siteTargets.Count(target => !target.IsHazard), Is.EqualTo(2), site.ToString());
            }
        }

        [Test]
        public void Scene_ContainsNavigationHudAndCoordinator()
        {
            Assert.That(Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None), Has.Length.EqualTo(10));
            Assert.That(Object.FindObjectsByType<TrainingHud>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<NpcChatPanel>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<TrainingCoordinator>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<SiteIsolationController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Camera.main, Is.Not.Null);
        }

        [Test]
        public void ReturnPortals_DoNotOverlapInspectableSiteObjects()
        {
            var returnPortals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => portal.ReturnsToHub).ToArray();
            var entryPortals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub).ToArray();
            var camera = Camera.main;

            foreach (var portal in returnPortals)
            {
                var portalCollider = portal.GetComponent<BoxCollider>();
                var overlaps = Physics.OverlapBox(portalCollider.bounds.center, portalCollider.bounds.extents,
                        portal.transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide)
                    .Where(collider => !collider.transform.IsChildOf(portal.transform) &&
                        collider.GetComponentInParent<InspectionTarget>() != null)
                    .Select(collider => collider.name).ToArray();
                Assert.That(overlaps, Is.Empty,
                    $"{portal.DestinationSite} return portal must have a dedicated click lane: " +
                    string.Join(", ", overlaps));
                var entry = entryPortals.Single(candidate =>
                    candidate.DestinationSite == portal.DestinationSite);
                camera.transform.position = entry.ArrivalPoint + Vector3.up * 1.36f;
                var direction = portalCollider.bounds.center - camera.transform.position;
                var firstInteractive = Physics.RaycastAll(camera.transform.position, direction.normalized,
                        direction.magnitude + 0.1f, Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Collide)
                    .OrderBy(hit => hit.distance)
                    .Select(hit => hit.collider.transform)
                    .FirstOrDefault(target => target.GetComponentInParent<SitePortal>() != null ||
                        target.GetComponentInParent<InspectionTarget>() != null ||
                        target.GetComponentInParent<NpcTalkInteractable>() != null);
                Assert.That(firstInteractive?.GetComponentInParent<SitePortal>(), Is.SameAs(portal),
                    $"{portal.DestinationSite} return portal must be the first clickable object in its lane.");
            }
        }

        [Test]
        public void Scene_IsolationContractOwnsFiveSitesAndThreeHubRoots()
        {
            var isolation = Object.FindFirstObjectByType<SiteIsolationController>();
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);

            Assert.That(isolation.SiteCount, Is.EqualTo(5));
            Assert.That(isolation.HubRootCount, Is.EqualTo(3));
            Assert.That(zones, Has.Length.EqualTo(5));
            foreach (var zone in zones)
            {
                var names = zone.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                Assert.That(names, Does.Contain("RealEnvironment - Opaque Hoarding Entry Center"), zone.name);
                Assert.That(names, Does.Contain("Isolation Collider Entry Center"), zone.name);
                Assert.That(zone.transform.Find("Site Lighting and Reflection"), Is.Not.Null, zone.name);
            }
        }

        [Test]
        public void Scene_IsolatedSitesHaveBalancedWallFillLighting()
        {
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);

            foreach (var zone in zones)
            {
                var fills = zone.transform.Find("Site Lighting and Reflection")
                    .GetComponentsInChildren<Light>(true)
                    .Where(light => light.name.StartsWith("Wall Fill Light")).ToArray();
                Assert.That(fills, Has.Length.EqualTo(2), zone.name);
                Assert.That(fills.All(light => light.type == LightType.Point), Is.True, zone.name);
                Assert.That(fills.All(light => light.intensity >= 0.65f && light.range >= 7f), Is.True, zone.name);
            }
        }

        [Test]
        public void Scene_SiteEnvironmentsUseDiscreteMeshesNotFlatImageBackdrops()
        {
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);

            foreach (var zone in zones)
            {
                Assert.That(zone.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty, zone.name);
                Assert.That(zone.GetComponentsInChildren<RawImage>(true), Is.Empty, zone.name);
                var meshes = zone.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(meshes.Length, Is.GreaterThanOrEqualTo(20), zone.name);
                Assert.That(meshes.Where(item => item.sharedMesh != null)
                    .Select(item => item.sharedMesh.name), Has.None.EqualTo("Quad"), zone.name);
                Assert.That(zone.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name.StartsWith("RealEnvironment -")),
                    Is.GreaterThanOrEqualTo(6), zone.name);
            }
        }

        [Test]
        public void Scene_IsolationControllerShowsExactlyOneSiteOrOnlyHub()
        {
            var isolation = Object.FindFirstObjectByType<SiteIsolationController>();

            isolation.ShowSite(TrainingSiteId.FireResponse);
            Assert.That(isolation.HubVisible, Is.False);
            Assert.That(isolation.VisibleSiteCount, Is.EqualTo(1));
            Assert.That(isolation.IsSiteVisible(TrainingSiteId.FireResponse), Is.True);

            isolation.ShowHub();
            Assert.That(isolation.HubVisible, Is.True);
            Assert.That(isolation.VisibleSiteCount, Is.EqualTo(0));
        }

        [Test]
        public void Scene_ModePortalArrivalsAreInsideClosedSiteBoundaries()
        {
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub).ToArray();
            Assert.That(portals, Has.Length.EqualTo(5));
            foreach (var portal in portals)
            {
                var zone = zones.Single(item => item.SiteId == portal.DestinationSite);
                var localArrival = portal.ArrivalPoint - zone.transform.position;
                Assert.That(localArrival.x, Is.InRange(-4.2f, 4.2f), portal.name);
                Assert.That(localArrival.z, Is.InRange(-4f, 3.8f), portal.name);
                Assert.That(localArrival.y, Is.InRange(0f, 0.1f), portal.name);
            }
        }

        [Test]
        public void Scene_HudUsesProfessionalCanvasAssetsWithoutPrimitiveBackplate()
        {
            var hud = Object.FindFirstObjectByType<TrainingHud>();
            var canvas = hud.GetComponent<Canvas>();
            var panels = hud.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.Contains("Shell")).ToArray();

            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(80));
            Assert.That(hud.transform.parent, Is.Null);
            Assert.That(GameObject.Find("HUD Backplate"), Is.Null);
            Assert.That(GameObject.Find("HUD Accent"), Is.Null);
            Assert.That(panels, Has.Length.GreaterThanOrEqualTo(3));
            Assert.That(panels.All(panel => panel.sprite != null), Is.True);
            Assert.That(panels.All(panel => panel.material.name.Contains("Safety HUD UI")), Is.True);
            var feedbackShell = hud.transform.Find("Feedback Shell").GetComponent<RectTransform>();
            var progressLabel = hud.transform.Find("Feedback Shell/Progress Label").GetComponent<RectTransform>();
            var feedback = hud.transform.Find("Feedback Shell/Feedback").GetComponent<Text>();
            Assert.That(feedbackShell.anchoredPosition.y, Is.GreaterThanOrEqualTo(12f),
                "The mission brief must remain fully inside the bottom edge of the viewport.");
            Assert.That(feedbackShell.sizeDelta.y, Is.LessThanOrEqualTo(145f));
            Assert.That(feedback.rectTransform.rect.width, Is.GreaterThanOrEqualTo(700f),
                "The mission brief needs enough width for complete zone-specific instructions.");
            Assert.That(feedback.fontSize, Is.LessThanOrEqualTo(21),
                "The mission brief type must fit long instructions without truncating their final control noun.");
            Assert.That(progressLabel.anchoredPosition.y + progressLabel.sizeDelta.y,
                Is.LessThanOrEqualTo(feedbackShell.sizeDelta.y),
                "The session label must remain inside the mission panel.");
        }

        [Test]
        public void Scene_ChatUsesOverlaySidecarPlacementWithoutWorldOcclusion()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var canvas = chat.GetComponent<Canvas>();
            var scaler = chat.GetComponent<CanvasScaler>();
            var panel = chat.transform.Find("NPC Chat Panel").GetComponent<RectTransform>();

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(100));
            Assert.That(chat.transform.parent, Is.Null);
            Assert.That(panel.anchorMin.x, Is.EqualTo(0f));
            Assert.That(panel.anchorMin.y, Is.EqualTo(0f));
            Assert.That(panel.anchoredPosition.x, Is.GreaterThanOrEqualTo(20f));
            Assert.That(panel.anchoredPosition.y, Is.GreaterThanOrEqualTo(20f));
            Assert.That(panel.sizeDelta.x, Is.LessThanOrEqualTo(scaler.referenceResolution.x * 0.45f));
            Assert.That(panel.sizeDelta.y, Is.LessThanOrEqualTo(scaler.referenceResolution.y * 0.46f));
        }

        [Test]
        public void Scene_ChatUsesTransparentGlassWithoutFadingInteractiveControls()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var panel = chat.transform.Find("NPC Chat Panel").GetComponent<Image>();
            var input = chat.transform.Find("NPC Chat Panel/Chat Input").GetComponent<Image>();
            var send = chat.transform.Find("NPC Chat Panel/Send Button").GetComponent<Image>();

            Assert.That(panel.color.r, Is.EqualTo(0.063f).Within(0.002f));
            Assert.That(panel.color.g, Is.EqualTo(0.11f).Within(0.002f));
            Assert.That(panel.color.b, Is.EqualTo(0.153f).Within(0.002f));
            Assert.That(panel.color.a, Is.EqualTo(0.72f).Within(0.01f));
            Assert.That(input.color.a, Is.EqualTo(1f));
            Assert.That(send.color.a, Is.EqualTo(1f));
        }

        [Test]
        public void Scene_ChatProvidesExplicitCloseControl()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();

            var closeButton = chat.transform.Find("NPC Chat Panel/Close Button");

            Assert.That(closeButton, Is.Not.Null);
            Assert.That(closeButton.GetComponent<Button>(), Is.Not.Null);
        }

        [Test]
        public void Scene_ChatProvidesMouseAndXrUiEventDispatch()
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var canvas = chat.GetComponent<Canvas>();

            Assert.That(eventSystems, Has.Length.EqualTo(1));
            var inputModule = eventSystems[0].GetComponent<XRUIInputModule>();
            Assert.That(inputModule, Is.Not.Null);
            Assert.That(inputModule.enableMouseInput, Is.True);
            Assert.That(inputModule.enableTouchInput, Is.True);
            Assert.That(inputModule.enableXRInput, Is.True);
            Assert.That(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null);
        }

        [Test]
        public void Scene_ChatTranscriptFitsLongCoachResponsesWithoutTruncation()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var transcript = chat.transform.Find("NPC Chat Panel/Coach Response").GetComponent<Text>();

            Assert.That(transcript.resizeTextForBestFit, Is.True);
            Assert.That(transcript.resizeTextMinSize, Is.LessThanOrEqualTo(18));
            Assert.That(transcript.resizeTextMaxSize, Is.EqualTo(22));
            Assert.That(transcript.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
        }

        [Test]
        public void LlmCoachPrompt_RequestsNaturalAnswerWithoutGroundingMetaLanguage()
        {
            var promptField = typeof(OpenAiCompatibleConversationService).GetField("SystemPrompt",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(promptField, Is.Not.Null, "LLM coach must expose a stable system-prompt quality contract.");
            var prompt = (string)promptField.GetRawConstantValue();
            Assert.That(prompt, Does.Contain("Answer the learner directly"));
            Assert.That(prompt, Does.Contain("Do not mention verified facts"));
            Assert.That(prompt, Does.Contain("Never refer to the learner's progress"));
        }

        [Test]
        public void ChatSendControl_SubmitsCurrentInputAfterRuntimeBinding()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var agent = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None).First();
            var input = chat.transform.Find("NPC Chat Panel/Chat Input").GetComponent<InputField>();
            var send = chat.transform.Find("NPC Chat Panel/Send Button").GetComponent<Button>();
            typeof(NpcConversationAgent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(agent, null);
            typeof(NpcChatPanel).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(chat, null);
            typeof(NpcChatPanel).GetField("activeAgent", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(chat, agent);
            input.text = "What is the highest-priority control?";

            send.onClick.Invoke();

            Assert.That(input.text, Is.Empty);
        }

        [Test]
        public void NpcClick_WithOverlayChat_DoesNotStartUnsolicitedCoachRequest()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var talk = Object.FindFirstObjectByType<NpcTalkInteractable>();
            var agent = talk.GetComponent<NpcConversationAgent>();
            var coordinator = Object.FindFirstObjectByType<TrainingCoordinator>();
            typeof(TrainingCoordinator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(coordinator, null);
            typeof(NpcConversationAgent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(agent, null);
            typeof(NpcChatPanel).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(chat, null);
            typeof(NpcTalkInteractable).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(talk, null);
            var initialCoachTurns = coordinator.CompletedCoachTurns;

            talk.Ask();

            Assert.That(coordinator.CompletedCoachTurns, Is.EqualTo(initialCoachTurns),
                "Clicking a mentor should open the learner-led chat without racing an automatic prompt.");
            chat.Close();
        }

        [Test]
        public void NpcClick_WithOverlayChat_DoesNotArmAutomaticDismissal()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var talk = Object.FindFirstObjectByType<NpcTalkInteractable>();
            typeof(NpcChatPanel).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(chat, null);
            typeof(NpcTalkInteractable).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(talk, null);

            talk.Ask();

            var dismissal = typeof(NpcTalkInteractable).GetField("dismissal",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dismissal.GetValue(talk), Is.Null,
                "Overlay chat must remain open until the learner closes it or leaves the site.");
            chat.Close();
        }

        [Test]
        public void NpcChatClose_BlocksTheClosingClickFromActivatingWorldObjects()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var talk = Object.FindFirstObjectByType<NpcTalkInteractable>();
            var agent = talk.GetComponent<NpcConversationAgent>();
            typeof(NpcChatPanel).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(chat, null);

            chat.Open(agent, agent.name);
            Assert.That(DesktopPointerInputGate.CanUseWorldPointer, Is.False,
                "World objects must be blocked before the UI button receives its mouse-up click.");
            chat.Close();

            Assert.That(DesktopPointerInputGate.CanUsePointer, Is.False,
                "The click that closes chat must not pass through to a portal or inspection target behind it.");
            DesktopPointerInputGate.BlockFor(0f);
        }

        [Test]
        public void Scene_NpcsHaveClickableSpeechBubblesAndObjectsHaveHoverFeedback()
        {
            var coaches = Object.FindObjectsByType<NpcTalkInteractable>(FindObjectsSortMode.None);
            var targets = Object.FindObjectsByType<InspectionTarget>(FindObjectsSortMode.None);

            Assert.That(coaches, Has.Length.EqualTo(5));
            foreach (var coach in coaches)
            {
                var bubble = coach.transform.Find("NPC Speech Bubble");
                Assert.That(coach.GetComponent<InteractiveHoverFeedback>(), Is.Not.Null, coach.name);
                Assert.That(bubble, Is.Not.Null, coach.name);
                Assert.That(bubble.gameObject.activeSelf, Is.False, coach.name);
                Assert.That(bubble.Find("Dialogue Backplate"), Is.Not.Null, coach.name);
                Assert.That(bubble.GetComponentInChildren<TMP_Text>(true), Is.Not.Null, coach.name);
            }

            Assert.That(targets.All(target => target.GetComponent<InteractiveHoverFeedback>() != null), Is.True);
        }

        [Test]
        public void SiteEntry_StagesCoachInsideVisibleConversationArc()
        {
            var camera = Camera.main;
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => !portal.ReturnsToHub).ToArray();
            var returnPortals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None)
                .Where(portal => portal.ReturnsToHub).ToArray();
            var safeTargetField = typeof(NpcSiteCompanion).GetField("safeTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var viewerField = typeof(NpcSiteCompanion).GetField("viewer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(safeTargetField, Is.Not.Null);
            Assert.That(viewerField, Is.Not.Null);
            foreach (var portal in portals)
            {
                var companion = zones.Single(zone => zone.SiteId == portal.DestinationSite)
                    .GetComponentInChildren<NpcSiteCompanion>(true);
                camera.transform.position = portal.ArrivalPoint + Vector3.up * 1.36f;
                viewerField.SetValue(companion, camera);

                companion.SetAccompanying(true);

                var followTarget = (Vector3)safeTargetField.GetValue(companion);
                var horizontalClearance = Vector2.Distance(
                    new Vector2(followTarget.x, followTarget.z),
                    new Vector2(camera.transform.position.x, camera.transform.position.z));
                Assert.That(horizontalClearance, Is.GreaterThanOrEqualTo(1.5f), portal.DestinationSite.ToString());
                var viewerForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                var viewerRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
                var targetDirection = Vector3.ProjectOnPlane(followTarget - camera.transform.position, Vector3.up).normalized;
                Assert.That(Vector3.Dot(viewerForward, targetDirection), Is.GreaterThanOrEqualTo(0.85f),
                    $"{portal.DestinationSite} coach must stage inside the learner's forward conversation arc.");
                var lateralOffset = Vector3.Dot(
                    Vector3.ProjectOnPlane(followTarget - camera.transform.position, Vector3.up), viewerRight);
                Assert.That(lateralOffset, Is.GreaterThanOrEqualTo(0.55f),
                    $"{portal.DestinationSite} coach must remain beside, not directly in front of, the learner.");
                var blockingColliders = Physics.OverlapCapsule(followTarget + Vector3.up * 0.48f,
                        followTarget + Vector3.up * 1.55f, 0.38f, Physics.AllLayers,
                        QueryTriggerInteraction.Ignore)
                    .Where(collider => !collider.transform.IsChildOf(companion.transform) &&
                        collider.GetType().Name != "TerrainCollider" && !collider.name.Contains("Floor") &&
                        !collider.name.Contains("Ground"))
                    .Select(collider => collider.name).ToArray();
                Assert.That(companion.IsPlacementClear(followTarget), Is.True,
                    $"{portal.DestinationSite} coach must not overlap site props at entry: " +
                    string.Join(", ", blockingColliders));
                var returnPortal = returnPortals.Single(candidate =>
                    candidate.DestinationSite == portal.DestinationSite);
                var returnCollider = returnPortal.GetComponent<Collider>();
                Assert.That(Vector3.Distance(followTarget, returnCollider.bounds.ClosestPoint(followTarget)),
                    Is.GreaterThanOrEqualTo(1.8f),
                    $"{portal.DestinationSite} coach must not block the return portal.");
                var coachViewport = camera.WorldToViewportPoint(followTarget + Vector3.up);
                var returnViewport = camera.WorldToViewportPoint(returnCollider.bounds.center);
                Assert.That(Vector2.Distance(coachViewport, returnViewport), Is.GreaterThanOrEqualTo(0.18f),
                    $"{portal.DestinationSite} coach must remain visually separated from the return portal.");
                Assert.That(Vector3.Distance(companion.transform.position, followTarget), Is.LessThanOrEqualTo(0.05f),
                    $"{portal.DestinationSite} coach must be visible immediately after site entry.");
            }
        }

        [Test]
        public void Certification_CompletesWithoutRequiringSafeObjectFalsePositives()
        {
            var coordinator = Object.FindFirstObjectByType<TrainingCoordinator>();
            typeof(TrainingCoordinator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(coordinator, null);
            var hazards = Object.FindObjectsByType<InspectionTarget>(FindObjectsSortMode.None)
                .Where(target => target.IsHazard).ToArray();
            var sessions = (System.Collections.Generic.Dictionary<TrainingSiteId, TrainingSession>)
                typeof(TrainingCoordinator).GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(coordinator);
            var guidedPlan = (GuidedSessionPlan)typeof(TrainingCoordinator)
                .GetField("guidedPlan", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(coordinator);
            var practical = Object.FindFirstObjectByType<ConstructionHandsOnController>();
            typeof(ConstructionHandsOnController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(practical, null);
            typeof(ConstructionHandsOnController)
                .GetField("complete", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(practical, true);
            typeof(ConstructionHandsOnController)
                .GetField("nextStep", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(practical, practical.TotalSteps);
            foreach (var sitePractical in Object.FindObjectsByType<SitePracticalController>(FindObjectsSortMode.None))
            {
                typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(sitePractical, null);
                sitePractical.Begin();
                foreach (var action in sitePractical.GetComponentsInChildren<SitePracticalAction>(true)
                             .OrderBy(item => item.StepIndex))
                {
                    typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(action, null);
                    action.transform.localPosition = action.TargetLocalPosition;
                    action.TryPerform();
                }
            }

            foreach (var hazard in hazards)
                sessions[hazard.SiteId].Inspect(hazard.TargetId);
            foreach (var site in (TrainingSiteId[])System.Enum.GetValues(typeof(TrainingSiteId)))
            {
                guidedPlan.Advance(site, GuidedSessionPlan.MinimumSiteSeconds);
                guidedPlan.RecordCoachTurn(site);
                guidedPlan.RecordCoachTurn(site);
            }

            Assert.That(coordinator.AllSitesComplete, Is.True);
            Assert.That(coordinator.ReviewedConditionCount, Is.EqualTo(10));
            Assert.That(coordinator.IdentifiedHazardCount, Is.EqualTo(coordinator.TotalHazardCount));
            Assert.That(coordinator.CertificationComplete, Is.True);
            Assert.That(coordinator.GuidedProgress01, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Scene_AllNpcPosesSupportIdleAndEncouragement()
        {
            var poses = Object.FindObjectsByType<NpcRelaxedPose>(FindObjectsSortMode.None);

            Assert.That(poses, Has.Length.EqualTo(5));
            foreach (var pose in poses)
            {
                Assert.That(pose.enabled, Is.True, pose.name);
                pose.PlayEncouragement(false);
                Assert.That(pose.IsEncouraging, Is.True, pose.name);
            }
        }

        [Test]
        public void DesktopPointerInput_IsBlockedImmediatelyAfterWindowFocus()
        {
            DesktopPointerInputGate.BlockFor(10f);
            Assert.That(DesktopPointerInputGate.CanUsePointer, Is.False);

            DesktopPointerInputGate.BlockFor(0f);
            Assert.That(DesktopPointerInputGate.CanUsePointer, Is.True);
        }

        [Test]
        public void Scene_AllNpcCoachesUseNaturalIdleWalkController()
        {
            var coaches = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None);

            Assert.That(coaches, Has.Length.EqualTo(5));
            foreach (var coach in coaches)
            {
                var animator = coach.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null, coach.name);
                Assert.That(animator.isHuman, Is.True, coach.name);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null, coach.name);
                Assert.That(animator.runtimeAnimatorController.name, Does.Contain("NpcLocomotion"), coach.name);
                Assert.That(animator.parameters.Any(parameter => parameter.name == "Moving" &&
                    parameter.type == AnimatorControllerParameterType.Bool), Is.True, coach.name);
            }
        }

        [Test]
        public void NpcCompanionsCanRecoverFormationAtDesktopTraversalSpeed()
        {
            var explorer = Object.FindFirstObjectByType<DesktopExplorerController>();
            var playerSpeed = (float)typeof(DesktopExplorerController)
                .GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(explorer);
            var catchUpSpeedField = typeof(NpcSiteCompanion)
                .GetField("catchUpSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            var catchUpDistanceField = typeof(NpcSiteCompanion)
                .GetField("catchUpDistance", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(catchUpDistanceField, Is.Not.Null);
            foreach (var companion in Object.FindObjectsByType<NpcSiteCompanion>(FindObjectsSortMode.None))
            {
                var catchUpSpeed = (float)catchUpSpeedField.GetValue(companion);
                var catchUpDistance = (float)catchUpDistanceField.GetValue(companion);
                Assert.That(catchUpSpeed, Is.GreaterThanOrEqualTo(playerSpeed * 1.2f), companion.name);
                Assert.That(catchUpDistance, Is.LessThanOrEqualTo(2.2f), companion.name);
            }
        }

        [Test]
        public void Scene_TrainingZonesAreVisuallyIsolatedAndWidelySeparated()
        {
            var zones = Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None);

            Assert.That(zones, Has.Length.EqualTo(5));
            for (var first = 0; first < zones.Length; first++)
            for (var second = first + 1; second < zones.Length; second++)
                Assert.That(Vector3.Distance(zones[first].transform.position, zones[second].transform.position),
                    Is.GreaterThanOrEqualTo(175f), $"{zones[first].name} / {zones[second].name}");
            foreach (var zone in zones)
            {
                Assert.That(zone.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name.StartsWith("RealEnvironment - Opaque Hoarding")),
                    Is.GreaterThanOrEqualTo(6), zone.name);
                Assert.That(zone.GetComponentsInChildren<BoxCollider>(true)
                    .Count(collider => collider.name.StartsWith("Isolation Collider")),
                    Is.EqualTo(6), zone.name);
            }
        }

        [Test]
        public void Scene_HasReturnPortalInsideEveryIsolatedZone()
        {
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None);

            Assert.That(portals.Count(portal => portal.ReturnsToHub), Is.EqualTo(5));
            Assert.That(portals.Count(portal => !portal.ReturnsToHub), Is.EqualTo(5));
        }

        [Test]
        public void Scene_UsesIndustrialKiosksInsteadOfPrimitivePortalPads()
        {
            var portals = Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None);
            var modePortals = portals.Where(portal => !portal.ReturnsToHub).ToArray();

            Assert.That(modePortals, Has.Length.EqualTo(5));
            foreach (var portal in portals)
            {
                Assert.That(portal.GetComponent<BoxCollider>(), Is.Not.Null, portal.name);
                Assert.That(portal.GetComponent<Renderer>(), Is.Null, portal.name);
                Assert.That(portal.GetComponentInChildren<Light>(true), Is.Not.Null, portal.name);
                Assert.That(portal.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(6),
                    portal.name);
            }

            foreach (var portal in modePortals)
            {
                Assert.That(portal.transform.Find("Industrial Access Kiosk"), Is.Not.Null, portal.name);
                Assert.That(portal.transform.Cast<Transform>().Any(child => child.name.StartsWith("Mode Prop -")),
                    Is.True, portal.name);
            }
        }

        [Test]
        public void Scene_StartMenuIsAnEnclosedModuleSelectionLobby()
        {
            var lobby = GameObject.Find("Construction Safety Module Selection Lobby");

            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.transform.Find("Lobby PBR Concrete Floor"), Is.Not.Null);
            Assert.That(lobby.transform.Find("Lobby Rear Boundary"), Is.Not.Null);
            Assert.That(lobby.transform.Cast<Transform>().Count(child => child.name.StartsWith("Lobby Back Wall Panel")),
                Is.EqualTo(7));
            Assert.That(lobby.transform.Cast<Transform>().Count(child => child.name.StartsWith("Lobby Left Wall Panel")),
                Is.EqualTo(5));
            Assert.That(lobby.transform.Cast<Transform>().Count(child => child.name.StartsWith("Lobby Right Wall Panel")),
                Is.EqualTo(5));
            Assert.That(lobby.transform.Cast<Transform>().Count(child => child.name.StartsWith("Lobby Ceiling Bay")),
                Is.EqualTo(5));
            Assert.That(lobby.GetComponentsInChildren<Collider>(true).Length, Is.GreaterThanOrEqualTo(24));
        }

        [Test]
        public void Scene_HasProximityBriefingAndOneCompanionPerSite()
        {
            Assert.That(Object.FindObjectsByType<SiteExperienceDirector>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<SiteExperienceZone>(FindObjectsSortMode.None), Has.Length.EqualTo(5));
            Assert.That(Object.FindObjectsByType<NpcSiteCompanion>(FindObjectsSortMode.None), Has.Length.EqualTo(5));
            Physics.SyncTransforms();
            foreach (var companion in Object.FindObjectsByType<NpcSiteCompanion>(FindObjectsSortMode.None))
            {
                var position = companion.transform.position;
                var blockers = Physics.OverlapCapsule(position + Vector3.up * 0.48f,
                        position + Vector3.up * 1.55f, 0.38f, Physics.AllLayers,
                        QueryTriggerInteraction.Ignore)
                    .Where(collider => !collider.transform.IsChildOf(companion.transform) &&
                                       collider.GetType().Name != "TerrainCollider" &&
                                       !collider.name.Contains("Floor") && !collider.name.Contains("Ground"))
                    .Select(collider => collider.name).ToArray();
                Assert.That(companion.IsPlacementClear(companion.transform.position), Is.True,
                    $"{companion.transform.parent.name}/{companion.name}: {string.Join(", ", blockers)}");
            }
        }

        [Test]
        public void Capture_PreservesActiveNpcChatCanvas()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var coach = Object.FindFirstObjectByType<NpcConversationAgent>();
            chat.Open(coach, "Safety Coach");
            var disableRigRenderers = typeof(VisualCaptureTour).GetMethod("DisableRigRenderers",
                BindingFlags.NonPublic | BindingFlags.Static);

            disableRigRenderers.Invoke(null, new object[] { Camera.main });

            Assert.That(chat.GetComponent<Canvas>().enabled, Is.True);
            chat.Close();
        }

        [Test]
        public void Capture_ClosesPreviousChatBeforeEnteringNextSite()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var coach = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None)
                .First(agent => agent.SiteName == "Construction");
            var hud = Object.FindFirstObjectByType<TrainingHud>();
            var director = Object.FindFirstObjectByType<SiteExperienceDirector>();
            chat.Open(coach, "Safety Coach");
            hud.gameObject.SetActive(false);
            hud.SetVisible(false);
            director.enabled = true;

            VisualCaptureTour.PrepareSiteCapture(TrainingSiteId.Warehouse);

            Assert.That(chat.IsVisible, Is.False);
            Assert.That(hud.gameObject.activeSelf, Is.True);
            Assert.That(hud.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(director.enabled, Is.False);
            Assert.That(Object.FindFirstObjectByType<TrainingCoordinator>().ActiveSite,
                Is.EqualTo(TrainingSiteId.Warehouse));
        }

        [Test]
        public void Chat_OldCoachCannotCloseCurrentCoachConversation()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var coaches = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None);
            var first = coaches.First(agent => agent.SiteName == "Construction");
            var current = coaches.First(agent => agent.SiteName == "Electrical Maintenance");
            chat.Open(first, "First Coach");
            chat.Open(current, "Current Coach");

            chat.CloseFor(first);

            Assert.That(chat.IsOpenFor(current), Is.True);
            Assert.That(chat.IsVisible, Is.True);
            chat.Close();
        }

        [Test]
        public void Chat_CloseImmediatelyEndsNpcConversationState()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var talk = Object.FindFirstObjectByType<NpcTalkInteractable>();
            var state = typeof(NpcTalkInteractable).GetField("conversationStarted",
                BindingFlags.NonPublic | BindingFlags.Instance);
            state.SetValue(talk, true);
            chat.Open(talk.GetComponent<NpcConversationAgent>(), "Safety Coach");

            chat.Close();

            Assert.That(talk.ConversationActive, Is.False);
        }

        [Test]
        public void Chat_CloseRestoresHudOnlyWhileLearnerRemainsInSite()
        {
            var chat = Object.FindFirstObjectByType<NpcChatPanel>();
            var talk = Object.FindObjectsByType<NpcTalkInteractable>(FindObjectsSortMode.None)
                .First(item => item.GetComponent<NpcConversationAgent>().SiteName == "Construction");
            var coordinator = Object.FindFirstObjectByType<TrainingCoordinator>();
            var hud = Object.FindFirstObjectByType<TrainingHud>();
            var canvasGroup = hud.GetComponent<CanvasGroup>();
            typeof(TrainingHud).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(hud, null);
            typeof(TrainingCoordinator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(coordinator, null);
            coordinator.EnterSite(TrainingSiteId.Construction);
            chat.Open(talk.GetComponent<NpcConversationAgent>(), "Safety Coach");
            hud.SetVisible(false);

            chat.Close();

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

            chat.Open(talk.GetComponent<NpcConversationAgent>(), "Safety Coach");
            coordinator.LeaveSite(TrainingSiteId.Construction);
            chat.Close();

            Assert.That(canvasGroup.alpha, Is.EqualTo(0f));
        }

        [Test]
        public void Scene_ContainsOneVisibleRocketboxCoach()
        {
            var coaches = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None);

            Assert.That(coaches, Has.Length.EqualTo(5));
            foreach (var coach in coaches)
            {
                var model = coach.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.That(model, Is.Not.Null, coach.SiteName);
                Assert.That(model.gameObject.activeInHierarchy, Is.True, coach.SiteName);
                Assert.That(model.enabled, Is.True, coach.SiteName);
                Assert.That(coach.GetComponent<NpcTalkInteractable>(), Is.Not.Null, coach.name);
            }
        }

        [Test]
        public void Scene_StartsWithRigAboveWalkableFloor()
        {
            var rig = GameObject.Find("XR Origin (Safety Training)");
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.transform.position.y, Is.InRange(0f, 0.1f));
            var controller = rig.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.height, Is.GreaterThan(1f));
        }

        [Test]
        public void Scene_HasContinuousCampusGroundAndFivePortals()
        {
            var ground = GameObject.Find("Continuous Walkable Ground");
            Assert.That(ground, Is.Not.Null);
            Assert.That(ground.GetComponent<Terrain>(), Is.Not.Null);
            Assert.That(ground.GetComponent<TerrainCollider>(), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<SitePortal>(FindObjectsSortMode.None), Has.Length.EqualTo(10));
        }

        [Test]
        public void Scene_UsesRealEnvironmentAssetsAndPbrSurfaces()
        {
            var environmentModels = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(item => item.name.StartsWith("RealEnvironment - ")).ToArray();
            var construction = GameObject.Find("Construction Site");
            var floor = construction.transform.Find("Floor").GetComponent<Renderer>();
            var wall = construction.transform.Find("Back Wall").GetComponent<Renderer>();

            Assert.That(environmentModels.Length, Is.GreaterThanOrEqualTo(16));
            Assert.That(floor.sharedMaterial.GetTexture("_MainTex"), Is.Not.Null);
            Assert.That(floor.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
            Assert.That(wall.enabled, Is.True);
            Assert.That(wall.sharedMaterial.GetTexture("_MainTex"), Is.Not.Null);
            Assert.That(RenderSettings.skybox.name, Does.Contain("Construction Yard"));
        }

        [Test]
        public void Construction_HasFiveOrderedHandsOnActions()
        {
            var practical = Object.FindObjectsByType<ConstructionHandsOnController>(FindObjectsSortMode.None);
            var actions = Object.FindObjectsByType<ConstructionActionInteractable>(FindObjectsSortMode.None)
                .OrderBy(action => action.StepIndex).ToArray();

            Assert.That(practical, Has.Length.EqualTo(1));
            Assert.That(actions, Has.Length.EqualTo(5));
            Assert.That(actions.Select(action => action.StepIndex), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(actions.All(action => action.GetComponent<Collider>() != null), Is.True);
        }

        [Test]
        public void EveryNonConstructionSite_HasThreeOrderedHandsOnActions()
        {
            // Given: the generated five-site training scene.
            var siteRoots = new[]
            {
                GameObject.Find("Warehouse"),
                GameObject.Find("Fire Response"),
                GameObject.Find("Chemical Processing"),
                GameObject.Find("Electrical Maintenance")
            };

            // When: each non-construction practical sequence is inspected.
            foreach (var siteRoot in siteRoots)
            {
                var controllers = siteRoot.GetComponents<MonoBehaviour>()
                    .Where(component => component.GetType().Name == "SitePracticalController").ToArray();
                var actions = siteRoot.GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(component => component.GetType().Name == "SitePracticalAction")
                    .OrderBy(component => (int)component.GetType().GetProperty("StepIndex").GetValue(component))
                    .ToArray();

                // Then: the site offers a complete, clickable three-step practical.
                Assert.That(controllers, Has.Length.EqualTo(1), siteRoot.name);
                Assert.That(actions, Has.Length.EqualTo(3), siteRoot.name);
                Assert.That(actions.Select(component =>
                        (int)component.GetType().GetProperty("StepIndex").GetValue(component)),
                    Is.EqualTo(new[] { 0, 1, 2 }), siteRoot.name);
                Assert.That(actions.All(component => component.GetComponent<Collider>() != null),
                    Is.True, siteRoot.name);
            }
        }

        [Test]
        public void EveryModule_UsesPhysicalGrabAndPlacementTargetsInsteadOfClickCompletion()
        {
            var actions = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(component => component.GetType().Name == "SitePracticalAction" ||
                                    component.GetType().Name == "ConstructionActionInteractable")
                .ToArray();

            Assert.That(actions, Has.Length.EqualTo(17));
            foreach (var action in actions)
            {
                var placement = action.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().Name == "HandsOnPlacementInteractable");
                Assert.That(placement, Is.Not.Null, action.name);
                Assert.That(action.GetComponent<XRGrabInteractable>(), Is.Not.Null, action.name);
                Assert.That(action.GetComponent<Rigidbody>(), Is.Not.Null, action.name);
                Assert.That(action.transform.parent.Find($"Placement Target - {action.name}"), Is.Not.Null,
                    action.name);
            }
        }

        [Test]
        public void EveryModule_ClickFailsAndReleaseAtTargetAdvancesFirstStep()
        {
            foreach (var controller in Object.FindObjectsByType<SitePracticalController>(FindObjectsSortMode.None))
            {
                typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);
                var first = controller.GetComponentsInChildren<SitePracticalAction>(true)
                    .Single(action => action.StepIndex == 0);
                typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(first, null);
                controller.Begin();

                first.TryPerform("Click");
                Assert.That(controller.CompletedSteps, Is.Zero, controller.SiteId.ToString());
                first.transform.localPosition = first.TargetLocalPosition;
                first.TryPerform("DesktopDrag");
                Assert.That(controller.CompletedSteps, Is.EqualTo(1), controller.SiteId.ToString());
            }

            var construction = Object.FindFirstObjectByType<ConstructionHandsOnController>();
            typeof(ConstructionHandsOnController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(construction, null);
            var constructionFirst = construction.GetComponentsInChildren<ConstructionActionInteractable>(true)
                .Single(action => action.StepIndex == 0);
            typeof(ConstructionActionInteractable).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(constructionFirst, null);
            construction.Begin();

            constructionFirst.TryPerform("Click");
            Assert.That(construction.CompletedSteps, Is.Zero);
            constructionFirst.transform.localPosition = constructionFirst.TargetLocalPosition;
            constructionFirst.TryPerform("DesktopDrag");
            Assert.That(construction.CompletedSteps, Is.EqualTo(1));
        }

        [Test]
        public void DesktopDrag_MissedMouseUpStillEndsPlacementWhenButtonIsNoLongerHeld()
        {
            var controller = GameObject.Find("Warehouse").GetComponent<SitePracticalController>();
            typeof(SitePracticalController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            var action = controller.GetComponentsInChildren<SitePracticalAction>(true)
                .Single(item => item.StepIndex == 0);
            typeof(SitePracticalAction).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(action, null);
            controller.Begin();
            var placement = action.GetComponent<HandsOnPlacementInteractable>();
            var ray = new Ray(action.transform.position + Vector3.up * 3f, Vector3.down);
            Assert.That(placement.BeginDesktopDrag(ray), Is.True);

            var explorer = Object.FindFirstObjectByType<DesktopExplorerController>();
            var activeDrag = typeof(DesktopExplorerController).GetField("draggedPlacement",
                BindingFlags.Instance | BindingFlags.NonPublic);
            activeDrag.SetValue(explorer, placement);
            var handleActiveDrag = typeof(DesktopExplorerController).GetMethod("HandleActiveDesktopDrag",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(handleActiveDrag, Is.Not.Null);
            handleActiveDrag.Invoke(explorer, new object[] { ray, false });
            Assert.That(activeDrag.GetValue(explorer), Is.Null);
            Assert.That(action.Completed, Is.False);
        }

        [Test]
        public void DesktopWorldPointer_IsBlockedWhileChatOrUiOwnsTheClick()
        {
            var policy = typeof(DesktopExplorerController).GetMethod("ShouldBlockWorldPointer",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(policy, Is.Not.Null);
            Assert.That(policy.Invoke(null, new object[] { true, false }), Is.True);
            Assert.That(policy.Invoke(null, new object[] { false, true }), Is.True);
            Assert.That(policy.Invoke(null, new object[] { false, false }), Is.False);
        }

        [Test]
        public void DesktopLocomotion_IsBlockedWhileChatOrTextEntryOwnsTheKeyboard()
        {
            var policy = typeof(DesktopExplorerController).GetMethod("ShouldBlockLocomotion",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(policy, Is.Not.Null);
            Assert.That(policy.Invoke(null, new object[] { true, false }), Is.True);
            Assert.That(policy.Invoke(null, new object[] { false, true }), Is.True);
            Assert.That(policy.Invoke(null, new object[] { false, false }), Is.False);
        }

        [Test]
        public void Scene_ReplacesPlaceholderPropsWithImportedModels()
        {
            var importedProps = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(item => item.name.StartsWith("RealAsset - ")).ToArray();

            Assert.That(importedProps.Length, Is.GreaterThanOrEqualTo(30));
            AssertRealReplacement("Crates in Access Route");
            AssertRealReplacement("Pallet in Vehicle Lane");
            AssertRealReplacement("Pallet at Emergency Exit");
            AssertRealReplacement("Inspection Clipboard");
        }

        [Test]
        public void Scene_ChemicalAndElectricalControlsUseDetailedLowProfileAssemblies()
        {
            var eyewash = GameObject.Find("Emergency Eyewash");
            Assert.That(VisibleMeshCount(eyewash), Is.GreaterThanOrEqualTo(8), "Emergency eyewash detail");

            foreach (var name in new[] { "Loose Cable Crossing", "Protected Cable Ramp" })
            {
                var cable = GameObject.Find(name);
                Assert.That(VisibleMeshCount(cable), Is.GreaterThanOrEqualTo(6), name);
                Assert.That(VisibleBounds(cable).size.y, Is.LessThanOrEqualTo(0.55f), name);
            }

            foreach (var name in new[] { "Open Electrical Panel", "Locked Isolated Panel" })
                Assert.That(VisibleMeshCount(GameObject.Find(name)), Is.GreaterThanOrEqualTo(8), name);
        }

        [Test]
        public void Scene_HighImpactSafetyEquipmentUsesDedicatedMeshesAndModelShells()
        {
            var eyewashBowl = GameObject.Find("Emergency Eyewash").transform.Find("Eyewash Bowl");
            Assert.That(eyewashBowl, Is.Not.Null);
            Assert.That(eyewashBowl.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("Eyewash Bowl Mesh"));
            Assert.That(eyewashBowl.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.GreaterThanOrEqualTo(96));
            var bowlBounds = eyewashBowl.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.That(bowlBounds.size.y, Is.GreaterThanOrEqualTo(0.16f));
            Assert.That(bowlBounds.size.y / bowlBounds.size.x, Is.GreaterThanOrEqualTo(0.15f));
            Assert.That(eyewashBowl.GetComponent<Renderer>().sharedMaterial.GetFloat("_Metallic"),
                Is.LessThanOrEqualTo(0.15f));
            var eyewashPedestal = GameObject.Find("Emergency Eyewash").transform.Find("Pedestal");
            Assert.That(eyewashPedestal.GetComponent<Renderer>().sharedMaterial.GetFloat("_Metallic"),
                Is.LessThanOrEqualTo(0.45f));

            var cableShell = GameObject.Find("Protected Cable Ramp").transform.Find("Cable Protector Shell");
            Assert.That(cableShell, Is.Not.Null);
            Assert.That(cableShell.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("Cable Protector Shell Mesh"));
            Assert.That(cableShell.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.GreaterThanOrEqualTo(24));

            foreach (var name in new[] { "Open Electrical Panel", "Locked Isolated Panel" })
            {
                var panel = GameObject.Find(name);
                var cabinet = panel.transform.Find("Electrical Cabinet Shell");
                Assert.That(cabinet, Is.Not.Null, name);
                Assert.That(cabinet.GetComponent<MeshFilter>().sharedMesh.name,
                    Is.EqualTo("Electrical Cabinet Shell Mesh"), name);
                Assert.That(cabinet.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                    Is.GreaterThanOrEqualTo(32), name);
            }
        }

        [Test]
        public void FireExtinguishers_AreUprightAndUseEnglishTags()
        {
            foreach (var name in new[] { "Blocked Extinguisher", "Accessible Extinguisher" })
            {
                var root = GameObject.Find(name);
                var model = root.transform.Cast<Transform>()
                    .First(child => child.name.StartsWith("RealAsset - korean_fire_extinguisher"));
                var tag = root.GetComponentsInChildren<TextMesh>(true)
                    .First(label => label.text.Contains("FIRE EXTINGUISHER"));

                Assert.That(Mathf.DeltaAngle(model.localEulerAngles.x, 270f), Is.EqualTo(0f).Within(0.1f), name);
                Assert.That(tag.text, Does.Contain("P.A.S.S."), name);
                Assert.That(tag.GetComponentInParent<BillboardLabel>(), Is.Not.Null, name);
                Assert.That(Mathf.Abs(tag.GetComponentInParent<BillboardLabel>().transform.localPosition.x),
                    Is.GreaterThanOrEqualTo(0.7f), name);
            }
        }

        [Test]
        public void FireExtinguishers_RestOnFloorAndTagsDoNotOverlapEquipment()
        {
            foreach (var name in new[] { "Blocked Extinguisher", "Accessible Extinguisher" })
            {
                var root = GameObject.Find(name).transform;
                var model = root.Cast<Transform>()
                    .First(child => child.name.StartsWith("RealAsset - korean_fire_extinguisher"));
                var modelRenderers = model.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled).ToArray();
                var modelBounds = modelRenderers[0].bounds;
                foreach (var renderer in modelRenderers.Skip(1))
                    modelBounds.Encapsulate(renderer.bounds);
                var plate = root.Find("English Fire Extinguisher Tag/English Tag Backplate").GetComponent<Renderer>();
                var collider = root.GetComponent<BoxCollider>();

                Assert.That(modelBounds.min.y, Is.EqualTo(root.position.y - 0.02f).Within(0.015f), name);
                Assert.That(plate.bounds.min.x, Is.GreaterThan(modelBounds.max.x + 0.04f), name);
                Assert.That(root.Find("Fire Extinguisher Floor Base"), Is.Not.Null, name);
                Assert.That(root.Find("Fire Extinguisher Floor Support"), Is.Not.Null, name);
                Assert.That(collider, Is.Not.Null, name);
                Assert.That(collider.bounds.Contains(modelBounds.center), Is.True, name);
                Assert.That(collider.bounds.min.y, Is.LessThanOrEqualTo(modelBounds.min.y), name);
                Assert.That(collider.bounds.max.y, Is.GreaterThanOrEqualTo(modelBounds.max.y), name);
            }
        }

        [Test]
        public void FireBlockedExtinguisher_HasPhysicalObstructionInsideInspectableAssembly()
        {
            var extinguisher = GameObject.Find("Blocked Extinguisher");
            var obstruction = extinguisher.transform.Find("Blocked Access Obstruction");

            Assert.That(obstruction, Is.Not.Null,
                "The access obstruction must read as part of the blocked-extinguisher hazard.");
            Assert.That(obstruction.GetComponentsInChildren<Renderer>(true)
                    .Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy &&
                        renderer.GetComponent<TextMesh>() == null),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(obstruction.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name.StartsWith("RealAsset - old_military_crate")),
                Is.GreaterThanOrEqualTo(3));
            var obstructionBounds = VisibleBounds(obstruction.gameObject);
            Assert.That(obstructionBounds.max.y, Is.GreaterThanOrEqualTo(extinguisher.transform.position.y + 0.1f));
            Assert.That(obstructionBounds.size.z, Is.GreaterThanOrEqualTo(0.65f));
        }

        [Test]
        public void FireInteractables_DoNotClaimTheSamePhysicalCollider()
        {
            var fire = GameObject.Find("Fire Response");
            var claims = fire.GetComponentsInChildren<XRSimpleInteractable>(true)
                .SelectMany(interactable =>
                {
                    var colliders = interactable.colliders.Count > 0
                        ? interactable.colliders
                        : interactable.GetComponentsInChildren<Collider>(true)
                            .Where(collider => !collider.isTrigger).ToList();
                    return colliders.Select(collider => new { Collider = collider, Owner = interactable });
                })
                .GroupBy(claim => claim.Collider)
                .Where(group => group.Select(claim => claim.Owner).Distinct().Count() > 1)
                .ToArray();

            Assert.That(claims, Is.Empty,
                "Each Fire Response collider must belong to exactly one XR interactable. Conflicts: " +
                string.Join(", ", claims.Select(group =>
                    $"{group.Key.name} => {string.Join(" / ", group.Select(claim => claim.Owner.name).Distinct())}")));
        }

        [Test]
        public void ChemicalLeak_HasVisibleGroundPuddleAndDripTrail()
        {
            var drum = GameObject.Find("Leaking Solvent Drum");
            var evidence = drum.transform.Find("Visible Solvent Leak");

            Assert.That(evidence, Is.Not.Null,
                "The learner must be able to identify the leak before clicking the drum.");
            Assert.That(VisibleMeshCount(evidence.gameObject), Is.GreaterThanOrEqualTo(4));
            var bounds = VisibleBounds(evidence.gameObject);
            Assert.That(bounds.min.y, Is.InRange(-0.02f, 0.03f));
            Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(0.75f));
            Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(0.9f));
        }

        [Test]
        public void SiteMentors_ReserveFirefighterModelForFireResponse()
        {
            var coaches = Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None);

            foreach (var coach in coaches)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(coach.gameObject);
                var assetPath = AssetDatabase.GetAssetPath(source);
                var usesFirefighterModel = assetPath.Contains("Fire_Female_01");
                Assert.That(usesFirefighterModel, Is.EqualTo(coach.SiteName == "Fire Response"),
                    $"{coach.SiteName} mentor must wear role-appropriate PPE.");
            }
        }

        [Test]
        public void Coaches_HaveFullBodyDesktopClickVolumes()
        {
            var coaches = Object.FindObjectsByType<NpcTalkInteractable>(FindObjectsSortMode.None);

            Assert.That(coaches, Has.Length.EqualTo(5));
            foreach (var coach in coaches)
            {
                var capsule = coach.GetComponent<CapsuleCollider>();
                Assert.That(capsule, Is.Not.Null, coach.name);
                Assert.That(capsule.height, Is.GreaterThanOrEqualTo(1.8f), coach.name);
                Assert.That(capsule.center.y + capsule.height * 0.5f,
                    Is.GreaterThanOrEqualTo(1.65f), coach.name);
            }
        }

        [Test]
        public void CoachSpeechBubbles_UseReadableProfessionalWorldSpaceUi()
        {
            // Given: every site coach has a compact in-world reply surface.
            var coaches = Object.FindObjectsByType<NpcTalkInteractable>(FindObjectsSortMode.None);

            // When: the generated speech-bubble composition is inspected.
            foreach (var coach in coaches)
            {
                var bubble = coach.transform.Find("NPC Speech Bubble");
                var panel = bubble.Find("Dialogue Backplate");
                var canvas = bubble.GetComponent<Canvas>();
                var body = bubble.Find("Dialogue Backplate/Coach Reply").GetComponent<TextMeshProUGUI>();
                var eyebrow = bubble.Find("Dialogue Backplate/Coach Eyebrow").GetComponent<TextMeshProUGUI>();

                // Then: a layered, high-contrast world-space UI replaces the primitive cube label.
                Assert.That(bubble.localPosition.y, Is.InRange(2.08f, 2.3f), coach.name);
                Assert.That(bubble.localPosition.x, Is.InRange(0.5f, 0.75f), coach.name);
                Assert.That(canvas, Is.Not.Null, coach.name);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace), coach.name);
                Assert.That(canvas.sortingOrder, Is.GreaterThanOrEqualTo(200), coach.name);
                Assert.That(bubble.GetComponent<CanvasGroup>(), Is.Not.Null, coach.name);
                Assert.That(bubble.localScale.x, Is.GreaterThanOrEqualTo(0.0035f), coach.name);
                Assert.That(panel.GetComponent<Image>().sprite, Is.Not.Null, coach.name);
                Assert.That(panel.GetComponent<Image>().type, Is.EqualTo(Image.Type.Sliced), coach.name);
                Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(42), coach.name);
                Assert.That(body.fontSizeMin, Is.GreaterThanOrEqualTo(34), coach.name);
                Assert.That(body.color.grayscale, Is.GreaterThan(0.8f), coach.name);
                Assert.That(body.outlineWidth, Is.GreaterThanOrEqualTo(0.08f), coach.name);
                eyebrow.ForceMeshUpdate();
                body.ForceMeshUpdate();
                Assert.That(eyebrow.isTextOverflowing, Is.False, $"{coach.name}: header clipped");
                Assert.That(body.isTextOverflowing, Is.False, $"{coach.name}: default prompt clipped");
                Assert.That(bubble.GetComponentInChildren<TextMesh>(true), Is.Null, coach.name);
            }
        }

        [TestCase("Pallet in Vehicle Lane")]
        [TestCase("Cargo in Staging Bay")]
        [TestCase("Pallet at Emergency Exit")]
        [TestCase("Blocked Extinguisher")]
        [TestCase("Leaking Solvent Drum")]
        public void ImportedReplacement_IsVisibleAboveFloor(string objectName)
        {
            var root = GameObject.Find(objectName);
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            Assert.That(bounds.size.y, Is.GreaterThan(0.35f), $"{objectName}: {bounds}");
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.05f), $"{objectName}: {bounds}");
        }

        [TestCase("Blocked Extinguisher")]
        [TestCase("Leaking Solvent Drum")]
        public void ImportedUprightEquipment_UsesVerticalMajorAxis(string objectName)
        {
            var root = GameObject.Find(objectName);
            var equipmentModel = root.transform.Cast<Transform>()
                .First(child => child.name.StartsWith("RealAsset -"));
            var renderers = equipmentModel.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            Assert.That(bounds.size.y, Is.GreaterThan(bounds.size.x), $"{objectName}: {bounds}");
            Assert.That(bounds.size.y, Is.GreaterThan(bounds.size.z), $"{objectName}: {bounds}");
        }

        static void AssertRealReplacement(string objectName)
        {
            var root = GameObject.Find(objectName);
            Assert.That(root, Is.Not.Null, objectName);
            Assert.That(root.GetComponent<Renderer>().enabled, Is.False, objectName);
            Assert.That(root.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.enabled),
                Is.True, objectName);
        }

        static int VisibleMeshCount(GameObject root)
        {
            Assert.That(root, Is.Not.Null);
            return root.GetComponentsInChildren<Renderer>(true)
                .Count(renderer => renderer.enabled && renderer.GetComponent<TextMesh>() == null);
        }

        static Bounds VisibleBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.GetComponent<TextMesh>() == null).ToArray();
            Assert.That(renderers, Is.Not.Empty, root.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
