# Changelog

## [0.7.1] - 2026-08-31
- Dependency alignment: com.hung.data 0.12.2 -> 0.12.3. No runtime or API change.

## [0.6.0] - 2026-08-17
- Dependency alignment: com.hung.base 0.19.4 -> 0.20.0; com.hung.data 0.11.1 -> 0.12.0; adds com.hung.persistence 0.1.0 (transitive contract dependency for the ledger).

## [0.5.3] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.5.2] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 and Data 0.10.2 for the F4-IE editor prerequisite.

## [0.5.1] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.5.0] - 2026-07-24
### Added
- Added logical purchase catalog, explicit legacy `IAP_ITEM -> PurchaseProductId` mapping, durable fail-closed purchase ledger, transaction coordinator, local Unity receipt validator seam, Unity Purchasing v5 store adapter, and legacy callback bridge.
- Added EditMode and PlayMode tests for catalog validation, ledger recovery, validation results, coordinator crash checkpoints, Unity order translation, legacy callback behavior, DesktopPremium omission, and file-backed recovery.

### Changed
- `IAPManager` is now composition/bootstrap compatibility only; package internals no longer mutate `GameData`, `Locator.Items`, or analytics.
- Legacy callback success now waits for durable completed purchase processing.
- Store confirmation now occurs only after validated purchase and durable game grant.

### Notes
- This is a candidate integrity wave. Deterministic Unity tests pass, but Android/iOS sandbox purchases and production player-build evidence are still required before stable promotion.

## [0.4.1] - 2026-07-21
- Migrated purchasing item data and documentation to canonical `ItemId`; aligned dependency versions with the ItemId release.

## [0.4.0] - 2026-07-14
### Added
- `Runtime/Data/DataManager.IAP.cs` - a `partial class DataManager` compiled into `Hung.Data` via this package's existing `Hung.Data.asmref`. It owns the `[SerializeField] IAPData iapData` field and implements `TryGetServiceSOData<T>`, both moved out of `com.hung.data` 0.8.0 so the data package no longer depends on this one's types.
### Why
Wave 2 of the Horror1Game adoption (B4). `com.hung.data` could not compile without IAP installed.

## [0.3.0] - 2026-07-11
- BREAKING: `IAP` namespace renamed to `Hung.IAP` (B1 Pass 5 namespace pass). `rootNamespace` set on `Hung.IAP.asmdef` (was empty).

## [0.2.0] - 2026-07-11
- Added `Doubles/NullIapService : IIAPService` (all purchase/restore calls resolve to the failure callback - never fakes a success).
- README documents the transaction state machine and its double-grant/failed-restore risk analysis (Ph6 step 8).

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (contracts moved to com.hung.base).
