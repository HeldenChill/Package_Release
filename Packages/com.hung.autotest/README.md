# Hung AutoTest (L6 dev tooling)

Game-agnostic automated-test core for scenario-driven game validation.

Version 0.4.0 adds an enforced runtime-capability contract (`AutoTestCaseData.requiredCapabilities`) and an optional `Hung.AutoTest.FakeInput` companion assembly with reusable keyboard/mouse and UGUI gesture drivers — see [Required capabilities and fake input](#required-capabilities-and-fake-input) below. Version 0.3.2 makes the editor CLI launch domain-reload safe by persisting its one-shot launch record in `UnityEditor.SessionState` and resuming it idempotently after Enter Play Mode. Version 0.3.1 adds per-logical-run host boot reset coordination: hosts register `AutoTestBootstrapper.HostBootReset` alongside `BootKick`; normal runs reset at `StartRun`; and CLI preboot resets before readiness polling without allowing runner startup to erase that host state. `AutoTestRunner.Awake()` is not the reset boundary. Version 0.3.0 adds string-keyed assertion registration alongside the existing `AutoTestAssertionType` enum, for host games that have exhausted the reserved enum extension slots. Version 0.2.7 adds a `-rcState` command-line flag, strict CLI flag validation (`RC_CLI_DUPLICATE_FLAG`/`RC_CLI_VALUE_MISSING`), and product-neutral run-identity fields on `RuntimeEvidenceRecord` for host games composing separate seed/verify processes. Version 0.2.6 adds the snapshot extension envelope. Version 0.2.5 removed the remaining Base/Data/DesignPattern package dependencies.

## Package dependencies

AutoTest has no `com.hung.*` package dependency. Host-game integrations belong in a separate glue assembly that references `Hung.AutoTest` and the host's own gameplay assemblies.

## Snapshot extensions

`RuntimeSnapshot` carries only game-neutral data. A host game attaches its own payload through the extension envelope:

```csharp
snapshot.SetExtension("mygame.snapshot.v1", myDto);          // write, once per capture
snapshot.TryGetExtension("mygame.snapshot.v1", out MyDto d); // read, in an assertion
```

Contract:

- The payload DTO must be `[Serializable]` and `JsonUtility`-round-trippable. It lives in the host's glue assembly; the package never names a product type.
- IDs are compared with `StringComparer.Ordinal` and must be unique. `SetExtension` replaces an existing ID and keeps `extensions` sorted by ID.
- A blank ID throws `ArgumentException`; a null value throws `ArgumentNullException`.
- Reads return `false` — never throw — when the ID is missing, duplicated, or the payload is malformed.
- Reports include the payload: JSON directly, Markdown as an ordinal-ordered `### Extensions` subsection of fenced JSON blocks. An empty list emits no heading.

Serialized-data decision and rollback contract: `Docs/adr/ADR-E5-autotest-runtime-snapshot-extensions.md`.

## Game glue contract (each game implements)
- `IAutoTestScenarioExecutor` — prepares/runs/cleans up a scenario; casts `AutoTestCaseData.scenario` (ScriptableObject) to its own scenario type.
- `IRuntimeSnapshotBuilder` — builds `RuntimeSnapshot` from live game state.
- `AutoTestAssertionRegistry.Register(...)` — game-specific assertion creators (core handles NoExceptionLog, ScenarioStarted, ScenarioTimeout, NoNaNTransform). Two overloads: `Register(AutoTestAssertionType, creator)` for the reserved enum extension slots, and `Register(string id, creator, descriptor)` for string-keyed assertions once those slots are exhausted.
- `AutoTestRunner.ExecutorFactory` / `SnapshotBuilderFactory` — assign in a `[RuntimeInitializeOnLoadMethod]` bootstrap.
- `AutoTestBootstrapper.BootKick` and `AutoTestBootstrapper.HostBootReset` — register both host boot hooks. Normal runs invoke the reset at `StartRun`; CLI preboot invokes it before readiness polling. `AutoTestRunner.Awake()` is not a reset boundary.
- `AutoTestBootstrapper.ExtraReadyCheck` — game readiness condition (e.g. composition-root manager exists).

Each host game supplies its own glue assembly, executor, snapshot builder, and domain assertions.

## String-keyed assertions

For assertions beyond the reserved enum slots, register by string ID instead:

```csharp
AutoTestAssertionRegistry.Register(
    "mygame.feature.ready",
    config => new FeatureReadyAssertion(config),
    new AutoTestAssertionDescriptor("mygame.feature.ready", "Feature Ready", "Waits for feature readiness."));
```

Author it on a case with `AutoTestAssertionConfig.Create("mygame.feature.ready", ...)`. Setting `assertionId` takes precedence over `type` on the same config. An unregistered ID fails explicitly with `AUTOTEST_ASSERTION_ID_UNKNOWN` rather than silently falling back to the enum path or a different assertion. IDs are compared with `StringComparer.Ordinal`; `Register` throws on a blank ID, a null creator, or a duplicate ID. See `Docs/adr/ADR-E5-autotest-string-keyed-assertions.md`.

## Runtime confidence player contract

Built players can emit compact terminal evidence through:

```text
-rcScenario <id> -rcRun <run-id> -rcOutput <folder> -rcPhase <phase> -rcProfile <profile>
```

`AutoTestPlayerEntrypoint` activates only when `-rcScenario` is present. Games may handle the command with `AutoTestPlayerEntrypoint.ExternalRunner`; otherwise the package resolves an `AutoTestSuiteData` through `SuiteResolver`.

Evidence JSON is append-only and written with unique filenames under `-rcOutput`. Sensitive values following `receipt`, `token`, `password`, `secret`, `keyalias`, or `keystore` are redacted. Artifacts are referenced by path and SHA-256 only.

Exit codes:

- `0`: mandatory runtime confidence scenario passed.
- `1`: scenario completed and failed.
- `3`: scenario was blocked, timed out, missing, or threw before producing a product result.

## Required capabilities and fake input

A case declares a runtime precondition instead of assuming it's always available:

```csharp
testCase.requiredCapabilities = AutoTestRuntimeCapability.FakeInput;
```

`AutoTestRunner` evaluates every required capability via `AutoTestCapabilityRegistry` immediately
after `context.Begin`, before the executor's `Prepare`/`Run`/`Cleanup`. If unavailable, the case
fails fast with a fatal `"Setup"` failure (`assertionId = "AUTOTEST_CAPABILITY_UNAVAILABLE"`) and
the executor is never invoked. A required capability is a run precondition only — it proves the
input mechanism was available, not that the gesture achieved its intended effect; product
assertions still prove outcomes.

### Optional prerequisites

`FakeInput` is provided by the optional `Hung.AutoTest.FakeInput` assembly
(`Runtime/FakeInput/Hung.AutoTest.FakeInput.asmdef`), enabled only when both
`com.unity.inputsystem` and `com.unity.ugui` are installed (asmdef `versionDefines`/
`defineConstraints`). It is not a `package.json` dependency — `Hung.AutoTest` core stays free of
Input System/UGUI references. Host glue that needs it adds an explicit asmdef reference to
`Hung.AutoTest.FakeInput`.

### AutoTestInputSystemDriver

Owns Input System keyboard/mouse device synthesis and cleanup:

```csharp
using var input = new AutoTestInputSystemDriver();
input.QueueText("hello");
input.PumpOneFrame(); // pumps one queued character/key/pointer action per call
```

- Resolves a supplied device or creates and owns a virtual one; `Cleanup()`/`Dispose()` releases
  pressed state and removes only devices this instance created — never a pre-existing device.
- `Cleanup()` is idempotent; calling a mutating method afterward throws `ObjectDisposedException`.

### AutoTestEventSystemPointerDriver

Owns UGUI EventSystem click/drag gesture sequencing. Accepts already-resolved `GameObject`
targets and screen positions — it does not resolve semantic target keys or app IDs; that stays in
host glue:

```csharp
var pointer = new AutoTestEventSystemPointerDriver();
bool started = pointer.TryClick(targetGameObject, screenPosition);
yield return pointer.Drag(sourceGameObject, startScreen, destinationGameObject, endScreen, moveFrames: 6);
pointer.Cancel(); // ends an in-flight drag exactly once; safe to call when idle
```

- Click sequence: enter, down, up, click. Drag sequence: initialize, begin, at least six move
  samples, drop, end, pointer up. `Cancel()` dispatches end/up cleanup exactly once.
- Resolves `EventSystem.current`, falling back to an inactive-object scene search if unset.
- Returns `false`/no-ops with a diagnostic in `LastDiagnostic` when the target or EventSystem is
  missing — never throws a null-reference exception.

See `Docs/adr/ADR-E5-autotest-required-capabilities.md` for the serialized-field compatibility
contract.

## Known debt
- RuntimeSnapshot schema carries TD-shaped sections (PetSnapshot etc.) — pure data, no type coupling; generalize together with com.hung.combatstats.
- AutoTestAssertionType enum carries domain members (serialized in assets) — migrate to string ids later if churn hurts.
- CLI: `-autoTestSuite <path> -autoTestReadyTimeout <s> -autoTestGameplayScene <name>` via `Hung.AutoTest.Editor.AutoTestCliRunner.RunSuiteFromCommandLine`.
