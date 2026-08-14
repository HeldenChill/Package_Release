#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hung.AutoTest.Editor
{
    public static class AutoTestCliRunner
    {
        static AutoTestRunner runner;
        static AutoTestSuiteData pendingSuite;
        static float readyDeadline;
        static string requiredGameplayScene;

        public static void RunSuiteFromCommandLine()
        {
            string suitePath = GetArg("-autoTestSuite");
            AutoTestSuiteData suite = AssetDatabase.LoadAssetAtPath<AutoTestSuiteData>(suitePath);
            if (suite == null)
            {
                Debug.LogError("[AutoTestCli] Suite not found at: " + suitePath);
                EditorApplication.Exit(2);
                return;
            }

            OpenStartupSceneIfNeeded();
            AutoTestCliSessionState.Begin(
                suitePath,
                GetArg("-autoTestGameplayScene"),
                GetFloatArg("-autoTestReadyTimeout", 60f));
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterDomainReload()
        {
            EditorApplication.delayCall += TryResumePendingLaunch;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!ShouldQueuePendingLaunch(state))
                return;

            EditorApplication.delayCall += TryResumePendingLaunch;
        }

        internal static bool ShouldQueuePendingLaunch(PlayModeStateChange state)
        {
            return state == PlayModeStateChange.EnteredPlayMode;
        }

        static void TryResumePendingLaunch()
        {
            if (!EditorApplication.isPlaying)
                return;
            if (!AutoTestCliSessionState.TryClaim(out AutoTestCliLaunch launch))
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            pendingSuite = AssetDatabase.LoadAssetAtPath<AutoTestSuiteData>(launch.SuitePath);
            requiredGameplayScene = launch.RequiredGameplayScene;
            readyDeadline = Time.realtimeSinceStartup + launch.ReadyTimeoutSeconds;
            if (pendingSuite == null)
            {
                ExitCli(2, "[AutoTestCli] Suite disappeared after domain reload: " + launch.SuitePath);
                return;
            }

            AutoTestBootstrapper.ResetForCliPreboot();
            EditorApplication.update += WaitForGameplayReady;
        }

        static void WaitForGameplayReady()
        {
            // One-press boot: from the CryptoLoader init screen this presses Start
            // automatically (LoadStart then chains into GameScene by itself).
            AutoTestBootstrapper.TryKickBoot();

            if (!string.IsNullOrEmpty(requiredGameplayScene) &&
                SceneManager.GetActiveScene().name != requiredGameplayScene)
            {
                ExitIfReadyTimedOut();
                return;
            }

            // TestScenarioModeManager is auto-created by the scenario executor when missing.
            // IsGameplayReady uses Exists (never Ins — Ins auto-creates an empty manager).
            if (!AutoTestBootstrapper.IsGameplayReady)
            {
                ExitIfReadyTimedOut();
                return;
            }

            EditorApplication.update -= WaitForGameplayReady;
            GameObject go = new GameObject("AutoTestRunner_CLI");
            runner = go.AddComponent<AutoTestRunner>();
            runner.SetSuite(pendingSuite);
            runner.RunCompleted += OnRunCompleted;
            runner.RunConfiguredSuite();
        }

        static void OnRunCompleted(AutoTestReport report)
        {
            if (runner != null)
                runner.RunCompleted -= OnRunCompleted;

            Debug.Log("[AutoTestCli] Result: " + (report != null ? report.status.ToString() : "no report"));
            ExitCli(GetExitCode(report != null ? report.status : AutoTestStatus.Error), null);
        }

        internal static int GetExitCode(AutoTestStatus status)
        {
            return status == AutoTestStatus.Passed ? 0 : 1;
        }

        static void ExitIfReadyTimedOut()
        {
            if (Time.realtimeSinceStartup < readyDeadline)
                return;

            ExitCli(3, "[AutoTestCli] Gameplay readiness timeout. Scene=" + SceneManager.GetActiveScene().name
                + ", ExtraReadyCheck=" + AutoTestBootstrapper.ExtraReadyCheck());
        }

        static void ExitCli(int code, string error)
        {
            EditorApplication.update -= WaitForGameplayReady;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (runner != null)
                runner.RunCompleted -= OnRunCompleted;
            AutoTestCliSessionState.Clear();
            AutoTestBootstrapper.ClearPreparedRunnerStart();
            if (!string.IsNullOrEmpty(error))
                Debug.LogError(error);
            EditorApplication.Exit(code);
        }

        static void OpenStartupSceneIfNeeded()
        {
            string startupScenePath = ResolveStartupScenePath(
                SceneManager.GetActiveScene().path,
                EditorBuildSettings.scenes);
            if (string.IsNullOrEmpty(startupScenePath))
                return;

            Debug.Log("[AutoTestCli] Opening startup Build Settings scene: " + startupScenePath);
            EditorSceneManager.OpenScene(startupScenePath, OpenSceneMode.Single);
        }

        internal static string ResolveStartupScenePath(string activeScenePath, EditorBuildSettingsScene[] buildScenes)
        {
            if (!string.IsNullOrEmpty(activeScenePath) && activeScenePath.StartsWith("Assets/", StringComparison.Ordinal))
                return string.Empty;

            foreach (EditorBuildSettingsScene scene in buildScenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                    return scene.path;
            }

            return string.Empty;
        }

        static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return string.Empty;
        }

        static float GetFloatArg(string name, float fallback)
        {
            string value = GetArg(name);
            return float.TryParse(value, out float parsed) ? parsed : fallback;
        }
    }
}
#endif
