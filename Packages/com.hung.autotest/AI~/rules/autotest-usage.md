# Rule: AutoTest Usage (run it yourself, don't run dry)

**Scope:** Framework (`com.hung.autotest`). Project values: `.claude/rules/project-values.md` § `autotest-usage`.

---

## Policy

When a task needs a gameplay scenario tested in play mode, **use the AutoTest module and run it end-to-end yourself via MCP `execute_code`.** Do not ask the user to press Start, open a window, or drive play mode. Do not narrate "waiting" like a human. Every step is scriptable.

Full recipe: playbook `run-from-agent.md` of `com.hung.autotest` (listed in `.cursor/pkg-ai-index.md`). This rule is the short discipline list. Project-specific steps (arm call, boot chain, case gates) are in `project-values.md` § `autotest-usage`.

### The five hard rules

0. **Arm the play BEFORE `manage_editor play`, if the project defines `AUTOTEST_ARM`.** One `execute_code` call. Without it the game may take its normal auto-start path and a UI-only case starts in the wrong state.

1. **Trigger in the SAME flow as entering play — never schedule a wakeup between "enter play" and "start the run".** The very next `execute_code` call after `manage_editor play` finds or creates the runner GO and calls `RunConfiguredSuite` / `RunConfiguredSingleCase`. Creating the runner and requesting a run is what drives the boot forward (`AutoTestBootstrapper.BootKick` fires only then); never poll for boot readiness first. Schedule a wakeup only AFTER the trigger call, to poll for completion.

2. **One play session, no churn.** Do NOT enter / stop / re-enter play between calls — that drops the runner and yields `Status=Error` with an empty `Running` report. Enter once, trigger, leave alone.

3. **Use the stable runner GO name `AutoTestRunner`** (find-or-create). A differently-named probe GO breaks the window's lifecycle assumptions.

4. **Read results from the disk JSON** in `<ProjectRoot>/AutoTestReports/*.json` (survives play-mode exit), or `runner.LastReport` while still playing. Per-case `finalSnapshot` is the full `RuntimeSnapshot` — inspect it to diagnose a red without a second run.

5. **Diagnose reds with a per-frame sampler, not a single snapshot.** For transient state (a stat briefly dropping, a GO briefly active), install a temporary `EditorApplication.update` sampler for N seconds and aggregate. Separate a *feature bug* from a *test-board / assertion issue* before editing anything.

### Auto-stop on finish

`AutoTestRunner` exits play mode when a suite or single-case run finishes (`autoStopPlayModeOnFinish`, default true, Editor-only, covers the abort path too). Read the report once `isPlaying == false` or once the report has all cases. Set the flag false only for an interactive debug session.

## Why

The recurring failure was setting up a run and then pausing before pressing go — entering play, scheduling a wakeup instead of triggering, then churning play mode on retries. AutoTest is fully agent-drivable: trigger immediately, let it self-boot, read the JSON.
