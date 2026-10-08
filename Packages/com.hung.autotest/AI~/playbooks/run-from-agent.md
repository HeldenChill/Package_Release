# Run AutoTest from the agent

The step-by-step recipe behind the `autotest-usage` rule. Everything runs through Unity MCP; nobody presses a button.

## Recipe

1. **Author** the case or suite `.asset` (+ `.meta` with a fresh GUID). Hand-written YAML round-trips cleanly. Write files with absolute paths.
2. **Import:** `execute_code` → `AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)`, then `AssetDatabase.LoadAssetAtPath<AutoTestCaseData>` (or `AutoTestSuiteData`) and assert it is non-null with the expected `assertions.Count` / `scenario`.
3. **Arm, then enter play once.** If `.claude/rules/project-values.md` § `autotest-usage` defines `AUTOTEST_ARM`, run it in one `execute_code` call first. Then `manage_editor play`. Never enter / stop / re-enter between calls.
4. **Trigger in the very next `execute_code` call.** Guard `EditorApplication.isPlaying` (if false, diagnose compilation or play entry; do not build an edit-mode runner). Find or create a GameObject named exactly `AutoTestRunner` with an `AutoTestRunner` component, then `SetSingleCase(case)` + `RunConfiguredSingleCase()` or `SetSuite(suite)` + `RunConfiguredSuite()`. If the project's glue sets `AutoTestBootstrapper.BootKick`, the runner starts the game itself; do not wait for a scene first.
5. **Poll** with a later `execute_code`: `runner.Status` (`Running` → `Passed` / `Failed` / `Stopped`) and `runner.LastReport` (`status`, `cases[i].{status, testId, failureCount, failures[].{assertionId, message}, finalSnapshot}`). A wakeup is fine here — after the trigger, never before it.
6. **Read the report on disk** in `<ProjectRoot>/AutoTestReports/*.json` once play mode has exited (`autoStopPlayModeOnFinish`, default true).

## Anti-pattern: babysitting boot

`manage_editor play` → schedule a wakeup "to wait for boot" → poll `isPlaying` or the scene by hand → repeat. Boot readiness is not something to poll for: creating the `AutoTestRunner` GO and requesting a run is what drives boot forward (`BootKick` fires only once a runner exists and a run is requested). Enter play and trigger in back-to-back calls.

## Diagnosing a red

- Read `case.finalSnapshot` first.
- For state that changes briefly (a GO going active, a stack building), install a temporary `EditorApplication.update` per-frame sampler for N seconds and log an aggregate; one snapshot can miss it.
- **Observe-once assertions must latch success.** The runner re-evaluates every assertion against one final snapshot and latches only Failed ids, so an assertion that passes on transient state must set an `observed` flag on first sighting (reset in `OnTestStarted`) and return Passed afterwards.
- Separate a feature bug from a test-board or assertion bug before editing anything.
