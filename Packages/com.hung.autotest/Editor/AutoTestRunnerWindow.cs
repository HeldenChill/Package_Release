#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hung.AutoTest.Editor
{
    /// <summary>
    /// Live AutoTest dashboard (DamageStatistics window style).
    /// Passive observer: polls the scene AutoTestRunner every editor Update.
    /// The CLI path (AutoTestCliRunner) never touches this window.
    /// </summary>
    public sealed class AutoTestRunnerWindow : EditorWindow
    {
        private AutoTestRunner runner;
        private AutoTestSuiteData suite;
        private AutoTestCaseData singleCase;

        private Vector2 leftScroll;
        private Vector2 centerScroll;
        private Vector2 rightScroll;

        private bool autoRefresh = true;
        private bool sawRunning;

        private bool showAssertions = true;
        private bool showFailures = true;
        private bool showLogs = true;
        private readonly HashSet<int> expandedFailures = new HashSet<int>();

        private const float ToolbarHeight = 22f;
        private const float FieldsHeight = 24f;
        private const float StatusHeight = 20f;
        private const float RowH = 22f;
        private const int LogTailCount = 10;

        // Resizable panels.
        private const float MinCenterWidth = 600f;
        private const float MinPanelWidth = 300f;
        private const float SplitterW = 4f;
        private float leftWidth = 260f;
        private float rightWidth = 340f;
        private int draggingSplitter; // 0 none, 1 left, 2 right

        // "Find/Create Runner" pressed in edit mode → enter play, then find/create.
        // SessionState survives the play-mode domain reload.
        private const string PendingRunnerKey = "AutoTestRunnerWindow.PendingFindRunner";

        private static readonly Color PanelBg = new Color(0.16f, 0.16f, 0.16f);
        private static readonly Color CardBg = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color PassCol = new Color(0.2f, 0.45f, 0.25f);
        private static readonly Color FailCol = new Color(0.5f, 0.2f, 0.2f);
        private static readonly Color RunCol = new Color(0.2f, 0.4f, 0.6f);
        private static readonly Color WarnCol = new Color(0.5f, 0.4f, 0.15f);
        private static readonly Color IdleCol = new Color(0.3f, 0.3f, 0.3f);

        [MenuItem("Tools/Testing/Auto Test Runner")]
        public static void Open()
        {
            GetWindow<AutoTestRunnerWindow>("Auto Test Runner");
        }

        private void OnEnable()
        {
            // Must fit MinPanelWidth * 2 + MinCenterWidth + both splitters.
            minSize = new Vector2(MinPanelWidth * 2 + MinCenterWidth + SplitterW * 2, 440f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            // Deferred Find/Create requested from edit mode — runs once play mode is up.
            if (SessionState.GetBool(PendingRunnerKey, false))
            {
                SessionState.EraseBool(PendingRunnerKey);
                FindOrCreateRunner();
                Repaint();
            }

            if (!autoRefresh)
                return;

            if (runner == null)
                runner = FindRunner();

            if (runner == null)
                return;

            if (runner.Status == AutoTestStatus.Running)
            {
                if (!sawRunning)
                    expandedFailures.Clear();
                sawRunning = true;
                Repaint();
            }
            else if (sawRunning)
            {
                // Run just finished (out of cases / stopped / error).
                sawRunning = false;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (runner == null)
                runner = FindRunner();

            DrawToolbar(new Rect(0, 0, position.width, ToolbarHeight));
            DrawFieldsRow(new Rect(0, ToolbarHeight, position.width, FieldsHeight));
            DrawStatusStrip(new Rect(0, ToolbarHeight + FieldsHeight, position.width, StatusHeight));

            float bodyY = ToolbarHeight + FieldsHeight + StatusHeight;
            Rect body = new Rect(0, bodyY, position.width, position.height - bodyY);
            DrawBody(body);
        }

        // ---------- Toolbar ----------

        private void DrawToolbar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);
            using (new GUILayout.AreaScope(rect))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find/Create Runner", EditorStyles.toolbarButton, GUILayout.Width(130)))
                {
                    if (Application.isPlaying)
                    {
                        FindOrCreateRunner();
                    }
                    else
                    {
                        // Only create/find after play mode is actually running.
                        SessionState.SetBool(PendingRunnerKey, true);
                        Debug.Log("[AutoTestWindow] Entering play mode — runner will be found/created once playing.");
                        EditorApplication.EnterPlaymode();
                    }
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying || runner == null))
                {
                    using (new EditorGUI.DisabledScope(suite == null))
                    {
                        if (GUILayout.Button("▶ Run Suite", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        {
                            runner.SetSuite(suite);
                            runner.RunConfiguredSuite();
                        }
                    }

                    using (new EditorGUI.DisabledScope(singleCase == null))
                    {
                        if (GUILayout.Button("▶ Run Case", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        {
                            runner.SetSingleCase(singleCase);
                            runner.RunConfiguredSingleCase();
                        }
                    }

                    if (GUILayout.Button("■ Stop", EditorStyles.toolbarButton, GUILayout.Width(55)))
                        runner.StopCurrentRun();
                }

                GUILayout.FlexibleSpace();

                autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(45));

                if (GUILayout.Button("Reports", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    EditorUtility.RevealInFinder(AutoTestFilePathUtility.GetReportFolder());
            }
        }

        private void DrawFieldsRow(Rect rect)
        {
            using (new GUILayout.AreaScope(rect))
            using (new EditorGUILayout.HorizontalScope())
            {
                suite = (AutoTestSuiteData)EditorGUILayout.ObjectField("Suite", suite, typeof(AutoTestSuiteData), false);
                singleCase = (AutoTestCaseData)EditorGUILayout.ObjectField("Single Case", singleCase, typeof(AutoTestCaseData), false);
            }
        }

        // ---------- Status strip (colored, DamageStats style) ----------

        private void DrawStatusStrip(Rect rect)
        {
            Color bg;
            string label;

            if (!Application.isPlaying)
            {
                bg = IdleCol; label = "Edit Mode — enter Play Mode to run tests. One press boots CryptoLoader → LoadStart → GameScene automatically.";
            }
            else if (runner == null)
            {
                bg = FailCol; label = "No AutoTestRunner. Click Find/Create Runner.";
            }
            else if (runner.Status == AutoTestStatus.Running)
            {
                bg = RunCol;
                AutoTestContext ctx = runner.Context;
                string caseName = ctx != null && ctx.CurrentCase != null ? ctx.CurrentCase.displayName : "…";
                string counter = runner.CurrentCaseIndex >= 0 && runner.TotalCases > 0
                    ? " (" + (runner.CurrentCaseIndex + 1) + "/" + runner.TotalCases + ")"
                    : string.Empty;
                float timeout = ctx != null && ctx.CurrentCase != null ? ctx.CurrentCase.timeoutSeconds : 0f;
                label = "● Running: " + caseName + counter
                    + string.Format("   {0:0.0}s / {1:0.0}s", ctx != null ? ctx.ElapsedSeconds : 0f, timeout);
            }
            else if (runner.LastReport != null)
            {
                bool passed = runner.LastReport.status == AutoTestStatus.Passed;
                bg = passed ? PassCol : runner.LastReport.status == AutoTestStatus.Failed ? FailCol : WarnCol;
                label = "Last Run: " + runner.LastReport.runId + " — " + runner.LastReport.status
                    + string.Format("   ({0:0.0}s)", runner.LastReport.durationSeconds);
            }
            else
            {
                bg = WarnCol; label = "Idle — pick a Suite or Single Case and press Run.";
            }

            EditorGUI.DrawRect(rect, bg);
            GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height), label, EditorStyles.boldLabel);
        }

        // ---------- Body ----------

        private void DrawBody(Rect rect)
        {
            // Clamp so center keeps at least MinCenterWidth.
            float maxLeft = rect.width - rightWidth - MinCenterWidth - SplitterW * 2;
            leftWidth = Mathf.Clamp(leftWidth, MinPanelWidth, Mathf.Max(MinPanelWidth, maxLeft));
            float maxRight = rect.width - leftWidth - MinCenterWidth - SplitterW * 2;
            rightWidth = Mathf.Clamp(rightWidth, MinPanelWidth, Mathf.Max(MinPanelWidth, maxRight));

            Rect left = new Rect(rect.x, rect.y, leftWidth, rect.height);
            Rect leftSplit = new Rect(left.xMax, rect.y, SplitterW, rect.height);
            Rect right = new Rect(rect.xMax - rightWidth, rect.y, rightWidth, rect.height);
            Rect rightSplit = new Rect(right.x - SplitterW, rect.y, SplitterW, rect.height);
            Rect center = new Rect(leftSplit.xMax, rect.y, rightSplit.x - leftSplit.xMax, rect.height);

            HandleSplitter(leftSplit, 1, rect);
            HandleSplitter(rightSplit, 2, rect);

            EditorGUI.DrawRect(left, PanelBg);
            EditorGUI.DrawRect(right, PanelBg);
            EditorGUI.DrawRect(leftSplit, new Color(0.1f, 0.1f, 0.1f));
            EditorGUI.DrawRect(rightSplit, new Color(0.1f, 0.1f, 0.1f));

            using (new GUILayout.AreaScope(new Rect(left.x + 6, left.y + 6, left.width - 12, left.height - 12)))
                DrawCaseList();

            using (new GUILayout.AreaScope(new Rect(center.x + 8, center.y + 6, center.width - 16, center.height - 12)))
                DrawLiveGameState();

            using (new GUILayout.AreaScope(new Rect(right.x + 6, right.y + 6, right.width - 12, right.height - 12)))
                DrawAssertionsAndLogs();
        }

        private void HandleSplitter(Rect splitter, int id, Rect body)
        {
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (splitter.Contains(e.mousePosition))
                    {
                        draggingSplitter = id;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (draggingSplitter == id)
                    {
                        if (id == 1)
                            leftWidth = e.mousePosition.x - body.x;
                        else
                            rightWidth = body.xMax - e.mousePosition.x;
                        e.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (draggingSplitter == id)
                    {
                        draggingSplitter = 0;
                        e.Use();
                    }
                    break;
            }
        }

        // ---------- Left: case list ----------

        private void DrawCaseList()
        {
            EditorGUILayout.LabelField("Cases", EditorStyles.boldLabel);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

            AutoTestReport report = runner != null ? runner.LastReport : null;
            AutoTestSuiteData runningSuite = runner != null ? runner.CurrentSuite : null;
            bool running = runner != null && runner.Status == AutoTestStatus.Running;
            int rowIndex = 0;

            HashSet<string> finishedIds = new HashSet<string>();
            if (report != null)
            {
                foreach (AutoTestCaseReport caseReport in report.cases)
                {
                    if (caseReport == null)
                        continue;
                    finishedIds.Add(caseReport.testId);
                    bool passed = caseReport.status == AutoTestStatus.Passed;
                    DrawCaseRow(rowIndex++,
                        caseReport.displayName,
                        passed ? "TestPassed" : "TestFailed",
                        string.Format("{0:0.0}s", caseReport.durationSeconds),
                        passed ? PassCol : FailCol);
                }
            }

            if (running && runningSuite != null)
            {
                for (int i = 0; i < runningSuite.testCases.Count; i++)
                {
                    AutoTestCaseData testCase = runningSuite.testCases[i];
                    if (testCase == null || finishedIds.Contains(testCase.testId))
                        continue;

                    bool isCurrent = i == runner.CurrentCaseIndex;
                    DrawCaseRow(rowIndex++,
                        testCase.displayName,
                        isCurrent ? "PlayButton" : "TestNormal",
                        isCurrent ? "running" : "pending",
                        isCurrent ? RunCol : Color.clear);
                }
            }

            if (rowIndex == 0)
                EditorGUILayout.LabelField("No cases yet — run a suite or case.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawCaseRow(int index, string name, string stateIcon, string trailing, Color accent)
        {
            const float StateWidth = 74f;  // icon + time, right-aligned block

            Rect row = GUILayoutUtility.GetRect(100, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(row, RowBg(index));
            if (accent != Color.clear)
                EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), accent);

            // Name column — clipped, never overlaps the state block.
            float nameX = row.x + 8;
            float nameW = row.width - StateWidth - 12;
            GUI.Label(new Rect(nameX, row.y + 2, nameW, 18), name, EditorStyles.label);

            // State column — icon then time, right-aligned.
            Rect stateRect = new Rect(row.xMax - StateWidth, row.y + 2, StateWidth, 18);
            GUIContent stateContent = EditorGUIUtility.IconContent(stateIcon);
            if (stateContent != null && stateContent.image != null)
                GUI.Label(new Rect(stateRect.x, stateRect.y, 18, 16),
                    new GUIContent(stateContent.image), EditorStyles.label);
            GUI.Label(new Rect(stateRect.x + 20, stateRect.y + 1, stateRect.width - 20, 16),
                trailing, EditorStyles.miniLabel);
        }

        // ---------- Center: live game state ----------

        private void DrawLiveGameState()
        {
            centerScroll = EditorGUILayout.BeginScrollView(centerScroll);

            RuntimeSnapshot snapshot = runner != null && runner.Context != null ? runner.Context.LatestSnapshot : null;
            if (snapshot == null)
            {
                EditorGUILayout.HelpBox("No snapshot yet. Live game state appears here while a case runs.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            CombatSnapshot combat = snapshot.combat;

            EditorGUILayout.LabelField("Live Game State", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Card("PHASE", ShortPhase(combat.gameplayPhase), "gameplay", RunCol);
                Card("WAVE", combat.waveIndex.ToString(), combat.isRunning ? "running" : "idle", new Color(0.5f, 0.45f, 0.2f));
                Card("ENEMIES", combat.enemiesAlive + " alive",
                    combat.enemiesSpawned + "/" + combat.enemiesExpected + " spawned, " + combat.enemiesKilled + " killed",
                    new Color(0.6f, 0.3f, 0.25f));
                Card("DAMAGE", combat.totalDamage.ToString("0"), string.Format("DPS {0:0.0}", combat.dps), new Color(0.2f, 0.55f, 0.4f));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Pets", EditorStyles.boldLabel);

            if (snapshot.pets.Count == 0)
            {
                EditorGUILayout.LabelField("No pets on grid.", EditorStyles.miniLabel);
            }
            else
            {
                // Columns
                const float xName = 8f, xFsm = 130f, xTarget = 215f, xDist = 330f, xRange = 380f, xAtk = 440f;

                Rect header = GUILayoutUtility.GetRect(100, 18, GUILayout.ExpandWidth(true));
                GUI.Label(new Rect(header.x + xName, header.y, 118, 16), "Pet", EditorStyles.miniBoldLabel);
                GUI.Label(new Rect(header.x + xFsm, header.y, 80, 16), "FSM", EditorStyles.miniBoldLabel);
                GUI.Label(new Rect(header.x + xTarget, header.y, 110, 16), "Target", EditorStyles.miniBoldLabel);
                GUI.Label(new Rect(header.x + xDist, header.y, 45, 16), "Dist", EditorStyles.miniBoldLabel);
                GUI.Label(new Rect(header.x + xRange, header.y, 55, 16), "InRange", EditorStyles.miniBoldLabel);
                GUI.Label(new Rect(header.x + xAtk, header.y, 80, 16), "ATK / SPD", EditorStyles.miniBoldLabel);

                for (int i = 0; i < snapshot.pets.Count; i++)
                {
                    PetSnapshot pet = snapshot.pets[i];
                    Rect row = GUILayoutUtility.GetRect(100, RowH, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(row, RowBg(i));

                    bool attacking = pet.fsmState == "ATTACK";
                    if (attacking)
                        EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), PassCol);

                    float ty = row.y + 3f;
                    GUI.Label(new Rect(row.x + xName, ty, 118, 16), pet.petId + (pet.isAscended ? " ★" : ""), EditorStyles.boldLabel);
                    GUI.Label(new Rect(row.x + xFsm, ty, 80, 16), pet.fsmState ?? "—", attacking ? EditorStyles.boldLabel : EditorStyles.label);
                    GUI.Label(new Rect(row.x + xTarget, ty, 110, 16), pet.hasTarget ? pet.targetName : "—", EditorStyles.label);
                    GUI.Label(new Rect(row.x + xDist, ty, 45, 16), pet.targetDistance >= 0f ? pet.targetDistance.ToString("0.0") : "—", EditorStyles.label);
                    GUI.Label(new Rect(row.x + xRange, ty, 55, 16), pet.isOutOfRange ? "no" : "yes", EditorStyles.label);
                    GUI.Label(new Rect(row.x + xAtk, ty, 90, 16), string.Format("{0:0} / {1:0.00}", pet.atk, pet.atkSpd), EditorStyles.label);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void Card(string label, string value, string sub, Color accent)
        {
            Rect r = GUILayoutUtility.GetRect(120, 56, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, CardBg);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 4, r.height), accent);
            GUI.Label(new Rect(r.x + 10, r.y + 4, r.width - 14, 16), label, EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(r.x + 10, r.y + 18, r.width - 14, 22), value, EditorStyles.boldLabel);
            GUI.Label(new Rect(r.x + 10, r.y + 38, r.width - 14, 14), sub, EditorStyles.miniLabel);
        }

        private static string ShortPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase))
                return "—";
            return phase.StartsWith("PHASE_") ? phase.Substring(6) : phase;
        }

        // ---------- Right: assertions + failures/logs ----------

        private void DrawAssertionsAndLogs()
        {
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            AutoTestContext ctx = runner != null ? runner.Context : null;

            IReadOnlyList<AutoTestRunner.AssertionStateView> assertions =
                runner != null ? runner.AssertionStates : null;
            int assertionCount = assertions != null ? assertions.Count : 0;
            int failureCount = ctx != null ? ctx.Failures.Count : 0;
            int logCount = ctx != null ? ctx.Logs.Count : 0;

            showAssertions = SectionHeader("Assertions (" + assertionCount + ")", showAssertions, RunCol);
            if (showAssertions)
                DrawAssertionsSection(assertions);

            EditorGUILayout.Space(6);
            showFailures = SectionHeader("Failures (" + failureCount + ")", showFailures,
                failureCount > 0 ? FailCol : IdleCol);
            if (showFailures)
                DrawFailuresSection(ctx);

            EditorGUILayout.Space(6);
            showLogs = SectionHeader("Logs (" + logCount + ")", showLogs, WarnCol);
            if (showLogs)
                DrawLogsSection(ctx);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Full-width colored, clickable section header. Responsive — spans whatever the right bar is resized to.</summary>
        private static bool SectionHeader(string title, bool open, Color accent)
        {
            Rect r = GUILayoutUtility.GetRect(100, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, accent);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                open = !open;
            GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 12, 16),
                (open ? "▼ " : "▶ ") + title, EditorStyles.boldLabel);
            return open;
        }

        private void DrawAssertionsSection(IReadOnlyList<AutoTestRunner.AssertionStateView> assertions)
        {
            if (assertions == null || assertions.Count == 0)
            {
                EditorGUILayout.LabelField("No assertions.", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < assertions.Count; i++)
            {
                AutoTestRunner.AssertionStateView view = assertions[i];
                string icon = !view.Evaluated ? "TestNormal"
                    : view.LastStatus == AutoTestAssertionStatus.Failed ? "TestFailed"
                    : view.LastStatus == AutoTestAssertionStatus.Passed ? "TestPassed"
                    : "TestInconclusive";

                Rect row = GUILayoutUtility.GetRect(100, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(row, RowBg(i));
                if (view.Evaluated && view.LastStatus == AutoTestAssertionStatus.Failed)
                    EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), FailCol);
                else if (view.Evaluated && view.LastStatus == AutoTestAssertionStatus.Passed)
                    EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), PassCol);

                GUI.Label(new Rect(row.x + 8, row.y + 2, row.width - 12, 18), IconLabel(icon, view.Id), EditorStyles.label);
            }
        }

        private void DrawFailuresSection(AutoTestContext ctx)
        {
            if (ctx == null || ctx.Failures.Count == 0)
            {
                EditorGUILayout.LabelField("None.", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < ctx.Failures.Count; i++)
            {
                AutoTestFailure failure = ctx.Failures[i];
                if (failure == null)
                    continue;

                bool expanded = expandedFailures.Contains(i);

                // Header row — click to expand/collapse.
                Rect row = GUILayoutUtility.GetRect(100, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(row, RowBg(i));
                EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height),
                    failure.severity == AutoTestAssertionSeverity.Fatal ? FailCol : WarnCol);

                // Whole row is the click target; name flexes, meta stays right-aligned.
                if (GUI.Button(new Rect(row.x + 4, row.y, row.width - 4, row.height), GUIContent.none, GUIStyle.none))
                {
                    if (expanded) expandedFailures.Remove(i);
                    else expandedFailures.Add(i);
                    expanded = !expanded;
                }

                string meta = string.Format("[{0}]  {1:0.0}s  f{2}", failure.severity, failure.elapsed, failure.frame);
                float metaW = EditorStyles.miniLabel.CalcSize(new GUIContent(meta)).x + 4f;
                float nameW = Mathf.Max(40f, row.width - metaW - 20f);

                GUI.Label(new Rect(row.x + 8, row.y + 2, nameW, 18),
                    (expanded ? "▼ " : "▶ ") + failure.assertionId, EditorStyles.label);
                GUI.Label(new Rect(row.xMax - metaW - 4, row.y + 3, metaW, 16), meta, EditorStyles.miniLabel);

                // Message always visible — this is the "what failed".
                EditorGUILayout.LabelField(failure.message, EditorStyles.wordWrappedMiniLabel);

                if (!expanded)
                    continue;

                // Expanded: root-cause context.
                using (new EditorGUI.IndentLevelScope())
                {
                    if (!string.IsNullOrEmpty(failure.category))
                        EditorGUILayout.LabelField("Category: " + failure.category, EditorStyles.miniLabel);

                    EditorGUILayout.LabelField("Game state at failure:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(
                        string.IsNullOrEmpty(failure.contextSummary)
                            ? "No context captured (failure predates snapshot, or old report)."
                            : failure.contextSummary,
                        MessageType.None);

                    string hint = BuildFixHint(failure);
                    if (!string.IsNullOrEmpty(hint))
                    {
                        EditorGUILayout.LabelField("How to fix:", EditorStyles.miniBoldLabel);
                        EditorGUILayout.HelpBox(hint, MessageType.Info);
                    }

                    if (!string.IsNullOrEmpty(failure.stackTrace))
                    {
                        EditorGUILayout.LabelField("Stack:", EditorStyles.miniBoldLabel);
                        EditorGUILayout.LabelField(failure.stackTrace, EditorStyles.wordWrappedMiniLabel);
                    }
                }
                EditorGUILayout.Space(4);
            }
        }

        /// <summary>
        /// Triage hint per assertion type + context signals.
        /// Mirrors the triage table: wrong phase → phase bug; no target + in range → sensor bug;
        /// target + IDLE → FSM bug; detectionZoneCells 0 → init bug.
        /// </summary>
        private static string BuildFixHint(AutoTestFailure failure)
        {
            string id = failure.assertionId ?? string.Empty;
            string ctx = failure.contextSummary ?? string.Empty;
            var hints = new List<string>();

            // Context-signal hints (apply to any assertion).
            if (ctx.Contains("SCENARIO STILL APPLYING"))
                hints.Add("Scenario was still applying — assertion fired too early. Increase delayAfterScenarioRunSeconds on the case, or check TestScenarioModeManager.OnScenarioApplied never fired.");
            if (ctx.Contains("detectionZoneCells=0!"))
                hints.Add("A pet has detectionZoneCells=0 — detection zone never initialized. Check pet placement/grid registration in the scenario (init bug, PetLogicModule / grid).");
            if (ctx.Contains(", target=none") && ctx.Contains("Enemies") && !ctx.Contains(" 0 alive"))
                hints.Add("Pet has no target while enemies are alive — sensor bug. Check target acquisition (detection zone overlap, enemy layer/tag registration).");
            if ((ctx.Contains(": IDLE, target=") && !ctx.Contains("target=none")) )
                hints.Add("Pet holds a target but FSM stays IDLE — FSM transition bug. Check PetLogicModule state transitions from IDLE with valid target.");
            if (ctx.Contains("OUT OF RANGE"))
                hints.Add("Target out of range — check atkRange vs targetDistance; possible range/scale mismatch or pet placed too far in scenario.");

            // Per-assertion-type hints.
            if (id.StartsWith("StatusEffectActive_") || id.StartsWith("StatusStackBehavior_"))
                hints.Add("Status assertion: verify pet's on-hit status template actually applies this tag (mem: pet on-hit status), pet is attacking (FSM=ATTACK in context above), and stack/duration config in the StatusEffect data matches expectation.");
            else if (id.StartsWith("SynergyTriggered_"))
                hints.Add("Synergy did not trigger: both component statuses must be present on the same enemy. Check event counts (statusTagCounts) — if one tag count is 0, that applier is broken, not the synergy system.");
            else if (id.StartsWith("SynergyNotTriggered_"))
                hints.Add("Synergy triggered when it must not: scenario likely has an unintended status applier — check pet loadout and support items in the scenario asset.");
            else if (id.StartsWith("PetStatValue_") || id.StartsWith("PetStatDeltaFromBaseline_"))
                hints.Add("Stat assertion: compare expected vs actual in message. If delta is 0 — buff never applied (check support/skill card application order and scenario delays). If wrong magnitude — check stacking/multiplier math in the stat pipeline.");
            else if (id.StartsWith("SupportAffectsPet_"))
                hints.Add("Support link missing: check support placement adjacency in the scenario and the support's affectedPetIds population logic.");
            else if (id == "WaveStartedAssertion" || id == "WaveNotStuckAssertion")
                hints.Add("Wave issue: check gameplayPhase in context — if not a combat phase, the phase machine is stuck (phase bug), not the spawner. If phase OK but 0 spawned, check Roguelike wave spawn config for this level.");
            else if (id == "EnemySpawnedAssertion")
                hints.Add("No enemies spawned: check wave data for scenario.levelIndex and that the wave actually started (see wave/phase in context).");
            else if (id == "ScenarioStartedAssertion" || id == "ScenarioTimeoutAssertion")
                hints.Add("Scenario failed to start/apply: check TestScenarioModeManager logs and that the scenario asset's levelIndex/pets are valid.");
            else if (id == "NoExceptionLogAssertion")
                hints.Add("Runtime exception during case — the real bug is in the stack of the logged exception (see Logs section), not the test.");
            else if (id == "NoNaNTransformAssertion")
                hints.Add("NaN transform: divide-by-zero or un-normalized direction vector in movement/projectile code. Check the named entity in message.");
            else if (id == "NoInvalidEnemyStateAssertion")
                hints.Add("Invalid enemy state: enemy HP/speed corrupt — check status effect modifiers restoring baseMoveSpeed and damage application order.");
            else if (id == "CombatDamageProgressAssertion" || id == "DamageStatisticsHasEventsAssertion")
                hints.Add("No damage recorded: if pets show ATTACK in context, DamageStatisticsRecorder session is missing (check StartSession call); if not attacking, it's a combat bug upstream — see pet FSM/target hints above.");
            else if (id.StartsWith("PetTargetAcquired_"))
                hints.Add("Sensor bug: pet never targeted despite enemies alive. Check detection zone overlap with enemy path, enemy layer/tag registration, and detectionZoneCells in context.");
            else if (id.StartsWith("PetAttacking_"))
                hints.Add("Pet never attacked. Message says which half: 'holding a target' → FSM transition bug (PetLogicModule); 'no target' → sensor bug upstream.");
            else if (id == "PetDetectionZoneInitializedAssertion")
                hints.Add("Detection zone empty after grace period — pet placement or grid registration failed in scenario setup. Check pet gridPos validity for this level's map.");
            else if (id == "EnemyKilledCountAssertion")
                hints.Add("Enemies damaged but not killed enough: check total damage in context — high damage + 0 kills means kill/death handling broken; low damage means combat throughput issue (see pet hints).");
            else if (id == "AllEnemiesClearedAssertion")
                hints.Add("Wave never cleared: compare spawned/expected/alive in message. Not all spawned → spawner stalled; spawned but alive stuck → surviving enemy unreachable or unkillable (check its gridPos in JSON report).");
            else if (id == "WaveCompletedAssertion")
                hints.Add("Wave completion never registered: if enemies are cleared but wavesCompleted stays 0, the end-of-wave transition is broken (phase machine), not combat.");
            else if (id == "EnemyCountConsistentAssertion")
                hints.Add("Enemy bookkeeping leak: an enemy was destroyed without kill-count update (despawn path bypassing death event) or a kill was double-counted. Grep enemy despawn/death handlers.");
            else if (id.StartsWith("EnemySlowed_"))
                hints.Add("Slow status present but moveSpeed unchanged — the status applies stacks but the movement modifier isn't wired. Check status effect movement modifier application and baseMoveSpeed restore logic.");
            else if (id == "EnemyOverloadTriggeredAssertion")
                hints.Add("Overload never fired: check the shock/overload reaction prerequisites — required stack count on the same enemy, and that the reaction is enabled in the synergy config.");
            else if (id == "TotalDamageAtLeastAssertion")
                hints.Add("Damage below threshold: balance or throughput regression. Check DPS in context; compare against the case's expected loadout (pet levels, support buffs applied?).");
            else if (id.StartsWith("GameplayPhaseReached_"))
                hints.Add("Phase machine stuck — see 'stuck at' phase in message. Check the transition out of that phase (its exit condition never satisfied).");
            else if (id.StartsWith("SupportAlive_"))
                hints.Add("Support died during the case: check enemy targeting rules (should enemies attack supports?) and support hp config in the scenario.");

            return hints.Count > 0 ? string.Join("\n\n", hints.ToArray()) : null;
        }

        private void DrawLogsSection(AutoTestContext ctx)
        {
            if (ctx == null || ctx.Logs.Count == 0)
            {
                EditorGUILayout.LabelField("None.", EditorStyles.miniLabel);
                return;
            }

            int start = Mathf.Max(0, ctx.Logs.Count - LogTailCount);
            for (int i = start; i < ctx.Logs.Count; i++)
            {
                AutoTestLogEntry entry = ctx.Logs[i];
                if (entry == null)
                    continue;

                string icon = entry.type == LogType.Warning ? "console.warnicon.sml"
                    : entry.type == LogType.Log ? "console.infoicon.sml"
                    : "console.erroricon.sml";
                GUILayout.Label(IconLabel(icon, entry.condition), EditorStyles.wordWrappedMiniLabel);
            }
        }

        // ---------- Helpers ----------

        private static Color RowBg(int index)
        {
            return index % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.19f, 0.19f, 0.19f);
        }

        /// <summary>Built-in editor icon + text. Emoji don't render in the editor font.</summary>
        private static GUIContent IconLabel(string iconName, string text)
        {
            GUIContent icon = EditorGUIUtility.IconContent(iconName);
            return new GUIContent(" " + text, icon != null ? icon.image : null);
        }

        private static AutoTestRunner FindRunner()
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindFirstObjectByType<AutoTestRunner>();
#else
            return Object.FindObjectOfType<AutoTestRunner>();
#endif
        }

        private void FindOrCreateRunner()
        {
            runner = FindRunner();
            if (runner != null)
                return;

            GameObject go = new GameObject("AutoTestRunner");
            runner = go.AddComponent<AutoTestRunner>();
            Selection.activeGameObject = go;
        }
    }
}
#endif
