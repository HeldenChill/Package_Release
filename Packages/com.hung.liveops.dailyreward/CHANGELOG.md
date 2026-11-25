## [0.3.16] - 2026-11-23
### Fixed
- Popup prefabs no longer reference project art or project data SOs; nested items use com.hung.ui prefabs. Games skin with variants and assign data.

## [0.3.18] - 2026-11-26
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.17] - 2026-11-25
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.14] - 2026-11-13
### Fixed
- `ClaimFree` keys its reward claim id by the previous free claim instead of the current clock ticks. A retry after a crash between the grant and the cooldown save reuses the id, so the coordinator dedupes it instead of granting the free reward twice. Covered by `DailyRewardIntegrityTests.ClaimFree_*`.

## [0.3.11] - 2026-11-02

- Patch release: re-pin dependencies to com.hung.designpattern 0.5.1 (pool statics reset on SubsystemRegistration). No code change in this package.

## [0.3.10] - 2026-10-31

- Dependency alignment: com.hung.base 0.27.0 -> 0.28.0; com.hung.data 0.13.0 -> 0.14.0; com.hung.ui 0.8.1 -> 0.9.0

## Unreleased - Horizon 1 BW4

- Add explicit focused Item/reward composition with immutable offers, unavailable defaults and selected recoverable DailyReward/SpinWheel sessions/views.
- Preserve shipped IDs, historical claim/fingerprint bytes and legacy APIs/assets. Selected feature schema 2 requires explicit composition and its own rollback disposition.
- Package evidence is distinct from deferred game/player/consumer adoption.

# Changelog

## [0.3.15] - 2026-11-14
### Fixed
- `DailyRewardSession.Initialize` no longer fails with `REWARD_RECOVERY_FAILED` because another feature has a claim that can never be granted. Recovery still retries every pending claim; only this feature's own pending claim gates startup (BUG-261114-382e, owner picked option B).

## [0.3.13] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.30.0 (UI contracts moved to com.hung.ui). No code change in this package.

## [0.3.12] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.29.0 (init-flow seams). No code change in this package.

## [0.3.9] - Unreleased

- Migrate affected service ownership to read-only Locator/ServiceSlots registration and exact-instance cleanup. Base adds explicit Ads/IAP Unsupported defaults and removes product gameplay contracts; see Base migration notes. Unreleased source; consumers are not automatically upgraded.
- Update declared dependency versions for the tested P2-C cohort; this does not activate optional product features.

## [0.3.8] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.1 -> 0.7.2.

## [0.3.7] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.0 -> 0.7.1.

## [0.3.6] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6; com.hung.ui 0.6.0 -> 0.7.0.

## [0.3.5] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.3.3] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.3.2] - 2026-08-11
- Dependency-only patch: align Base 0.19.2, Data 0.10.2, and UI 0.5.3 for the F4-IE editor prerequisite.

## [0.3.1] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.ui 0.5.1 -> 0.5.2; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.3.0] - 2026-07-27
- DailyReward now reconciles reward days through injectable UTC `IClock` and `RewardDayPolicy`.
- Free reward cooldown uses absolute UTC ticks across midnight and year boundaries.
- Sequence and free rewards now grant through `IRewardClaimCoordinator`; ad rewards require `Placement.DAILY_REWARD` authorization and fail closed when authorization is missing.
- Added EditMode tests for UTC cooldown, day reconciliation, rollback freeze, and ad authorization.

## [0.2.1] - 2026-07-21
- Migrated daily-reward item configuration to canonical `ItemId`; aligned dependency versions with the ItemId release.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 6):** `DailyReward`->`Hung.LiveOps.DailyReward`; `UI` files merged into `Hung.UI`. Asmdef file renamed `Hung.Subsystem.DailyReward.asmdef`->`Hung.LiveOps.DailyReward.asmdef` (deferred from Ph5); stale generated `Hung.Subsystem.DailyReward.csproj` deleted. `rootNamespace` set (was empty).

## [0.1.0] - 2026-07-09
- Extracted from Package_Repo's `_SubSystem/DailyReward` (no PvM merge needed — Phase 5 plan Task 4).
- `IDailyRewardService`/`Locator.DailyReward` moved into `com.hung.base/Runtime/Services/Contracts/DailyReward/`; `DailyRewardDataSO`/`DailyRewardSaveData` moved into `com.hung.data/Runtime/LiveOps/DailyReward/`. `Hung.Base.asmref`/`Hung.Data.asmref`/`Hung.UI.asmref` injection sites retired.
- Asmdef renamed `Hung.Subsystem.DailyReward` → `Hung.LiveOps.DailyReward` (GUID preserved); `Hung.UI.Game` reference dropped (no live callers — `HomeCanvas`/`MainMenuCanvas` usages were already commented out); `Hung.Utilities` added (was implicitly available via the old asmref host, needed directly now).
- `DailyRewardDataSO.asset` instance left in place under `Assets/_Game/_SubSystem/DailyReward/Data/` (script GUID preserved by the move, asset reference unaffected).
