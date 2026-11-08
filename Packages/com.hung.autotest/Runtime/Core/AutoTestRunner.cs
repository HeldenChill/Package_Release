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

        private AutoTestRoutineSupervisor activeSupervisor;
        private AutoTestSuiteData activeSuite;
        private AutoTestCaseData activeTestCase;
        private AutoTestCaseReport activeCaseReport;
        private bool isRunActive;
        private bool isFinalized;
        private bool destroySuiteAfterRun;
        private float previousTimeScale = 1f;
        private float suiteStartTime;
        private string currentRunId;
        private bool hasExportedReport;
        private bool stopping;
        private bool executorPhaseActive;
        private const float GracefulStopSeconds = 20f;

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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (runSuiteOnStart && suite != null)
                RunSuite(suite);
        }

        private void OnDisable()
        {
            // Disable kills this runner's coroutines, so graceful async Cleanup is impossible: finalize now.
            stopRequested = true;
            if (isRunActive && !isFinalized)
                FinalizeRun(AutoTestStatus.Stopped, "OnDisable");
            logCollector.Stop();
            eventCollector.Stop();
        }

        /// <summary>Exit play mode after a run finishes (Editor only). The Auto Test Runner window drives this from its "Auto Stop" toggle.</summary>
        public bool AutoStopPlayModeOnFinish
        {
            get => autoStopPlayModeOnFinish;
            set => autoStopPlayModeOnFinish = value;
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

            StartRun(RunSuiteRoutine(targetSuite, false), targetSuite, false);
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

            StartRun(RunSuiteRoutine(virtualSuite, true), virtualSuite, true);
        }

        public void StopCurrentRun()
        {
            stopRequested = true;

            if (isFinalized || stopping)
                return;

            if (!(isRunActive || runRoutine != null || (context != null && context.Status == AutoTestStatus.Running)))
                return;

            // Inside a case with a live executor: let the run routine unwind so supervised
            // executor Cleanup runs. A watchdog force-finalizes if it never unwinds.
            if (executorPhaseActive && runRoutine != null && isActiveAndEnabled)
            {
                stopping = true;
                StartCoroutine(StopWatchdog());
                return;
            }

            FinalizeRun(AutoTestStatus.Stopped, "StopCurrentRun");
        }

        private IEnumerator StopWatchdog()
        {
            float until = Time.realtimeSinceStartup + GracefulStopSeconds;
            while (!isFinalized && Time.realtimeSinceStartup < until)
                yield return null;

            if (!isFinalized)
                FinalizeRun(AutoTestStatus.Stopped, "StopWatchdog");
        }

        private void StartRun(IEnumerator routine, AutoTestSuiteData targetSuite, bool destroySuiteAfter)
        {
            if (isRunActive && !isFinalized)
                FinalizeRun(AutoTestStatus.Stopped, "Restart");

            AutoTestBootstrapper.ResetForRunnerStart();

            stopRequested = false;
            stopping = false;
            executorPhaseActive = false;
            activeTestCase = null;
            activeCaseReport = null;
            isFinalized = false;
            hasExportedReport = false;
            activeSuite = targetSuite;
            destroySuiteAfterRun = destroySuiteAfter;
            currentRunId = AutoTestFilePathUtility.CreateTimestampRunId();
            lastReport = AutoTestReport.Create(currentRunId, targetSuite);
            suiteStartTime = Time.realtimeSinceStartup;
            previousTimeScale = Time.timeScale;
            isRunActive = true;
            context.Status = AutoTestStatus.Running;

            runRoutine = StartCoroutine(routine);
        }

        private void FinalizeRun(AutoTestStatus terminalStatus, string reason)
        {
            if (isFinalized)
                return;

            isFinalized = true;

            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
                runRoutine = null;
            }

            if (activeSupervisor != null)
            {
                activeSupervisor.Dispose();
                activeSupervisor = null;
            }

            // Fallback synchronous release for executors implementing IDisposable
            if (scenarioExecutor is IDisposable disposableExecutor)
            {
                try
                {
                    disposableExecutor.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoTestRunner] Executor Dispose exception: {ex}");
                }

                // Disposed executors must never be reused by the next run.
                scenarioExecutor = null;
            }
            executorPhaseActive = false;

            // Copy terminal evidence into activeCaseReport before releasing collectors
            if (activeCaseReport != null)
            {
                activeCaseReport.failures.Clear();
                activeCaseReport.failures.AddRange(context.Failures);
                activeCaseReport.failureCount = context.Failures.Count;

                activeCaseReport.logs.Clear();
                activeCaseReport.logs.AddRange(context.Logs);

                if (activeCaseReport.finalSnapshot == null && context.LatestSnapshot != null)
                    activeCaseReport.finalSnapshot = context.LatestSnapshot;

                if (stopRequested || terminalStatus == AutoTestStatus.Stopped)
                {
                    activeCaseReport.status = AutoTestStatus.Stopped;
                }
                else if (context.HasFatalFailure || context.Failures.Count > 0)
                {
                    activeCaseReport.status = AutoTestStatus.Failed;
                }
                else if (terminalStatus == AutoTestStatus.Failed || terminalStatus == AutoTestStatus.Error)
                {
                    activeCaseReport.status = terminalStatus;
                }
                else if (activeTestCase != null)
                {
                    activeCaseReport.status = BuildCaseStatus(activeTestCase, activeCaseReport.finalSnapshot, context);
                }

                if (stopRequested && activeCaseReport.status == AutoTestStatus.Passed)
                    activeCaseReport.status = AutoTestStatus.Stopped;

                if (lastReport != null && !lastReport.cases.Contains(activeCaseReport))
                    lastReport.cases.Add(activeCaseReport);
            }

            Time.timeScale = previousTimeScale;
            logCollector.Stop();
            eventCollector.Stop();

            if (lastReport == null)
                lastReport = AutoTestReport.Create(currentRunId ?? AutoTestFilePathUtility.CreateTimestampRunId(), activeSuite);

            lastReport.finishedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastReport.durationSeconds = Time.realtimeSinceStartup - suiteStartTime;

            if (stopRequested || terminalStatus == AutoTestStatus.Stopped)
            {
                lastReport.status = AutoTestStatus.Stopped;
            }
            else if (terminalStatus == AutoTestStatus.Error)
            {
                lastReport.status = AutoTestStatus.Error;
            }
            else if (terminalStatus == AutoTestStatus.Failed)
            {
                lastReport.status = AutoTestStatus.Failed;
            }
            else
            {
                lastReport.status = ComputeSuiteStatus(lastReport);
            }

            if (stopRequested && lastReport.status == AutoTestStatus.Passed)
                lastReport.status = AutoTestStatus.Stopped;

            context.Status = lastReport.status;
            lastReport.summary = AutoTestReportSummary.FromCases(lastReport.cases);

            if (activeSuite != null && activeSuite.exportReportAfterRun && !hasExportedReport)
            {
                hasExportedReport = true;
                try
                {
                    string reportFolder = AutoTestFilePathUtility.GetReportFolder();
                    AutoTestJsonExporter.Export(lastReport, reportFolder, activeSuite.reportFileNamePrefix);
                    AutoTestMarkdownExporter.Export(lastReport, reportFolder, activeSuite.reportFileNamePrefix);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoTestRunner] Report export failed: {ex}");
                    context.AddFailure(AutoTestFailure.Create(
                        "Reporting",
                        "REPORT_EXPORT_FAILED",
                        $"Report export failed: {ex.Message}",
                        AutoTestAssertionSeverity.Failure,
                        context,
                        ex.ToString()));
                }
            }

            RunCompleted?.Invoke(lastReport);

            CurrentSuite = null;
            CurrentCaseIndex = -1;
            isRunActive = false;

            if (destroySuiteAfterRun && activeSuite != null)
            {
                Destroy(activeSuite);
                activeSuite = null;
            }

            if (logVerbose)
                Debug.Log("[AutoTestRunner] Suite completed: " + lastReport.status);

            MaybeStopPlayMode();
        }

        private IEnumerator RunSuiteRoutine(AutoTestSuiteData targetSuite, bool destroySuiteAfter = false)
        {
            CurrentSuite = targetSuite;
            CurrentCaseIndex = -1;

            if (logVerbose)
                Debug.Log("[AutoTestRunner] Run suite: " + targetSuite.displayName);

            yield return AutoTestBootstrapper.EnsureGameBooted(bootTimeoutSeconds);
            if (!AutoTestBootstrapper.IsGameplayReady)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Boot",
                    "GAMEPLAY_NEVER_READY",
                    "Aborting run: gameplay never became ready within timeout.",
                    AutoTestAssertionSeverity.Fatal,
                    context));
                Debug.LogError("[AutoTestRunner] Aborting run: gameplay never became ready.");
                FinalizeRun(AutoTestStatus.Error, "GameplayNeverReady");
                yield break;
            }

            for (int caseIndex = 0; caseIndex < targetSuite.testCases.Count; caseIndex++)
            {
                if (stopRequested)
                    break;

                AutoTestCaseData testCase = targetSuite.testCases[caseIndex];
                if (testCase == null)
                    continue;

                CurrentCaseIndex = caseIndex;

                yield return RunCaseRoutine(targetSuite, testCase, currentRunId);

                if (stopRequested)
                    break;

                if (activeCaseReport != null && activeCaseReport.status == AutoTestStatus.Failed && targetSuite.stopSuiteOnFirstFailure)
                    break;
            }

            if (!isFinalized)
            {
                FinalizeRun(AutoTestStatus.Passed, "SuiteCompleted");
            }
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
            string runId)
        {
            context.Begin(runId, targetSuite != null ? targetSuite.suiteId : string.Empty, testCase);
            context.Events = eventCollector;
            activeTestCase = testCase;
            activeCaseReport = AutoTestCaseReport.Create(testCase);
            float caseStart = Time.realtimeSinceStartup;

            if (!AutoTestCapabilityRegistry.TryEvaluate(testCase.requiredCapabilities, out string capabilityDiagnostic))
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Setup",
                    "AUTOTEST_CAPABILITY_UNAVAILABLE",
                    capabilityDiagnostic,
                    AutoTestAssertionSeverity.Fatal,
                    context));
                activeCaseReport.durationSeconds = 0f;
                activeCaseReport.status = AutoTestStatus.Failed;
                activeCaseReport.failureCount = context.Failures.Count;
                activeCaseReport.failures.AddRange(context.Failures);
                activeCaseReport.logs.AddRange(context.Logs);
                if (!lastReport.cases.Contains(activeCaseReport))
                    lastReport.cases.Add(activeCaseReport);
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
                activeCaseReport.durationSeconds = 0f;
                activeCaseReport.status = AutoTestStatus.Failed;
                activeCaseReport.failureCount = context.Failures.Count;
                activeCaseReport.failures.AddRange(context.Failures);
                activeCaseReport.logs.AddRange(context.Logs);
                if (!lastReport.cases.Contains(activeCaseReport))
                    lastReport.cases.Add(activeCaseReport);
                yield break;
            }

            try
            {
                scenarioExecutor.ResetRuntimeState();
            }
            catch (Exception ex)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Setup",
                    "AutoTestRunner",
                    $"scenarioExecutor.ResetRuntimeState exception: {ex.Message}",
                    AutoTestAssertionSeverity.Fatal,
                    context,
                    ex.ToString()));
            }

            logCollector.Start(context);
            eventCollector.Clear();
            eventCollector.Start();

            float savedTimeScale = Time.timeScale;
            Time.timeScale = testCase.timeScale;
            executorPhaseActive = true;

            float caseDeadline = Time.realtimeSinceStartup + testCase.timeoutSeconds;
            float nextSnapshotAt = 0f;

            // Phase 1: Prepare (supervised)
            if (!stopRequested && !context.HasFatalFailure)
            {
                IEnumerator prepareRoutine = null;
                try
                {
                    prepareRoutine = scenarioExecutor.Prepare(testCase, context);
                }
                catch (Exception ex)
                {
                    context.AddFailure(AutoTestFailure.Create(
                        "Prepare",
                        "AutoTestRunner",
                        $"scenarioExecutor.Prepare exception: {ex.Message}",
                        AutoTestAssertionSeverity.Fatal,
                        context,
                        ex.ToString()));
                }

                if (prepareRoutine != null)
                {
                    activeSupervisor = new AutoTestRoutineSupervisor(
                        prepareRoutine,
                        deadlineRealtime: caseDeadline,
                        isCancelled: () => stopRequested,
                        context: context,
                        phaseName: "Prepare");

                    while (activeSupervisor.MoveNext())
                    {
                        if (stopRequested)
                            break;
                        yield return activeSupervisor.Current;
                    }

                    activeSupervisor.Dispose();
                    activeSupervisor = null;
                }
            }

            // Phase 2: Run (supervised)
            if (!stopRequested && !context.HasFatalFailure)
            {
                IEnumerator runScenarioRoutine = null;
                try
                {
                    runScenarioRoutine = scenarioExecutor.Run(testCase, context);
                }
                catch (Exception ex)
                {
                    context.AddFailure(AutoTestFailure.Create(
                        "Run",
                        "AutoTestRunner",
                        $"scenarioExecutor.Run exception: {ex.Message}",
                        AutoTestAssertionSeverity.Fatal,
                        context,
                        ex.ToString()));
                }

                if (runScenarioRoutine != null)
                {
                    activeSupervisor = new AutoTestRoutineSupervisor(
                        runScenarioRoutine,
                        deadlineRealtime: caseDeadline,
                        isCancelled: () => stopRequested,
                        context: context,
                        phaseName: "Run");

                    while (activeSupervisor.MoveNext())
                    {
                        if (stopRequested)
                            break;

                        float elapsed = context.ElapsedSeconds;
                        if (elapsed >= nextSnapshotAt)
                        {
                            SafeBuildAndEvaluateSnapshot(testCase, ref nextSnapshotAt, elapsed);
                        }

                        if (context.HasFatalFailure && testCase.failFast)
                            break;

                        yield return activeSupervisor.Current;
                    }

                    activeSupervisor.Dispose();
                    activeSupervisor = null;
                }
            }

            // Phase 3: Assertion wait loop
            while (!stopRequested && !context.HasFatalFailure)
            {
                float now = Time.realtimeSinceStartup;
                if (now >= caseDeadline)
                {
                    context.AddFailure(AutoTestFailure.Create(
                        "Timeout",
                        "AUTOTEST_DEADLINE_EXCEEDED",
                        $"Test case '{testCase.testId}' exceeded timeout of {testCase.timeoutSeconds:F2}s.",
                        AutoTestAssertionSeverity.Fatal,
                        context));
                    break;
                }

                float elapsed = context.ElapsedSeconds;
                if (elapsed >= nextSnapshotAt)
                {
                    SafeBuildAndEvaluateSnapshot(testCase, ref nextSnapshotAt, elapsed);
                }

                if (context.HasFatalFailure && testCase.failFast)
                    break;

                if (IsPassConditionMet(testCase, context.LatestSnapshot))
                    break;

                yield return null;
            }

            // Phase 4: Final snapshot & assertion evaluation
            RuntimeSnapshot finalSnapshot = null;
            try
            {
                finalSnapshot = snapshotBuilder.Build(context);
                context.LatestSnapshot = finalSnapshot;
                EvaluateAssertions(finalSnapshot, testCase);
            }
            catch (Exception ex)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Assertion",
                    "SnapshotBuilder",
                    $"Final snapshot evaluation exception: {ex.Message}",
                    AutoTestAssertionSeverity.Fatal,
                    context,
                    ex.ToString()));
            }

            // Phase 5: Cleanup (supervised with separate bound)
            float cleanupDeadline = Time.realtimeSinceStartup + Mathf.Max(5f, Mathf.Min(15f, testCase.timeoutSeconds));
            IEnumerator cleanupRoutine = null;
            try
            {
                cleanupRoutine = scenarioExecutor.Cleanup(testCase, context);
            }
            catch (Exception ex)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Cleanup",
                    "AutoTestRunner",
                    $"scenarioExecutor.Cleanup exception: {ex.Message}",
                    AutoTestAssertionSeverity.Failure,
                    context,
                    ex.ToString()));
            }

            if (cleanupRoutine != null)
            {
                activeSupervisor = new AutoTestRoutineSupervisor(
                    cleanupRoutine,
                    deadlineRealtime: cleanupDeadline,
                    isCancelled: null, // Cleanup must run even after a stop request; bounded by cleanupDeadline.
                    context: context,
                    phaseName: "Cleanup");

                while (activeSupervisor.MoveNext())
                    yield return activeSupervisor.Current;

                activeSupervisor.Dispose();
                activeSupervisor = null;
            }

            executorPhaseActive = false;
            Time.timeScale = savedTimeScale;
            logCollector.Stop();
            eventCollector.Stop();

            activeCaseReport.durationSeconds = Time.realtimeSinceStartup - caseStart;
            activeCaseReport.status = BuildCaseStatus(testCase, finalSnapshot, context);
            if (stopRequested && activeCaseReport.status == AutoTestStatus.Passed)
                activeCaseReport.status = AutoTestStatus.Stopped;

            activeCaseReport.failureCount = context.Failures.Count;
            activeCaseReport.failures.Clear();
            activeCaseReport.failures.AddRange(context.Failures);
            activeCaseReport.logs.Clear();
            activeCaseReport.logs.AddRange(context.Logs);
            activeCaseReport.finalSnapshot = finalSnapshot;

            if (!lastReport.cases.Contains(activeCaseReport))
                lastReport.cases.Add(activeCaseReport);
        }

        private void SafeBuildAndEvaluateSnapshot(AutoTestCaseData testCase, ref float nextSnapshotAt, float elapsed)
        {
            try
            {
                RuntimeSnapshot snapshot = snapshotBuilder.Build(context);
                context.LatestSnapshot = snapshot;
                EvaluateAssertions(snapshot, testCase);
            }
            catch (Exception ex)
            {
                context.AddFailure(AutoTestFailure.Create(
                    "Assertion",
                    "SnapshotBuilder",
                    $"Snapshot evaluation exception: {ex.Message}",
                    AutoTestAssertionSeverity.Fatal,
                    context,
                    ex.ToString()));
            }
            nextSnapshotAt = elapsed + Mathf.Max(0.05f, testCase.snapshotIntervalSeconds);
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
