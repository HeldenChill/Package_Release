# Changelog

## [0.6.0] - 2026-11-14
### Changed
- Player builds no longer contain AutoTest. `Hung.AutoTest` and `Hung.AutoTest.FakeInput` compile only when `UNITY_EDITOR || HUNG_AUTOTEST` is defined; add `HUNG_AUTOTEST` to the scripting defines of evidence or QA player builds that use `AutoTestPlayerEntrypoint` (`-rcScenario`). Before, every shipping player could be driven by a test run and write an evidence file at a command-line path (BUG-261114-7ee0, owner option A). Assemblies referencing `Hung.AutoTest` from runtime code need the same define constraint.
### Fixed
- `FinalizeRun` marks the report and runner status `Error` when report export fails; it used to keep the status computed before export (BUG-261114-1b86).
- `RunCompleted` subscribers run after the runner state reset, each in its own try/catch: a throwing subscriber no longer skips the others, the single-case suite cleanup or the Editor auto-stop (BUG-261114-f6ec).
### Migration
- Game glue or harness asmdefs that reference `Hung.AutoTest` from runtime code must add the define constraint `UNITY_EDITOR || HUNG_AUTOTEST`, or player builds fail to compile. Known: BumpHero `BumpHero.AutoTest.Glue`, PetVsMonster `PetVsMonster.RuntimeConfidence` (Stage B builds also define `HUNG_AUTOTEST`).

## [0.5.1] - 2026-11-08

### Added
- Auto Test Runner window toolbar "Auto Stop" toggle (saved in EditorPrefs, default on) controls whether play mode exits after a run finishes. Backed by new public `AutoTestRunner.AutoStopPlayModeOnFinish`.

## [0.5.0] - 2026-11-08

### Changed
- `StopCurrentRun()` is now graceful while a case is executing: the status stays `Running` for a few frames while executor `Cleanup` runs, then becomes `Stopped`. Callers that assumed a synchronous `Stopped` must wait for `RunCompleted` or a non-`Running` status.

### Fixed
- Fixed uncontained exceptions in `IAutoTestScenarioExecutor.Prepare`, `Run`, and `Cleanup` stranding `AutoTestRunner` in `Running` status without generating terminal case or suite reports.
- Fixed `AutoTestRunner` yielding scenario `Prepare` and `Run` before the assertion/timeout loop: timeouts now enforce a real-time deadline across Prepare, Run, and assertion wait, including when `Time.timeScale` is 0.
- Fixed `StopCurrentRun()` and `OnDisable()` bypassing report finalization, executor cleanup, and `Time.timeScale` restoration: repeated stops, component disable, and play exit now converge on a unified idempotent finalizer.
- Fixed unbounded cleanup execution hanging report generation: cleanup is now supervised under its own separate real-time bound.
- Fixed exceptions during snapshot building and assertion evaluation escaping final report generation.

### Added
- Added internal `AutoTestRoutineSupervisor` to traverse root and nested `IEnumerator` stacks, enforce real-time deadlines, monitor cancellation predicates, evaluate supported yield instructions (`null`, nested `IEnumerator`, `CustomYieldInstruction`, `AsyncOperation`, `WaitForSeconds`, `WaitForSecondsRealtime`; yielding a raw `Coroutine` handle is rejected with an explicit diagnostic because it cannot be supervised), ensure exactly-once enumerator disposal, and preserve original fatal failure diagnostics while recording secondary cleanup/disposal exceptions as evidence.
- Added optional `IDisposable` lifecycle interface support for `IAutoTestScenarioExecutor`: executors implementing `IDisposable` receive a synchronous fallback call when the run finalizes; a disposed executor is dropped and recreated by the next run. `StopCurrentRun()` lets supervised executor `Cleanup` run (watchdog force-finalizes after 20s); only `OnDisable` finalizes immediately. Legacy non-`IDisposable` executors remain fully compatible, but cannot guarantee asynchronous cleanup completion on sudden play-mode exit or component disable.

## [0.4.4] - 2026-10-08

### Documentation

- Add `AI~/playbooks/adopt.md` with reusable host adoption steps and evidence boundaries (T4). No runtime behavior or dependency changes.


## [0.4.3] - 2026-10-08
- Added `AI~/`: `rules/autotest-usage.md` and `playbooks/run-from-agent.md`, installed into projects by claude-unity-shared sync as `pkg-autotest-*` (Unity ignores `AI~`).

## [0.4.2] - 2026-09-29
- Fixed `AutoTestAssertionRegistry` string ids surviving Enter Play Mode with domain reload
  disabled: the registry now clears on `SubsystemRegistration`, so game glue re-registering on
  `BeforeSceneLoad` no longer throws `Assertion id '...' is already registered` on the second play.

## [0.4.0] - 2026-08-15
- Added `AutoTestRuntimeCapability` (`[Flags]` enum, `FakeInput = 1`) and
  `AutoTestCaseData.requiredCapabilities` (additive, default `None`) so a case can declare a
  required runtime capability.
- Added `AutoTestCapabilityRegistry`/`AutoTestCapabilityCheck` to `Hung.AutoTest`: providers
  register one probe per capability bit; `AutoTestRunner.RunCaseRoutine` evaluates required
  capabilities before executor `Prepare`/`Run`/`Cleanup` and records a fatal
  `AUTOTEST_CAPABILITY_UNAVAILABLE` setup failure (never a silent pass) when unavailable.
- Added optional companion assembly `Hung.AutoTest.FakeInput` (version-gated on
  `com.unity.inputsystem`/`com.unity.ugui`, `Hung.AutoTest` core stays dependency-neutral):
  `AutoTestInputSystemDriver` (keyboard/mouse device synthesis, owns only devices it creates) and
  `AutoTestEventSystemPointerDriver` (UGUI click/drag/cancel sequencing with EventSystem
  resolution fallback).
- Consumer migration note: host games that currently own a local fake-input driver (e.g. Horror's
  `HorrorInputSystemDriver`, PVM's `EventSystemClickActionExecutor`/`EventSystemDragActionExecutor`)
  may delegate to the official drivers and mark their fake-input cases with
  `requiredCapabilities = AutoTestRuntimeCapability.FakeInput`; this is opt-in per case, not
  automatic. See `Docs/adr/ADR-E5-autotest-required-capabilities.md`.
- Backward compatible: existing case/suite assets deserialize `requiredCapabilities` to `None`;
  no existing field renamed, reordered, or removed.

## [0.3.2] - 2026-08-12
- Persisted the editor CLI launch record in `UnityEditor.SessionState` so suite, readiness scene, and timeout survive Enter Play Mode domain reload.
- Added an idempotent resume path for both the play-mode callback and editor load initialization; a claimed launch is cleared before side effects.
- Preserved the public runtime API and product-neutral package boundary.

## [0.3.1] - 2026-08-12
- Added product-neutral `AutoTestBootstrapper.HostBootReset` and per-logical-run reset APIs.
- Moved runner boot reset from `AutoTestRunner.Awake()` to the actual run boundary.
- Added a one-shot CLI preboot handoff so runner startup does not erase host boot state.
- Backward compatible: existing readiness, boot-kick, runner, suite, case, and serialized asset contracts are unchanged.

## [0.3.0] - 2026-08-11
- Added `AutoTestAssertionConfig.assertionId` (string, additive) and a parallel ordinal-keyed string registry (`AutoTestAssertionRegistry.Register(string, creator, descriptor)` / `TryCreate(string, config)`) for host games that have exhausted the reserved `AutoTestAssertionType` enum extension slots (29–42).
- `AutoTestAssertionFactory` dispatches by `assertionId` first when set; an unregistered string ID fails explicitly with `AUTOTEST_ASSERTION_ID_UNKNOWN` via the new `InvalidAssertionConfigurationAssertion`, never silently falling back to enum dispatch.
- Added optional `AutoTestAssertionResult.evidence` / `AutoTestFailure.evidence`; Markdown reports add a fenced `json` Evidence block only when nonempty.
- Added `AutoTestAssertionConfigDrawer` (editor) to author either enum or string-ID assertions on the same config without clearing the other mode's fields.
- Backward compatible: existing enum serialization, `AutoTestAssertionType` integer values (0–42), `Register(AutoTestAssertionType, creator)`, and all existing case/suite assets are unchanged. See `Docs/adr/ADR-E5-autotest-string-keyed-assertions.md`.

## [0.2.7] - 2026-08-02
- Added `-rcState` command-line flag and strict recognized-flag validation: duplicate Stage B flags record `RC_CLI_DUPLICATE_FLAG`, a recognized flag missing its value records `RC_CLI_VALUE_MISSING`. Parsing never throws; unrecognized/Unity-native arguments are ignored as before.
- Added product-neutral evidence identity fields to `RuntimeEvidenceRecord`: `phase`, `schemaVersion`, `scenarioVersion`, `packageIdentity`, `manifestSha256`, `lockSha256`, `playerSha256`, `stateFixtureId`, `stateBeforeSha256`, `stateAfterSha256`, and `byteLength` on `RuntimeEvidenceArtifact`.
- `Complete(Passed, ...)` now throws `InvalidOperationException` if any recorded assertion has `passed == false`, preventing a false-pass record. `Failed`/`Blocked` completion is unaffected.
- Backward compatible: all existing `AutoTestCommandLine`/`RuntimeEvidenceRecord` members and behavior for non-Stage-B callers are unchanged.

## [0.2.6] - 2026-08-01
- Added `RuntimeSnapshot.extensions`, a serialized envelope of `{ id, json }` entries with `SetExtension`/`TryGetExtension` helpers, so host games attach product snapshot data without adding product types to the package. See `ADR-E5-autotest-runtime-snapshot-extensions`.
- Markdown reports emit an ordinal-ordered `### Extensions` subsection; JSON reports carry the payload directly.
- Runner window repaints on a ~30fps budget while a run is in flight instead of relying on the ~10fps editor `Update` tick.

## [0.2.5] - 2026-08-01
- Removed unused Base, Data, and DesignPattern dependencies so host games can adopt AutoTest without importing unrelated package owners.

## [0.2.4] - 2026-08-01
- Added a game-neutral legacy integer extension seam for serialized host-game case compatibility.

## [0.2.3] - 2026-08-01
- Removed the remaining concrete locator diagnostics from the batchmode CLI readiness timeout path.

## [0.2.2] - 2026-08-01
- Removed the concrete Base/Data locator readiness dependency; host games now supply readiness solely through `AutoTestBootstrapper.ExtraReadyCheck`.

## [0.2.1] - 2026-08-01
- Added generic event-channel aggregation, projectile snapshot evidence, and optional editor play-mode auto-stop after a run.
- Replaced game-specific package defaults and documentation with game-neutral contracts.

## [0.2.0] - 2026-07-29
- Added append-only runtime confidence evidence records with redaction, artifact hashing, terminal result states, and built-player command-line flags.
- Added a built-player entrypoint seam for game-owned runtime scenario runners and exit codes 0/1/3 for pass/fail/blocked confidence gates.

## [0.1.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no AutoTest API or runtime behavior changed.

## [0.1.0] - 2026-07-07
- Extracted the game-agnostic core after its seam refactor: stats hook, assertion registry, executor/snapshot factories, and pluggable ready-check.
