# Changelog

## [0.4.14] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.30.0 (UI contracts moved to com.hung.ui). No code change in this package.

## [0.4.13] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.29.0 (init-flow seams). No code change in this package.

## [0.4.12] - 2026-11-02

- Patch release: re-pin dependencies to com.hung.designpattern 0.5.1 (pool statics reset on SubsystemRegistration). No code change in this package.

## [0.4.11] - 2026-10-31

- Dependency alignment: com.hung.base 0.27.0 -> 0.28.0; com.hung.data 0.13.0 -> 0.14.0; com.hung.ui 0.8.1 -> 0.9.0

## [0.4.10] - Unreleased

- Migrate affected service ownership to read-only Locator/ServiceSlots registration and exact-instance cleanup. Base adds explicit Ads/IAP Unsupported defaults and removes product gameplay contracts; see Base migration notes. Unreleased source; consumers are not automatically upgraded.
- Update declared dependency versions for the tested P2-C cohort; this does not activate optional product features.

## [0.4.9] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.1 -> 0.7.2.

## [0.4.8] - 2026-09-28
- Dependency alignment: com.hung.ui 0.7.0 -> 0.7.1.

## [0.4.7] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6; com.hung.ui 0.6.0 -> 0.7.0.

## [0.4.6] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.4.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.4.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2, Data 0.10.2, and UI 0.5.3 for the F4-IE editor prerequisite.

## [0.4.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.ui 0.5.1 -> 0.5.2; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.4.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no piggy-bank API or runtime behavior changed.

## [0.4.0] - 2026-07-16
- Added `NullPiggyBankService` (`Runtime/Doubles/`) — an all-no-op `IPiggyBankService`, same role as Ph6's `NullAdsService`/`NullIapService`. Additive only; no existing behavior changed.
- **Why now:** `PiggyBankManager` is this package's only `IPiggyBankService` implementation and it is a `Singleton<T>` MonoBehaviour needing scene wiring plus a `Database` save round-trip. A game that calls the contract without running the feature had no way to satisfy it — `Locator.PiggyBank` stayed null and unguarded call sites NRE'd. Found while adopting into Horror1Game (B4 Wave 5); same gap Ph6 closed for ads/IAP.
- **Semantics:** `PiggyGoldProgress` stays `0` and `IncreasePiggyProgress()` is dropped rather than counted in memory. The real manager persists every increment through `Database`, so a double that counted would report progress that silently vanishes on the next launch. Reporting "the feature is off" is honest; reporting progress that no store backs is not.

## [0.3.0] - 2026-07-14
- Adopted `PiggyBankRewardClaimedEvent` (new `Runtime/PiggyBankEvents.cs`, namespace `Hung.LiveOps.PiggyBank`). It previously lived in `com.hung.designpattern`'s `EVENTS.cs`, which was deleted in designpattern 0.4.0 — this package raises it and is its natural owner.
- No behavior change. `PiggyBankPopup.cs` (which sits in `namespace Hung.UI`, not the package's own namespace) and TemplateGame's `HomeCanvas.cs` both gained `using Hung.LiveOps.PiggyBank;`.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 6):** `PiggyBank`->`Hung.LiveOps.PiggyBank`; `UI` files merged into `Hung.UI`. Asmdef file renamed `Hung.Subsystem.PiggyBank.asmdef`->`Hung.LiveOps.PiggyBank.asmdef` (deferred from Ph5). `rootNamespace` set (was empty).

## [0.1.0] - 2026-07-09
- Extracted from Package_Repo's `_SubSystem/PiggyBank` (Phase 5 plan Task 6).
- `IPiggyBankService`/`Locator.PiggyBank` moved into `com.hung.base/Runtime/Services/Contracts/PiggyBank/`; `PiggyBankDataSO`/`PiggyBankSaveData` moved into `com.hung.data/Runtime/LiveOps/PiggyBank/`. `Hung.Base.asmref`/`Hung.Data.asmref`/`Hung.UI.asmref` injection sites retired.
- Asmdef renamed `Hung.Subsystem.PiggyBank` → `Hung.LiveOps.PiggyBank` (GUID preserved); raw-GUID reference list normalized to names (`Hung.Base`, `Hung.DesignPattern`, `Hung.Data`); `Hung.UI.Game` reference dropped, `Hung.Common`/`Hung.Utilities`/`Unity.TextMeshPro` added (needed by `PiggyBankPopup.cs`, previously available transitively via the old asmref host).
- **HomeCanvas decouple:** `PiggyBankPopup.OnPiggyBankPurchased` called `UIManager.Ins.GetUI<HomeCanvas>().FlyGoldReward(...)` directly (a fly-to-icon gold animation, not a generic refresh — `UpdateAllUI()` could not replace it since it needs the claimed amount and the button's screen position). Replaced with `EventBus<PiggyBankRewardClaimedEvent>.Raise(new PiggyBankRewardClaimedEvent { goldAmount })`; `HomeCanvas` now subscribes and calls its own `FlyGoldReward(PiggyBankBtnTransform.position, e.goldAmount)` — same visual behavior, call direction reversed. New event added to `com.hung.designpattern`'s `EVENTS.cs`.
- `HomeCanvas.cs` still references the concrete `PiggyBankPopup` type (`Locator.UI.OpenUI<PiggyBankPopup>()`), so `Hung.UI.Game.asmdef` gained a `Hung.LiveOps.PiggyBank` reference (unlike DailyReward/Heart, which had no live concrete-class callers).
