using System;
using System.Collections;
using UnityEngine;

namespace Hung.AutoTest
{
    public sealed class AutoTestPlayerEntrypoint : MonoBehaviour
    {
        public static Func<AutoTestCommandLine, bool> ExternalRunner;
        public static Func<string, AutoTestSuiteData> SuiteResolver;

        private const float TimeoutSeconds = 300f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            AutoTestCommandLine commandLine = AutoTestCommandLine.Parse(Environment.GetCommandLineArgs());
            if (!commandLine.IsRuntimeConfidenceRun)
                return;
            if (ExternalRunner != null && ExternalRunner(commandLine))
                return;

            var host = new GameObject("AutoTestPlayerEntrypoint");
            DontDestroyOnLoad(host);
            host.AddComponent<AutoTestPlayerEntrypoint>().Begin(commandLine);
        }

        private void Begin(AutoTestCommandLine commandLine)
        {
            StartCoroutine(Run(commandLine));
        }

        private IEnumerator Run(AutoTestCommandLine commandLine)
        {
            RuntimeEvidenceRecord evidence = RuntimeEvidenceRecord.Start(
                commandLine.ScenarioId,
                string.IsNullOrEmpty(commandLine.RunId) ? Guid.NewGuid().ToString("N") : commandLine.RunId,
                RuntimeEvidenceAdapter.Fake);

            AutoTestSuiteData suite = SuiteResolver != null ? SuiteResolver(commandLine.ScenarioId) : null;
            if (suite == null)
            {
                evidence.Complete(RuntimeEvidenceResult.Blocked, "RC_SUITE_NOT_FOUND");
                WriteEvidence(commandLine, evidence);
                Quit(3);
                yield break;
            }

            var runnerObject = new GameObject("AutoTestRunner");
            DontDestroyOnLoad(runnerObject);
            var runner = runnerObject.AddComponent<AutoTestRunner>();
            runner.SetSuite(suite);

            bool completed = false;
            AutoTestReport terminalReport = null;
            runner.RunCompleted += report =>
            {
                terminalReport = report;
                completed = true;
            };

            runner.RunConfiguredSuite();
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!completed && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!completed)
            {
                evidence.Complete(RuntimeEvidenceResult.Blocked, "RC_HARNESS_TIMEOUT");
                WriteEvidence(commandLine, evidence);
                Quit(3);
                yield break;
            }

            bool passed = terminalReport != null && terminalReport.status == AutoTestStatus.Passed;
            evidence.Complete(passed ? RuntimeEvidenceResult.Passed : RuntimeEvidenceResult.Failed,
                passed ? "RC_AUTOTEST_PASSED" : "RC_AUTOTEST_FAILED");
            WriteEvidence(commandLine, evidence);
            Quit(passed ? 0 : 1);
        }

        private static void WriteEvidence(AutoTestCommandLine commandLine, RuntimeEvidenceRecord evidence)
        {
            if (!string.IsNullOrEmpty(commandLine.OutputPath))
                RuntimeEvidenceWriter.WriteJson(evidence, commandLine.OutputPath);
        }

        private static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit(exitCode);
#endif
        }
    }
}
