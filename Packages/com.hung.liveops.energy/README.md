# Hung LiveOps Energy

Transaction-safe Energy (lives) LiveOps package. Replaces `com.hung.liveops.heart` as a new
package identity (breaking lineage, no Heart save-format migration — see
`Docs/migrations/liveops-heart-to-energy.md`).

Status: functionally complete (Tasks 1-8 of the implementation plan). 90/90 EditMode tests
passing. PVM adoption is a separate, later wave.

## Public Boundary

See `.cursor/plans/LiveOps_Energy_Safety_Design_2026-07-21.md` and the implementation plan
`.cursor/plans/LiveOps_Energy_Safety_Implementation_2026-07-21.md` for the approved
`IEnergyService` / `EnergyServiceFactory` contract.

## API Index

| Type | Kind | Purpose |
|---|---|---|
| `IEnergyService` | interface | Public Energy service boundary: current snapshot, run reservation lifecycle, grants, reconcile, `Changed` event. |
| `EnergySnapshot` | readonly struct | Immutable read-only view of current Energy state (renewable/bonus/total, cap, Unlimited status/expiry, next regeneration, rollback flag, active-run summary). |
| `EnergyActiveRunSnapshot` | readonly struct | Nested summary of the currently reserved run (run ID, free/paid, entered-gameplay), embedded in `EnergySnapshot`. |
| `EnergyResultOutcome` | enum | Shared outcome across all command results: `Success`, `IdempotentReplay`, `Insufficient`, `InvalidInput`, `Conflict`, `ConfigurationFailure`, `ClockRollback`, `PersistenceFailure`, `Unavailable`. |
| `RunOutcome` | enum | `Win` / `Loss` / `Abandoned`, passed to `CompleteRun`. |
| `EnergyRunStartResult` | readonly struct | Result of `TryStartRun`: outcome, whether the run was free, resulting snapshot. |
| `EnergyRunEntryResult` | readonly struct | Result of `MarkRunEntered`: outcome only. |
| `EnergyRunCompletionResult` | readonly struct | Result of `CompleteRun`: outcome, recorded `RunOutcome?`, resulting snapshot. |
| `EnergyRunCancellationResult` | readonly struct | Result of `CancelFailedStart`: outcome, resulting snapshot. |
| `EnergyGrantResult` | readonly struct | Result of `AddRenewable` / `AddBonus` / `GrantUnlimited`: outcome, resulting snapshot. |
| `EnergyServiceFactory` | static class | Public composition entry point. `CreateLocal(EnergyConfigSO, string stateFilePath, IClock)` validates config, builds a local store, and returns a ready service or a creation failure — never a permissive fallback. |
| `EnergyCreationResult` | readonly struct | Result of `EnergyServiceFactory.CreateLocal`: either a ready `IEnergyService` or an actionable `ErrorMessage`, never both. |

## Configuration

Create an asset via **Assets > Create > Hung > LiveOps > Energy Config** (`EnergyConfigSO`).
Fields:

| Field | Meaning |
|---|---|
| `renewableMax` | Cap on the regenerating (renewable) Energy pool. Regeneration pauses at/above this cap. |
| `regenerationIntervalSeconds` | Seconds to regenerate one renewable Energy point. Must be positive. |
| `runCost` | Energy spent per run reservation (`TryStartRun`), taken from renewable first, then bonus. |
| `initialRenewable` | Renewable balance seeded on first-ever run (no persisted state found). |
| `initialBonus` | Bonus (non-capped, non-regenerating) balance seeded on first-ever run. |
| `transactionRetentionCapacity` | Max ledger entries kept for idempotent-replay detection before eviction. |

**Missing or invalid configuration is a startup failure, never a fallback.** `EnergyServiceFactory.CreateLocal`
validates the config before constructing anything; if `config` is null or any field fails validation
(non-positive interval, negative amounts, etc.), it returns a failed `EnergyCreationResult` and **no service is
created**. There is no "permissive" or "unlimited" service returned when config is bad — per Global Constraints,
"Production must never silently grant unlimited Energy." Callers must treat a failed creation as blocking (disable
run entry, surface an error) rather than proceeding with a guessed default.

## Creation

```csharp
EnergyCreationResult result = EnergyServiceFactory.CreateLocal(config, stateFilePath);
if (!result.Success)
{
    // Actionable failure — log result.ErrorMessage, block run entry, do not fall back.
    return;
}
IEnergyService energy = result.Service;
```

`clock` is optional and defaults to `SystemClock` (wall-clock UTC); pass a fake `IClock` only in tests.

## Command IDs (idempotency contract)

Every mutating call — `AddRenewable`, `AddBonus`, `GrantUnlimited`, `TryStartRun`, `MarkRunEntered`,
`CompleteRun`, `CancelFailedStart` — takes a caller-provided, stable ID (`transactionId` or `runId`).
Replaying the same ID with the same command/payload returns `IdempotentReplay` and re-applies nothing;
reusing an ID with a *different* command or payload returns `Conflict`.

**Do not generate a fresh GUID inside a retry path.** A fresh ID on retry defeats idempotency — if the
first attempt's network call timed out but the server-side persist actually succeeded, a retry with a
new ID will double-grant or double-spend. Derive the ID from something durable that survives the retry:

```csharp
// Wrong — a new ID every attempt means every retry looks like a brand-new grant:
energy.AddRenewable(amount, Guid.NewGuid().ToString());

// Right — derive from a stable, durable identifier (purchase receipt, IAP transaction ID,
// a run ID persisted to local save *before* the network/gameplay call that might fail):
energy.AddRenewable(amount, purchaseReceipt.TransactionId);
```

## Lifecycle sequence (runs)

1. `TryStartRun(runId)` — call **before** loading gameplay. Reserves cost, persists atomically.
2. `MarkRunEntered(runId)` — call **after** gameplay has successfully loaded/entered.
3. Exactly one terminal call:
   - `CompleteRun(runId, outcome)` on Win / Loss / Abandoned, **or**
   - `CancelFailedStart(runId)` — only valid if gameplay never successfully entered. Restores the
     exact renewable/bonus sources that were reserved.

`CancelFailedStart` is **rejected** once `MarkRunEntered` has succeeded for that run — a caller must
pick one terminal path, not both. Use `EnergyResultOutcome` on each result to check success.

## Recovery behavior

- A corrupt or unreadable state file is **quarantined**, not deleted — moved aside under a unique
  evidence filename (`energy-state.corrupt-<utcTicks>-<guid>.json`), never overwriting prior evidence.
  A fresh state is then initialized from config, as if this were a first run.
- Any command whose persistence step fails returns `PersistenceFailure` and leaves in-memory state
  **unchanged** — no partial mutation, and the `Changed` event does **not** fire. Callers should treat
  `PersistenceFailure` as "nothing happened" and are safe to retry with the *same* command ID (see
  Command IDs above); retrying does not risk a double-apply.

## No Heart save compatibility

There is no migration path from `com.hung.liveops.heart`'s save data. This package is a new package
identity (see Changelog) with its own persistence format; a consumer adopting Energy starts fresh —
no reader, converter, or dual-write path from Heart's saved state is provided or planned.
