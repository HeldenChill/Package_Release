# Changelog

## [0.2.3] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.2.2] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 for the F4-IE editor prerequisite.

## [0.2.1] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1.

## [0.2.0] - 2026-07-27
### Changed
- Replaced the package-local `IClock` / `SystemClock` contracts with shared `Hung.Base.IClock` / `Hung.Base.SystemClock`.
- Added truthful `com.hung.base` dependency. Energy state and behavior are unchanged.

## [0.1.0] - 2026-07-21

**This is a new package identity, not a continuation of `com.hung.liveops.heart`'s version
lineage.** `0.1.0` is the first release of `com.hung.liveops.energy` as its own package `name`
in `package.json` — it is not "the version after Heart's `0.3.1`", not a downgrade, and not a
patch/major bump on Heart. Semver continuity does not apply across a package identity change.
There is no upgrade path from Heart; consumers migrating from Heart must adopt Energy fresh
(no save-format migration — see `Docs/migrations/liveops-heart-to-energy.md`).

Functionally complete for standalone (non-PVM) use:

- Primitive-only persistence DTO with explicit mapper and exact round-trip (Task 2).
- Validated immutable `EnergyConfig` / `EnergyConfigSO`, `IClock` / `SystemClock` (Task 3).
- Durable local JSON store with corrupt-payload quarantine (never deletes evidence) and
  atomic temp-file-then-replace saves (Task 4).
- Public contract (`IEnergyService`, `EnergySnapshot`, result types) and
  `EnergyServiceFactory.CreateLocal` composition entry point; internal save model (Task 5).
- Copy-on-write reconciliation/regeneration through a single commit helper; failed persistence
  never mutates published state or fires `Changed` (Task 6).
- Structured idempotent ledger for grants and `GrantUnlimited`, using an in-package FNV-1a
  payload fingerprint (Task 7).
- Run reservation state machine: `TryStartRun` / `MarkRunEntered` / `CompleteRun` /
  `CancelFailedStart`, all copy-on-write and idempotent (Task 8).

90/90 EditMode tests passing; both `Hung.LiveOps.Energy.csproj` and
`Hung.LiveOps.Energy.Tests.csproj` build with 0 errors.
