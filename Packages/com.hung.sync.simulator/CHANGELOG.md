# Changelog

## [0.1.0] - 2026-08-19

### Added
- `SyncSimulatorServer`: in-memory accounts, per-stream canonical state and revisions, operation-id deduplication, offline and auth controls.
- `SyncFaultScript`: attempt-indexed deterministic fault injection.
- `SimulatorTransport` and `SimulatorAuthProvider` implementing the `com.hung.sync` ports.
