using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SafetyTraining.Editor
{
    internal static class ImportedPropDresser
    {
        const string Models = "Assets/ThirdParty/PolyHaven/Models/";

        readonly struct PropSpec
        {
            public PropSpec(string asset, Vector3 position, Vector3 scale, Vector3 rotation)
            {
                AssetPath = Models + asset;
                Position = position;
                Scale = scale;
                Rotation = Quaternion.Euler(rotation);
            }

            public string AssetPath { get; }
            public Vector3 Position { get; }
            public Vector3 Scale { get; }
            public Quaternion Rotation { get; }
        }

        public static void DressConstruction(Transform site)
        {
            ReplaceStack(site, "Crates in Access Route", "old_military_crate_1k.fbx", 3, 0.78f);
            ReplaceStack(site, "Stored Materials", "old_military_crate_1k.fbx", 2, 0.82f);
            ReplaceVisual(site, "PPE Kit - Helmet and Vest",
                Spec("metal_toolbox_1k.fbx", new Vector3(0f, -0.62f, 0f), 0.9f, new Vector3(0f, 18f, 0f)));
            ReplaceVisual(site, "Barricade Gate",
                Spec("concrete_road_barrier_1k.fbx", new Vector3(0f, -0.65f, 0f), 0.55f, Vector3.zero));
            ReplaceVisual(site, "Guardrail Kit",
                Spec("ladder_sectioned_01_1k.fbx", new Vector3(0f, -0.34f, 0f), 0.42f, new Vector3(0f, 0f, 90f)));
            ReplaceVisual(site, "Material Cart",
                Spec("hand_truck_1k.fbx", new Vector3(0f, -0.55f, 0f), 0.8f, Vector3.zero));
            ReplaceVisual(site, "Inspection Clipboard",
                Spec("clipboard_1k.fbx", Vector3.zero, 0.85f, new Vector3(0f, 12f, 0f)));

            Add(site, Spec("industrial_storage_cart_1k.fbx", new Vector3(3.15f, 0f, -1.7f), 0.72f,
                new Vector3(0f, 180f, 0f)));
            Add(site, Spec("cement_bag_1k.fbx", new Vector3(1.55f, 0.15f, -2.25f), 0.75f,
                new Vector3(0f, -18f, 0f)));
            Add(site, Spec("Drill_01_1k.fbx", new Vector3(-0.7f, 1.12f, -3.25f), 0.32f,
                new Vector3(0f, 25f, 90f)));
            AddScaffoldSafetyDetails(site);
        }

        public static void DressWarehouse(Transform site)
        {
            ReplaceCargo(site, "Pallet in Vehicle Lane", "plastic_crate_02_1k.fbx");
            ReplaceCargo(site, "Cargo in Staging Bay", "cardboard_box_01_1k.fbx");
            Add(site, Spec("hand_truck_1k.fbx", new Vector3(-2.5f, 0f, -2.1f), 0.7f,
                new Vector3(0f, 180f, 0f)));
            Add(site, Spec("industrial_storage_cart_1k.fbx", new Vector3(-0.9f, 0f, -2.5f), 0.68f,
                new Vector3(0f, 165f, 0f)));
            Add(site, Spec("metal_toolbox_1k.fbx", new Vector3(-2.55f, 0.15f, -2.35f), 0.65f,
                new Vector3(0f, -10f, 0f)));
            AddWarehousePalletRacking(site);
            AddConvexSafetyMirror(site, new Vector3(-4.4f, 2.2f, -1.5f));
            AddProtectionBollards(site, new Vector3(-1.15f, 0.45f, 0.2f), new Vector3(-1.15f, 0.45f, 1.4f));
        }

        public static void DressFireResponse(Transform site)
        {
            ReplaceStack(site, "Obstruction", "old_military_crate_1k.fbx", 3, 1.4f);
            ReplaceCargo(site, "Pallet at Emergency Exit", "plastic_crate_02_1k.fbx");
            ReplaceExtinguisher(site, "Blocked Extinguisher");
            ReplaceExtinguisher(site, "Accessible Extinguisher");
            var blockedExtinguisher = site.Find("Blocked Extinguisher");
            var obstruction = site.Find("Obstruction");
            if (blockedExtinguisher != null && obstruction != null)
            {
                obstruction.name = "Blocked Access Obstruction";
                obstruction.SetParent(blockedExtinguisher, true);
            }
            Add(site, Spec("ladder_sectioned_01_1k.fbx", new Vector3(-1.4f, 0.02f, -2.2f), 0.8f,
                new Vector3(0f, -18f, 0f)));
            AddEmergencyExitDoorAndSign(site, new Vector3(3.45f, 0f, 4.15f));
            AddFireAlarmPullStation(site, new Vector3(-3.85f, 1.45f, 4.15f));
        }

        public static void DressChemicalProcessing(Transform site)
        {
            ReplaceDrum(site, "Leaking Solvent Drum");
            ReplaceDrum(site, "Unlabeled Chemical Drum");
            ReplaceDrum(site, "Labeled Compatible Drum");
            Add(site, Spec("cement_bag_1k.fbx", new Vector3(-1.2f, 0.1f, -2.4f), 0.6f,
                new Vector3(0f, 24f, 0f)));
            AddFlammableCabinet(site, new Vector3(3.4f, 0f, -2.5f));
            AddSecondaryContainmentSump(site, new Vector3(-2.5f, 0.04f, 0.6f));
        }

        public static void DressElectricalMaintenance(Transform site)
        {
            Add(site, Spec("Drill_01_1k.fbx", new Vector3(2.3f, 0.8f, -1.3f), 0.35f,
                new Vector3(0f, 15f, 90f)));
            Add(site, Spec("ladder_sectioned_01_1k.fbx", new Vector3(-1.7f, 0.02f, -2.3f), 0.75f,
                new Vector3(0f, 20f, 0f)));
            AddLotoStation(site, new Vector3(-3.85f, 1.5f, 4.15f));
            AddMultimeterTestingPad(site, new Vector3(2.5f, 0.04f, -1.8f));
        }

        static void ReplaceStack(Transform site, string name, string asset, int count, float scale)
        {
            var root = PrepareRoot(site, name);
            if (root == null)
                return;

            for (var index = 0; index < count; index++)
            {
                var row = index % 2;
                var level = index / 2;
                Add(root, Spec(asset, new Vector3((row - 0.5f) * 0.75f, level * 0.72f - 0.4f, 0f), scale,
                    new Vector3(0f, index * 9f - 5f, 0f)));
            }
            FitRootColliderToVisuals(root);
        }

        static void ReplaceCargo(Transform site, string name, string asset)
        {
            var root = PrepareRoot(site, name);
            if (root == null)
                return;

            Add(root, Spec(asset, new Vector3(-0.48f, -0.12f, 0f), 0.9f, new Vector3(0f, -8f, 0f)));
            Add(root, Spec(asset, new Vector3(0.48f, -0.12f, 0.04f), 0.9f, new Vector3(0f, 7f, 0f)));
            Add(root, Spec(asset, new Vector3(0f, 0.65f, 0f), 0.86f, new Vector3(0f, 3f, 0f)));
            FitRootColliderToVisuals(root);
        }

        static void ReplaceDrum(Transform site, string name)
        {
            var root = PrepareRoot(site, name);
            if (root == null)
                return;

            Add(root, Spec("Barrel_01_1k.fbx", new Vector3(0f, 0.24f, 0f), 102f,
                new Vector3(90f, 0f, 0f)));
            FitRootColliderToVisuals(root);
            if (name == "Leaking Solvent Drum")
                CreateVisibleSolventLeak(root);
        }

        static void CreateVisibleSolventLeak(Transform parent)
        {
            var evidence = new GameObject("Visible Solvent Leak").transform;
            evidence.SetParent(parent, false);
            var floorOffset = -parent.localPosition.y;
            var solvent = new Color(0.025f, 0.08f, 0.07f);
            var pieces = new[]
            {
                SafetyScenePrimitives.Primitive(PrimitiveType.Sphere, "Solvent Puddle Main", evidence,
                    new Vector3(0.1f, floorOffset + 0.018f, -0.3f), new Vector3(0.78f, 0.025f, 0.91f), solvent),
                SafetyScenePrimitives.Primitive(PrimitiveType.Sphere, "Solvent Puddle Lobe A", evidence,
                    new Vector3(-0.24f, floorOffset + 0.021f, -0.18f), new Vector3(0.4f, 0.02f, 0.5f), solvent),
                SafetyScenePrimitives.Primitive(PrimitiveType.Sphere, "Solvent Puddle Lobe B", evidence,
                    new Vector3(0.38f, floorOffset + 0.022f, -0.16f), new Vector3(0.36f, 0.018f, 0.42f), solvent),
                SafetyScenePrimitives.Primitive(PrimitiveType.Capsule, "Active Solvent Drip", evidence,
                    new Vector3(0f, floorOffset + 0.34f, -0.34f), new Vector3(0.055f, 0.32f, 0.055f), solvent)
            };
            foreach (var piece in pieces)
                Object.DestroyImmediate(piece.GetComponent<Collider>());
        }

        static void ReplaceExtinguisher(Transform site, string name)
        {
            var root = PrepareRoot(site, name);
            if (root == null)
                return;

            var model = Add(root, Spec("korean_fire_extinguisher_01_1k.fbx", Vector3.zero, 125f,
                new Vector3(-90f, 0f, 0f)));
            GroundOnParentFloor(model, root, -0.02f);
            FitRootColliderToVisuals(root);
            CreateExtinguisherFloorStand(root);
            CreateEnglishExtinguisherTag(root);
        }

        static void CreateExtinguisherFloorStand(Transform parent)
        {
            var red = new Color(0.38f, 0.025f, 0.018f);
            var basePlate = SafetyScenePrimitives.Primitive(PrimitiveType.Cube,
                "Fire Extinguisher Floor Base", parent, new Vector3(0f, 0.025f, 0.06f),
                new Vector3(0.48f, 0.05f, 0.34f), red);
            var support = SafetyScenePrimitives.Primitive(PrimitiveType.Cube,
                "Fire Extinguisher Floor Support", parent, new Vector3(0f, 0.22f, 0.12f),
                new Vector3(0.14f, 0.42f, 0.14f), red);
            Object.DestroyImmediate(basePlate.GetComponent<Collider>());
            Object.DestroyImmediate(support.GetComponent<Collider>());
        }

        static void CreateEnglishExtinguisherTag(Transform parent)
        {
            var anchor = new GameObject("English Fire Extinguisher Tag");
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(0.76f, 1.28f, 0.02f);
            anchor.AddComponent<SafetyTraining.Runtime.BillboardLabel>();
            var plate = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "English Tag Backplate", anchor.transform,
                Vector3.zero, new Vector3(0.42f, 0.23f, 0.025f), new Color(0.62f, 0.035f, 0.025f));
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            SafetyScenePrimitives.Label("FIRE EXTINGUISHER\nP.A.S.S.", anchor.transform,
                new Vector3(0f, 0f, -0.02f), 0.028f);
        }

        static void GroundOnParentFloor(GameObject instance, Transform parent, float clearance)
        {
            if (instance == null)
                return;
            var renderers = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            instance.transform.position += Vector3.up * (parent.position.y + clearance - bounds.min.y);
        }

        static void ReplaceVisual(Transform site, string name, PropSpec spec)
        {
            var root = PrepareRoot(site, name);
            if (root != null)
            {
                Add(root, spec);
                FitRootColliderToVisuals(root);
            }
        }

        static void FitRootColliderToVisuals(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.GetComponent<TextMesh>() == null).ToArray();
            if (renderers.Length == 0)
                return;

            var worldBounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                worldBounds.Encapsulate(renderer.bounds);

            var localBounds = new Bounds(root.InverseTransformPoint(worldBounds.center), Vector3.zero);
            foreach (var x in new[] { worldBounds.min.x, worldBounds.max.x })
            foreach (var y in new[] { worldBounds.min.y, worldBounds.max.y })
            foreach (var z in new[] { worldBounds.min.z, worldBounds.max.z })
                localBounds.Encapsulate(root.InverseTransformPoint(new Vector3(x, y, z)));

            foreach (var collider in root.GetComponents<Collider>())
                Object.DestroyImmediate(collider);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = localBounds.size + Vector3.one * 0.06f;
        }

        static Transform PrepareRoot(Transform site, string name)
        {
            var root = site.Find(name);
            if (root == null)
            {
                Debug.LogWarning($"Placeholder prop was not found: {site.name}/{name}");
                return null;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                if (collider.transform != root)
                    collider.enabled = false;

            var originalScale = root.localScale;
            if (root.TryGetComponent<BoxCollider>(out var box))
                box.size = Vector3.Scale(box.size, originalScale);
            else if (root.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                capsule.radius *= Mathf.Max(originalScale.x, originalScale.z);
                capsule.height *= originalScale.y;
            }
            root.localScale = Vector3.one;
            return root;
        }

        static PropSpec Spec(string asset, Vector3 position, float uniformScale, Vector3 rotation)
        {
            return new PropSpec(asset, position, Vector3.one * uniformScale, rotation);
        }

        static GameObject Add(Transform parent, PropSpec spec)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.AssetPath);
            if (asset == null)
            {
                Debug.LogWarning($"Poly Haven model not imported yet: {spec.AssetPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = $"RealAsset - {asset.name}";
            instance.transform.localPosition = spec.Position;
            instance.transform.localRotation = spec.Rotation;
            instance.transform.localScale = spec.Scale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            return instance;
        }

        static void AddScaffoldSafetyDetails(Transform site)
        {
            var details = new GameObject("Scaffold Safety Signage").transform;
            details.SetParent(site, false);
            details.localPosition = new Vector3(-2.7f, 2.3f, 1.15f);

            var sign = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Fall Protection Sign", details,
                Vector3.zero, new Vector3(0.85f, 0.42f, 0.04f), new Color(0.95f, 0.68f, 0.08f));
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            SafetyScenePrimitives.Label("FALL PROTECTION\n100% TIE-OFF REQUIRED", details,
                new Vector3(0f, 0f, -0.03f), 0.038f);
        }

        static void AddWarehousePalletRacking(Transform site)
        {
            var rack = new GameObject("Warehouse Pallet Racking High-Bay").transform;
            rack.SetParent(site, false);
            rack.localPosition = new Vector3(-0.2f, 0f, 3.85f);

            var blueSteel = new Color(0.12f, 0.28f, 0.48f);
            var safetyOrange = new Color(0.92f, 0.45f, 0.08f);

            foreach (var x in new[] { -1.8f, 0f, 1.8f })
            {
                foreach (var z in new[] { -0.4f, 0.4f })
                {
                    var post = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Rack Upright", rack,
                        new Vector3(x, 1.8f, z), new Vector3(0.08f, 3.6f, 0.08f), blueSteel);
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }
            }

            foreach (var y in new[] { 1.1f, 2.2f })
            {
                foreach (var z in new[] { -0.4f, 0.4f })
                {
                    var beam = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Load Beam", rack,
                        new Vector3(0f, y, z), new Vector3(3.7f, 0.08f, 0.05f), safetyOrange);
                    Object.DestroyImmediate(beam.GetComponent<Collider>());
                }
            }

            Add(rack, Spec("cardboard_box_01_1k.fbx", new Vector3(-0.9f, 1.15f, 0f), 0.65f, Vector3.zero));
            Add(rack, Spec("plastic_crate_02_1k.fbx", new Vector3(0.9f, 1.15f, 0f), 0.65f, Vector3.zero));
        }

        static void AddConvexSafetyMirror(Transform site, Vector3 position)
        {
            var mirrorObj = new GameObject("Convex Blind Corner Safety Mirror").transform;
            mirrorObj.SetParent(site, false);
            mirrorObj.localPosition = position;

            var post = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Mirror Post", mirrorObj,
                new Vector3(0f, -0.6f, 0f), new Vector3(0.06f, 0.6f, 0.06f), new Color(0.36f, 0.42f, 0.48f));
            Object.DestroyImmediate(post.GetComponent<Collider>());

            var frame = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Mirror Frame", mirrorObj,
                Vector3.zero, new Vector3(0.72f, 0.04f, 0.72f), new Color(0.95f, 0.68f, 0.08f));
            frame.transform.localRotation = Quaternion.Euler(60f, 45f, 0f);
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            var lens = SafetyScenePrimitives.Primitive(PrimitiveType.Sphere, "Mirror Dome Lens", mirrorObj,
                new Vector3(0f, 0f, -0.02f), new Vector3(0.64f, 0.64f, 0.18f), new Color(0.85f, 0.88f, 0.92f));
            lens.transform.localRotation = Quaternion.Euler(60f, 45f, 0f);
            Object.DestroyImmediate(lens.GetComponent<Collider>());
        }

        static void AddProtectionBollards(Transform site, params Vector3[] positions)
        {
            var root = new GameObject("Pedestrian Safety Bollards").transform;
            root.SetParent(site, false);

            foreach (var pos in positions)
            {
                var bollard = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Protection Bollard", root,
                    pos, new Vector3(0.24f, 0.45f, 0.24f), new Color(0.95f, 0.68f, 0.08f));
                Object.DestroyImmediate(bollard.GetComponent<Collider>());

                var stripe = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Reflective Stripe", bollard.transform,
                    new Vector3(0f, 0.35f, 0f), new Vector3(1.04f, 0.18f, 1.04f), new Color(0.1f, 0.12f, 0.14f));
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
            }
        }

        static void AddEmergencyExitDoorAndSign(Transform site, Vector3 position)
        {
            var doorRoot = new GameObject("Emergency Exit Door Assembly").transform;
            doorRoot.SetParent(site, false);
            doorRoot.localPosition = position;

            var frame = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Exit Door Frame", doorRoot,
                new Vector3(0f, 1.25f, 0f), new Vector3(1.4f, 2.5f, 0.12f), new Color(0.18f, 0.22f, 0.26f));
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            var leaf = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Exit Door Leaf", doorRoot,
                new Vector3(0f, 1.25f, 0.02f), new Vector3(1.25f, 2.38f, 0.06f), new Color(0.72f, 0.75f, 0.78f));
            Object.DestroyImmediate(leaf.GetComponent<Collider>());

            var pushBar = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Panic Push Bar", doorRoot,
                new Vector3(0f, 1.05f, -0.06f), new Vector3(1.05f, 0.08f, 0.08f), new Color(0.85f, 0.12f, 0.08f));
            Object.DestroyImmediate(pushBar.GetComponent<Collider>());

            var sign = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Illuminated EXIT Sign", doorRoot,
                new Vector3(0f, 2.72f, -0.08f), new Vector3(1.1f, 0.42f, 0.12f), new Color(0.08f, 0.48f, 0.24f));
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            SafetyScenePrimitives.Label("EXIT  →", doorRoot, new Vector3(0f, 2.72f, -0.16f), 0.08f);
        }

        static void AddFireAlarmPullStation(Transform site, Vector3 position)
        {
            var pullRoot = new GameObject("Fire Alarm Manual Pull Station").transform;
            pullRoot.SetParent(site, false);
            pullRoot.localPosition = position;

            var box = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Pull Station Body", pullRoot,
                Vector3.zero, new Vector3(0.35f, 0.48f, 0.14f), new Color(0.85f, 0.12f, 0.08f));
            Object.DestroyImmediate(box.GetComponent<Collider>());

            var handle = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Pull Handle", pullRoot,
                new Vector3(0f, -0.06f, -0.08f), new Vector3(0.18f, 0.14f, 0.05f), Color.white);
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            var strobe = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Emergency Horn Strobe", pullRoot,
                new Vector3(0f, 0.38f, 0f), new Vector3(0.24f, 0.12f, 0.24f), Color.white);
            Object.DestroyImmediate(strobe.GetComponent<Collider>());

            SafetyScenePrimitives.Label("PULL\nALARM", pullRoot, new Vector3(0f, 0.12f, -0.08f), 0.035f);
        }

        static void AddFlammableCabinet(Transform site, Vector3 position)
        {
            var cabinet = new GameObject("OSHA Flammable Storage Cabinet").transform;
            cabinet.SetParent(site, false);
            cabinet.localPosition = position;

            var safetyYellow = new Color(0.95f, 0.72f, 0.08f);
            var body = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Cabinet Body", cabinet,
                new Vector3(0f, 0.95f, 0f), new Vector3(1.2f, 1.9f, 0.65f), safetyYellow);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var placard = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Flammable Placard", cabinet,
                new Vector3(0f, 1.45f, -0.34f), new Vector3(0.85f, 0.35f, 0.03f), new Color(0.85f, 0.12f, 0.08f));
            Object.DestroyImmediate(placard.GetComponent<Collider>());

            SafetyScenePrimitives.Label("FLAMMABLE\nKEEP FIRE AWAY", cabinet, new Vector3(0f, 1.45f, -0.37f), 0.045f);
        }

        static void AddSecondaryContainmentSump(Transform site, Vector3 position)
        {
            var sump = new GameObject("Secondary Containment Spill Bund").transform;
            sump.SetParent(site, false);
            sump.localPosition = position;

            var yellowPoly = new Color(0.95f, 0.78f, 0.05f);
            var basin = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Sump Basin", sump,
                new Vector3(0f, 0.08f, 0f), new Vector3(1.5f, 0.16f, 1.5f), yellowPoly);
            Object.DestroyImmediate(basin.GetComponent<Collider>());

            var grate = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Floor Grating", sump,
                new Vector3(0f, 0.17f, 0f), new Vector3(1.4f, 0.04f, 1.4f), new Color(0.15f, 0.17f, 0.2f));
            Object.DestroyImmediate(grate.GetComponent<Collider>());
        }

        static void AddLotoStation(Transform site, Vector3 position)
        {
            var station = new GameObject("LOTO Lockout Tagout Station").transform;
            station.SetParent(site, false);
            station.localPosition = position;

            var board = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "LOTO Board", station,
                Vector3.zero, new Vector3(1.1f, 0.85f, 0.04f), new Color(0.95f, 0.72f, 0.08f));
            Object.DestroyImmediate(board.GetComponent<Collider>());

            SafetyScenePrimitives.Label("LOCKOUT / TAGOUT\nSTATION", station, new Vector3(0f, 0.26f, -0.03f), 0.05f);

            foreach (var x in new[] { -0.35f, -0.12f, 0.12f, 0.35f })
            {
                var lockBody = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Padlock Body", station,
                    new Vector3(x, -0.1f, -0.04f), new Vector3(0.12f, 0.15f, 0.06f), new Color(0.85f, 0.12f, 0.08f));
                Object.DestroyImmediate(lockBody.GetComponent<Collider>());

                var shackle = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Padlock Shackle", station,
                    new Vector3(x, 0.02f, -0.04f), new Vector3(0.06f, 0.08f, 0.06f), new Color(0.72f, 0.75f, 0.78f));
                Object.DestroyImmediate(shackle.GetComponent<Collider>());
            }
        }

        static void AddMultimeterTestingPad(Transform site, Vector3 position)
        {
            var padRoot = new GameObject("Multimeter Verification Pad").transform;
            padRoot.SetParent(site, false);
            padRoot.localPosition = position;

            var mat = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Insulating Mat", padRoot,
                Vector3.zero, new Vector3(1.1f, 0.02f, 0.9f), new Color(0.18f, 0.18f, 0.22f));
            Object.DestroyImmediate(mat.GetComponent<Collider>());

            var tester = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Digital Multimeter", padRoot,
                new Vector3(0f, 0.04f, 0f), new Vector3(0.24f, 0.06f, 0.42f), new Color(0.95f, 0.65f, 0.05f));
            Object.DestroyImmediate(tester.GetComponent<Collider>());

            SafetyScenePrimitives.Label("0.0V TESTED", padRoot, new Vector3(0f, 0.08f, 0f), 0.028f);
        }
    }
}
