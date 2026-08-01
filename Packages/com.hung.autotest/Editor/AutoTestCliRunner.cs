#if UNITY_EDITOR
using System;
using UnityEditor;
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

            pendingSuite = suite;
            readyDeadline = Time.realtimeSinceStartup + GetFloatArg("-autoTestReadyTimeout", 60f);
            requiredGameplayScene = GetArg("-autoTestGameplayScene");
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
            runner.RunConfiguredSuite();
            EditorApplication.update += PollCompletion;
        }

        static void PollCompletion()
        {
            if (runner == null)
                return;

            if (runner.Status == AutoTestStatus.Running)
                return;

            EditorApplication.update -= PollCompletion;
            bool passed = runner.LastReport != null && runner.LastReport.status == AutoTestStatus.Passed;
            Debug.Log("[AutoTestCli] Result: " + (runner.LastReport != null ? runner.LastReport.status.ToString() : "no report"));
            EditorApplication.Exit(passed ? 0 : 1);
        }

        static void ExitIfReadyTimedOut()
        {
            if (Time.realtimeSinceStartup < readyDeadline)
                return;

            EditorApplication.update -= WaitForGameplayReady;
            Debug.LogError("[AutoTestCli] Gameplay readiness timeout. Scene=" + SceneManager.GetActiveScene().name
                + ", ExtraReadyCheck=" + AutoTestBootstrapper.ExtraReadyCheck());
            EditorApplication.Exit(3);
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
