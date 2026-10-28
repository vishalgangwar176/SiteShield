using SafetyTraining.Core;
using SafetyTraining.Runtime;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace SafetyTraining.Editor
{
    public static class SafetyTrainingSceneBuilder
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";
        const string ConfigPath = "Assets/SafetyTraining/LlmEndpointConfig.asset";
        const string XriVersion = "3.4.1";
        const string StarterAssetsName = "Starter Assets";
        const string XrRigPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        static readonly Vector3 ConstructionOrigin = new(0f, 0f, 120f);
        static readonly Vector3 WarehouseOrigin = new(180f, 0f, 120f);
        static readonly Vector3 FireOrigin = new(360f, 0f, 120f);
        static readonly Vector3 ChemicalOrigin = new(540f, 0f, 120f);
        static readonly Vector3 ElectricalOrigin = new(720f, 0f, 120f);
        static readonly Vector3 HubArrival = new(0f, 0.02f, -9.2f);

        [MenuItem("Safety Training/Build Prototype Scene")]
        public static void Build()
        {
            if (!OpenXrProjectConfigurator.ConfigureStandalone())
                throw new System.InvalidOperationException("OpenXR Standalone configuration failed.");
            EnsureTmpEssentials();
            EnsureFolder("Assets/SafetyTraining/Scenes");
            EnsureFolder("Assets/SafetyTraining/GeneratedMaterials");
            EnsureFolder("Assets/SafetyTraining/Generated");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            RealEnvironmentDresser.ApplyConstructionYardSkybox();
            CreateInteractionManager();
            CreateViewerRig();
            var navigation = CreateNavigationGround();
            var lobby = CreateTrainingHubLobby();
            var coordinator = new GameObject("Training Coordinator");
            coordinator.AddComponent<TrainingCoordinator>();
            coordinator.AddComponent<SiteExperienceDirector>();
            var isolation = coordinator.AddComponent<SiteIsolationController>();
            CreateHud();
            var portalRoot = CreateSitePortals();

            var config = LoadOrCreateEndpointConfig();
            var construction = SafetySiteFactory.Construction(ConstructionOrigin);
            construction.gameObject.AddComponent<SiteExperienceZone>().Configure(TrainingSiteId.Construction);
            RealEnvironmentDresser.DressSite(construction, RealEnvironmentDresser.SiteStyle.Construction);
            RealEnvironmentDresser.AddIsolationEnclosure(construction, RealEnvironmentDresser.SiteStyle.Construction);
            ImportedPropDresser.DressConstruction(construction);
            SafetyCoachFactory.Create(construction, TrainingSiteId.Construction, "construction safety supervisor",
                "Exposed elevated edges need guardrails or approved fall arrest. Access routes must remain clear.",
                SafetyCoachFactory.ConstructionCoachPath);
            var warehouse = SafetySiteFactory.Warehouse(WarehouseOrigin);
            warehouse.gameObject.AddComponent<SiteExperienceZone>().Configure(TrainingSiteId.Warehouse);
            RealEnvironmentDresser.DressSite(warehouse, RealEnvironmentDresser.SiteStyle.Warehouse);
            RealEnvironmentDresser.AddIsolationEnclosure(warehouse, RealEnvironmentDresser.SiteStyle.Warehouse);
            ImportedPropDresser.DressWarehouse(warehouse);
            SafetyCoachFactory.Create(warehouse, TrainingSiteId.Warehouse, "warehouse safety lead",
                "Uncontrolled spills require isolation and cleanup. Pedestrian and vehicle routes must remain separated and clear.",
                SafetyCoachFactory.ConstructionCoachPath);
            var fire = SafetySiteFactory.FireResponse(FireOrigin);
            fire.gameObject.AddComponent<SiteExperienceZone>().Configure(TrainingSiteId.FireResponse);
            RealEnvironmentDresser.DressSite(fire, RealEnvironmentDresser.SiteStyle.FireResponse);
            RealEnvironmentDresser.AddIsolationEnclosure(fire, RealEnvironmentDresser.SiteStyle.FireResponse);
            ImportedPropDresser.DressFireResponse(fire);
            SafetyCoachFactory.Create(fire, TrainingSiteId.FireResponse, "fire response trainer",
                "Fire extinguishers need immediate clear access. Emergency egress routes and exits must remain unobstructed.",
                SafetyCoachFactory.FireCoachPath);
            var chemical = SafetySiteFactory.ChemicalProcessing(ChemicalOrigin);
            chemical.gameObject.AddComponent<SiteExperienceZone>().Configure(TrainingSiteId.ChemicalProcessing);
            RealEnvironmentDresser.DressSite(chemical, RealEnvironmentDresser.SiteStyle.ChemicalProcessing);
            RealEnvironmentDresser.AddIsolationEnclosure(chemical, RealEnvironmentDresser.SiteStyle.ChemicalProcessing);
            ImportedPropDresser.DressChemicalProcessing(chemical);
            SafetyCoachFactory.Create(chemical, TrainingSiteId.ChemicalProcessing, "process safety specialist",
                "Chemical containers need legible labels and compatible segregation. Eyewash access must remain clear.",
                SafetyCoachFactory.ConstructionCoachPath);
            var electrical = SafetySiteFactory.ElectricalMaintenance(ElectricalOrigin);
            electrical.gameObject.AddComponent<SiteExperienceZone>().Configure(TrainingSiteId.ElectricalMaintenance);
            RealEnvironmentDresser.DressSite(electrical, RealEnvironmentDresser.SiteStyle.ElectricalMaintenance);
            RealEnvironmentDresser.AddIsolationEnclosure(electrical, RealEnvironmentDresser.SiteStyle.ElectricalMaintenance);
            ImportedPropDresser.DressElectricalMaintenance(electrical);
            SafetyCoachFactory.Create(electrical, TrainingSiteId.ElectricalMaintenance, "electrical maintenance lead",
                "Lockout tags and barriers must be visible before panel access. Route cables through protected crossings.",
                SafetyCoachFactory.ConstructionCoachPath);
            SitePracticalFactory.CreateAll(warehouse, fire, chemical, electrical);

            CreateReturnPortal(construction, TrainingSiteId.Construction, new Color(0.95f, 0.55f, 0.08f));
            CreateReturnPortal(warehouse, TrainingSiteId.Warehouse, new Color(0.12f, 0.48f, 0.85f));
            CreateReturnPortal(fire, TrainingSiteId.FireResponse, new Color(0.78f, 0.16f, 0.1f));
            CreateReturnPortal(chemical, TrainingSiteId.ChemicalProcessing, new Color(0.26f, 0.65f, 0.52f));
            CreateReturnPortal(electrical, TrainingSiteId.ElectricalMaintenance, new Color(0.32f, 0.38f, 0.72f));

            CreateSiteLightingAndProbes(construction, warehouse, fire, chemical, electrical);
            isolation.Configure(new[] { navigation, lobby, portalRoot }, new[]
            {
                construction.GetComponent<SiteExperienceZone>(),
                warehouse.GetComponent<SiteExperienceZone>(),
                fire.GetComponent<SiteExperienceZone>(),
                chemical.GetComponent<SiteExperienceZone>(),
                electrical.GetComponent<SiteExperienceZone>()
            });

            foreach (var agent in Object.FindObjectsByType<NpcConversationAgent>(FindObjectsSortMode.None))
                agent.ConfigureEndpoint(config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        [MenuItem("Safety Training/Open Prototype Scene")]
        public static void OpenGeneratedScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
            return item;
        }

        static TextMesh CreateLabel(string text, Transform parent, Vector3 localPosition, float size)
        {
            var label = new GameObject($"Label - {text}");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.identity;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            return mesh;
        }

        static Material CreateMaterial(Color color)
        {
            var fileName = ColorUtility.ToHtmlStringRGB(color);
            var path = $"Assets/SafetyTraining/GeneratedMaterials/{fileName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateLighting()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            light.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.48f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.34f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.15f);
            RenderSettings.reflectionIntensity = 0.82f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.52f, 0.61f, 0.7f);
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 125f;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var skybox = new Material(skyShader);
                skybox.SetColor("_SkyTint", new Color(0.42f, 0.58f, 0.78f));
                skybox.SetColor("_GroundColor", new Color(0.27f, 0.25f, 0.22f));
                skybox.SetFloat("_AtmosphereThickness", 0.95f);
                skybox.SetFloat("_Exposure", 1.15f);
                RenderSettings.skybox = skybox;
            }
        }

        static void CreateSiteLightingAndProbes(params Transform[] sites)
        {
            foreach (var site in sites)
            {
                var environment = new GameObject("Site Lighting and Reflection").transform;
                environment.SetParent(site, false);
                var workLightObject = new GameObject("Isolated Site Work Light");
                workLightObject.transform.SetParent(environment, false);
                workLightObject.transform.localPosition = new Vector3(0f, 5.2f, -4.2f);
                workLightObject.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
                var workLight = workLightObject.AddComponent<Light>();
                workLight.type = LightType.Spot;
                workLight.color = new Color(1f, 0.86f, 0.66f);
                workLight.intensity = 2.2f;
                workLight.range = 17f;
                workLight.spotAngle = 72f;
                workLight.shadows = LightShadows.Soft;
                workLight.shadowStrength = 0.45f;

                foreach (var side in new[] { -1f, 1f })
                {
                    var fillObject = new GameObject(side < 0f
                        ? "Wall Fill Light Left"
                        : "Wall Fill Light Right");
                    fillObject.transform.SetParent(environment, false);
                    fillObject.transform.localPosition = new Vector3(side * 3.65f, 2.65f, -0.65f);
                    var fill = fillObject.AddComponent<Light>();
                    fill.type = LightType.Point;
                    fill.color = new Color(1f, 0.82f, 0.64f);
                    fill.intensity = 0.82f;
                    fill.range = 8.5f;
                    fill.shadows = LightShadows.None;
                }

                var probeObject = new GameObject("Isolated Site Reflection Probe");
                probeObject.transform.SetParent(environment, false);
                probeObject.transform.localPosition = new Vector3(0f, 2.1f, 0f);
                var probe = probeObject.AddComponent<ReflectionProbe>();
                probe.size = new Vector3(12f, 6f, 12f);
                probe.resolution = 128;
                probe.boxProjection = true;
                probe.mode = ReflectionProbeMode.Baked;
            }
        }

        static void CreateInteractionManager()
        {
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        }

        static void CreateViewerRig()
        {
            var rigAsset = LoadOrImportXrRig();
            if (rigAsset != null)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset);
                rig.name = "XR Origin (Safety Training)";
                rig.transform.position = new Vector3(0f, 0.02f, -10.2f);
                rig.AddComponent<StartupGroundingGuard>();
                var rigCamera = rig.GetComponentInChildren<Camera>(true);
                if (rigCamera != null && rigCamera.GetComponent<DesktopExplorerController>() == null)
                    rigCamera.gameObject.AddComponent<DesktopExplorerController>();
                return;
            }

            if (EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)"))
            {
                Debug.LogWarning("Starter Assets XR Rig was unavailable. The fallback XR Origin may need controller setup.");
                return;
            }

            var camera = new GameObject("Desktop Preview Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, -8f), Quaternion.Euler(10f, 0f, 0f));
            camera.gameObject.AddComponent<DesktopExplorerController>();
            camera.gameObject.AddComponent<StartupGroundingGuard>();
        }

        static Transform CreateNavigationGround()
        {
            var root = new GameObject("Training Campus Navigation").transform;
            RealEnvironmentDresser.CreateCampusTerrain(root);
            RealEnvironmentDresser.AddCampusPerimeter(root);
            foreach (var x in new[] { -6f, -3f, 0f, 3f, 6f })
            {
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Site Approach Strip", root,
                    new Vector3(x, 0.015f, -0.5f), new Vector3(2.35f, 0.03f, 10f),
                    new Color(0.12f, 0.16f, 0.18f));
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Approach Edge Left", root,
                    new Vector3(x - 1.2f, 0.04f, -0.5f), new Vector3(0.06f, 0.05f, 10f),
                    new Color(0.95f, 0.68f, 0.08f));
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Approach Edge Right", root,
                    new Vector3(x + 1.2f, 0.04f, -0.5f), new Vector3(0.06f, 0.05f, 10f),
                    new Color(0.95f, 0.68f, 0.08f));
            }
            RealEnvironmentDresser.DressNavigationMarkings(root);
            return root;
        }

        static Transform CreateTrainingHubLobby()
        {
            var root = new GameObject("Construction Safety Module Selection Lobby").transform;
            var floor = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Lobby PBR Concrete Floor", root,
                new Vector3(0f, 0.01f, -6.5f), new Vector3(13.4f, 0.08f, 10.2f), Color.white);
            floor.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.ConcretePanel;

            for (var index = 0; index < 7; index++)
            {
                var x = -6f + index * 2f;
                var panel = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, $"Lobby Back Wall Panel {index + 1}",
                    root, new Vector3(x, 2f, -1.62f), new Vector3(1.98f, 4f, 0.18f), Color.white);
                panel.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
            }

            for (var index = 0; index < 5; index++)
            {
                var z = -2.6f - index * 2.05f;
                CreateLobbyWallPanel(root, $"Lobby Left Wall Panel {index + 1}", new Vector3(-6.7f, 2f, z),
                    new Vector3(0.18f, 4f, 2.03f));
                CreateLobbyWallPanel(root, $"Lobby Right Wall Panel {index + 1}", new Vector3(6.7f, 2f, z),
                    new Vector3(0.18f, 4f, 2.03f));
                var ceiling = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, $"Lobby Ceiling Bay {index + 1}",
                    root, new Vector3(0f, 4f, z), new Vector3(13.4f, 0.14f, 2.03f), Color.white);
                ceiling.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
            }

            CreateLobbyWallPanel(root, "Lobby Rear Boundary", new Vector3(0f, 2f, -11.82f),
                new Vector3(13.4f, 4f, 0.18f));
            var header = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Safety Lobby Header Panel", root,
                new Vector3(0f, 3.43f, -1.76f), new Vector3(8.2f, 0.62f, 0.08f),
                new Color(0.025f, 0.045f, 0.06f));
            Object.DestroyImmediate(header.GetComponent<Collider>());
            var title = SafetyScenePrimitives.Label("CONSTRUCTION SAFETY TRAINING", root,
                new Vector3(0f, 3.53f, -1.82f), 0.18f);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.98f, 0.68f, 0.08f);
            var subtitle = SafetyScenePrimitives.Label("SELECT A HANDS-ON FIELD MODULE", root,
                new Vector3(0f, 3.3f, -1.82f), 0.08f);
            subtitle.color = new Color(0.72f, 0.82f, 0.85f);

            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Lobby Hazard Stripe Upper", root,
                new Vector3(0f, 3.93f, -1.83f), new Vector3(13.1f, 0.08f, 0.08f),
                new Color(0.98f, 0.68f, 0.08f));
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Lobby Hazard Stripe Lower", root,
                new Vector3(0f, 0.13f, -1.83f), new Vector3(13.1f, 0.08f, 0.08f),
                new Color(0.98f, 0.68f, 0.08f));

            foreach (var x in new[] { -4.8f, -1.6f, 1.6f, 4.8f })
            {
                var lightObject = new GameObject("Lobby Industrial Work Light");
                lightObject.transform.SetParent(root, false);
                lightObject.transform.localPosition = new Vector3(x, 3.72f, -6.2f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.9f, 0.72f);
                light.intensity = 1.45f;
                light.range = 6f;
                light.shadows = LightShadows.Soft;
            }
            return root;
        }

        static void CreateLobbyWallPanel(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var panel = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, name, parent, position, scale,
                Color.white);
            panel.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
        }

        static Transform CreateSitePortals()
        {
            var root = new GameObject("Site Navigation Portals").transform;
            IndustrialPortalBuilder.CreateModePortal(root, "01", "CONSTRUCTION", "FALL PROTECTION",
                TrainingSiteId.Construction, new Vector3(-4.4f, 0f, -3.15f),
                ConstructionOrigin + new Vector3(0.9f, 0.02f, -3.55f), new Color(0.95f, 0.55f, 0.08f),
                "cement_bag_1k.fbx", new Vector3(0f, -12f, 0f));
            IndustrialPortalBuilder.CreateModePortal(root, "02", "WAREHOUSE", "VEHICLE ROUTES",
                TrainingSiteId.Warehouse, new Vector3(-2.2f, 0f, -3.72f),
                WarehouseOrigin + new Vector3(0f, 0.02f, -3.55f), new Color(0.12f, 0.48f, 0.85f),
                "plastic_crate_02_1k.fbx", new Vector3(0f, 10f, 0f));
            IndustrialPortalBuilder.CreateModePortal(root, "03", "FIRE RESPONSE", "EXTINGUISHER + EGRESS",
                TrainingSiteId.FireResponse, new Vector3(0f, 0f, -4f),
                FireOrigin + new Vector3(0.85f, 0.02f, -3.55f), new Color(0.78f, 0.16f, 0.1f),
                "korean_fire_extinguisher_01_1k.fbx", new Vector3(-90f, 0f, 0f));
            IndustrialPortalBuilder.CreateModePortal(root, "04", "CHEMICAL", "LABEL + SEGREGATE",
                TrainingSiteId.ChemicalProcessing, new Vector3(2.2f, 0f, -3.72f),
                ChemicalOrigin + new Vector3(0f, 0.02f, -3.55f), new Color(0.26f, 0.65f, 0.52f),
                "Barrel_01_1k.fbx", new Vector3(90f, 0f, 0f));
            IndustrialPortalBuilder.CreateModePortal(root, "05", "ELECTRICAL", "LOCKOUT / TAGOUT",
                TrainingSiteId.ElectricalMaintenance, new Vector3(4.4f, 0f, -3.15f),
                ElectricalOrigin + new Vector3(0.65f, 0.02f, -3.55f), new Color(0.32f, 0.38f, 0.72f),
                "metal_toolbox_1k.fbx", new Vector3(0f, 12f, 0f));
            return root;
        }

        static void CreateReturnPortal(Transform site, TrainingSiteId siteId, Color color)
        {
            var position = siteId switch
            {
                TrainingSiteId.Construction => new Vector3(0f, 0f, 2.85f),
                TrainingSiteId.Warehouse => new Vector3(0f, 0f, 2.85f),
                TrainingSiteId.FireResponse => new Vector3(0f, 0f, 2.85f),
                TrainingSiteId.ChemicalProcessing => new Vector3(0f, 0f, 2.85f),
                TrainingSiteId.ElectricalMaintenance => new Vector3(0f, 0f, 2.85f),
                _ => new Vector3(0f, 0f, 2.85f)
            };
            IndustrialPortalBuilder.CreateReturnPortal(site, siteId, position,
                HubArrival, color);
        }

        static Vector3[] SiteOrigins() => new[]
        {
            ConstructionOrigin, WarehouseOrigin, FireOrigin, ChemicalOrigin, ElectricalOrigin
        };

        static GameObject LoadOrImportXrRig()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPath);
            if (rig != null)
                return rig;

            var starterAssets = Sample.FindByPackage("com.unity.xr.interaction.toolkit", XriVersion)
                .FirstOrDefault(sample => sample.displayName == StarterAssetsName);
            if (string.IsNullOrWhiteSpace(starterAssets.displayName))
            {
                Debug.LogError("XR Interaction Toolkit Starter Assets sample was not found.");
                return null;
            }

            if (!starterAssets.isImported && !starterAssets.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                Debug.LogError("XR Interaction Toolkit Starter Assets import failed.");
                return null;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPath);
        }

        static void EnsureTmpEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                return;

            TMPro.TMP_PackageResourceImporter.ImportResources(
                importEssentials: true,
                importExamples: false,
                interactive: false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static void CreateHud()
        {
            var viewer = Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>();
            ProfessionalHudBuilder.Create(viewer);
            ProfessionalChatBuilder.Create(viewer);
        }

        static LlmEndpointConfig LoadOrCreateEndpointConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LlmEndpointConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<LlmEndpointConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
