# Changelog

## [0.1.2] - 2026-11-23
### Changed
- Release snapshot release-2026.11.23.1: dependency declarations re-pinned to actual versions (designpattern 0.5.3, utilities 0.2.5, ui 0.10.2) and previously unreleased committed changes published. No API or behaviour change from this entry.

## [0.1.1] - 2026-10-31

- Dependency alignment: com.hung.sync 0.1.0 -> 0.1.1


## [0.1.0] - 2026-08-19

### Added
- `SyncSimulatorServer`: in-memory accounts, per-stream canonical state and revisions, operation-id deduplication, offline and auth controls.
- `SyncFaultScript`: attempt-indexed deterministic fault injection.
- `SimulatorTransport` and `SimulatorAuthProvider` implementing the `com.hung.sync` ports.
