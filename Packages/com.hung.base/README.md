# Hung Base (L1)

## Product-owned automatic startup selection

`Hung.Base.Composition.AutomaticStartupKind`, `AutomaticStartupProfile`, and `AutomaticStartupPolicy` select only legacy automatic Data/Analytics startup. A product may own exactly one `Resources/HungAutomaticStartup.asset`. Its independent `legacyPersistence` and `legacyAnalytics` flags default true; absence also enables both. Set a flag false only when product composition owns that startup path. Packages ship no profile instance.

`AutomaticStartupPolicy.ShouldStart(kind)` loads the asset at every callback, with no cross-play cache. `Evaluate(profile, kind)` exposes the same decision and rejects unknown selectors. Data returns before installing its default factory; Analytics returns before backend, Locator, tracking or store work. Explicit `PersistenceBootstrap.CreateDefault`, Analytics `Install` and `InstallDeferred` remain available. Opt-out does not shut down existing services or control unrelated SDK hooks.

Before rolling package/composition versions back, coordinate the profile too: older packages ignore it and resume automatic startup. See [serialized configuration ADR](../../Docs/adr/adr-e5-p2c-automatic-startup-profile.md) and [P2-C execution evidence](../../Docs/reports/horizon1-boundaries/p2c-shared/gap-closure/README.md). Player/provider/device verification is separate from the Editor proof.

## UI Scope Contract

`IUIAcquisitionService` and `UIAcquireResult<T>` provide an optional addressed acquisition contract with `UIAcquireFailure` categories. `IUIService` remains source compatible; consumers that need an addressed result can use an implementation that also exposes `IUIAcquisitionService`.

`Hung.UI.Scoping.UiScopePath`, `UiScope`, `IScopedUi`, and `UiScopeRegistry` provide hierarchy-based canvas lifetime rules. `UICanvas.Scope` defaults to `UiScope.Global`, so existing canvases retain their old lifetime. A scoped canvas binds to the current path on open; `UiScopeRegistry.Enter(path)` closes canvases whose bound scope has ended. Product-specific path names and phase transitions remain in the consuming game.

`UICanvas.OnScopeExit()` unregisters before its outro, and a later `Open()` invalidates an older close completion. The generic registry can be instantiated in tests; production code uses `UiScopeRegistry.Instance` unless a canvas overrides `ScopeRegistry`.

`UICanvas.Open`/`Close` are virtual. `Setup(param)` pushes the back entry; `CloseDirectly(param)` closes with no transition and no `OnClose`. `UICanvas.BackPushPolicy` (`UIBackPushPolicy.OnOpen` default, `ExplicitSetup`) chooses whether `Open` pushes. `IUIService.CloseUIDirectly`, `IsContain`, `IsInBackStack` and `UIBackStack.Contains` query and close without the back key.

Core runtime: Locator + service contracts (LocatorServices), init flow (InitManager/LoadStart), Stats, GameData, base app glue. Second assembly: Hung.Utilities.Input.

## ItemId Runtime Contract

Item identity is `ItemId`: a serialized, namespaced value such as `base.gold` or `pet_vs_monster.gem`. Runtime save data and item service APIs use `ItemId`; the removed numeric item enum is no longer part of the contract.

`GameData.SaveKey` is `GameData.item-id-v1`. Call `InitData(IEnumerable<ItemId>)` with catalog IDs during startup. Existing unknown saved IDs are preserved; missing catalog IDs are added without deleting saved entries.

Use `Locator.Items.GetPresentation(id)` for icons, display names, and rarity. Do not read metadata from `GameplayData.Items`; that legacy catalog has been removed.

## Persistence Contract

`Hung.Base.Persistence` owns the DI-neutral contracts used by save consumers. Existing synchronous calls remain valid:

```csharp
Database.Save(gameData, GameData.SaveKey);
GameData loaded = Database.Load<GameData>(GameData.SaveKey);
```

`Database` is a compatibility facade. New package internals should receive `IPersistenceService` explicitly and use a registered `SaveDefinition<T>`. Installing `com.hung.data` configures the default service before scene load. A Base-only project must assign `Database.ServiceFactory` and `Database.CompatibilityDefinitionFactory`; it does not fall back to raw PlayerPrefs persistence.

Known debt (carried from extraction, see Docs/audit/canon-decisions.md):
- Hung.Base.asmdef references spine-unity, Unity.TextMeshPro, Unity.InputSystem - vendor refs in L1; spine removal planned Phase 1b.

## Purchase Integrity Contracts

Base owns only vendor-neutral purchase contracts. It does not reference Unity Purchasing, Steamworks, store SDKs, receipts, or product reward data.

Use `PurchaseProductId` as the stable logical identity (`starter-pack`, `gold.pack_1`). `IPurchaseIntegrityService` returns explicit availability, purchase, restore, reconcile, and transaction snapshot results. `IPurchaseGrantHandler` is implemented by the consuming game because reward mutation and entitlement state are game-owned.

DesktopPremium projects that sell the game but do not sell in-game transactions can install `UnsupportedPurchaseIntegrityService` or keep only the legacy `UnsupportedIapService`. Both fail deterministically and never report a fake purchase success.

## Ads Request Contracts

Base owns the vendor-neutral Ads request/result surface. `AdsRequestId`, `AdsShowRequest`, `AdsShowResult`, and `IAdsRequestService` describe request-scoped rewarded and interstitial completion without referencing mediation SDK types.

`AdsShowResult.IsEarnedReward` is true only for rewarded requests that complete with provider reward evidence. `ShouldContinueFlow` allows callers to continue after completed, skipped, unavailable, or unsupported outcomes while treating misconfiguration and overlap as blocking failures.

`IRewardAds.IsCanShowFor(Placement)` is a default interface method (default `false`) reporting whether a rewarded ad can be shown now for a specific placement — for consumers using per-placement ads routing (`com.hung.services.ads` routed mode). Implementations that predate placement routing need no change.

## Pause Lease Service

`IPauseService` coordinates pause ownership through `PauseLease` values. Releasing an Ads lease releases only that Ads request and never clears active popup, tutorial, gameplay, application, or debug pauses.

The first lease captures the current scale; the last release restores it only if the scale is still 0, so a pause never changes the game speed and never overwrites a speed another owner set meanwhile. `TryDeferResumeScale(scale)` lets a caller change the resume speed while paused without unpausing (returns false when nothing is paused, so the caller writes the scale itself).

`PauseService` writes through `ITimeScale`; production composition can use `UnityTimeScale`, while tests can inject a deterministic scale wrapper. `Locator.Pause` is a compatibility slot during composition migration.

## Time And Reward Integrity Contracts

Base owns the neutral UTC clock and reward claim contracts used by LiveOps packages. `IClock.UtcNow` must be UTC; production code uses `SystemClock`, while tests and composition can inject deterministic clocks.

`RewardDayPolicy` resolves stable `RewardDayKey` values from UTC instants and a reset offset in minutes after midnight UTC. New time-gated rewards must persist full reward-day keys, not day-of-year-only values.

`RewardClaimId.Create(feature, track, cycle, slot, definition, profile)` creates deterministic profile-scoped IDs with FNV-1a 64-bit lowercase hex over canonical UTF-8 parts. Domain callers own stable identity; retryable claims must not use runtime-generated GUIDs.

`IRewardClaimCoordinator` describes the recoverable reward claim protocol. It persists intent, calls an idempotent `IRewardGrantService`, and finalizes feature state, but it is not a physical cross-store transaction. The consuming game must persist item balance and reward receipt together for exactly-once value.

## Explicit feature composition (Horizon 1 P2-A, unreleased)

`Hung.Base.Composition.FeatureHost` is an instance-owned synchronous lifecycle
coordinator. Products explicitly register selected features and supply typed
capabilities through constructors or closures. Feature dependencies use exact
ordinal keys; there is no reflection discovery, automatic dependency installation,
service resolution or global feature host. Unselected features remain inactive.

Required failure blocks product readiness and unwinds initialized features.
Optional failure blocks its dependent branch while independent features continue;
a required dependent makes that failure fatal. Missing dependencies/cycles are
checked before callbacks. Shutdown cleans partial initialization and runs once,
in reverse successful-start order; cleanup exceptions are recorded and fatal.
Shutdown does not undo committed saves, grants, purchases or external effects.

`Start()` caches the historical startup report. `GetReport()` returns current
immutable states and diagnostics, including after disposal. Repeated Start has no
callbacks; registration freezes on first Start. Dispose is idempotent; Start or
Register after disposal throws. Callback reentry into lifecycle operations is
rejected. Use a fresh host for retry and durable replay guards for first-run work.

`IAudioSettings` exposes live BGM/SFX mute facts. `IItemBalanceReader` returns
detached `ItemBalance` snapshots, with zero distinguished from unavailable or
unknown items. `IItemSpendService` is a separate durable capability: a query-only
adapter must not advertise successful spend. Products own catalogs, lock/economy
policy, settings commands, persistence and presentation. Reuse IRewardGrantService
and IAdsEntitlements; do not change shipped IDs or existing enum values.

This helper shows the host seam with a product-supplied playback adapter. It does
not adapt the existing AudioManager automatically:

```csharp
using System;
using Hung.Base;
using Hung.Base.Composition;

public static class ProductAudioCompositionExample
{
    public static FeatureHost Start(IAudioSettings settings,
        Action<IAudioSettings> initializePlayback, Action shutdownPlayback)
    {
        if (initializePlayback == null) throw new ArgumentNullException(nameof(initializePlayback));
        if (shutdownPlayback == null) throw new ArgumentNullException(nameof(shutdownPlayback));
        var host = new FeatureHost();
        host.Register(new FeatureRegistration("settings", true, Array.Empty<string>(),
            () => new FeatureStartResult(settings != null, "AUDIO_SETTINGS_MISSING"),
            () => { })); // Borrowed settings: this callback owns no resource.
        host.Register(new FeatureRegistration("audio", true, new[] { "settings" },
            () =>
            {
                initializePlayback(settings);
                return new FeatureStartResult(true);
            }, shutdownPlayback));
        FeatureBootReport report = host.Start();
        if (!report.ProductReady)
        {
            host.Dispose();
            throw new InvalidOperationException("Required audio composition failed; inspect the report diagnostics.");
        }
        return host; // Caller publishes gameplay entry and disposes at host teardown.
    }
}
```

Keep one initialization owner: disable/replace the old automatic owner during a
separately verified host cutover. Do not combine explicit startup with the same
component's Awake/Start startup. Publish legacy Locator bridges only after the
required readiness barrier; clear only registrations owned by that bridge.

P2-A retains GameData, DataManager, IItemService and legacy serialized contracts.
It does not extract TemplateGame, migrate consumers, integrate typed audio cues
or remove Base's existing vendor dependencies. See boundary design section 18.45
and the P2-A report for exact evidence and remaining adoption gates.
