using System;
using System.IO;
using System.Text.Json;
using SafetyTraining.Core;

using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine("Packages", "manifest.json")));
if (!manifest.RootElement.GetProperty("dependencies").TryGetProperty("com.unity.modules.unitywebrequest", out _))
    throw new InvalidOperationException("Unity Web Request module must be enabled for the LLM conversation client.");
if (!manifest.RootElement.GetProperty("dependencies").TryGetProperty("com.unity.modules.screencapture", out _))
    throw new InvalidOperationException("Screen Capture module must be enabled for visual tour verification.");

var session = new TrainingSession(TrainingSiteId.Construction, new[]
{
    new InspectionTargetSpec("fall", true),
    new InspectionTargetSpec("access", true),
    new InspectionTargetSpec("guarded", false)
});
if (session.Inspect("fall").Outcome != InspectionOutcome.CorrectHazard)
    throw new InvalidOperationException("First required hazard was rejected.");
if (session.Inspect("fall").Outcome != InspectionOutcome.AlreadyInspected)
    throw new InvalidOperationException("Duplicate hazard changed the score.");
if (session.Inspect("unknown").Outcome != InspectionOutcome.UnknownTarget)
    throw new InvalidOperationException("Unknown hazard changed the score.");
if (session.Inspect("guarded").Outcome != InspectionOutcome.SafeObjectSelected || session.Score != 75)
    throw new InvalidOperationException("Safe-object penalty was not applied exactly once.");
if (session.Inspect("access").Outcome != InspectionOutcome.CorrectHazard || !session.IsComplete)
    throw new InvalidOperationException("Session did not complete after all required hazards.");
if (session.Score != 175 || session.FalsePositives != 1)
    throw new InvalidOperationException("Final scoring state is incorrect.");

Console.WriteLine("Core safety-session smoke test passed.");
