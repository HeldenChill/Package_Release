# Changelog

## [0.2.0] - 2026-11-14

- Feature: revision conflicts are reconcilable in place (BUG-261114-ce13). A conflict marks its operation and blocks only its own stream (`SyncQueue.MarkConflicted`, `IsStreamBlocked`); `TryPeek` skips blocked streams, so other streams keep dispatching and order within a stream is kept. New `SyncQueue.Rebase(operationId, newExpectedRevision, newPayload)` reconciles with the same id and persists, rolling back on a failed write; `Complete` on the conflicted id also unblocks. The mark is in memory only: after a restart the record conflicts once more and returns fresh canonical state. No serialized field changed.
- Behavior change: `SyncEngine.DispatchAll` no longer stops on `RevisionConflict`; it still stops on authentication-required and retryable transport failure.
- Fix: `SyncQueue.Enqueue`, `RecordAttempt` and `Complete` changed memory before persisting and kept the change when the save failed. A failed persist now undoes the change before throwing, so memory matches disk and retrying `Enqueue` with the same id after the exception works (BUG-261114-4e3c).
- Docs: README hard rule corrected; payloads are stored in the queue (needed for retry), only tokens are never stored (BUG-261114-3262).
- Dependency alignment: com.hung.persistence 0.2.0 -> 0.2.1.

## [0.1.1] - 2026-10-31

- Dependency alignment: com.hung.persistence 0.1.0 -> 0.2.0


## [0.1.0] - 2026-08-19

### Added
- `SyncOperation` envelope with stable idempotency ids, expected revisions, opaque payloads, and UTC-normalized client timestamps.
- `SyncResult`, `SyncResultKind`, `SyncTransportResponse`, and `SyncTransportOutcome`.
- `ISyncTransport`, `ISyncAuthProvider`, `ISyncClock`, `ISyncDiagnostics` ports.
- `SyncRetryClassifier`: pure classification and queue-removal policy.
- `SyncQueue` durable pending queue persisted through `com.hung.persistence` with `FailClosed` policy.
- `SyncEngine` dispatch with idempotent redelivery, deterministic conflicts, and payload-free diagnostics.
