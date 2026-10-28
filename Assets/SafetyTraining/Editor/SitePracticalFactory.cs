using System.Linq;
using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Editor
{
    internal static class SitePracticalFactory
    {
        const string Models = "Assets/ThirdParty/PolyHaven/Models/";
        const float IndustrialCartSize = 1.35f;
        const float InspectionCartSize = 0.8f;
        const float PortableAcceptanceRadius = 0.85f;
        const float LargeAcceptanceRadius = 1.1f;

        readonly struct ActionSpec
        {
            public ActionSpec(string name, string asset, Vector3 position, float scale, Vector3 rotation,
                string instruction, Vector3 destination, Vector3 destinationRotation)
            {
                Name = name;
                Asset = asset;
                Position = position;
                Scale = scale;
                Rotation = rotation;
                Instruction = instruction;
                Destination = destination;
                DestinationRotation = destinationRotation;
            }

            public string Name { get; }
            public string Asset { get; }
            public Vector3 Position { get; }
            public float Scale { get; }
            public Vector3 Rotation { get; }
            public string Instruction { get; }
            public Vector3 Destination { get; }
            public Vector3 DestinationRotation { get; }
        }

        public static void CreateAll(Transform warehouse, Transform fire, Transform chemical, Transform electrical)
        {
            Create(warehouse, TrainingSiteId.Warehouse,
                "Warehouse traffic-control sequence.",
                "Warehouse practical complete. Ask the safety lead for the shift handoff.",
                new[]
                {
                    Spec("Deploy spill response kit", "metal_toolbox_1k.fbx", -0.55f, 0.65f, 0.72f, 12f,
                        "Open the spill kit at the isolated pedestrian aisle.", -3f, 0.35f, 0f),
                    Spec("Set vehicle route barrier", "concrete_road_barrier_1k.fbx", 0.75f, 0.75f, 0.52f, 90f,
                        "Move the barrier between the pedestrian and vehicle paths.", 0.8f, 0.7f, 90f),
                    Spec("Relocate aisle load", "industrial_storage_cart_1k.fbx", -1.65f, 1.8f, 0.66f, 160f,
                        "Return the cart to the cargo staging bay.", 2.5f, -2.25f, 180f)
                });
            Create(fire, TrainingSiteId.FireResponse,
                "Fire-equipment readiness sequence.",
                "Fire response practical complete. The equipment and exit route are ready.",
                new[]
                {
                    Spec("Remove fire-point obstruction", "old_military_crate_1k.fbx", -1.95f, 0.6f, 1.05f, -8f,
                        "Move stored material outside the extinguisher access zone.", -3.7f, 3f, 8f),
                    Spec("Ready fire response kit", "metal_toolbox_1k.fbx", 0.75f, 0.75f, 0.72f, -12f,
                        "Place the response kit beside the accessible fire point.", -3.6f, -1.8f, 0f),
                    Spec("Verify emergency egress", "clipboard_1k.fbx", 0f, 0.05f, 0.86f, 12f,
                        "Complete the exit-route walkdown and sign the check sheet.", 0f, 2.3f, 0f)
                });
            Create(chemical, TrainingSiteId.ChemicalProcessing,
                "Chemical-release control sequence.",
                "Chemical practical complete. Containment, labeling, and eyewash checks are recorded.",
                new[]
                {
                    Spec("Deploy spill containment", "metal_toolbox_1k.fbx", -0.55f, 0.65f, 0.72f, 10f,
                        "Position the containment kit outside the leaking drum splash zone.", -0.8f, -0.9f, 0f),
                    Spec("Apply GHS identification", "clipboard_1k.fbx", 0.75f, 0.75f, 0.86f, -10f,
                        "Confirm the product identity and attach the correct GHS record.", 2.5f, 0.55f, 0f),
                    Spec("Test emergency eyewash", "metal_toolbox_1k.fbx", -1.65f, 1.8f, 0.68f, -8f,
                        "Use the test kit and document clear flow at the eyewash.", 0.1f, -1.7f, 0f)
                });
            Create(electrical, TrainingSiteId.ElectricalMaintenance,
                "Electrical-isolation control sequence.",
                "Electrical practical complete. Energy isolation and access controls are verified.",
                new[]
                {
                    Spec("Apply lockout tagout kit", "metal_toolbox_1k.fbx", -0.55f, 0.65f, 0.72f, 8f,
                        "Place the lockout kit at the disconnect and apply personal control.", -2.45f, 0.25f, 0f),
                    Spec("Verify zero energy", "clipboard_1k.fbx", -0.25f, 1.35f, 0.86f, -8f,
                        "Record the approved absence-of-voltage verification.", 2.35f, 0.4f, 0f),
                    Spec("Protect cable crossing", "concrete_road_barrier_1k.fbx", -1.65f, 1.8f, 0.5f, 90f,
                        "Position the protective control across the exposed cable route.", 0.3f, -0.1f, 90f)
                });
        }

        static ActionSpec Spec(string name, string asset, float x, float z, float scale, float yaw,
            string instruction, float destinationX, float destinationZ, float destinationYaw)
        {
            return new ActionSpec(name, asset, new Vector3(x, 0f, z), scale, new Vector3(0f, yaw, 0f),
                instruction, new Vector3(destinationX, 0f, destinationZ), new Vector3(0f, destinationYaw, 0f));
        }

        static void Create(Transform site, TrainingSiteId siteId, string introduction, string completion,
            ActionSpec[] specs)
        {
            var controller = site.gameObject.AddComponent<SitePracticalController>();
            controller.Configure(siteId, introduction, completion);
            for (var index = 0; index < specs.Length; index++)
            {
                if (siteId == TrainingSiteId.FireResponse && index == 0)
                    CreateExistingFireObstructionAction(site, controller, specs[index]);
                else
                    CreateAction(site, controller, index, specs[index]);
            }
        }

        static void CreateExistingFireObstructionAction(Transform site, SitePracticalController controller,
            ActionSpec spec)
        {
            var root = site.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Blocked Access Obstruction");
            if (root == null)
                throw new System.InvalidOperationException("Fire access obstruction asset is missing.");
            var sitePosition = site.InverseTransformPoint(root.position);
            sitePosition.x = spec.Position.x;
            sitePosition.z = spec.Position.z;
            root.position = site.TransformPoint(sitePosition);
            var simpleInteractable = root.GetComponent<XRSimpleInteractable>();
            if (simpleInteractable != null)
                Object.DestroyImmediate(simpleInteractable);
            var extinguisherInteractable = root.parent.GetComponent<XRSimpleInteractable>();
            extinguisherInteractable.colliders.Clear();
            extinguisherInteractable.colliders.Add(root.parent.GetComponent<Collider>());
            if (root.GetComponent<InteractiveHoverFeedback>() == null)
                root.gameObject.AddComponent<InteractiveHoverFeedback>();
            var action = root.gameObject.AddComponent<SitePracticalAction>();
            var completedLocalPosition = root.parent.InverseTransformPoint(site.TransformPoint(spec.Destination));
            var acceptanceRadius = AcceptanceRadius(spec.Asset);
            var target = PlacementTargetFactory.Create(root.parent, root.name, completedLocalPosition,
                spec.Name, acceptanceRadius);
            action.Configure(controller, 0, spec.Name, spec.Instruction, completedLocalPosition,
                spec.DestinationRotation, acceptanceRadius, target);
            AddTaskLabel(root, 1, spec.Name);
            var marker = root.Find("Practical Step Marker");
            marker.localPosition += Vector3.right * 0.45f;
        }

        static void CreateAction(Transform site, SitePracticalController controller, int step, ActionSpec spec)
        {
            var root = new GameObject($"Practical {step + 1} - {spec.Name}");
            root.transform.SetParent(site, false);
            root.transform.localPosition = spec.Position;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + spec.Asset);
            if (asset == null)
                throw new System.InvalidOperationException($"Required practical asset is missing: {spec.Asset}");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            model.name = $"RealAsset - {asset.name}";
            model.transform.localRotation = Quaternion.Euler(spec.Rotation);
            model.transform.localScale = Vector3.one * spec.Scale;
            if (spec.Asset == "industrial_storage_cart_1k.fbx")
                NormalizeLargestDimension(model, IndustrialCartSize);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            Ground(model, site);
            if (spec.Asset == "clipboard_1k.fbx")
                MountClipboardOnInspectionCart(root.transform, model, site);
            FitCollider(root.transform);
            root.AddComponent<InteractiveHoverFeedback>();
            var highlight = root.AddComponent<ObjectiveHighlight>();
            highlight.color = new Color(0.184f, 0.490f, 1.0f);
            highlight.enabled = false;
            CreateDistanceMarker(root.transform, spec.Name);
            var action = root.AddComponent<SitePracticalAction>();
            var acceptanceRadius = AcceptanceRadius(spec.Asset);
            var target = PlacementTargetFactory.Create(site, root.name, spec.Destination, spec.Name,
                acceptanceRadius);
            action.Configure(controller, step, spec.Name, spec.Instruction, spec.Destination,
                spec.DestinationRotation, acceptanceRadius, target);
            AddTaskLabel(root.transform, step + 1, spec.Name);
        }

        static void CreateDistanceMarker(Transform parent, string title)
        {
            var markerObj = new GameObject("Objective Marker");
            markerObj.transform.SetParent(parent, false);
            markerObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            markerObj.AddComponent<ObjectiveDistanceMarker>();

            var textObj = new GameObject("Distance Label");
            textObj.transform.SetParent(markerObj.transform, false);
            var text = textObj.AddComponent<TextMesh>();
            text.text = "▼";
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.184f, 0.490f, 1.0f);
            text.fontStyle = FontStyle.Bold;

            markerObj.SetActive(false);
        }

        static void Ground(GameObject model, Transform site)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var item in renderers.Skip(1))
                bounds.Encapsulate(item.bounds);
            model.transform.position += Vector3.up * (site.position.y + 0.025f - bounds.min.y);
        }

        static void MountClipboardOnInspectionCart(Transform root, GameObject clipboard, Transform site)
        {
            var cartAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "industrial_storage_cart_1k.fbx");
            if (cartAsset == null)
                throw new System.InvalidOperationException("Required inspection cart asset is missing.");
            var cart = (GameObject)PrefabUtility.InstantiatePrefab(cartAsset, root);
            cart.name = "RealAsset - Mobile Inspection Cart";
            cart.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            NormalizeLargestDimension(cart, InspectionCartSize);
            foreach (var collider in cart.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            Ground(cart, site);

            var cartBounds = CombinedBounds(cart);
            var clipboardBounds = CombinedBounds(clipboard);
            clipboard.transform.position += Vector3.up * (cartBounds.max.y + 0.025f - clipboardBounds.min.y);
        }

        static Bounds CombinedBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var item in renderers.Skip(1))
                bounds.Encapsulate(item.bounds);
            return bounds;
        }

        static void NormalizeLargestDimension(GameObject model, float targetSize)
        {
            model.transform.localScale = Vector3.one;
            var size = CombinedBounds(model).size;
            var largestDimension = Mathf.Max(size.x, size.y, size.z);
            model.transform.localScale = Vector3.one * (targetSize / Mathf.Max(largestDimension, 0.001f));
        }

        static void FitCollider(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var item in renderers.Skip(1))
                bounds.Encapsulate(item.bounds);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = root.InverseTransformPoint(bounds.center);
            box.size = bounds.size + Vector3.one * 0.08f;
        }

        static void AddTaskLabel(Transform root, int step, string title)
        {
            var anchor = new GameObject("Practical Step Marker");
            anchor.transform.SetParent(root, false);
            var collider = root.GetComponent<BoxCollider>();
            var labelHeight = collider.center.y + collider.size.y * 0.5f + 0.22f;
            anchor.transform.localPosition = new Vector3(0f, labelHeight, 0f);
            anchor.AddComponent<BillboardLabel>();
            var clickSurface = anchor.AddComponent<BoxCollider>();
            clickSurface.size = new Vector3(1.4f, 0.5f, 0.12f);
            clickSurface.isTrigger = true;
            clickSurface.enabled = false;
            var label = SafetyScenePrimitives.Label($"STEP {step}\n{title.ToUpperInvariant()}", anchor.transform,
                Vector3.zero, 0.045f);
            label.color = new Color(1f, 0.78f, 0.18f);
        }

        static float AcceptanceRadius(string asset)
        {
            return asset == "concrete_road_barrier_1k.fbx" ||
                   asset == "industrial_storage_cart_1k.fbx"
                ? LargeAcceptanceRadius
                : PortableAcceptanceRadius;
        }
    }
}
