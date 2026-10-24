using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SafetyTraining.Editor
{
    internal static class RealEnvironmentDresser
    {
        const string ModelRoot = "Assets/ThirdParty/PolyHaven/Environment/Models/";
        const string HdriPath = "Assets/ThirdParty/PolyHaven/Environment/HDRI/construction_yard_1k.hdr";
        const string GeneratedRoot = "Assets/SafetyTraining/GeneratedMaterials/RealEnvironment";

        public enum SiteStyle
        {
            Construction,
            Warehouse,
            FireResponse,
            ChemicalProcessing,
            ElectricalMaintenance
        }

        readonly struct Placement
        {
            public Placement(string asset, string name, Vector3 position, float size, Vector3 rotation)
            {
                Asset = asset;
                Name = name;
                Position = position;
                Size = size;
                Rotation = rotation;
            }

            public string Asset { get; }
            public string Name { get; }
            public Vector3 Position { get; }
            public float Size { get; }
            public Vector3 Rotation { get; }
        }

        public static void ApplyConstructionYardSkybox()
        {
            if (AssetImporter.GetAtPath(HdriPath) is TextureImporter importer &&
                importer.textureShape != TextureImporterShape.TextureCube)
            {
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
                importer.SaveAndReimport();
            }

            var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath);
            var shader = Shader.Find("Skybox/Cubemap");
            if (cubemap == null || shader == null)
                return;

            SafetyScenePrimitives.EnsureFolder(GeneratedRoot);
            var path = $"{GeneratedRoot}/Construction Yard HDRI.mat";
            var skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (skybox == null)
            {
                skybox = new Material(shader);
                AssetDatabase.CreateAsset(skybox, path);
            }
            skybox.name = "Real Construction Yard HDRI";
            skybox.SetTexture("_Tex", cubemap);
            skybox.SetFloat("_Exposure", 0.82f);
            skybox.SetFloat("_Rotation", 96f);
            RenderSettings.skybox = skybox;
            RenderSettings.reflectionIntensity = 0.92f;
            DynamicGI.UpdateEnvironment();
        }

        public static void CreateCampusTerrain(Transform parent)
        {
            SafetyScenePrimitives.EnsureFolder(GeneratedRoot);
            var dataPath = $"{GeneratedRoot}/Campus Terrain.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.heightmapResolution = 129;
            data.size = new Vector3(96f, 2f, 36f);
            data.SetHeights(0, 0, new float[129, 129]);
            data.terrainLayers = new[] { RealEnvironmentMaterials.AsphaltTerrainLayer() };
            var terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Continuous Walkable Ground";
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.localPosition = new Vector3(-48f, -0.06f, -10f);
            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 250f;
        }

        public static void DressNavigationMarkings(Transform navigation)
        {
            foreach (var renderer in navigation.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Site Approach Strip")
                    renderer.sharedMaterial = RealEnvironmentMaterials.Asphalt;
        }

        public static void AddCampusPerimeter(Transform parent)
        {
            foreach (var x in new[] { -32f, -16f, 0f, 16f, 32f })
                Place(parent, new Placement("modular_chainlink_fence_1k.fbx", "Perimeter Fence",
                    new Vector3(x, 0f, 15.5f), 10f, new Vector3(0f, 90f, 0f)));
        }

        public static void DressSite(Transform site, SiteStyle style)
        {
            DressPrimitiveSurfaces(site, style);
            CreateStructuralFraming(site);
            Place(site, new Placement("caged_hanging_light_1k.fbx", "Industrial Work Light",
                new Vector3(0f, 3.25f, 3.45f), 0.75f, Vector3.zero));

            switch (style)
            {
                case SiteStyle.Construction:
                    Place(site, new Placement("portable_generator_1k.fbx", "Portable Generator",
                        new Vector3(3.25f, 0f, -1.8f), 1.7f, new Vector3(0f, 150f, 0f)));
                    break;
                case SiteStyle.Warehouse:
                    AddRollerDoor(site, new Vector3(2.4f, 0f, 4.12f));
                    Place(site, new Placement("modular_chainlink_fence_1k.fbx", "Warehouse Separation Fence",
                        new Vector3(-3.5f, 0f, 0f), 6f, new Vector3(0f, 90f, 0f)));
                    break;
                case SiteStyle.FireResponse:
                    AddRollerDoor(site, new Vector3(-1.8f, 0f, 4.12f));
                    break;
                case SiteStyle.ChemicalProcessing:
                    Place(site, new Placement("modular_industrial_pipes_01_1k.fbx", "Process Pipe Rack",
                        new Vector3(-3.2f, 0f, 2.8f), 4.5f, new Vector3(0f, 90f, 0f)));
                    break;
                case SiteStyle.ElectricalMaintenance:
                    Place(site, new Placement("utility_box_01_1k.fbx", "Utility Cabinet A",
                        new Vector3(-2.5f, 0f, 3.25f), 1.8f, Vector3.zero));
                    Place(site, new Placement("utility_box_01_1k.fbx", "Utility Cabinet B",
                        new Vector3(2.5f, 0f, 3.25f), 1.6f, new Vector3(0f, 180f, 0f)));
                    break;
            }
        }

        public static void AddIsolationEnclosure(Transform site, SiteStyle style)
        {
            CreateCorrugatedHoarding(site, "Opaque Hoarding Left", new Vector3(-4.82f, 0f, 0f),
                8.6f, 4.2f, 90f);
            CreateCorrugatedHoarding(site, "Opaque Hoarding Right", new Vector3(4.82f, 0f, 0f),
                8.6f, 4.2f, -90f);
            CreateCorrugatedHoarding(site, "Opaque Hoarding Rear", new Vector3(0f, 0f, 4.18f),
                9.7f, 4.2f, 180f);
            CreateCorrugatedHoarding(site, "Opaque Hoarding Entry Left", new Vector3(-3.35f, 0f, -4.18f),
                3.25f, 4.2f, 0f);
            CreateCorrugatedHoarding(site, "Opaque Hoarding Entry Right", new Vector3(3.35f, 0f, -4.18f),
                3.25f, 4.2f, 0f);
            CreateCorrugatedHoarding(site, "Opaque Hoarding Entry Center", new Vector3(0f, 0f, -4.18f),
                3.45f, 4.2f, 0f);

            AddBoundaryCollider(site, "Isolation Collider Left", new Vector3(-4.9f, 2.1f, 0f),
                new Vector3(0.25f, 4.2f, 8.6f));
            AddBoundaryCollider(site, "Isolation Collider Right", new Vector3(4.9f, 2.1f, 0f),
                new Vector3(0.25f, 4.2f, 8.6f));
            AddBoundaryCollider(site, "Isolation Collider Rear", new Vector3(0f, 2.1f, 4.3f),
                new Vector3(10f, 4.2f, 0.25f));
            AddBoundaryCollider(site, "Isolation Collider Entry Left", new Vector3(-3.4f, 2.1f, -4.3f),
                new Vector3(3.2f, 4.2f, 0.25f));
            AddBoundaryCollider(site, "Isolation Collider Entry Right", new Vector3(3.4f, 2.1f, -4.3f),
                new Vector3(3.2f, 4.2f, 0.25f));
            AddBoundaryCollider(site, "Isolation Collider Entry Center", new Vector3(0f, 2.1f, -4.3f),
                new Vector3(3.55f, 4.2f, 0.25f));
        }

        static void CreateCorrugatedHoarding(Transform parent, string name, Vector3 localPosition,
            float width, float height, float yaw)
        {
            const float corrugationWidth = 0.18f;
            const float corrugationDepth = 0.055f;
            var segments = Mathf.Max(8, Mathf.CeilToInt(width / corrugationWidth));
            var vertices = new List<Vector3>((segments + 1) * 2);
            var uvs = new List<Vector2>((segments + 1) * 2);
            var triangles = new List<int>(segments * 12);
            for (var index = 0; index <= segments; index++)
            {
                var t = index / (float)segments;
                var x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                var ridge = (Mathf.PingPong(index * 0.5f, 1f) - 0.5f) * corrugationDepth * 2f;
                vertices.Add(new Vector3(x, 0f, ridge));
                vertices.Add(new Vector3(x, height, ridge));
                uvs.Add(new Vector2(t * width * 0.5f, 0f));
                uvs.Add(new Vector2(t * width * 0.5f, height * 0.5f));
                if (index == segments)
                    continue;
                var start = index * 2;
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            var mesh = new Mesh { name = $"{name} Corrugated Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            const string meshFolder = "Assets/SafetyTraining/Generated/IsolationMeshes";
            SafetyScenePrimitives.EnsureFolder(meshFolder);
            var meshName = $"{parent.name}-{name}".Replace(" ", "_");
            var meshPath = $"{meshFolder}/{meshName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            var panel = new GameObject($"RealEnvironment - {name}");
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            panel.AddComponent<MeshFilter>().sharedMesh = mesh;
            panel.AddComponent<MeshRenderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
            CreateHoardingPost(panel.transform, -width * 0.5f, height);
            CreateHoardingPost(panel.transform, width * 0.5f, height);
        }

        static void CreateHoardingPost(Transform parent, float x, float height)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Galvanized Hoarding Post";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(x, height * 0.5f, 0f);
            post.transform.localScale = new Vector3(0.075f, height * 0.5f, 0.075f);
            post.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }

        static void AddBoundaryCollider(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.localPosition = localPosition;
            barrier.AddComponent<BoxCollider>().size = size;
        }

        static void AddRollerDoor(Transform site, Vector3 localPosition)
        {
            Place(site, new Placement("rollershutter_door_1k.fbx", "Industrial Roller Door",
                localPosition, 3.8f, new Vector3(0f, 180f, 0f)));
        }

        static void CreateStructuralFraming(Transform site)
        {
            var framing = new GameObject("Structural Steel Framing").transform;
            framing.SetParent(site, false);

            var cornerPositions = new[]
            {
                new Vector3(-4.75f, 2.1f, -4.15f),
                new Vector3(4.75f, 2.1f, -4.15f),
                new Vector3(-4.75f, 2.1f, 4.15f),
                new Vector3(4.75f, 2.1f, 4.15f)
            };

            foreach (var pos in cornerPositions)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = "Steel H-Column";
                col.transform.SetParent(framing, false);
                col.transform.localPosition = pos;
                col.transform.localScale = new Vector3(0.28f, 4.2f, 0.28f);
                col.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
                Object.DestroyImmediate(col.GetComponent<Collider>());

                var boot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boot.name = "Impact Protection Base";
                boot.transform.SetParent(framing, false);
                boot.transform.localPosition = new Vector3(pos.x, 0.45f, pos.z);
                boot.transform.localScale = new Vector3(0.44f, 0.9f, 0.44f);
                boot.GetComponent<Renderer>().sharedMaterial = SafetyScenePrimitives.Material(new Color(0.95f, 0.68f, 0.08f));
                Object.DestroyImmediate(boot.GetComponent<Collider>());
            }

            foreach (var z in new[] { -2.2f, 0.8f, 3.8f })
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "Ceiling Structural I-Beam";
                beam.transform.SetParent(framing, false);
                beam.transform.localPosition = new Vector3(0f, 4.15f, z);
                beam.transform.localScale = new Vector3(9.7f, 0.25f, 0.18f);
                beam.GetComponent<Renderer>().sharedMaterial = RealEnvironmentMaterials.MetalSheet;
                Object.DestroyImmediate(beam.GetComponent<Collider>());
            }
        }

        static void DressPrimitiveSurfaces(Transform site, SiteStyle style)
        {
            foreach (var renderer in site.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TextMesh>() != null)
                    continue;
                var name = renderer.name;
                if (name == "Worklight Header")
                    renderer.enabled = false;
                else if (name == "Back Wall")
                    renderer.sharedMaterial = style == SiteStyle.Warehouse ||
                                              style == SiteStyle.ElectricalMaintenance
                        ? RealEnvironmentMaterials.MetalSheet
                        : RealEnvironmentMaterials.ConcreteWall;
                else if (name == "Floor")
                    renderer.sharedMaterial = RealEnvironmentMaterials.DamagedConcrete;
                else if (name.Contains("Platform") || name.Contains("Deck"))
                    renderer.sharedMaterial = RealEnvironmentMaterials.ConcretePanel;
                else if (name.Contains("Scaffold") || name.Contains("Support") || name.Contains("Rail") ||
                         name.Contains("Panel") || name.Contains("Protector"))
                    renderer.sharedMaterial = RealEnvironmentMaterials.MetalSheet;
            }
        }

        static GameObject Place(Transform parent, Placement placement)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + placement.Asset);
            if (asset == null)
                return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = $"RealEnvironment - {placement.Name}";
            instance.transform.localPosition = placement.Position;
            instance.transform.localRotation = Quaternion.Euler(placement.Rotation);
            instance.transform.localScale = Vector3.one;
            var children = instance.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child == null || child == instance.transform)
                    continue;
                if (child.name.ToLowerInvariant().Contains("graffiti"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(true);
                }
            }
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            var renderers = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                return instance;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            var largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            instance.transform.localScale = Vector3.one * (placement.Size / Mathf.Max(largest, 0.001f));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            instance.transform.position += Vector3.up * (parent.position.y - bounds.min.y);
            return instance;
        }
    }
}
