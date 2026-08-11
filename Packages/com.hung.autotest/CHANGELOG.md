# Changelog

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
