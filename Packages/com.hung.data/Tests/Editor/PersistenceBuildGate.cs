using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Hung.Data.Editor.Tests
{
    public static class PersistenceBuildGate
    {
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("Persistence build gate requires at least one enabled scene.");

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string outputRoot = Path.GetFullPath(Path.Combine("Temp", "PersistenceBuild"));
            Directory.CreateDirectory(outputRoot);
            string outputPath = Path.Combine(outputRoot, OutputName(target));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = outputPath,
                options = BuildOptions.CleanBuildCache | BuildOptions.StrictMode | BuildOptions.DetailedBuildReport
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Console.WriteLine(
                $"PERSISTENCE_BUILD target={target} result={report.summary.result} " +
                $"duration={report.summary.totalTime} warnings={report.summary.totalWarnings} " +
                $"errors={report.summary.totalErrors} output={outputPath}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Persistence build gate failed: {report.summary.result}");
        }

        private static string OutputName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "PackageRepo.apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "PackageRepo.exe";
                default:
                    return "PackageRepo";
            }
        }
    }
}
