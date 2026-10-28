using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SafetyTraining.Editor
{
    public static class StandaloneBuildRunner
    {
        const string ScenePath = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        [MenuItem("Safety Training/Build Windows Training App")]
        public static void BuildWindows()
        {
            var outputDirectory = Path.GetFullPath(Path.Combine("Builds", "Windows"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "SiteShield.exe");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Windows build failed: {report.summary.result}, errors={report.summary.totalErrors}");

            UnityEngine.Debug.Log(
                $"Windows build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
        }
    }
}
