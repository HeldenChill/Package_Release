# Changelog

## [0.4.0] - 2026-08-14
### Breaking
- DailyGiftManager and DailyGiftPopup now serialize a `ScriptableObject` which must implement `Hung.Data.LiveOps.IDailyGiftConfig`.
- DailyGift no longer references global `DailyGiftDataSO`, `DailyGiftItem`, or `GameData.ItemData` configuration models.

## [0.3.2] - 2026-08-11
- Dependency-only patch: align Base 0.19.2, Data 0.10.2, and UI 0.5.3 for the F4-IE editor prerequisite.

## [0.3.1] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.ui 0.5.1 -> 0.5.2.

## [0.3.0] - 2026-07-27
### Changed
- DailyGift now reconciles eligibility through injectable UTC `IClock` and `RewardDayPolicy` instead of local day polling.
- Added additive full reward-day, cycle, slot, and streak key state adoption on `DailyGiftDbModel`.
- Single-claim and claim-all paths can route through `IRewardClaimCoordinator` for recoverable reward grants; legacy Locator item grant remains only as compatibility fallback when no coordinator is configured.

### Added
- EditMode tests for elapsed-day progression, backward-clock freeze, and coordinator-backed claim finalization.

## [0.2.1] - 2026-07-21
- Migrated reward item references and grant calls to canonical `ItemId`; aligned dependency versions with the ItemId release.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 6):** `DailyGift`/`UI.DailyGift`->`Hung.LiveOps.DailyGift`; `UI` files merged into `Hung.UI` alongside com.hung.ui's own rename. Asmdef file renamed `Hung.Subsystem.DailyGift.asmdef`->`Hung.LiveOps.DailyGift.asmdef` (the `"name"` field already said `Hung.LiveOps.DailyGift`, only the filename was stale - deferred from Ph5). `rootNamespace` set (was empty).

## [0.1.0] - 2026-07-07
- Extracted from Package_Repo's `_SubSystem/DailyGift`, reconciled with PetVsMonster's live-hardened version (login-streak feature ported; PvM's disabled reward-claim path and incomplete item-type migration rejected — see `com.hung.base` CHANGELOG for detail).
- `IDailyGiftService`/`Locator.DailyGift`/`DailyGiftDbModel`/`DailyGiftTrack` moved into `com.hung.base/Runtime/Services/Contracts/DailyGift/`; `DailyGiftDataSO` moved into `com.hung.data/Runtime/LiveOps/DailyGift/`. Both `Hung.Base.asmref`/`Hung.Data.asmref` injection sites retired.
- Asmdef renamed `Hung.Subsystem.DailyGift` → `Hung.LiveOps.DailyGift` (GUID preserved); `Hung.UI.Game` reference dropped (EventBus/UIManager only, per vision's LiveOps sideways-comms rule).
- `DailyGiftManager.prefab` brought in from PetVsMonster (Package_Repo had no install prefab of its own).
