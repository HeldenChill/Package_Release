# Changelog

## [0.1.0] - 2026-08-19

### Added
- `SyncOperation` envelope with stable idempotency ids, expected revisions, opaque payloads, and UTC-normalized client timestamps.
- `SyncResult`, `SyncResultKind`, `SyncTransportResponse`, and `SyncTransportOutcome`.
- `ISyncTransport`, `ISyncAuthProvider`, `ISyncClock`, `ISyncDiagnostics` ports.
- `SyncRetryClassifier`: pure classification and queue-removal policy.
- `SyncQueue` durable pending queue persisted through `com.hung.persistence` with `FailClosed` policy.
- `SyncEngine` dispatch with idempotent redelivery, deterministic conflicts, and payload-free diagnostics.
