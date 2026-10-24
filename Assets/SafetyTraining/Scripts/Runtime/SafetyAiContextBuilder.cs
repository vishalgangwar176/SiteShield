using System.Collections.Generic;
using System.Text;
using SafetyTraining.Core;

namespace SafetyTraining.Runtime
{
    public static class SafetyAiContextBuilder
    {
        public const string BaseSystemPrompt =
            "You are the SiteShield Senior Safety Training Coach, an expert industrial safety mentor certified in OSHA, NFPA, and GHS standards. " +
            "Your mission is to guide workplace safety trainees through interactive field training. " +
            "Guidelines: " +
            "1. Be concise, professional, supportive, and direct (max 65 words per reply). " +
            "2. Explain the safety 'why' behind controls rather than just giving away direct answers. " +
            "3. If the trainee asks for a hint, provide a practical hint tailored to their current task without spoiling all subsequent steps. " +
            "4. Never invent regulations or claim an unsafe situation is safe. " +
            "5. For high-risk hazards (active electrical current, fire, hazardous chemical splash), prioritize immediate isolation, PPE, or evacuation. " +
            "6. Answer using complete, natural sentences.";

        public static string BuildSystemInstructions(TrainingSiteId? activeSite)
        {
            var sb = new StringBuilder();
            sb.AppendLine(BaseSystemPrompt);
            sb.AppendLine();

            if (activeSite.HasValue)
            {
                sb.AppendLine("=== SITE-SPECIFIC INDUSTRIAL SAFETY KNOWLEDGE BASE ===");
                sb.AppendLine(GetSiteKnowledgeBase(activeSite.Value));
            }

            return sb.ToString();
        }

        public static string BuildDynamicContext(TrainingSiteId? activeSite, string learnerMessage)
        {
            var sb = new StringBuilder();
            var coordinator = TrainingCoordinator.Instance;
            var taskManager = SafetyTaskManager.Instance;

            if (activeSite.HasValue)
            {
                sb.AppendLine($"[Current Training Module]: {GetModuleDisplayName(activeSite.Value)}");

                if (taskManager != null)
                {
                    var currentTask = taskManager.GetCurrentTask(activeSite.Value);
                    if (currentTask != null)
                    {
                        sb.AppendLine($"[Active Objective]: {currentTask.Title} - {currentTask.Description}");
                        sb.AppendLine($"[Task Hint Available]: {currentTask.HintText}");
                    }
                    else
                    {
                        sb.AppendLine("[Active Objective]: All module practical tasks completed.");
                    }

                    var completed = taskManager.GetCompletedCount(activeSite.Value);
                    var total = taskManager.GetTotalCount(activeSite.Value);
                    sb.AppendLine($"[Task Progress]: {completed}/{total} tasks completed.");
                }

                if (coordinator != null)
                {
                    sb.AppendLine($"[Session Score]: {coordinator.OverallScore} pts | " +
                                  $"Hazards Found: {coordinator.IdentifiedHazardCount}/{coordinator.TotalHazardCount}");
                    if (!string.IsNullOrWhiteSpace(coordinator.HudFeedback))
                        sb.AppendLine($"[Latest System Feedback]: {coordinator.HudFeedback}");
                }
            }
            else
            {
                sb.AppendLine("[Current Location]: Safety Training Campus Hub");
                sb.AppendLine("[Status]: Preparing to enter field training module.");
            }

            sb.AppendLine();
            sb.AppendLine($"[Trainee Asks]: {learnerMessage}");
            return sb.ToString();
        }

        public static string GetModuleDisplayName(TrainingSiteId siteId) => siteId switch
        {
            TrainingSiteId.Construction => "Construction Safety (Fall Protection - OSHA 1926 Subpart M)",
            TrainingSiteId.Warehouse => "Warehouse Safety (Vehicle Routes & Traffic - OSHA 1910.178)",
            TrainingSiteId.FireResponse => "Fire Response & Evacuation (NFPA 10 & OSHA 1910.38)",
            TrainingSiteId.ChemicalProcessing => "Chemical Safety & Segregation (GHS / OSHA 1910.1200)",
            TrainingSiteId.ElectricalMaintenance => "Electrical Safety & LOTO (OSHA 1910.147 & NFPA 70E)",
            _ => siteId.ToString()
        };

        public static string GetSiteKnowledgeBase(TrainingSiteId siteId) => siteId switch
        {
            TrainingSiteId.Construction =>
                "- OSHA 1926.501 requires fall protection for walking/working surfaces with an unprotected edge 6 feet (1.8m) or higher.\n" +
                "- Standard guardrail criteria: top rail at 42 inches (+/- 3 in), mid-rail at 21 inches, capable of withstanding 200 lbs force.\n" +
                "- Personal Fall Arrest Systems (PFAS) require a full-body harness, shock-absorbing lanyard, and an anchor point certified for 5,000 lbs per worker.\n" +
                "- Access paths and scaffolding platforms must be kept clear of loose materials, tripping hazards, and debris.",

            TrainingSiteId.Warehouse =>
                "- OSHA 1910.178 requires safe separation between pedestrian aisles and powered industrial trucks (forklifts).\n" +
                "- Permanent aisles and passageways must be clearly demarcated with durable yellow/white line markings.\n" +
                "- Blind intersections require convex safety mirrors, audible horn warnings, and pedestrian right-of-way.\n" +
                "- Liquid spills represent an immediate slip/fall hazard and must be cordoned off and neutralized using appropriate absorbent spill kits.",

            TrainingSiteId.FireResponse =>
                "- NFPA 10 specifies that portable fire extinguishers must be mounted in conspicuous, unobstructed locations with at least 36 inches clear clearance.\n" +
                "- PASS operating technique: Pull the pin, Aim at the base of the fire, Squeeze the operating handle, Sweep side-to-side.\n" +
                "- Critical decision rule: Only fight incipient-stage fires (trashcan size or smaller). If heavy smoke develops or the escape path is threatened, EVACUATE immediately.\n" +
                "- Emergency exit routes must lead directly outside and remain completely unobstructed at all times.",

            TrainingSiteId.ChemicalProcessing =>
                "- OSHA 1910.1200 (Hazard Communication) mandates that every chemical container must have a GHS-compliant label with product identifier, signal word, pictograms, and hazard statements.\n" +
                "- Chemical segregation: Never store oxidizers adjacent to flammable liquids; keep acids separated from bases and cyanides.\n" +
                "- Secondary containment must hold at least 110% of the single largest container's capacity.\n" +
                "- ANSI Z358.1 requires emergency eyewash stations to be accessible within 10 seconds (55 feet), unobstructed, and deliver 15 minutes of continuous flushing.",

            TrainingSiteId.ElectricalMaintenance =>
                "- OSHA 1910.147 (Lockout/Tagout - LOTO) requires de-energization, physical padlock application, and tagging before servicing equipment.\n" +
                "- Three-step absence-of-voltage test: Test calibrated meter on a known live source, test the target isolated circuit (must read 0.0V), re-test on the known live source to verify meter function.\n" +
                "- Exposed energized conductors (busbars) present severe arc flash and shock hazards; unauthorized access must be physically barriered.\n" +
                "- Temporary cables crossing walkways must be protected by heavy-duty rated industrial cable ramp protectors to prevent mechanical damage and trip hazards.",

            _ => "Follow approved industrial safety protocols and consult the safety coordinator."
        };
    }
}
