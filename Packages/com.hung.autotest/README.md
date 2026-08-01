# Hung AutoTest (L6 dev tooling)

Game-agnostic automated-test core for scenario-driven game validation.

Version 0.2.3 uses the host-game readiness seam without requiring a concrete locator implementation, including batchmode diagnostics.

## Game glue contract (each game implements)
- `IAutoTestScenarioExecutor` — prepares/runs/cleans up a scenario; casts `AutoTestCaseData.scenario` (ScriptableObject) to its own scenario type.
- `IRuntimeSnapshotBuilder` — builds `RuntimeSnapshot` from live game state.
- `AutoTestAssertionRegistry.Register(...)` — game-specific assertion creators (core handles NoExceptionLog, ScenarioStarted, ScenarioTimeout, NoNaNTransform).
- `AutoTestRunner.ExecutorFactory` / `SnapshotBuilderFactory` — assign in a `[RuntimeInitializeOnLoadMethod]` bootstrap.
- `AutoTestBootstrapper.ExtraReadyCheck` — game readiness condition (e.g. composition-root manager exists).

Each host game supplies its own glue assembly, executor, snapshot builder, and domain assertions.

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

## Known debt
- RuntimeSnapshot schema carries TD-shaped sections (PetSnapshot etc.) — pure data, no type coupling; generalize together with com.hung.combatstats.
- AutoTestAssertionType enum carries domain members (serialized in assets) — migrate to string ids later if churn hurts.
- CLI: `-autoTestSuite <path> -autoTestReadyTimeout <s> -autoTestGameplayScene <name>` via `Hung.AutoTest.Editor.AutoTestCliRunner.RunSuiteFromCommandLine`.
