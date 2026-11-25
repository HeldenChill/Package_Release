## [0.3.15] - 2026-11-23
### Fixed
- Popup prefab no longer references project art, VFX material or data SO, and loses a missing-script component on Fx_Rotate. Games skin with variants and assign data.

## [0.3.19] - 2026-11-26
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.18] - 2026-11-26
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.17] - 2026-11-26
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.16] - 2026-11-25
### Changed
- Re-pinned `com.hung.*` dependencies for the Unity 6.6 compile fix release.

## [0.3.11] - 2026-11-02

- Patch release: re-pin dependencies to com.hung.designpattern 0.5.1 (pool statics reset on SubsystemRegistration). No code change in this package.

## [0.3.10] - 2026-10-31

- Dependency alignment: com.hung.base 0.27.0 -> 0.28.0; com.hung.data 0.13.0 -> 0.14.0; com.hung.ui 0.8.1 -> 0.9.0

## Unreleased - Horizon 1 BW4

- Add explicit focused Item/reward composition with immutable offers, unavailable defaults and selected recoverable DailyReward/SpinWheel sessions/views.
- Preserve shipped IDs, historical claim/fingerprint bytes and legacy APIs/assets. Selected feature schema 2 requires explicit composition and its own rollback disposition.
- Package evidence is distinct from deferred game/player/consumer adoption.

# Changelog

## [0.3.14] - 2026-11-14
### Fixed
- `SpinWheelSession.Initialize` no longer fails on another feature's permanently failing claim (BUG-261114-382e, option B). `SpinWheelManager.CompleteSpin` returns `PersistenceFailure` and restores counters and the pending spin when the feature save fails, instead of throwing (BUG-261114-c587).

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
- SpinWheel now reconciles daily counters through injectable UTC `IClock` and `RewardDayPolicy`.
- Spin preparation persists selected reward intent before animation; completion finalizes through `IRewardClaimCoordinator`.
- Ad spins require `Placement.SPIN` authorization and fail closed when Ads/authorization is missing.
- Added EditMode tests for UTC reset, rollback freeze, prepared completion, and ad authorization.

## [0.2.1] - 2026-07-21
- Migrated spin-wheel reward identity to canonical `ItemId`; aligned dependency versions with the ItemId release.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 6):** `SpinWheel`->`Hung.LiveOps.SpinWheel`; `UI` files merged into `Hung.UI`. Asmdef file renamed `Hung.Subsystem.SpinWheel.asmdef`->`Hung.LiveOps.SpinWheel.asmdef` (deferred from Ph5) - this pass closes B1's asmdef-file-rename item. `rootNamespace` set (was empty).

## [0.1.0] - 2026-07-09
- Extracted from Package_Repo's `_SubSystem/SpinWheel` (Phase 5 plan Task 7). `_SubSystem/` folder retired entirely — this was the last subsystem.
- `ISpinWheelService`/`Locator.SpinWheel`/`SpinItem` moved into `com.hung.base/Runtime/Services/Contracts/SpinWheel/`; `SpinWheelDataSO`/`SpinWheelSave` moved into `com.hung.data/Runtime/LiveOps/SpinWheel/`. `Hung.Base.asmref`/`Hung.Data.asmref`/`Hung.UI.asmref` injection sites retired.
- Asmdef renamed `Hung.Subsystem.SpinWheel` → `Hung.LiveOps.SpinWheel` (GUID preserved); `Hung.UI.Game` reference dropped; `Hung.Common`/`Hung.Utilities`/`DOTween.Modules`/`Unity.TextMeshPro` added (needed by `SpinWheelPopup.cs`, previously available transitively via the old asmref host).
- `SpinWheelPopup.cs:208` (`ResetWheelData` test button): `UIManager.Ins.GetUI<HomeCanvas>()?.UpdateUI()` replaced with `UIManager.Ins.UpdateAllUI()` — already the pattern used for the real spin-complete flow at line 187, verified equivalent-or-broader refresh (no EventBus needed here, unlike PiggyBank's fly-reward case).
- `HomeCanvas.cs` still references the concrete `SpinWheelPopup` type (`Locator.UI.OpenUI<SpinWheelPopup>()`), so `Hung.UI.Game.asmdef` gained a `Hung.LiveOps.SpinWheel` reference.
- `SpinDataSO.asset` instance left in place under `Assets/_Game/_SubSystem/SpinWheel/Data/` (script GUID preserved, asset reference unaffected) — same as DailyReward/PiggyBank.
