using System;
using System.Collections.Generic;
using System.Linq;
using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    [DefaultExecutionOrder(-950)]
    public sealed class SafetyTaskManager : MonoBehaviour
    {
        public static SafetyTaskManager Instance { get; private set; }

        readonly Dictionary<TrainingSiteId, List<SafetyTaskDefinition>> siteTasks =
            new Dictionary<TrainingSiteId, List<SafetyTaskDefinition>>();

        readonly Dictionary<TrainingSiteId, ModuleAssessmentSummary> siteAssessments =
            new Dictionary<TrainingSiteId, ModuleAssessmentSummary>();

        public event Action<SafetyTaskDefinition> TaskCompleted;
        public event Action<TrainingSiteId, ModuleAssessmentSummary> ModuleAssessmentCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeTaskCatalog();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (TrainingCoordinator.Instance != null)
            {
                TrainingCoordinator.Instance.InspectionCompleted += OnInspectionCompleted;
            }
        }

        public void InitializeTaskCatalog()
        {
            siteTasks.Clear();

            // Module 1: Construction — Fall Protection
            siteTasks[TrainingSiteId.Construction] = new List<SafetyTaskDefinition>
            {
                new SafetyTaskDefinition(
                    "const_haz_01",
                    "Inspect Open Elevated Edge",
                    "Identify the unprotected work platform edge lacking top and mid-rail fall protection.",
                    TrainingSiteId.Construction, 0, SafetyTaskType.HazardIdentification, "fall-edge",
                    "Look for the elevated platform above 6 feet without guardrails.", 100, 25),

                new SafetyTaskDefinition(
                    "const_haz_02",
                    "Identify Access Route Obstruction",
                    "Identify construction crates blocking the marked walkway.",
                    TrainingSiteId.Construction, 1, SafetyTaskType.HazardIdentification, "blocked-access",
                    "Examine the yellow-striped walking route leading toward the work platform.", 100, 25),

                new SafetyTaskDefinition(
                    "const_ppe_03",
                    "Stage Fall Arrest PPE Kit",
                    "Pick up the PPE kit containing hard hat and harness and deliver it to the entry checkpoint.",
                    TrainingSiteId.Construction, 2, SafetyTaskType.PpeVerification, "step_0",
                    "The yellow PPE kit is resting on the staging bench.", 20, 0),

                new SafetyTaskDefinition(
                    "const_act_04",
                    "Erect Exclusion Barricade Gate",
                    "Position the portable barricade gate to isolate the fall hazard area.",
                    TrainingSiteId.Construction, 3, SafetyTaskType.PhysicalControl, "step_1",
                    "Move the yellow and black barricade to the open-edge exclusion boundary.", 20, 0),

                new SafetyTaskDefinition(
                    "const_act_05",
                    "Install Modular Guardrail System",
                    "Transport the guardrail kit and install top/mid rails on the open platform edge.",
                    TrainingSiteId.Construction, 4, SafetyTaskType.PhysicalControl, "step_2",
                    "Fit the rail uprights and crossbars to the open concrete edge.", 20, 0),

                new SafetyTaskDefinition(
                    "const_act_06",
                    "Relocate Material Cart to Staging Bay",
                    "Move the heavy materials cart out of the walkway into the designated staging zone.",
                    TrainingSiteId.Construction, 5, SafetyTaskType.PhysicalControl, "step_3",
                    "Roll the cart into the yellow-marked staging bay at the rear.", 20, 0),

                new SafetyTaskDefinition(
                    "const_sign_07",
                    "Complete Fall Protection Walkdown Sign-Off",
                    "Pick up the clipboard and deliver the verified safety checklist to the control point.",
                    TrainingSiteId.Construction, 6, SafetyTaskType.VerificationSignOff, "step_4",
                    "Grab the clipboard from the stand and bring it to the supervisor control station.", 20, 0)
            };

            // Module 2: Warehouse — Vehicle Routes & Traffic Management
            siteTasks[TrainingSiteId.Warehouse] = new List<SafetyTaskDefinition>
            {
                new SafetyTaskDefinition(
                    "wh_haz_01",
                    "Detect Pedestrian Slip Hazard",
                    "Locate and report the liquid spill on the pedestrian aisle floor.",
                    TrainingSiteId.Warehouse, 0, SafetyTaskType.HazardIdentification, "floor-spill",
                    "Scan the green pedestrian aisle for slippery liquid pools.", 100, 25),

                new SafetyTaskDefinition(
                    "wh_haz_02",
                    "Detect Vehicle Lane Obstruction",
                    "Identify the pallet protruding into the powered industrial truck route.",
                    TrainingSiteId.Warehouse, 1, SafetyTaskType.HazardIdentification, "vehicle-route",
                    "Check the yellow-lined forklift travel lane for out-of-boundary freight.", 100, 25),

                new SafetyTaskDefinition(
                    "wh_act_03",
                    "Deploy Spill Response Kit",
                    "Place the mobile spill response kit beside the slippery surface to isolate the area.",
                    TrainingSiteId.Warehouse, 2, SafetyTaskType.PhysicalControl, "step_0",
                    "Grab the metal spill kit and drop it at the isolated pedestrian aisle.", 25, 0),

                new SafetyTaskDefinition(
                    "wh_act_04",
                    "Set Route Separation Barrier",
                    "Erect the concrete safety barrier between the forklift route and the walking aisle.",
                    TrainingSiteId.Warehouse, 3, SafetyTaskType.PhysicalControl, "step_1",
                    "Position the road barrier directly along the demarcation boundary.", 25, 0),

                new SafetyTaskDefinition(
                    "wh_act_05",
                    "Relocate Aisle Freight to Staging Bay",
                    "Move the industrial storage cart into the designated cargo staging bay.",
                    TrainingSiteId.Warehouse, 4, SafetyTaskType.PhysicalControl, "step_2",
                    "Push or carry the storage cart into the marked staging rectangle.", 25, 0)
            };

            // Module 3: Fire Response — Extinguisher & Evacuation
            siteTasks[TrainingSiteId.FireResponse] = new List<SafetyTaskDefinition>
            {
                new SafetyTaskDefinition(
                    "fire_haz_01",
                    "Identify Obstructed Fire Extinguisher",
                    "Locate the fire extinguisher point blocked by stored wooden crates.",
                    TrainingSiteId.FireResponse, 0, SafetyTaskType.HazardIdentification, "blocked-extinguisher",
                    "Fire points must have 36 inches of immediate clear approach clearance.", 100, 25),

                new SafetyTaskDefinition(
                    "fire_haz_02",
                    "Identify Blocked Emergency Exit",
                    "Locate the cargo pallet obstructing the illuminated emergency egress door.",
                    TrainingSiteId.FireResponse, 1, SafetyTaskType.HazardIdentification, "blocked-exit",
                    "Look toward the green EXIT sign for cargo blocking the doorway.", 100, 25),

                new SafetyTaskDefinition(
                    "fire_act_03",
                    "Remove Fire-Point Obstruction",
                    "Relocate stored crates outside the fire extinguisher approach zone.",
                    TrainingSiteId.FireResponse, 2, SafetyTaskType.PhysicalControl, "step_0",
                    "Move the military crate to the side perimeter away from the extinguisher.", 25, 0),

                new SafetyTaskDefinition(
                    "fire_act_04",
                    "Ready Fire Response Equipment Kit",
                    "Stage the inspection response kit adjacent to the accessible extinguisher point.",
                    TrainingSiteId.FireResponse, 3, SafetyTaskType.PhysicalControl, "step_1",
                    "Place the metal response kit next to the clear extinguisher post.", 25, 0),

                new SafetyTaskDefinition(
                    "fire_act_05",
                    "Verify Emergency Egress Walkdown",
                    "Complete the egress walkdown and sign off the exit verification clipboard.",
                    TrainingSiteId.FireResponse, 4, SafetyTaskType.VerificationSignOff, "step_2",
                    "Carry the clipboard down the marked egress route to confirm clearance.", 25, 0)
            };

            // Module 4: Chemical Safety — Label & Segregation
            siteTasks[TrainingSiteId.ChemicalProcessing] = new List<SafetyTaskDefinition>
            {
                new SafetyTaskDefinition(
                    "chem_haz_01",
                    "Identify Leaking Solvent Drum",
                    "Locate the chemical drum leaking liquid outside secondary containment.",
                    TrainingSiteId.ChemicalProcessing, 0, SafetyTaskType.HazardIdentification, "chemical-leak",
                    "Inspect the dark drum on the transfer aisle for pooling liquid.", 100, 25),

                new SafetyTaskDefinition(
                    "chem_haz_02",
                    "Identify Unlabeled Chemical Container",
                    "Detect the chemical container that lacks compliant GHS hazard labeling.",
                    TrainingSiteId.ChemicalProcessing, 1, SafetyTaskType.HazardIdentification, "unlabeled-drum",
                    "All chemical containers must clearly display identity and GHS pictograms.", 100, 25),

                new SafetyTaskDefinition(
                    "chem_act_03",
                    "Deploy Spill Containment Berm",
                    "Place the containment kit outside the chemical splash perimeter.",
                    TrainingSiteId.ChemicalProcessing, 2, SafetyTaskType.PhysicalControl, "step_0",
                    "Deploy the containment kit directly beside the leaking drum.", 25, 0),

                new SafetyTaskDefinition(
                    "chem_act_04",
                    "Attach Verified GHS Safety Placard",
                    "Cross-reference the Safety Data Sheet (SDS) and attach the proper hazard label.",
                    TrainingSiteId.ChemicalProcessing, 3, SafetyTaskType.PhysicalControl, "step_1",
                    "Take the verified clipboard record to the compatible drum staging area.", 25, 0),

                new SafetyTaskDefinition(
                    "chem_act_05",
                    "Perform Weekly Eyewash Inspection",
                    "Document the functional test and clear approach to the emergency eyewash station.",
                    TrainingSiteId.ChemicalProcessing, 4, SafetyTaskType.VerificationSignOff, "step_2",
                    "Bring the testing kit to the emergency eyewash station to record the flow test.", 25, 0)
            };

            // Module 5: Electrical Safety — Lockout/Tagout (LOTO)
            siteTasks[TrainingSiteId.ElectricalMaintenance] = new List<SafetyTaskDefinition>
            {
                new SafetyTaskDefinition(
                    "elec_haz_01",
                    "Detect Open Energized Switchgear",
                    "Identify the open electrical cabinet with exposed energized busbars.",
                    TrainingSiteId.ElectricalMaintenance, 0, SafetyTaskType.HazardIdentification, "open-panel",
                    "Check for open cabinet doors exposing live three-phase conductors.", 100, 25),

                new SafetyTaskDefinition(
                    "elec_haz_02",
                    "Detect Unprotected Cable Crossing",
                    "Identify the temporary electrical cable traversing the walkway without a bridge ramp.",
                    TrainingSiteId.ElectricalMaintenance, 1, SafetyTaskType.HazardIdentification, "loose-cable",
                    "Look across the walking aisle for high-voltage cables lacking physical protection.", 100, 25),

                new SafetyTaskDefinition(
                    "elec_act_03",
                    "Apply Lockout/Tagout Isolation Hasp",
                    "Position the LOTO kit and personal safety padlock on the switchboard disconnect.",
                    TrainingSiteId.ElectricalMaintenance, 2, SafetyTaskType.PhysicalControl, "step_0",
                    "Move the LOTO toolkit to the electrical cabinet disconnect point.", 25, 0),

                new SafetyTaskDefinition(
                    "elec_act_04",
                    "Verify Absence of Voltage (Zero Energy)",
                    "Document three-point voltage test verification confirming zero energy state.",
                    TrainingSiteId.ElectricalMaintenance, 3, SafetyTaskType.VerificationSignOff, "step_1",
                    "Bring the calibration clipboard to the isolated panel to sign the zero energy permit.", 25, 0),

                new SafetyTaskDefinition(
                    "elec_act_05",
                    "Install Heavy-Duty Cable Protection Ramp",
                    "Position the high-visibility cable protector bridge across the exposed aisle.",
                    TrainingSiteId.ElectricalMaintenance, 4, SafetyTaskType.PhysicalControl, "step_2",
                    "Move the protective road barrier ramp over the cable crossing route.", 25, 0)
            };
        }

        public IReadOnlyList<SafetyTaskDefinition> GetTasksForSite(TrainingSiteId siteId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list))
            {
                list = new List<SafetyTaskDefinition>();
                siteTasks[siteId] = list;
            }
            return list;
        }

        public SafetyTaskDefinition GetCurrentTask(TrainingSiteId siteId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list))
                return null;

            return list.FirstOrDefault(t => !t.IsCompleted);
        }

        public int GetCompletedCount(TrainingSiteId siteId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list))
                return 0;

            return list.Count(t => t.IsCompleted);
        }

        public int GetTotalCount(TrainingSiteId siteId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list))
                return 0;

            return list.Count;
        }

        public bool IsSiteTasksComplete(TrainingSiteId siteId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list) || list.Count == 0)
                return false;

            return list.All(t => t.IsCompleted);
        }

        public void CompleteTask(TrainingSiteId siteId, string targetOrStepId)
        {
            if (!siteTasks.TryGetValue(siteId, out var list))
                return;

            var task = list.FirstOrDefault(t => !t.IsCompleted && t.TargetId == targetOrStepId);
            if (task == null)
                return;

            task.IsCompleted = true;
            task.CompletedTimestamp = Time.time;
            TaskCompleted?.Invoke(task);

            // Check if all tasks for this site are complete
            if (list.All(t => t.IsCompleted))
            {
                GenerateModuleAssessment(siteId);
            }
        }

        public void NotifyPracticalStepCompleted(TrainingSiteId siteId, int stepIndex)
        {
            CompleteTask(siteId, $"step_{stepIndex}");
        }

        void OnInspectionCompleted(TrainingSiteId siteId, InspectionResult result)
        {
            if (result.Outcome == InspectionOutcome.CorrectHazard)
            {
                // Find matching hazard task
                if (siteTasks.TryGetValue(siteId, out var list))
                {
                    foreach (var task in list)
                    {
                        if (!task.IsCompleted && task.Type == SafetyTaskType.HazardIdentification)
                        {
                            // In this demo, each hazard inspected advances one hazard task
                            task.IsCompleted = true;
                            task.CompletedTimestamp = Time.time;
                            TaskCompleted?.Invoke(task);
                            break;
                        }
                    }

                    if (list.All(t => t.IsCompleted))
                        GenerateModuleAssessment(siteId);
                }
            }
        }

        public ModuleAssessmentSummary GenerateModuleAssessment(TrainingSiteId siteId)
        {
            var coordinator = TrainingCoordinator.Instance;
            var siteTasksList = siteTasks.ContainsKey(siteId) ? siteTasks[siteId] : new List<SafetyTaskDefinition>();

            var totalTasks = siteTasksList.Count;
            var completedTasks = siteTasksList.Count(t => t.IsCompleted);

            var siteName = siteId switch
            {
                TrainingSiteId.Construction => "Construction Fall Protection",
                TrainingSiteId.Warehouse => "Warehouse Traffic & Routes",
                TrainingSiteId.FireResponse => "Fire Response & Evacuation",
                TrainingSiteId.ChemicalProcessing => "Chemical Safety & Segregation",
                TrainingSiteId.ElectricalMaintenance => "Electrical Safety & LOTO",
                _ => siteId.ToString()
            };

            var score = coordinator != null ? coordinator.OverallScore : 300;
            var hazardsFound = coordinator != null ? coordinator.IdentifiedHazardCount : 2;
            var totalHazards = coordinator != null ? coordinator.TotalHazardCount : 2;

            var rating = "EXEMPLARY (PASS)";
            var keyTakeaway = "All identified hazards neutralized with standard OSHA/ANSI engineering controls.";

            if (completedTasks < totalTasks)
            {
                rating = "NEEDS REINSPECTION";
                keyTakeaway = "Incomplete practical controls. Revisit the site and verify all mandatory physical barriers.";
            }

            var summary = new ModuleAssessmentSummary
            {
                SiteId = siteId,
                SiteName = siteName,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                HazardsFound = hazardsFound,
                TotalHazards = totalHazards,
                Score = score,
                FalsePositives = 0,
                ElapsedTimeSeconds = coordinator != null ? coordinator.SessionElapsedSeconds : 60f,
                PerformanceRating = rating,
                KeyTakeaway = keyTakeaway
            };

            siteAssessments[siteId] = summary;
            ModuleAssessmentCompleted?.Invoke(siteId, summary);
            return summary;
        }

        public ModuleAssessmentSummary GetAssessment(TrainingSiteId siteId)
        {
            if (siteAssessments.TryGetValue(siteId, out var summary))
                return summary;

            return GenerateModuleAssessment(siteId);
        }
    }
}
