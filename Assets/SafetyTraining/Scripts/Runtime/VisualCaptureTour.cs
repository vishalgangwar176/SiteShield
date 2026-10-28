using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class VisualCaptureTour : MonoBehaviour
    {
        const string EnabledVariable = "SAFETY_CAPTURE_TOUR";
        const string DirectoryVariable = "SAFETY_CAPTURE_DIR";
        const string StartDelayVariable = "SAFETY_CAPTURE_START_DELAY";
        const string HoldVariable = "SAFETY_CAPTURE_HOLD_SECONDS";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(EnabledVariable), "1", StringComparison.Ordinal))
                return;

            new GameObject("Visual Capture Tour").AddComponent<VisualCaptureTour>();
        }

        IEnumerator Start()
        {
            var outputDirectory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Captures"));
            Directory.CreateDirectory(outputDirectory);

            var startDelay = ReadSeconds(StartDelayVariable, 0f);
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);
            yield return WaitForSceneReady();
            var viewer = Camera.main;
            if (viewer == null)
            {
                Debug.LogError("Visual capture tour could not find the Main Camera.");
                yield break;
            }

            UnityEngine.XR.XRSettings.enabled = false;
            DisablePoseOverrides(viewer);
            yield return CaptureView(viewer, outputDirectory, "01-campus-hub.png",
                new Vector3(0f, 2.2f, -10.2f), new Vector3(0f, 0.9f, -3.5f));

            PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId.Construction);
            var constructionOrigin = SiteOrigin("Construction Site");
            yield return CaptureView(viewer, outputDirectory, "02-construction-overview.png",
                constructionOrigin + new Vector3(0f, 2.2f, -6.8f), constructionOrigin + new Vector3(0f, 1f, 0.2f));
            yield return CapturePropShowcase(viewer, outputDirectory, "02b-construction-real-props.png",
                constructionOrigin);
            var constructionHazard = FindObjectsByType<InspectionTarget>(FindObjectsSortMode.None)
                .FirstOrDefault(target => target.TargetId == "fall-edge");
            if (constructionHazard != null)
                TrainingCoordinator.Instance.Inspect(constructionHazard);
            yield return new WaitForSecondsRealtime(0.8f);
            ShowCaptureHud();
            yield return CaptureView(viewer, outputDirectory, "03-construction-feedback.png",
                constructionOrigin + new Vector3(0f, 2.7f, -6.2f), constructionOrigin + new Vector3(0f, 0.85f, -3.5f));
            TrainingCoordinator.Instance.SetHandsOnFeedback(
                "Hands-on 1/5: PPE secured. Next, place the exclusion barricade.");
            yield return new WaitForSecondsRealtime(0.5f);
            ShowCaptureHud();
            yield return CaptureView(viewer, outputDirectory, "03b-construction-hands-on.png",
                constructionOrigin + new Vector3(0f, 2.7f, -6.2f), constructionOrigin + new Vector3(0f, 0.85f, -3.5f));
            yield return CaptureNpc(viewer, outputDirectory, "Construction Site", "04-construction-npc.png");

            PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId.Warehouse);
            var warehouseOrigin = SiteOrigin("Warehouse");
            yield return CaptureView(viewer, outputDirectory, "05-warehouse-overview.png",
                warehouseOrigin + new Vector3(0f, 2.2f, -6.8f), warehouseOrigin + new Vector3(0f, 1f, 0.2f));
            yield return CapturePropShowcase(viewer, outputDirectory, "05b-warehouse-real-props.png",
                warehouseOrigin);
            yield return CaptureNpc(viewer, outputDirectory, "Warehouse", "06-warehouse-npc.png");

            PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId.FireResponse);
            var fireOrigin = SiteOrigin("Fire Response");
            yield return CaptureView(viewer, outputDirectory, "07-fire-response-overview.png",
                fireOrigin + new Vector3(0f, 2.2f, -6.8f), fireOrigin + new Vector3(0f, 1f, 0.2f));
            yield return CapturePropShowcase(viewer, outputDirectory, "07b-fire-response-real-props.png",
                fireOrigin);
            yield return CaptureNpc(viewer, outputDirectory, "Fire Response", "08-fire-response-npc.png");

            PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId.ChemicalProcessing);
            var chemicalOrigin = SiteOrigin("Chemical Processing");
            yield return CaptureView(viewer, outputDirectory, "09-chemical-overview.png",
                chemicalOrigin + new Vector3(0f, 2.2f, -6.8f), chemicalOrigin + new Vector3(0f, 1f, 0.2f));
            yield return CapturePropShowcase(viewer, outputDirectory, "09b-chemical-real-props.png",
                chemicalOrigin);
            yield return CaptureNpc(viewer, outputDirectory, "Chemical Processing", "10-chemical-npc.png");

            PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId.ElectricalMaintenance);
            var electricalOrigin = SiteOrigin("Electrical Maintenance");
            yield return CaptureView(viewer, outputDirectory, "11-electrical-overview.png",
                electricalOrigin + new Vector3(0f, 2.2f, -6.8f), electricalOrigin + new Vector3(0f, 1f, 0.2f));
            yield return CapturePropShowcase(viewer, outputDirectory, "11b-electrical-real-props.png",
                electricalOrigin);
            yield return CaptureNpc(viewer, outputDirectory, "Electrical Maintenance", "12-electrical-npc.png");

            File.WriteAllText(Path.Combine(outputDirectory, "tour-complete.txt"),
                DateTime.UtcNow.ToString("O"));
            Debug.Log($"Visual capture tour completed: {outputDirectory}");
            Application.Quit(0);
        }

        static IEnumerator WaitForSceneReady()
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while ((Camera.main == null || TrainingCoordinator.Instance == null) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return new WaitForSecondsRealtime(1f);
        }

        static float ReadSeconds(string variable, float fallback)
        {
            return float.TryParse(Environment.GetEnvironmentVariable(variable),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds)
                ? Mathf.Max(0f, seconds)
                : fallback;
        }

        static Vector3 SiteOrigin(string siteName)
        {
            var site = GameObject.Find(siteName);
            if (site != null)
                return site.transform.position;

            return siteName switch
            {
                "Construction Site" => new Vector3(0f, 0f, 120f),
                "Warehouse" => new Vector3(180f, 0f, 120f),
                "Fire Response" => new Vector3(360f, 0f, 120f),
                "Chemical Processing" => new Vector3(540f, 0f, 120f),
                "Electrical Maintenance" => new Vector3(720f, 0f, 120f),
                _ => Vector3.zero
            };
        }

        public static void PrepareSiteCapture(SafetyTraining.Core.TrainingSiteId siteId)
        {
            (NpcChatPanel.Instance ?? FindFirstObjectByType<NpcChatPanel>())?.Close();
            var director = FindFirstObjectByType<SiteExperienceDirector>();
            if (director != null)
                director.enabled = false;
            var coordinator = TrainingCoordinator.Instance ?? FindFirstObjectByType<TrainingCoordinator>();
            coordinator?.EnterSite(siteId);
            SiteIsolationController.Instance?.ShowSite(siteId);
            ShowCaptureHud();
        }

        static void ShowCaptureHud()
        {
            var hud = FindObjectsByType<TrainingHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
            if (hud != null)
            {
                hud.gameObject.SetActive(true);
                hud.SetVisible(true);
            }
        }

        static void HideCaptureHud()
        {
            var hud = FindObjectsByType<TrainingHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
            if (hud != null)
                hud.SetVisible(false);
        }

        static void DisablePoseOverrides(Camera viewer)
        {
            foreach (var behaviour in viewer.transform.root.GetComponentsInChildren<Behaviour>(true))
                if (behaviour is DesktopExplorerController ||
                    behaviour.GetType().Name.Contains("TrackedPoseDriver", StringComparison.Ordinal))
                    behaviour.enabled = false;
            viewer.rect = new Rect(0f, 0f, 1f, 1f);
            viewer.targetTexture = null;
            viewer.stereoTargetEye = StereoTargetEyeMask.None;
            viewer.clearFlags = CameraClearFlags.Skybox;
            DisableRigRenderers(viewer);
        }

        static void DisableRigRenderers(Camera viewer)
        {
            foreach (var rigRenderer in viewer.transform.root.GetComponentsInChildren<Renderer>(true))
                if (rigRenderer.GetComponentInParent<TrainingHud>() == null)
                    rigRenderer.enabled = false;
            foreach (var rigCanvas in viewer.transform.root.GetComponentsInChildren<Canvas>(true))
                if (rigCanvas.GetComponentInParent<TrainingHud>() == null &&
                    rigCanvas.GetComponentInParent<NpcChatPanel>() == null)
                    rigCanvas.enabled = false;
        }

        static IEnumerator CaptureNpc(
            Camera viewer,
            string directory,
            string siteObjectName,
            string fileName)
        {
            HideCaptureHud();
            var site = GameObject.Find(siteObjectName);
            var coach = site != null ? site.GetComponentInChildren<NpcConversationAgent>() : null;
            if (coach == null)
            {
                var allCoaches = FindObjectsByType<NpcConversationAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (allCoaches.Length > 0)
                    coach = allCoaches[0];
            }
            if (coach != null && TryGetBounds(coach.gameObject, out var idleBounds))
            {
                coach.GetComponent<NpcRelaxedPose>()?.ReturnToIdle();
                yield return new WaitForSecondsRealtime(0.4f);
                var idleDistance = Mathf.Max(3.2f, idleBounds.size.y * 1.65f);
                var idleFocus = idleBounds.center + Vector3.up * 0.65f;
                var idleFileStem = Path.GetFileNameWithoutExtension(fileName);
                yield return CaptureView(viewer, directory, $"{idleFileStem}-rest.png",
                    idleFocus + Vector3.back * idleDistance,
                    idleFocus);
                var pose = coach.GetComponent<NpcRelaxedPose>();
                pose?.SetMoving(true);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return CaptureView(viewer, directory, $"{idleFileStem}-walking-a.png",
                    idleFocus + Vector3.back * idleDistance,
                    idleFocus);
                yield return new WaitForSecondsRealtime(0.35f);
                yield return CaptureView(viewer, directory, $"{idleFileStem}-walking-b.png",
                    idleFocus + Vector3.back * idleDistance,
                    idleFocus);
                pose?.SetMoving(false);
                yield return new WaitForSecondsRealtime(0.35f);
            }
            var initialReply = coach?.LastReply;
            site?.GetComponentInChildren<NpcTalkInteractable>()?.Ask();
            var replyDeadline = Time.realtimeSinceStartup + 1.2f;
            while (coach != null && coach.LastReply == initialReply &&
                   Time.realtimeSinceStartup < replyDeadline)
                yield return null;

            if (coach != null && TryGetBounds(coach.gameObject, out var bounds))
            {
                var distance = Mathf.Max(3.2f, bounds.size.y * 1.65f);
                var dialogueFocus = bounds.center + Vector3.up * 0.65f;
                var fileStem = Path.GetFileNameWithoutExtension(fileName);
                coach.GetComponent<NpcRelaxedPose>()?.PlayEncouragement(false);
                yield return new WaitForSecondsRealtime(1.3f);
                yield return CaptureView(viewer, directory, fileName,
                    dialogueFocus + Vector3.back * distance,
                    dialogueFocus);
                (NpcChatPanel.Instance ?? FindFirstObjectByType<NpcChatPanel>())?.CloseFor(coach);
                yield return new WaitForSecondsRealtime(2f);
                coach.GetComponent<NpcRelaxedPose>()?.ReturnToIdle();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return CaptureView(viewer, directory, $"{fileStem}-settled.png",
                    dialogueFocus + Vector3.back * distance,
                    dialogueFocus);
                yield break;
            }

            var origin = site != null ? site.transform.position : SiteOrigin(siteObjectName);
            yield return CaptureView(viewer, directory, fileName,
                origin + new Vector3(0f, 2.4f, -1.4f),
                origin + new Vector3(0f, 1.5f, 2.8f));
        }

        static IEnumerator CapturePropShowcase(
            Camera viewer,
            string directory,
            string fileName,
            Vector3 siteOrigin)
        {
            var hud = FindFirstObjectByType<TrainingHud>();
            if (hud != null)
                hud.SetVisible(false);
            yield return CaptureView(viewer, directory, fileName,
                siteOrigin + new Vector3(6.8f, 3.8f, -6.2f),
                siteOrigin + new Vector3(0f, 0.75f, 0f));
            if (hud != null)
                hud.SetVisible(true);
        }

        static bool TryGetBounds(GameObject target, out Bounds bounds)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.GetComponent<TextMesh>() == null &&
                                   renderer.GetComponent<BillboardLabel>() == null)
                .ToArray();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return true;
        }

        static IEnumerator CaptureView(
            Camera viewer,
            string directory,
            string fileName,
            Vector3 position,
            Vector3 focus)
        {
            DisableRigRenderers(viewer);
            viewer.rect = new Rect(0f, 0f, 1f, 1f);
            viewer.targetTexture = null;
            viewer.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
            yield return null;
            yield return new WaitForEndOfFrame();
            CaptureCameraPng(viewer, Path.Combine(directory, fileName), 1168, 692);
            yield return new WaitForSecondsRealtime(ReadSeconds(HoldVariable, 1f));
        }

        static void CaptureCameraPng(Camera viewer, string path, int width, int height)
        {
            DisableRigRenderers(viewer);
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var captureObject = new GameObject("Visual QA Capture Camera");
            var capture = captureObject.AddComponent<Camera>();
            try
            {
                capture.enabled = false;
                capture.stereoTargetEye = StereoTargetEyeMask.None;
                capture.clearFlags = CameraClearFlags.Skybox;
                capture.backgroundColor = viewer.backgroundColor;
                capture.cullingMask = viewer.cullingMask;
                capture.fieldOfView = viewer.fieldOfView;
                capture.nearClipPlane = viewer.nearClipPlane;
                capture.farClipPlane = viewer.farClipPlane;
                capture.allowHDR = viewer.allowHDR;
                capture.allowMSAA = viewer.allowMSAA;
                capture.targetTexture = target;
                capture.aspect = (float)width / height;
                capture.transform.SetPositionAndRotation(viewer.transform.position, viewer.transform.rotation);
                capture.rect = new Rect(0f, 0f, 1f, 1f);
                capture.pixelRect = new Rect(0f, 0f, width, height);
                RenderTexture.active = target;
                GL.Clear(true, true, new Color(0.47f, 0.62f, 0.8f));
                capture.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                Destroy(captureObject);
                Destroy(target);
                Destroy(image);
            }
        }
    }
}
