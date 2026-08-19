# Hung Sync (`com.hung.sync`)

Generic semantic synchronization overlay. Layer 1. Depends only on `com.hung.persistence`.

Sync is **not** a save backend. Local persistence answers "did state survive process death".
Sync answers "was this operation authenticated, ordered, accepted, rejected, retried, reconciled".

## Public API index

| Type | Purpose |
|---|---|
| `SyncOperation` | Operation envelope: stable id, stream key, expected revision, opaque payload |
| `SyncResult` | Server outcome plus canonical revision and payload |
| `SyncResultKind` | Accepted, DuplicateAccepted, RejectedBusinessRule, RevisionConflict, AuthenticationRequired, RetryableTransportFailure, PermanentProtocolFailure |
| `SyncAuthority` | OptimisticAllowed / ConfirmationRequired |
| `ISyncTransport` | Send one operation, get one result. Implemented outside this package |
| `ISyncAuthProvider` | Opaque auth token plus expiry signal. Never persisted |
| `ISyncClock` | Injectable UTC time. Keeps the package Unity-free and tests deterministic |
| `ISyncDiagnostics` | Payload-free structured diagnostics |
| `SyncRetryClassifier` | Pure mapping from transport outcome to `SyncResultKind` |
| `SyncQueue` | Durable pending queue persisted via `com.hung.persistence` |
| `SyncEngine` | Dispatch, reconciliation, offline behavior |

## Hard rules

- Payloads and auth tokens are never logged and never stored in the queue.
- Retries reuse the same operation id; a new business intent gets a new id.
- Queue uses `SaveFailurePolicy.FailClosed`.
- Conflicts never resolve by last-write-wins.
- No Unity API calls. No product/domain types.
