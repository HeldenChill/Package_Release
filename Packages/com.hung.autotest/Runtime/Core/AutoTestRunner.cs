using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    public sealed class AutoTestRunner : MonoBehaviour
    {
        [Header("Run Target")]
        [SerializeField] private AutoTestSuiteData suite;
        [SerializeField] private AutoTestCaseData singleCase;

        [Header("Runtime")]
        [SerializeField] private bool runSuiteOnStart;
        [SerializeField] private bool logVerbose;
        [Tooltip("When true, exit play mode automatically after a suite or single-case run finishes (Editor only).")]
        [SerializeField] private bool autoStopPlayModeOnFinish = true;
        [Tooltip("Seconds to wait after a run finishes before auto-stopping play mode.")]
        [SerializeField] private float autoStopDelaySeconds = 5f;

        private readonly AutoTestContext context = new AutoTestContext();
        private readonly AutoTestLogCollector logCollector = new AutoTestLogCollector();
        private readonly AutoTestEventCollector eventCollector = new AutoTestEventCollector();
        /// <summary>Game glue must assign these before a run.
        /// Keeps the core runner free of game types.</summary>
        public static Func<IAutoTestScenarioExecutor> ExecutorFactory;
        public static Func<IRuntimeSnapshotBuilder> SnapshotBuilderFactory;

        private IAutoTestScenarioExecutor scenarioExecutor;
        private IRuntimeSnapshotBuilder snapshotBuilder;
        private readonly List<IAutoTestAssertion> activeAssertions = new List<IAutoTestAssertion>();
        private readonly HashSet<string> failedAssertionIds = new HashSet<string>();

        private Coroutine runRoutine;
        private bool stopRequested;
        private AutoTestReport lastReport;
        private readonly List<AssertionStateView> assertionStates = new List<AssertionStateView>();
        public event Action<AutoTestReport> RunCompleted;

        /// <summary>Read-only live view of one assertion for editor dashboards.</summary>
        public sealed class AssertionStateView
        {
            public string Id;
            public AutoTestAssertionStatus LastStatus;
            public string LastMessage;
            public bool Evaluated;
        }

        public AutoTestStatus Status
        {
            get { return context.Status; }
        }

        public AutoTestReport LastReport
        {
            get { return lastReport; }
        }

        public AutoTestContext Context
        {
            get { return context; }
        }

        public AutoTestEventCollector EventCollector
        {
            get { return eventCollector; }
        }

        // Read-only progress seams for the editor dashboard (no behavior).
        public AutoTestSuiteData CurrentSuite { get; private set; }
        public int CurrentCaseIndex { get; private set; } = -1;
        public int TotalCases
        {
            get { return CurrentSuite != null ? CurrentSuite.testCases.Count : 0; }
        }
        public IReadOnlyList<AssertionStateView> AssertionStates
        {
            get { return assertionStates; }
        }

        [Header("Boot")]
        [Tooltip("Seconds to wait for the CryptoLoader/LoadStart boot chain to reach GameScene.")]
        [SerializeField] private float bootTimeoutSeconds = 60f;

        private void Awake()
        {
            // Survives the CryptoLoader -> LoadStart -> GameScene chain when the run
            // is started from a startup scene.
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (runSuiteOnStart && suite != null)
                RunSuite(suite);
        }

        private void OnDisable()
        {
            StopCurrentRun();
            logCollector.Stop();
        }

        public void SetSuite(AutoTestSuiteData value)
        {
            suite = value;
        }

        public void SetSingleCase(AutoTestCaseData value)
        {
            singleCase = value;
        }

        public void RunConfiguredSuite()
        {
            if (suite == null)
            {
                Debug.LogError("[AutoTestRunner] Missing suite.");
                return;
            }

            RunSuite(suite);
        }

        public void RunConfiguredSingleCase()
        {
            if (singleCase == null)
            {
                Debug.LogError("[AutoTestRunner] Missing single case.");
                return;
            }

            RunSingleCase(singleCase);
        }

        public void RunSuite(AutoTestSuiteData targetSuite)
        {
            if (targetSuite == null)
            {
                Debug.LogError("[AutoTestRunner] Cannot run null suite.");
                return;
            }

            StartRun(RunSuiteRoutine(targetSuite));
        }

        public void RunSingleCase(AutoTestCaseData testCase)
        {
            if (testCase == null)
            {
                Debug.LogError("[AutoTestRunner] Cannot run null case.");
                return;
            }

            AutoTestSuiteData virtualSuite = ScriptableObject.CreateInstance<AutoTestSuiteData>();
            virtualSuite.suiteId = "SingleCaseRuntimeSuite";
            virtualSuite.displayName = "Single Case Runtime Suite";
            virtualSuite.hideFlags = HideFlags.DontSave;
            virtualSuite.stopSuiteOnFirstFailure = true;
            virtualSuite.exportReportAfterRun = true;
            virtualSuite.reportFileNamePrefix = "AutoTest_SingleCase";
            virtualSuite.testCases.Add(testCase);

            StartRun(RunSuiteRoutine(virtualSuite, true));
        }

        public void StopCurrentRun()
        {
            stopRequested = true;

            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
                runRoutine = null;
            }

            if (context.Status == AutoTestStatus.Running)
                context.Status = AutoTestStatus.Stopped;

            logCollector.Stop();
            eventCollector.Stop();
        }

        private void StartRun(IEnumerator routine)
        {
            StopCurrentRun();
            AutoTestBootstrapper.ResetForRunnerStart();
            stopRequested = false;
            runRoutine = StartCoroutine(routine);
        }

        private IEnumerator RunSuiteRoutine(AutoTestSuiteData targetSuite, bool destroySuiteAfterRun = false)
        {
            string runId = AutoTestFilePathUtility.CreateTimestampRunId();
            lastReport = AutoTestReport.Create(runId, targetSuite);
            float suiteStart = Time.realtimeSinceStartup;
            CurrentSuite = targetSuite;
            CurrentCaseIndex = -1;

            if (logVerbose)
                Debug.Log("[AutoTestRunner] Run suite: " + targetSuite.displayName);

            // One-press boot: from CryptoLoader/LoadStart this drives the game into GameScene.
            yield return AutoTestBootstrapper.EnsureGameBooted(bootTimeoutSeconds);
            if (!AutoTestBootstrapper.IsGameplayReady)
            {
                lastReport.status = AutoTestStatus.Error;
                lastReport.finishedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                context.Status = AutoTestStatus.Error;
                runRoutine = null;
                CurrentSuite = null;
                Debug.LogError("[AutoTestRunner] Aborting run: gameplay never became ready.");
                MaybeStopPlayMode();
                yield break;
            }

            for (int caseIndex = 0; caseIndex < targetSuite.testCases.Count; caseIndex++)
            {
                AutoTestCaseData testCase = targetSuite.testCases[caseIndex];
                if (stopRequested)
                    break;

                if (testCase == null)
                    continue;

                CurrentCaseIndex = caseIndex;

                AutoTestCaseReport caseReport = null;
                yield return RunCaseRoutine(targetSuite, testCase, runId, report => caseReport = report);

                if (caseReport != null)
                    lastReport.cases.Add(caseReport);

                if (caseReport != null && caseReport.status == AutoTestStatus.Failed && targetSuite.stopSuiteOnFirstFailure)
                    break;
            }

            lastReport.finishedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastReport.durationSeconds = Time.realtimeSinceStartup - suiteStart;
            lastReport.status = ComputeSuiteStatus(lastReport);
            lastReport.summary = AutoTestReportSummary.FromCases(lastReport.cases);

            if (targetSuite.exportReportAfterRun)
            {
                string reportFolder = AutoTestFilePathUtility.GetReportFolder();
                AutoTestJsonExporter.Export(lastReport, reportFolder, targetSuite.reportFileNamePrefix);
                AutoTestMarkdownExporter.Export(lastReport, reportFolder, targetSuite.reportFileNamePrefix);
            }

            RunCompleted?.Invoke(lastReport);

            runRoutine = null;
            CurrentSuite = null;
            CurrentCaseIndex = -1;

            if (destroySuiteAfterRun && targetSuite != null)
                Destroy(targetSuite);

            if (logVerbose)
                Debug.Log("[AutoTestRunner] Suite completed: " + lastReport.status);

            MaybeStopPlayMode();
        }

        private void MaybeStopPlayMode()
        {
            if (!autoStopPlayModeOnFinish)
                return;
#if UNITY_EDITOR
            if (autoStopDelaySeconds > 0f)
                StartCoroutine(StopPlayModeAfterDelay(autoStopDelaySeconds));
            else
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

#if UNITY_EDITOR
        private IEnumerator StopPlayModeAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif

        private IEnumerator RunCaseRoutine(
            AutoTestSuiteData targetSuite,
            AutoTestCaseData testCase,
            string runId,
            Action<AutoTestCaseReport> onCompleted)
        {
            context.Begin(runId, targetSuite != null ? targetSuite.suiteId : string.Empty, testCase);
            context.Events = eventCollector;

            if (!AutoTestCapabilityRegistry.TryEvaluate(testCase.requiredCapabilities, out string capabilityDiagnostic))
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Setup",
                    "AUTOTEST_CAPABILITY_UNAVAILABLE",
                    capabilityDiagnostic,
                    AutoTestAssertionSeverity.Fatal,
                    context));
                AutoTestCaseReport failedReport = AutoTestCaseReport.Create(testCase);
                failedReport.durationSeconds = 0f;
                failedReport.status = AutoTestStatus.Failed;
                failedReport.failureCount = context.Failures.Count;
                failedReport.failures.AddRange(context.Failures);
                failedReport.logs.AddRange(context.Logs);
                onCompleted?.Invoke(failedReport);
                yield break;
            }

            activeAssertions.Clear();
            failedAssertionIds.Clear();
            AutoTestAssertionFactory.CreateAssertions(testCase, activeAssertions);

            assertionStates.Clear();
            foreach (IAutoTestAssertion assertion in activeAssertions)
                assertionStates.Add(new AssertionStateView { Id = assertion.Id });

            foreach (IAutoTestAssertion assertion in activeAssertions)
                assertion.OnTestStarted(context);

            if (scenarioExecutor == null)
                scenarioExecutor = ExecutorFactory != null ? ExecutorFactory() : null;
            if (snapshotBuilder == null)
                snapshotBuilder = SnapshotBuilderFactory != null ? SnapshotBuilderFactory() : null;
            if (scenarioExecutor == null || snapshotBuilder == null)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Setup",
                    "AutoTestRunner",
                    "ExecutorFactory/SnapshotBuilderFactory not assigned — game glue must register them.",
                    AutoTestAssertionSeverity.Fatal,
                    context));
                yield break;
            }

            logCollector.Start(context);
            eventCollector.Clear();
            eventCollector.Start();
            scenarioExecutor.ResetRuntimeState();

            AutoTestCaseReport caseReport = AutoTestCaseReport.Create(testCase);
            float caseStart = Time.realtimeSinceStartup;
            float nextSnapshotAt = 0f;

            yield return scenarioExecutor.Prepare(testCase, context);

            if (!context.HasFatalFailure)
                yield return scenarioExecutor.Run(testCase, context);

            while (!stopRequested && !context.HasFatalFailure)
            {
                float elapsed = context.ElapsedSeconds;

                if (elapsed >= testCase.timeoutSeconds)
                    break;

                if (elapsed >= nextSnapshotAt)
                {
                    RuntimeSnapshot snapshot = snapshotBuilder.Build(context);
                    context.LatestSnapshot = snapshot;
                    EvaluateAssertions(snapshot, testCase);
                    nextSnapshotAt = elapsed + Mathf.Max(0.05f, testCase.snapshotIntervalSeconds);
                }

                if (context.HasFatalFailure && testCase.failFast)
                    break;

                if (IsPassConditionMet(testCase, context.LatestSnapshot))
                    break;

                yield return null;
            }

            RuntimeSnapshot finalSnapshot = snapshotBuilder.Build(context);
            context.LatestSnapshot = finalSnapshot;
            EvaluateAssertions(finalSnapshot, testCase);

            yield return scenarioExecutor.Cleanup(testCase, context);
            logCollector.Stop();
            eventCollector.Stop();

            caseReport.durationSeconds = Time.realtimeSinceStartup - caseStart;
            caseReport.status = BuildCaseStatus(testCase, finalSnapshot, context);
            caseReport.failureCount = context.Failures.Count;
            caseReport.failures.AddRange(context.Failures);
            caseReport.logs.AddRange(context.Logs);
            caseReport.finalSnapshot = finalSnapshot;

            onCompleted?.Invoke(caseReport);
        }

        private void EvaluateAssertions(RuntimeSnapshot snapshot, AutoTestCaseData testCase)
        {
            if (snapshot == null)
                return;

            for (int i = 0; i < activeAssertions.Count; i++)
            {
                IAutoTestAssertion assertion = activeAssertions[i];
                AutoTestAssertionResult result = assertion.Evaluate(snapshot, context);

                if (result != null && i < assertionStates.Count)
                {
                    AssertionStateView view = assertionStates[i];
                    view.LastStatus = result.status;
                    view.LastMessage = result.message;
                    view.Evaluated = true;
                }

                if (result == null || result.status != AutoTestAssertionStatus.Failed)
                    continue;

                if (!failedAssertionIds.Add(assertion.Id))
                    continue;

                AutoTestFailure failure = AutoTestFailure.Create(
                    "Assertion",
                    assertion.Id,
                    result.message,
                    assertion.Severity,
                    context,
                    evidence: result.evidence);

                context.AddFailure(failure);

                if (assertion.Severity == AutoTestAssertionSeverity.Fatal || testCase.failFast)
                    break;
            }
        }

        private static AutoTestStatus BuildCaseStatus(AutoTestCaseData testCase, RuntimeSnapshot snapshot, AutoTestContext context)
        {
            if (context.HasFatalFailure || context.Failures.Count > 0)
                return AutoTestStatus.Failed;

            if (!IsPassConditionMet(testCase, snapshot))
                return AutoTestStatus.Failed;

            return AutoTestStatus.Passed;
        }

        private static bool IsPassConditionMet(AutoTestCaseData testCase, RuntimeSnapshot snapshot)
        {
            if (testCase == null)
                return false;

            if (snapshot == null)
                return testCase.passCondition.type == AutoTestPassConditionType.NoFatalFailureUntilTimeout;

            AutoTestPassConditionConfig pass = testCase.passCondition;

            switch (pass.type)
            {
                case AutoTestPassConditionType.DurationSurvived:
                    return snapshot.elapsedSeconds >= pass.requiredDurationSeconds;

                case AutoTestPassConditionType.WaveCompleted:
                    return snapshot.combat.wavesCompleted >= pass.requiredWaveCompletedCount;

                case AutoTestPassConditionType.EnemyKilledCountReached:
                    return snapshot.combat.enemiesKilled >= pass.requiredEnemyKilledCount;

                case AutoTestPassConditionType.DamageReached:
                    return snapshot.combat.totalDamage >= pass.requiredTotalDamage;

                case AutoTestPassConditionType.NoFatalFailureUntilTimeout:
                    return snapshot.elapsedSeconds >= testCase.timeoutSeconds;

                default:
                    return false;
            }
        }

        private static AutoTestStatus ComputeSuiteStatus(AutoTestReport report)
        {
            if (report == null || report.cases.Count == 0)
                return AutoTestStatus.Failed;

            for (int i = 0; i < report.cases.Count; i++)
            {
                if (report.cases[i].status != AutoTestStatus.Passed)
                    return AutoTestStatus.Failed;
            }

            return AutoTestStatus.Passed;
        }
    }
}
