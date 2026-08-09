# Changelog

## [0.19.1] - 2026-08-09
- Fix: Stat Reset invalidates its cached final value; add the generic modifier-filter API.
- Dependency alignment: com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.19.0] - 2026-07-28
### Added
- Added vendor-neutral Ads request contracts: `AdsRequestId`, `AdsRequestKind`, `AdsRequestOutcome`, `AdsShowRequest`, `AdsShowResult`, and `IAdsRequestService`.
- Added pause lease contracts and default `PauseService` with `Locator.Pause` compatibility slot.

## [0.18.0] - 2026-07-27
### Added
- Added neutral UTC time contracts: `IClock`, `SystemClock`, `RewardDayKey`, and `RewardDayPolicy`.
- Added vendor-neutral reward integrity contracts: deterministic `RewardClaimId`, reward grant items/results, `IRewardGrantService`, `RewardAuthorization`, `RewardClaimRequest`, `RewardClaimResult`, `RewardRecoveryReport`, and `IRewardClaimCoordinator`.

### Notes
- `SystemClock` is the only affected-domain production type that reads `DateTime.UtcNow` directly.
- Reward claim coordination is a recoverable protocol boundary, not a physical cross-store transaction.

## [0.17.0] - 2026-07-24
### Added
- Added vendor-neutral purchase integrity contracts: logical product IDs, availability/results, transaction snapshots, restore/reconcile aggregates, `IPurchaseIntegrityService`, and `IPurchaseGrantHandler`.
- Added `UnsupportedPurchaseIntegrityService` for DesktopPremium/Base-only compositions that intentionally omit purchasing.

### Changed
- Existing `IIAPService` callback contract is now obsolete in favor of `IPurchaseIntegrityService`; source compatibility is preserved for legacy callers.

## [0.16.0] - 2026-07-23
### Added
- Added DI-neutral persistence contracts, typed save definitions, structured results, migrations, codecs, protectors, diagnostics, and compatibility-definition composition.

### Changed
- `Database.Save<T>` and `Database.Load<T>` remain synchronous and source-compatible but now delegate to a configured `IPersistenceService` instead of writing raw PlayerPrefs JSON.
- Projects without `com.hung.data` now receive an actionable configuration error instead of silently falling back to unsafe persistence.

## [0.15.0] - 2026-07-21
### Changed (BREAKING)
- Removed the legacy numeric item enum and runtime migration adapters. Item service APIs, save data, and reward contracts now use `ItemId`.
- Save data uses the versioned key `GameData.item-id-v1`.
- Moved `SDKToolkitWindow` into the Editor-only `Hung.Base.Editor` assembly; runtime behavior is unchanged.

## [0.14.0] - 2026-07-16
### Added
- **Canon input contract**: `partial interface IInputService` (`SetInput(bool active, params string[] maps)`, `DisableAllInput()`) and the `Locator.Input` slot, promoted from Horror1Game (B4 Wave 6).
- **Why:** every game needs an input service; the framework had no contract for one, so Horror1Game defined `IInputService` + `Locator.Input` itself inside its `Hung.Base.asmref` folder. The generic surface is now canon; the game-specific surface (Horror's `GetInputActions()` returning its generated `PlayerInputActions` wrapper) stays a game-side partial part in the asmref folder. Same open-vs-closed split as `Locator.Data`.

## [0.13.0] - 2026-07-15
- **BREAKING: `IUIService` gains `GetUIAsync<T>(Action<T> onComplete)`.** Any implementer must add it (in practice there is exactly one: `com.hung.ui`'s `UIManager`).
- **Why:** surfaced by Horror1Game's tutorial adoption (B4 Wave 3). A game may load its canvases asynchronously — Horror's tutorial canvases are Addressables, not registry-resolvable — but the framework's `TutorialUIController` could only call the synchronous `GetUI<T>()`. Canvas *loading strategy* is a per-game concern the framework had closed off, the same open-vs-closed mistake as `EVENTS.cs` (ADR-0002) and `GameData`/`CONSTANTS` (ADR-0003).
- The default implementation resolves synchronously and invokes the callback immediately, so games whose canvases are registry-resolvable (TemplateGame) are unaffected. Callers must not assume the callback has run when the call returns.

## [0.12.0] - 2026-07-14
### Changed (BREAKING)
- `Trigger` adopted Horror1Game's superset: a `Trigger(int frameReset = 1)` constructor (the reset delay was hardcoded to 1 frame), a `GetValue()` accessor, and the reset is now only scheduled when the value is set to `true` - setting it `false` no longer queues a redundant timer callback. Existing `new Trigger()` call sites keep the old 1-frame behaviour.
### Added
- `Hung.Base` asmdef now references `Unity.InputSystem` (already a declared package dependency). A game whose `Hung.Base.asmref` folder injects InputSystem-typed contracts - Horror1Game's `IInputService`/`PlayerInputActions` - could not compile without it.
### Why
Wave 2 of the Horror1Game adoption (B4). See `Docs/adr/adr-0003-horror-base-data-ui-adoption.md`.

## [0.11.0] - 2026-07-14
### Removed (BREAKING)
- **Game content moved out of the framework.** `ENUM.GAMEPLAY.cs` (`PLACE_TYPE`, `SURFACE_STATE`, `SURFACE_TYPE`, `LEVEL_DIFFICULTY`, `LEVEL_PERFORMANCE_TYPE`), `ENUM.BASE.cs` (`DIRECTION`) and `CONSTANTS.cs` (`VEHICLE_TAG`, `HOLDER_TAG`, `CUT_SCALE_VALUE`, `MAX_LEVEL`, ...) were TemplateGame's content, not the framework's. All three files are **deleted from this package**; each game now declares them in `namespace Hung.Base` from its own `Hung.Base.asmref` folder.
- `GameData.cs` lost the rice-game level-editor model: `LevelData` (outer), `UnitData`, `RiceUnitData`, `KnifeUnitData`, `StaticRiceUnitData`, `HolderSlotData`, `PlaceTypeData`. Same treatment - game-owned, moved to the consuming game.
### Changed (BREAKING)
- `GameData` is now `public partial class`. A game adds its own serialized members from its `Hung.Base.asmref` folder. `Database`, `ItemData` and the save model itself are unchanged and stay here.
### Why
Second instance of the finding that retired `EVENTS.cs` from `com.hung.designpattern` in 0.4.0: a closed framework file that is really an open, per-game extension surface. Horror1Game could not adopt this package because its `GameData`/`CONSTANTS`/`ENUM.GAMEPLAY` carry horror content (`MovingBeltSlotData`, `InventoryItemType`, `INPUT_*_MAP`) and TemplateGame's carry rice-game content - neither can surrender to the other. Asmref-into-the-package-assembly is the seam already used by `com.hung.data` (`Locator.Data`) and `com.hung.services.iap` (`IAPData`); type names, namespaces and assemblies are unchanged, so **no consumer edits were needed**.


## [0.10.0] - 2026-07-11
### Changed
- Namespace pass (B1 Pass 3 - base): `Base`->`Hung.Base` (67 files), `Base.Init`->`Hung.Base.Init` (4 files). rootNamespace updated on `Hung.Base.asmdef`. **Explicitly excluded** (squats owned by other families, per Pass 1/2 precedent): `Base.UI` (ui package, Pass 6), `Base.Combat`/`Base.StatusEffects` (combat package, already Pass 4's territory).
- Folded in 3 bare-`Base` files physically living outside this package that squatted this namespace: `com.hung.data`'s `IDataService.cs`/`Locator.Data.cs` (Pass 2 deferred these here), `com.hung.combat`'s `IDamageable.cs`/`SkillExecutionContext.cs`/`ISingleInstanceStackable.cs`, `com.hung.tools`'s `MetaGuidUnifierWindow.cs`, `com.hung.ui`'s `UISButton.cs`/`MaskRaycastImage.cs` - all renamed `Base`->`Hung.Base` alongside this package's own files, consistent with the squat-follows-namespace convention.
### Fixed
- **New shadow-bug shape found this pass** (same family as Pass 1's `Hung.Utilities.Input` shadow, worse): any file with `namespace Hung.Base { using Base.UI; }` (indented) now self-shadows - "Base" resolves to the enclosing `Hung.Base` namespace itself (nested-dot-namespace member lookup) instead of the intended global `Base` root, since `Hung.Base` textually contains a reachable member literally named "Base" (itself). Fixed via `using global::Base.UI;` in `IUIService.cs`, `DebugManager.cs`, `InitCanvas.cs` (that last one via bare `using UI;`, which shadowed to `Base.UI` under the OLD nesting and had to become `using global::Base.UI;` too).
- **Flip side found in the deferred `Base.UI`/`Base.Combat` families:** files there that relied on implicit enclosing-namespace access to bare-`Base` members (e.g. `UICanvas.cs` calling `Locator` unqualified) broke once `Base` became `Hung.Base` and `Base.UI`/`Base.Combat` didn't move with it - fixed by adding explicit `using Hung.Base;` (`UICanvas.cs`, `UIButton.cs`, `UIManager.cs` in com.hung.ui; `DamageDealtContext.cs` in com.hung.combat). **Any future pass touching `Base.UI`/`Base.Combat`/`Base.StatusEffects` should grep for unqualified base-family symbol usage first** - this class of break will recur.

## [0.9.0] - 2026-07-11
- **Migration (B1 Pass 1 - foundations):** the embedded `Hung.Utilities.Input` assembly (`Runtime/Utilities/Input/`, physically inside this package's folder despite the name) namespace `Utilities.Input` -> `Hung.Utilities.Input` (LineDrawInput.cs, PlayerInput.cs). asmdef `rootNamespace` updated to match.
- Consumer fix for the designpattern/utilities Pass 1 rename: this package's `using DesignPattern;`/`using Utilities;`/`using Utilities.Timer;` occurrences updated to `Hung.*` wherever they referenced real package types (`GameData.cs`, `DebugManager.cs`, `InitManager.cs`, `SceneGameManager.cs`, `AOEDetection.cs`, and the two Utilities.Input files above); left unchanged wherever they referenced this package's own base-owned squatter types (`DevLog`/`UTILITIES`/`ObjectContainer`, still bare `Utilities`/`DesignPattern` — those three files stay in-package, in scope for Pass 3 instead).

## [0.8.0] - 2026-07-11
- Added `IRevenueEventSink` contract + `Locator.RevenueSink` slot (Ph6, `Runtime/Services/Contracts/Ads`) - ads report mediation revenue through this instead of a direct AppsFlyer/analytics reference.

## [0.7.0] - 2026-07-10
- BREAKING: `Base.IDamageable` moved to com.hung.combat (Hung.Combat.Core) and recomposed as `IDamageTarget + ICombatDamageable + ILogicEffectReceiver` (typed DamageHit damage; old float/ELEMENTAL overloads removed).
- Added `IDamageTarget`, `ILogicEffectReceiver` contracts (from PetVsMonster).
- `AOEDamage` moved to com.hung.combat (calls TakeDamage(DamageHit); illegal at L1). AOEDetection/TargetDetection infra unchanged.

## [0.6.0] - 2026-07-07
- Items contracts (`IItemService`, `Locator.Items`, `ItemId`, and `ITEM_RARITY`) moved in from `_Items/Base/` to `Runtime/Services/Contracts/Items/`. Their `Hung.Base.asmref` injection site deleted. `Hung.Items` (the concrete manager, `Items.json`) stays game-side — only the contracts are shared.

## [0.5.0] - 2026-07-07
- Stats trio union-merge with Horror1Game canon: `Stat` gains `StatModifiers` (readonly view over the modifier list) and ctor chaining (`Stat(float) : this()`) from H1; PR-side members kept as-is (`Copy()`, `RemoveAllModifiersExceptSource`, null-guards, `CompareModifierOrder`).
- Added `TrackingStat.cs` (H1-only, verbatim) — bounded-value tracker over a max/min `Stat` pair with boundary-cross events.
- `DevId` enum gained `Gameplay` member (+ matching DevColors entry) — H1 character core logs with `DevId.Gameplay`.
- `Hung.Base.asmdef` gained a direct `Hung.Utilities` reference (needed by `TrackingStat`'s `Utilities` namespace usage; previously only reachable via `Hung.Utilities.STimer`, which does not re-export it).

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (mechanical move, no code changes).

## [0.2.0] - 2026-07-07
- Service contracts moved in from module Base/ folders: Ads, IAP, Analytics, Audio (interfaces, Locator slots, enums). Their Hung.Base.asmref injection sites deleted.

## [0.3.0] - 2026-07-07
- IGameplayService merged with FistInTheMist superset (additive): AddMoney, ChangeScene, UsingSkill/IsCanUseSkill, IsActiveScene/SetActiveScene, OnEndGame, ShakeCamera, GameplayCamera, PlayerTransform. Left out: DecreaseSleepHp (FitM-game-specific, user decision).
- Reconciliation verdicts: FitM IDataService identical to com.hung.data (no-op); FitM IAds family + IAnalytic REJECTED (older shapes, zero implementations in FitM — Contracts/ versions are canon); IRewardItemService left out (depends on FitM GameData.ItemData).

## [0.4.0] - 2026-07-07
- UI contracts (IUIService, Locator.UI) moved in from _UI/Base to Runtime/Services/Contracts/UI/. UICanvas, UITransition, UIBackStack follow in wave 2b (IUIService generic constraints require UICanvas in this assembly). Surface redesigned same wave — see Docs/2026-07-07-ui-framework-design.md.
