using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Editor
{
    internal static class SafetySiteFactory
    {
        static readonly Color Steel = new Color(0.36f, 0.42f, 0.48f);
        static readonly Color Concrete = new Color(0.38f, 0.4f, 0.43f);
        static readonly Color Timber = new Color(0.55f, 0.34f, 0.16f);
        static readonly Color SafetyYellow = new Color(0.95f, 0.68f, 0.08f);
        static readonly Color ExtinguisherRed = new Color(0.72f, 0.08f, 0.05f);
        static readonly Color NoGoRed = new Color(0.753f, 0.224f, 0.169f); // #C0392B

        public static Transform Construction(Vector3 origin)
        {
            var root = SiteRoot("Construction Site", origin, new Color(0.22f, 0.25f, 0.28f));
            AddLane(root, "Marked Access Route", new Vector3(2.5f, 0.02f, 1.35f),
                new Vector2(2.25f, 4.7f), new Color(0.26f, 0.28f, 0.3f), SafetyYellow);
            AddBay(root, "Materials Staging Zone", new Vector3(2.6f, 0.035f, -2.7f),
                new Vector2(2.5f, 1.9f), SafetyYellow);

            var openEdge = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Open Work Platform",
                root, new Vector3(-2.7f, 1.1f, 0.5f), new Vector3(3.2f, 0.2f, 2.2f), Concrete);
            SafetyScenePrimitives.Target(openEdge, TrainingSiteId.Construction, "fall-edge", true,
                "Unprotected elevated edge", "The work platform has an exposed edge without a guardrail.",
                "Install compliant top and mid rails or require approved fall arrest before access.");
            AddSupports(openEdge.transform, 1f);
            AddScaffoldTower(root, new Vector3(-2.7f, 0f, 1.8f));

            var crates = CrateStack(root, "Crates in Access Route", new Vector3(2.5f, 0f, 0.5f), 3);
            SafetyScenePrimitives.Target(crates, TrainingSiteId.Construction, "blocked-access", true,
                "Blocked access route", "Materials obstruct the marked route used to reach the work area.",
                "Move materials into the designated staging zone and restore the full route width.");

            var guarded = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Guarded Platform",
                root, new Vector3(-2.7f, 0.55f, -2.7f), new Vector3(3.2f, 0.18f, 1.5f), Concrete);
            AddGuardrail(guarded.transform);
            SafetyScenePrimitives.Target(guarded, TrainingSiteId.Construction, "guarded-edge", false,
                "Guarded work platform", "Top rails and mid rails control the fall exposure at this platform.",
                "Maintain the barrier and report damaged or missing sections.");

            var stored = CrateStack(root, "Stored Materials", new Vector3(2.6f, 0f, -2.7f), 2);
            SafetyScenePrimitives.Target(stored, TrainingSiteId.Construction, "stored-materials", false,
                "Materials in staging zone", "The materials are stable and outside the marked access route.",
                "Keep the stack stable and within the designated boundary.");
            CreateConstructionHandsOn(root);
            return root;
        }

        static void CreateConstructionHandsOn(Transform root)
        {
            var practical = root.gameObject.AddComponent<ConstructionHandsOnController>();
            var ppe = HandsOnProp(root, "PPE Kit - Helmet and Vest", new Vector3(-1.1f, 0.65f, 1.5f),
                new Vector3(0.9f, 0.55f, 0.7f), new Color(0.95f, 0.72f, 0.08f));
            AddPpeCaseDetail(ppe.transform);
            SafetyScenePrimitives.Label("PPE", ppe.transform, new Vector3(0f, 0.45f, -0.38f), 0.08f);
            ConfigureAction(ppe, practical, 0, "PPE check",
                "Pick up the kit and stage helmet plus vest at the entry check point.",
                new Vector3(0.15f, 0.65f, 2.05f), Vector3.zero, 0.85f);

            var barricade = HandsOnProp(root, "Barricade Gate", new Vector3(0.7f, 0.65f, -1.35f),
                new Vector3(1.6f, 0.18f, 0.18f), SafetyYellow);
            AddBarricadeDetail(barricade.transform);
            SafetyScenePrimitives.Label("BARRICADE", barricade.transform, new Vector3(0f, 0.22f, -0.18f), 0.06f);
            ConfigureAction(barricade, practical, 1, "Set exclusion barricade",
                "Place the barrier around the open-edge work zone.",
                new Vector3(-0.1f, 0.65f, -2.05f), Vector3.zero, 1.1f);

            var railKit = HandsOnProp(root, "Guardrail Kit", new Vector3(-2.3f, 0.35f, 2.6f),
                new Vector3(1.4f, 0.16f, 0.16f), new Color(0.94f, 0.94f, 0.88f));
            AddRailKitDetail(railKit.transform);
            SafetyScenePrimitives.Label("TOP RAIL", railKit.transform, new Vector3(0f, 0.22f, -0.18f), 0.06f);
            ConfigureAction(railKit, practical, 2, "Install guardrail kit",
                "Pick up the rail kit and fit top and mid rails to the platform edge.",
                new Vector3(-2.65f, 0.35f, -1.75f), Vector3.zero, 1.1f);

            var cart = HandsOnProp(root, "Material Cart", new Vector3(2.8f, 0.55f, 1.9f),
                new Vector3(1.0f, 0.55f, 0.72f), Timber);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Cart Handle", cart.transform,
                new Vector3(0f, 0.72f, 0.38f), new Vector3(0.8f, 0.12f, 0.12f), Steel);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Cart Wheel A", cart.transform,
                new Vector3(-0.55f, -0.55f, -0.35f), new Vector3(0.24f, 0.12f, 0.24f), Steel);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Cart Wheel B", cart.transform,
                new Vector3(0.55f, -0.55f, -0.35f), new Vector3(0.24f, 0.12f, 0.24f), Steel);
            SafetyScenePrimitives.Label("MOVE", cart.transform, new Vector3(0f, 0.55f, -0.38f), 0.07f);
            ConfigureAction(cart, practical, 3, "Move material cart",
                "Grab the cart and return it to the marked staging zone.",
                new Vector3(2.45f, 0.55f, -2.25f), new Vector3(0f, 180f, 0f), 1.1f);

            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Clipboard Staging Stand", root,
                new Vector3(0.4f, 0.32f, 3.05f), new Vector3(0.62f, 0.64f, 0.7f), Timber);
            var clipboard = HandsOnProp(root, "Inspection Clipboard", new Vector3(0.4f, 0.7f, 3.05f),
                new Vector3(0.55f, 0.08f, 0.78f), new Color(0.82f, 0.82f, 0.76f));
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Clipboard Clip", clipboard.transform,
                new Vector3(0f, 0.09f, 0.28f), new Vector3(0.25f, 0.04f, 0.12f), Steel);
            SafetyScenePrimitives.Label("FINAL CHECK", clipboard.transform, new Vector3(0f, 0.18f, -0.42f), 0.06f);
            ConfigureAction(clipboard, practical, 4, "Complete final walkdown",
                "Pick up the clipboard and deliver the completed check at the site control point.",
                new Vector3(0f, 0.7f, -0.25f), Vector3.zero, 0.85f);
        }

        static GameObject HandsOnProp(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var item = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, name, parent, position, scale, color);
            item.AddComponent<XRGrabInteractable>();
            return item;
        }

        static void ConfigureAction(GameObject item, ConstructionHandsOnController controller,
            int step, string title, string instruction, Vector3 destination,
            Vector3 destinationEulerAngles, float acceptanceRadius)
        {
            var action = item.AddComponent<ConstructionActionInteractable>();
            item.AddComponent<InteractiveHoverFeedback>();
            var highlight = item.AddComponent<ObjectiveHighlight>();
            highlight.color = new Color(0.184f, 0.490f, 1.0f); // #2F7DFF blue pulse
            highlight.enabled = false;
            CreateDistanceMarker(item.transform, title);
            var target = PlacementTargetFactory.Create(item.transform.parent, item.name, destination,
                title, acceptanceRadius);
            action.Configure(controller, step, title, instruction, destination,
                destinationEulerAngles, acceptanceRadius, target);
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
            text.color = new Color(0.184f, 0.490f, 1.0f); // #2F7DFF
            text.fontStyle = FontStyle.Bold;

            markerObj.SetActive(false);
        }

        static void AddPpeCaseDetail(Transform caseRoot)
        {
            SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Hard Hat", caseRoot,
                new Vector3(-0.2f, 0.36f, 0f), new Vector3(0.28f, 0.08f, 0.28f), new Color(0.98f, 0.78f, 0.08f));
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Vest Fold", caseRoot,
                new Vector3(0.24f, 0.36f, 0f), new Vector3(0.32f, 0.08f, 0.42f), new Color(0.96f, 0.44f, 0.05f));
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Case Latch", caseRoot,
                new Vector3(0f, -0.3f, -0.37f), new Vector3(0.18f, 0.08f, 0.04f), Steel);
        }

        static void AddBarricadeDetail(Transform gate)
        {
            foreach (var x in new[] { -0.72f, 0.72f })
            {
                SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Barricade Foot", gate,
                    new Vector3(x, -0.55f, 0f), new Vector3(0.22f, 0.08f, 0.22f), Steel);
                SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Barricade Post", gate,
                    new Vector3(x, -0.22f, 0f), new Vector3(0.08f, 0.35f, 0.08f), SafetyYellow);
            }
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Barricade Stripe", gate,
                new Vector3(0f, 0f, -0.03f), new Vector3(1.35f, 0.05f, 0.04f), new Color(0.88f, 0.16f, 0.06f));
        }

        static void AddRailKitDetail(Transform kit)
        {
            foreach (var x in new[] { -0.52f, 0.52f })
            {
                SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Rail Base Plate", kit,
                    new Vector3(x, -0.18f, 0f), new Vector3(0.16f, 0.04f, 0.16f), Steel);
                SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Rail Upright", kit,
                    new Vector3(x, 0.18f, 0f), new Vector3(0.06f, 0.35f, 0.06f), SafetyYellow);
            }
        }

        static void AddScaffoldTower(Transform root, Vector3 origin)
        {
            var scaffold = new GameObject("Scaffold Tower").transform;
            scaffold.SetParent(root, false);
            scaffold.localPosition = origin;
            foreach (var x in new[] { -1.25f, 1.25f })
                foreach (var z in new[] { -0.6f, 0.6f })
                {
                    SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Scaffold Upright", scaffold,
                        new Vector3(x, 1.3f, z), new Vector3(0.06f, 1.3f, 0.06f), Steel);
                    SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Base Jack", scaffold,
                        new Vector3(x, 0.06f, z), new Vector3(0.14f, 0.06f, 0.14f), SafetyYellow);
                }
            foreach (var y in new[] { 0.7f, 1.8f, 2.8f })
            {
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Scaffold Crossbar", scaffold,
                    new Vector3(0f, y, -0.6f), new Vector3(2.6f, 0.06f, 0.06f), Steel);
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Scaffold Deck", scaffold,
                    new Vector3(0f, y, 0f), new Vector3(2.5f, 0.06f, 1.3f), new Color(0.42f, 0.44f, 0.46f));
            }
            SafetyScenePrimitives.PillLabel("SCAFFOLD", scaffold, new Vector3(0.35f, 2.70f, -0.6f), 0.065f, "🏗", 1.4f, 0.38f);
        }

        public static Transform Warehouse(Vector3 origin)
        {
            var root = SiteRoot("Warehouse", origin, new Color(0.18f, 0.28f, 0.38f));
            AddLane(root, "Pedestrian Aisle", new Vector3(-2.4f, 0.02f, 0.55f),
                new Vector2(2.2f, 5.2f), new Color(0.12f, 0.34f, 0.28f), Color.white);
            AddLane(root, "Vehicle Lane", new Vector3(2.4f, 0.02f, 0.75f),
                new Vector2(2.4f, 4.8f), new Color(0.24f, 0.27f, 0.3f), SafetyYellow);
            AddBay(root, "Cargo Staging Bay", new Vector3(2.4f, 0.04f, -2.6f),
                new Vector2(2.8f, 2f), Color.white);
            AddBay(root, "Maintenance Boundary", new Vector3(-2.5f, 0.04f, -2.6f),
                new Vector2(2.4f, 2f), SafetyYellow);

            var spill = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Floor Spill",
                root, new Vector3(-2.4f, 0.03f, 0.4f), new Vector3(1.2f, 0.03f, 0.75f), new Color(0.08f, 0.22f, 0.32f));
            SafetyScenePrimitives.Target(spill, TrainingSiteId.Warehouse, "floor-spill", true,
                "Uncontrolled floor spill", "Liquid is present in an active pedestrian aisle without isolation.",
                "Isolate the area, use the spill procedure, and reopen only after the surface is safe.");

            var lanePallet = Pallet(root, "Pallet in Vehicle Lane", new Vector3(2.4f, 0f, 0.6f));
            SafetyScenePrimitives.Target(lanePallet, TrainingSiteId.Warehouse, "vehicle-route", true,
                "Obstruction in vehicle lane", "Cargo extends into the powered industrial truck route.",
                "Stop traffic and relocate the pallet to a designated staging or rack position.");

            var cone = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Maintenance Cone",
                root, new Vector3(-2.5f, 0.45f, -2.6f), new Vector3(0.45f, 0.45f, 0.45f), SafetyYellow);
            SafetyScenePrimitives.Target(cone, TrainingSiteId.Warehouse, "marked-maintenance", false,
                "Marked maintenance area", "The temporary work area is visibly isolated from normal travel.",
                "Keep the boundary intact until maintenance releases the area.");

            var rackCargo = Pallet(root, "Cargo in Staging Bay", new Vector3(2.4f, 0f, -2.6f));
            SafetyScenePrimitives.Target(rackCargo, TrainingSiteId.Warehouse, "racked-cargo", false,
                "Cargo in staging bay", "The pallet is stable and fully contained inside the marked bay.",
                "Maintain aisle clearance and verify the load remains stable.");
            return root;
        }

        public static Transform FireResponse(Vector3 origin)
        {
            var root = SiteRoot("Fire Response", origin, new Color(0.34f, 0.2f, 0.18f));
            AddLane(root, "Emergency Egress Lane", new Vector3(2.5f, 0.02f, 0.2f),
                new Vector2(2.5f, 6.2f), new Color(0.08f, 0.36f, 0.18f), Color.white);

            var blockedExtinguisher = Extinguisher(root, "Blocked Extinguisher", new Vector3(-2.6f, 0f, 0.9f));
            CrateStack(root, "Obstruction", new Vector3(-2.6f, 0f, 0.1f), 2);
            SafetyScenePrimitives.Target(blockedExtinguisher, TrainingSiteId.FireResponse, "blocked-extinguisher", true,
                "Blocked fire extinguisher", "Stored material prevents immediate access to the extinguisher.",
                "Remove the obstruction and preserve the required clear access area.");

            var exitBlock = Pallet(root, "Pallet at Emergency Exit", new Vector3(2.6f, 0f, 0.7f));
            Marking(root, "Exit Sign Backplate", new Vector3(3.45f, 1.7f, 4.05f),
                new Vector3(1.55f, 0.55f, 0.08f), new Color(0.03f, 0.38f, 0.18f));
            SafetyScenePrimitives.Label("EXIT", root, new Vector3(3.45f, 1.7f, 3.95f), 0.22f);
            SafetyScenePrimitives.Target(exitBlock, TrainingSiteId.FireResponse, "blocked-exit", true,
                "Blocked emergency exit", "A pallet obstructs the direct egress path to the marked exit.",
                "Remove the pallet immediately and keep the egress route continuously clear.");

            var clearExtinguisher = Extinguisher(root, "Accessible Extinguisher", new Vector3(-2.5f, 0f, -2.6f));
            SafetyScenePrimitives.Target(clearExtinguisher, TrainingSiteId.FireResponse, "clear-extinguisher", false,
                "Accessible fire extinguisher", "The extinguisher is visible and has clear approach space.",
                "Continue routine inspection and preserve clear access.");

            var clearRoute = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Clear Egress Route",
                root, new Vector3(2.5f, 0.02f, -2.6f), new Vector3(2.4f, 0.04f, 1.2f), new Color(0.12f, 0.45f, 0.2f));
            SafetyScenePrimitives.Target(clearRoute, TrainingSiteId.FireResponse, "clear-egress", false,
                "Clear egress route", "The marked evacuation route is unobstructed and easy to follow.",
                "Keep temporary storage and equipment outside the route.");
            return root;
        }

        public static Transform ChemicalProcessing(Vector3 origin)
        {
            var root = SiteRoot("Chemical Processing", origin, new Color(0.12f, 0.28f, 0.25f));
            AddLane(root, "Chemical Transfer Lane", new Vector3(2.5f, 0.02f, 0.7f),
                new Vector2(2.4f, 5.5f), new Color(0.14f, 0.3f, 0.25f), Color.white);
            AddBay(root, "Compatible Storage Bay", new Vector3(2.5f, 0.04f, -2.6f),
                new Vector2(2.8f, 2f), new Color(0.35f, 0.8f, 0.56f));

            var leakingDrum = Drum(root, "Leaking Solvent Drum", new Vector3(-2.5f, 0f, 0.6f),
                new Color(0.14f, 0.22f, 0.3f), new Color(0.95f, 0.68f, 0.08f));
            SafetyScenePrimitives.Target(leakingDrum, TrainingSiteId.ChemicalProcessing, "chemical-leak", true,
                "Leaking solvent drum", "A drum is visibly leaking into the transfer lane.",
                "Stop the transfer, isolate the area, and follow the site spill response procedure.");

            var unlabeledDrum = Drum(root, "Unlabeled Chemical Drum", new Vector3(-2.5f, 0f, -1.8f),
                new Color(0.68f, 0.68f, 0.7f), new Color(0.88f, 0.2f, 0.12f));
            SafetyScenePrimitives.Target(unlabeledDrum, TrainingSiteId.ChemicalProcessing, "unlabeled-drum", true,
                "Unlabeled chemical container", "The container lacks a legible identity and hazard label.",
                "Quarantine the container and restore the approved label before handling.");

            var labeledDrum = Drum(root, "Labeled Compatible Drum", new Vector3(2.5f, 0f, -2.6f),
                new Color(0.18f, 0.4f, 0.55f), new Color(0.2f, 0.8f, 0.42f));
            SafetyScenePrimitives.Target(labeledDrum, TrainingSiteId.ChemicalProcessing, "labeled-drum", false,
                "Labeled compatible container", "The container is labeled and inside the compatible storage bay.",
                "Keep the label legible and maintain segregation by compatibility.");

            var eyewash = Eyewash(root, "Emergency Eyewash", new Vector3(2.5f, 0f, 0.1f));
            SafetyScenePrimitives.Target(eyewash, TrainingSiteId.ChemicalProcessing, "clear-eyewash", false,
                "Accessible emergency eyewash", "The eyewash station has a clear approach from the transfer lane.",
                "Keep the approach clear and test the station on the site schedule.");
            return root;
        }

        public static Transform ElectricalMaintenance(Vector3 origin)
        {
            var root = SiteRoot("Electrical Maintenance", origin, new Color(0.16f, 0.2f, 0.34f));
            AddLane(root, "Protected Cable Crossing", new Vector3(2.5f, 0.02f, 0.9f),
                new Vector2(2.3f, 5.8f), new Color(0.18f, 0.22f, 0.34f), new Color(0.98f, 0.76f, 0.12f));
            AddBay(root, "Lockout Staging", new Vector3(2.5f, 0.04f, -2.6f),
                new Vector2(2.8f, 1.9f), new Color(0.62f, 0.7f, 0.95f));

            var openPanel = ElectricalPanel(root, "Open Electrical Panel", new Vector3(-2.5f, 0f, 0.8f), false);
            SafetyScenePrimitives.Target(openPanel, TrainingSiteId.ElectricalMaintenance, "open-panel", true,
                "Open energized panel", "The panel is open without a visible lockout boundary or verified isolation.",
                "Stop work, isolate the source, apply lockout/tagout, and verify zero energy.");

            var looseCable = CableBundle(root, "Loose Cable Crossing", new Vector3(-2.5f, 0f, -1.8f), false);
            SafetyScenePrimitives.Target(looseCable, TrainingSiteId.ElectricalMaintenance, "loose-cable", true,
                "Unprotected cable crossing", "A temporary cable crosses the walking route without a ramp or overhead support.",
                "Reroute the cable or install a rated cable protector before access resumes.");

            var lockedPanel = ElectricalPanel(root, "Locked Isolated Panel", new Vector3(2.5f, 0f, -2.6f), true);
            SafetyScenePrimitives.Target(lockedPanel, TrainingSiteId.ElectricalMaintenance, "locked-panel", false,
                "Locked and tagged panel", "The panel is closed with a visible lockout tag inside the staging boundary.",
                "Preserve the isolation and verify the tag remains legible.");

            var protectedCable = CableBundle(root, "Protected Cable Ramp", new Vector3(2.5f, 0f, 0.6f), true);
            SafetyScenePrimitives.Target(protectedCable, TrainingSiteId.ElectricalMaintenance, "protected-cable", false,
                "Protected cable crossing", "The cable crossing is covered by a high-visibility floor protector.",
                "Inspect the ramp before use and keep its edges seated.");
            return root;
        }

        static Transform SiteRoot(string title, Vector3 origin, Color color)
        {
            var root = new GameObject(title).transform;
            root.position = origin;
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Floor", root,
                new Vector3(0f, -0.15f, 0f), new Vector3(10f, 0.3f, 9f), color);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Floor Safety Border", root,
                new Vector3(0f, 0.03f, -4.2f), new Vector3(9.4f, 0.04f, 0.08f), SafetyYellow);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Back Wall", root,
                new Vector3(0f, 1.5f, 4.25f), new Vector3(10f, 3f, 0.25f), color * 1.35f);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Worklight Header", root,
                new Vector3(0f, 3.1f, 3.95f), new Vector3(7.4f, 0.08f, 0.08f), new Color(0.92f, 0.94f, 0.85f));
            SafetyScenePrimitives.PillLabel(title, root, new Vector3(0f, 2.7f, 3.92f), 0.075f, "⛑", 2.6f, 0.45f);
            return root;
        }

        static GameObject Drum(Transform parent, string name, Vector3 position, Color body, Color band)
        {
            var drum = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, name, parent,
                position + new Vector3(0f, 0.65f, 0f), new Vector3(0.72f, 0.65f, 0.72f), body);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, "Drum Top", drum.transform,
                new Vector3(0f, 0.94f, 0f), new Vector3(0.68f, 0.05f, 0.68f), body * 1.12f);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Hazard Band", drum.transform,
                new Vector3(0f, 0.15f, 0f), new Vector3(0.75f, 0.08f, 0.75f), band);
            SafetyScenePrimitives.Label("CHEM", drum.transform,
                new Vector3(0f, 0.65f, -0.38f), 0.08f);
            return drum;
        }

        static GameObject Eyewash(Transform parent, string name, Vector3 position)
        {
            var station = AssemblyRoot(parent, name, position, new Vector3(1.4f, 2.2f, 1.1f),
                new Vector3(0f, 1.05f, 0f));
            var green = new Color(0.08f, 0.48f, 0.24f);
            var stainless = new Color(0.58f, 0.64f, 0.65f);
            DecorativePrimitive(PrimitiveType.Cylinder, "Pedestal", station.transform,
                new Vector3(0f, 0.58f, 0f), new Vector3(0.16f, 0.58f, 0.16f), stainless, 0.35f, 0.6f);
            DecorativePrimitive(PrimitiveType.Cylinder, "Floor Flange", station.transform,
                new Vector3(0f, 0.07f, 0f), new Vector3(0.34f, 0.07f, 0.34f), stainless * 0.78f,
                0.35f, 0.55f);
            DecorativePrimitive(PrimitiveType.Cylinder, "Drain Collar", station.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(0.24f, 0.05f, 0.24f), stainless * 0.82f,
                0.35f, 0.55f);
            SafetyEquipmentMeshFactory.CreateEyewashBowl(station.transform, new Vector3(0f, 1.16f, 0f), green);
            foreach (var x in new[] { -0.22f, 0.22f })
            {
                DecorativePrimitive(PrimitiveType.Cylinder, "Nozzle Stem", station.transform,
                    new Vector3(x, 1.28f, 0f), new Vector3(0.045f, 0.11f, 0.045f), stainless, 0.35f, 0.62f);
                DecorativePrimitive(PrimitiveType.Sphere, "Nozzle Head", station.transform,
                    new Vector3(x, 1.4f, -0.03f), new Vector3(0.12f, 0.08f, 0.12f), green * 1.2f);
            }
            DecorativePrimitive(PrimitiveType.Cube, "Activation Paddle", station.transform,
                new Vector3(0.45f, 0.72f, -0.02f), new Vector3(0.46f, 0.08f, 0.14f), green * 1.2f);
            DecorativePrimitive(PrimitiveType.Cube, "Eyewash Sign", station.transform,
                new Vector3(0f, 1.92f, 0.28f), new Vector3(1.15f, 0.62f, 0.06f), green);
            SafetyScenePrimitives.Label("EMERGENCY\nEYEWASH", station.transform,
                new Vector3(0f, 1.92f, 0.2f), 0.065f);
            return station;
        }

        static GameObject ElectricalPanel(Transform parent, string name, Vector3 position, bool locked)
        {
            var panel = AssemblyRoot(parent, name, position, new Vector3(1.7f, 2.55f, 1.1f),
                new Vector3(0f, 1.25f, 0f));
            var cabinet = locked ? new Color(0.31f, 0.36f, 0.43f) : new Color(0.16f, 0.18f, 0.2f);
            SafetyEquipmentMeshFactory.CreateElectricalCabinetShell(panel.transform,
                new Vector3(0f, 1.25f, 0.08f), cabinet);
            DecorativePrimitive(PrimitiveType.Cube, "Panel Plinth", panel.transform,
                new Vector3(0f, 0.08f, 0f), new Vector3(1.48f, 0.16f, 0.52f), cabinet * 0.72f, 0.72f, 0.34f);
            DecorativePrimitive(PrimitiveType.Cube, "Panel Rain Cap", panel.transform,
                new Vector3(0f, 2.43f, 0f), new Vector3(1.42f, 0.1f, 0.48f), cabinet * 1.18f,
                0.72f, 0.34f);

            if (locked)
            {
                DecorativePrimitive(PrimitiveType.Cube, "Closed Panel Door", panel.transform,
                    new Vector3(0f, 1.25f, -0.19f), new Vector3(1.12f, 2.08f, 0.06f), cabinet * 1.28f);
                DecorativePrimitive(PrimitiveType.Cube, "Lock Body", panel.transform,
                    new Vector3(0.34f, 0.58f, -0.29f), new Vector3(0.18f, 0.24f, 0.12f), SafetyYellow);
                DecorativePrimitive(PrimitiveType.Cylinder, "Lock Shackle", panel.transform,
                    new Vector3(0.34f, 0.78f, -0.27f), new Vector3(0.08f, 0.13f, 0.08f), Steel);
            }
            else
            {
                DecorativePrimitive(PrimitiveType.Cube, "Panel Interior", panel.transform,
                    new Vector3(0f, 1.25f, -0.18f), new Vector3(1.06f, 1.96f, 0.06f), new Color(0.035f, 0.04f, 0.045f));
                var door = DecorativePrimitive(PrimitiveType.Cube, "Open Panel Door", panel.transform,
                    new Vector3(-0.72f, 1.25f, -0.62f), new Vector3(1.06f, 2.08f, 0.055f), cabinet * 1.22f);
                door.transform.localRotation = Quaternion.Euler(0f, 78f, 0f);
                foreach (var x in new[] { -0.3f, 0f, 0.3f })
                    DecorativePrimitive(PrimitiveType.Cube, "Insulated Bus Bar", panel.transform,
                        new Vector3(x, 1.5f, -0.24f), new Vector3(0.075f, 1.1f, 0.04f),
                        x < 0f ? new Color(0.72f, 0.18f, 0.12f) : x > 0f ? new Color(0.12f, 0.34f, 0.72f) : SafetyYellow);
                foreach (var y in new[] { 0.72f, 1.05f })
                    DecorativePrimitive(PrimitiveType.Cube, "Breaker Row", panel.transform,
                        new Vector3(0f, y, -0.29f), new Vector3(0.78f, 0.18f, 0.08f), Steel * 0.62f);
            }

            foreach (var y in new[] { 0.65f, 1.85f })
                DecorativePrimitive(PrimitiveType.Cylinder, "Door Hinge", panel.transform,
                    new Vector3(-0.56f, y, -0.24f), new Vector3(0.05f, 0.12f, 0.05f), Steel);
            DecorativePrimitive(PrimitiveType.Cube, "Door Handle", panel.transform,
                new Vector3(0.42f, 1.25f, -0.29f), new Vector3(0.08f, 0.34f, 0.1f), Steel * 0.7f);
            DecorativePrimitive(PrimitiveType.Cube, locked ? "Lockout Tag" : "Danger Placard", panel.transform,
                new Vector3(0f, 1.92f, -0.3f), new Vector3(0.58f, 0.34f, 0.05f),
                locked ? SafetyYellow : new Color(0.85f, 0.12f, 0.08f));
            SafetyScenePrimitives.Label(locked ? "LOCKED OUT" : "DANGER\nLIVE PARTS", panel.transform,
                new Vector3(0f, 1.92f, -0.36f), 0.055f);
            return panel;
        }

        static GameObject CableBundle(Transform parent, string name, Vector3 position, bool protectedRoute)
        {
            var cable = AssemblyRoot(parent, name, position, new Vector3(4.8f, 0.45f, 0.95f),
                new Vector3(0f, 0.18f, 0f));
            var cableColors = new[]
            {
                new Color(0.025f, 0.03f, 0.035f),
                new Color(0.42f, 0.055f, 0.035f),
                new Color(0.035f, 0.16f, 0.42f)
            };
            for (var index = 0; index < cableColors.Length; index++)
            {
                var conduit = DecorativePrimitive(PrimitiveType.Cylinder, "Flexible Power Cable", cable.transform,
                    new Vector3(0f, 0.09f, (index - 1) * 0.16f), new Vector3(0.038f, 2.15f, 0.038f),
                    cableColors[index]);
                conduit.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            if (protectedRoute)
            {
                SafetyEquipmentMeshFactory.CreateCableProtectorShell(cable.transform, Vector3.zero,
                    new Color(0.12f, 0.13f, 0.14f));
                foreach (var z in new[] { -0.34f, 0.34f })
                    DecorativePrimitive(PrimitiveType.Cube, "High Visibility Edge", cable.transform,
                        new Vector3(0f, 0.2f, z), new Vector3(4.72f, 0.12f, 0.08f), SafetyYellow);
                foreach (var x in new[] { -1.5f, 0f, 1.5f })
                    DecorativePrimitive(PrimitiveType.Cube, "Protector Grip Rib", cable.transform,
                        new Vector3(x, 0.3f, 0f), new Vector3(0.08f, 0.045f, 0.62f), Steel * 0.55f);
            }
            else
            {
                foreach (var x in new[] { -1.25f, 0f, 1.25f })
                    DecorativePrimitive(PrimitiveType.Cube, "Loose Cable Tie", cable.transform,
                        new Vector3(x, 0.11f, 0f), new Vector3(0.1f, 0.045f, 0.48f), SafetyYellow);
            }
            return cable;
        }

        static GameObject AssemblyRoot(Transform parent, string name, Vector3 position, Vector3 colliderSize,
            Vector3 colliderCenter)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            collider.center = colliderCenter;
            return root;
        }

        static GameObject DecorativePrimitive(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Color color, float metallic = 0f, float smoothness = 0.28f)
        {
            var item = SafetyScenePrimitives.Primitive(type, name, parent, position, scale, color);
            item.GetComponent<Renderer>().sharedMaterial =
                SafetyEquipmentMeshFactory.Material(color, metallic, smoothness);
            Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        static GameObject CrateStack(Transform parent, string name, Vector3 position, int count)
        {
            var root = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, name, parent,
                position + new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.8f, 1.1f), Timber);
            AddCrateDetail(root.transform, new Vector3(1.5f, 0.8f, 1.1f));
            for (var index = 1; index < count; index++)
            {
                var crate = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, $"Crate {index + 1}", root.transform,
                    new Vector3(0f, index, 0f), Vector3.one, Timber * (1f - index * 0.06f));
                AddCrateDetail(crate.transform, Vector3.one);
            }
            return root;
        }

        static void AddCrateDetail(Transform crate, Vector3 size)
        {
            var wood = new Color(0.7f, 0.45f, 0.22f);
            foreach (var x in new[] { -0.44f, 0.44f })
                foreach (var z in new[] { -0.38f, 0.38f })
                    SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Crate Corner Batten", crate,
                        new Vector3(x * size.x, 0f, z * size.z),
                        new Vector3(0.08f * size.x, 0.95f * size.y, 0.08f * size.z), wood);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Crate Top Slat", crate,
                new Vector3(0f, 0.48f * size.y, 0f), new Vector3(0.9f * size.x, 0.05f * size.y, 0.85f * size.z), wood);
        }

        static GameObject Pallet(Transform parent, string name, Vector3 position)
        {
            var pallet = SafetyScenePrimitives.Primitive(PrimitiveType.Cube, name, parent,
                position + new Vector3(0f, 0.12f, 0f), new Vector3(2.1f, 0.24f, 1.4f), Timber);
            foreach (var x in new[] { -0.75f, 0f, 0.75f })
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Pallet Slat", pallet.transform,
                    new Vector3(x, 0.14f, 0f), new Vector3(0.18f, 0.08f, 1.25f), Timber * 1.15f);
            foreach (var z in new[] { -0.46f, 0.46f })
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Pallet Runner", pallet.transform,
                    new Vector3(0f, -0.12f, z), new Vector3(1.85f, 0.12f, 0.14f), Timber * 0.85f);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Load", pallet.transform,
                new Vector3(0f, 2.1f, 0f), new Vector3(0.8f, 3.4f, 0.75f), Steel);
            return pallet;
        }

        static GameObject Extinguisher(Transform parent, string name, Vector3 position)
        {
            var body = SafetyScenePrimitives.Primitive(PrimitiveType.Cylinder, name, parent,
                position + new Vector3(0f, 0.65f, 0f), new Vector3(0.3f, 0.65f, 0.3f), ExtinguisherRed);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Handle", body.transform,
                new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.15f, 0.18f), Steel);
            return body;
        }

        static void AddSupports(Transform platform, float height)
        {
            foreach (var x in new[] { -0.42f, 0.42f })
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Support", platform,
                    new Vector3(x, -height * 2.7f, 0f), new Vector3(0.12f, height * 5f, 0.12f), Steel);
        }

        static void AddGuardrail(Transform platform)
        {
            foreach (var x in new[] { -0.47f, 0.47f })
                SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Rail Post", platform,
                    new Vector3(x, 3f, -0.45f), new Vector3(0.05f, 6f, 0.08f), SafetyYellow);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Top Rail", platform,
                new Vector3(0f, 5.5f, -0.45f), new Vector3(1f, 0.25f, 0.08f), SafetyYellow);
            SafetyScenePrimitives.Primitive(PrimitiveType.Cube, "Mid Rail", platform,
                new Vector3(0f, 3f, -0.45f), new Vector3(1f, 0.2f, 0.08f), SafetyYellow);
        }

        static void AddLane(
            Transform parent,
            string name,
            Vector3 center,
            Vector2 size,
            Color fill,
            Color edge)
        {
            Marking(parent, name, center, new Vector3(size.x, 0.025f, size.y), fill);
            var edgeOffset = size.x * 0.5f;
            Marking(parent, $"{name} Left Edge", center + Vector3.left * edgeOffset,
                new Vector3(0.055f, 0.035f, size.y), edge);
            Marking(parent, $"{name} Right Edge", center + Vector3.right * edgeOffset,
                new Vector3(0.055f, 0.035f, size.y), edge);
        }

        static void AddBay(Transform parent, string name, Vector3 center, Vector2 size, Color edge)
        {
            var halfX = size.x * 0.5f;
            var halfZ = size.y * 0.5f;
            Marking(parent, $"{name} Front", center + Vector3.back * halfZ,
                new Vector3(size.x, 0.035f, 0.055f), edge);
            Marking(parent, $"{name} Back", center + Vector3.forward * halfZ,
                new Vector3(size.x, 0.035f, 0.055f), edge);
            Marking(parent, $"{name} Left", center + Vector3.left * halfX,
                new Vector3(0.055f, 0.035f, size.y), edge);
            Marking(parent, $"{name} Right", center + Vector3.right * halfX,
                new Vector3(0.055f, 0.035f, size.y), edge);
        }

        static GameObject Marking(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            var item = SafetyScenePrimitives.Primitive(
                PrimitiveType.Cube, name, parent, position, scale, color);
            Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }
    }
}
