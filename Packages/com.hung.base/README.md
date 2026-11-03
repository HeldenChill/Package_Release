# Hung Base (L1)

## Product-owned automatic startup selection

`Hung.Base.Composition.AutomaticStartupKind`, `AutomaticStartupProfile`, and `AutomaticStartupPolicy` select only legacy automatic Data/Analytics startup. A product may own exactly one `Resources/HungAutomaticStartup.asset`. Its independent `legacyPersistence` and `legacyAnalytics` flags default true; absence also enables both. Set a flag false only when product composition owns that startup path. Packages ship no profile instance.

`AutomaticStartupPolicy.ShouldStart(kind)` loads the asset at every callback, with no cross-play cache. `Evaluate(profile, kind)` exposes the same decision and rejects unknown selectors. Data returns before installing its default factory; Analytics returns before backend, Locator, tracking or store work. Explicit `PersistenceBootstrap.CreateDefault`, Analytics `Install` and `InstallDeferred` remain available. Opt-out does not shut down existing services or control unrelated SDK hooks.

Before rolling package/composition versions back, coordinate the profile too: older packages ignore it and resume automatic startup. See [serialized configuration ADR](../../Docs/adr/adr-e5-p2c-automatic-startup-profile.md) and [P2-C execution evidence](../../Docs/reports/horizon1-boundaries/p2c-shared/gap-closure/README.md). Player/provider/device verification is separate from the Editor proof.

## UI Separation

All UI contracts, `UICanvas`, `UIToggle`, `UIBackStack`, `UITransition`, scoping (`Hung.UI.Scoping`), and `IUIService` reside in `com.hung.ui`. Service resolution is provided via `Hung.UI.UILocator.Service`. The base package contains zero UI-library dependencies.

Core runtime: Locator + service contracts (LocatorServices), init flow (InitManager/InitCanvas/InitToggle/LoadStart), Stats, GameData, base app glue. Second assembly: Hung.Utilities.Input.

## Init Flow

The init flow (Hung.Base.Init) manages boot initialization, debug overlays, and scene transitions using plain Unity UI components.

### Public Members Index

| Member | Type | Assembly | File | Description |
|---|---|---|---|---|
| InitToggle | Class | Hung.Base | Runtime/Init/InitToggle.cs | Base-owned plain toggle component with ValueChanged event |
| InitCanvas | Class | Hung.Base | Runtime/Init/InitCanvas.cs | Plain MonoBehaviour boot canvas with InitToggle controls |
| InitManager.StartGameFromAutomation() | Method | Hung.Base | Runtime/Init/InitManager.cs | Automation boot seam bypassing manual UI start |
| InitManager.StartedFromAutomation | Property | Hung.Base | Runtime/Init/InitManager.cs | True if the game was booted via automation in this session |
| DebugManager.Initialized | Event | Hung.Base | Runtime/Init/DebugManager.cs | Raised at the end of OnInit when debug setup completes |
| DebugManager.SetUseIap(bool) | Method | Hung.Base | Runtime/Init/DebugManager.cs | Flips IAP enabled status for debug bypasses |

## ItemId Runtime Contract

Item identity is `ItemId`: a serialized, namespaced value such as `base.gold` or `pet_vs_monster.gem`. Runtime save data and item service APIs use `ItemId`; the removed numeric item enum is no longer part of the contract.

`GameData.SaveKey` is `GameData.item-id-v1`. Call `InitData(IEnumerable<ItemId>)` with catalog IDs during startup. Existing unknown saved IDs are preserved; missing catalog IDs are added without deleting saved entries.

Use `Locator.Items.GetPresentation(id)` for icons, display names, and rarity. Do not read metadata from `GameplayData.Items`; that legacy catalog has been removed.

## Typed Ids

Shared infrastructure for strongly-typed string identifiers conforming to stable recipes across runtime and editor tooling.

Generated constant names reject unescaped C# reserved keywords; use `@` to escape them.
Escaped and unescaped spellings of the same identifier cannot both be generated.
Registry conflict diagnostics refresh when the shared definition scan is invalidated.

### Public Types Index

| Type | Kind | Assembly | File |
|---|---|---|---|
| `IStableId` | Interface | `Hung.Base` | `Runtime/Services/Contracts/Ids/IStableId.cs` |
| `StableIdGrammar` | Static class | `Hung.Base` | `Runtime/Services/Contracts/Ids/StableIdGrammar.cs` |
| `StableIdJsonConverter<TId>` | Abstract generic class | `Hung.Base` | `Runtime/Services/Contracts/Ids/StableIdConverters.cs` |
| `StableIdTypeConverter<TId>` | Abstract generic class | `Hung.Base` | `Runtime/Services/Contracts/Ids/StableIdConverters.cs` |
| `AudioId` | Struct | `Hung.Base` | `Runtime/Services/Contracts/Audio/AudioId.cs` |
| `AudioIdJsonConverter` | Sealed class | `Hung.Base` | `Runtime/Services/Contracts/Audio/AudioId.cs` |
| `AudioIdTypeConverter` | Sealed class | `Hung.Base` | `Runtime/Services/Contracts/Audio/AudioId.cs` |
| `BaseAudioIds` | Static class | `Hung.Base` | `Runtime/Services/Contracts/Audio/BaseAudioIds.cs` |
| `StableIdEditorOption<TId>` | Readonly struct | `Hung.Base.Editor` | `Editor/Ids/StableIdEditorOption.cs` |
| `IStableIdEditorSource<TId>` | Interface | `Hung.Base.Editor` | `Editor/Ids/IStableIdEditorSource.cs` |
| `StableIdEditorConflict<TId>` | Sealed class | `Hung.Base.Editor` | `Editor/Ids/StableIdEditorConflict.cs` |
| `StableIdConstantsSource<TId>` | Sealed class | `Hung.Base.Editor` | `Editor/Ids/StableIdConstantsSource.cs` |
| `StableIdEditorRegistry<TId>` | Sealed class | `Hung.Base.Editor` | `Editor/Ids/StableIdEditorRegistry.cs` |
| `StableIdField` | Static class | `Hung.Base.Editor` | `Editor/Ids/StableIdField.cs` |
| `StableIdCodeGenerator` | Static class | `Hung.Base.Editor` | `Editor/Ids/StableIdCodeGenerator.cs` |
| `AudioIdDrawer` | Sealed class | `Hung.Base.Editor` | `Editor/Audio/AudioIdDrawer.cs` |

### How to add a new `<Thing>Id`

1. **Struct:** Create `<Thing>Id : IStableId, IEquatable<<Thing>Id>, IComparable<<Thing>Id>` copying `AudioId` (serialized `string value`, strict grammar validation, Parse/TryParse, equality, comparisons).
2. **Converters:** Create `<Thing>IdJsonConverter : StableIdJsonConverter<<Thing>Id>` and `<Thing>IdTypeConverter : StableIdTypeConverter<<Thing>Id>` subclasses.
3. **Constants:** Create a public static class `Base<Thing>Ids` with standard package id constants.
4. **Drawer:** Create `<Thing>IdDrawer : OdinValueDrawer<<Thing>Id>` copying `AudioIdDrawer` (using `StableIdField.Draw` and `StableIdEditorRegistry<<Thing>Id>.Shared`).
5. **Registration:** Create `[InitializeOnLoad] internal static class Base<Thing>IdsEditorRegistration` that registers `new StableIdConstantsSource<<Thing>Id>(typeof(Base<Thing>Ids))` with `StableIdEditorRegistry<<Thing>Id>.RegisterSource`.

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

## Service slot registry foundation

ServiceSlots is an additive main-thread registry for framework contracts. Register<T> derives its
owner from the implementation assembly and returns ServiceRegistration: Registered, Replaced,
AlreadyRegistered, RefusedDuplicateOwner or RefusedNull. Unregister<T> removes only the current
exact reference. Get<T>/TryGet<T> resolve explicit registrations; OwnerOf<T> returns their owner.
ResetForTests clears references/owners without disposing services; since stage C the
AutomaticPluginHost SubsystemRegistration reset clears it before each play (see Automatic plugin
composition). Existing Locator properties and startup hooks did not move in stage A.

Editor-only ServiceArchitectureTestRunner in Hung.Base.Tests exports focused real test results;
it is not runtime service API. Unsupported defaults, typed accessor migration and automatic plugin
composition belong to stages B/C.

## Owner-checked Locator integration

Locator properties are read-only ServiceSlots accessors. Register<T> derives the owner from the
implementation assembly; Unregister<T> removes only the current exact reference. Get<T> resolves
real registration before fallback; TryGet<T> and OwnerOf<T> exclude defaults. SetFallback<T>
rejects null or a different default within one session. ResetForTests clears real owners and
recreates session defaults. ServiceSlotLifecycle.CanInitialize prevents a second active scene
owner from starting work; scene owners unregister in OnDestroy.

UnsupportedAdsService returns Unsupported outcomes, never grants rewards and completes legacy
continuations once. UnsupportedIapService invokes failure callbacks. These defaults cover Ads/IAP
only; remaining service contracts migrate in their own waves. IGameplayService and Locator.Gameplay
were removed from Base. TemplateGame presentation uses its product-owned ITemplateGameplay binding.
Automatic plugin collection and general suppression are a later source stage, not part of this cutover.

## Automatic plugin composition

AutomaticPluginHost.Collect accepts explicit FeatureRegistration declarations before boot. Plugins
submit during AfterAssembliesLoaded; one BeforeSceneLoad callback validates/freezes declarations and
starts existing FeatureHost in dependency order. GetReport returns actual post-boot state; Shutdown
is reverse and idempotent. The host owns Base's runtime reset, disposing its old session before
clearing ServiceSlots. Collection/lookup never initializes SDKs implicitly.

Optional product Resources/HungPluginStartup.asset uses PluginStartupProfile.DisabledFeatureIds.
PluginStartupPolicy.ShouldStart/Evaluate validate stable lowercase IDs; missing profile enables
new-policy plugins. OverrideForTests is scoped and resets between sessions. Build validation rejects
duplicate Resources identities and invalid entries. Packages ship no profile instance. Existing
AutomaticStartupProfile legacy flags keep their meaning; future Analytics migration must combine
legacyAnalytics AND new-policy enablement. No production bootstrap was migrated in this stage.

## Horizon 1 BW4 explicit Items/rewards

Opt-in focused presentation, immutable flat reward offers, frozen pending payloads and checked recoverable feature finalization. Existing managers, assets, ItemId values and aggregate Item/IAP APIs remain compatibility paths. Schema 2 applies only to the selected explicit DailyReward/SpinWheel definitions; never compose legacy owners against that same selected root. No game integration or release acceptance is claimed.

See [explicit composition, schema and recovery contract](../com.hung.data/Documentation~/bw4-explicit-rewards.md). Evidence: `Docs/reports/horizon1-boundaries/service-architecture/bw4-items-rewards/`.
