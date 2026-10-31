# Changelog

## [0.1.1] - 2026-10-31

- Dependency alignment: com.hung.sync 0.1.0 -> 0.1.1


## [0.1.0] - 2026-08-19

### Added
- `SyncSimulatorServer`: in-memory accounts, per-stream canonical state and revisions, operation-id deduplication, offline and auth controls.
- `SyncFaultScript`: attempt-indexed deterministic fault injection.
- `SimulatorTransport` and `SimulatorAuthProvider` implementing the `com.hung.sync` ports.
