# Hung Services Ads

Ads service wrapper, mediation-vendor-isolated (Ph6, paper §11.2).

Contracts (`IAdsService`/`IAdsRequestService`/`IAds`/`IRewardAds`/`IInterAds`/`IBannerAds`/`IRevenueEventSink` + Locator slots + enums) live in `com.hung.base` `Runtime/Services/Contracts/Ads`.

## Assembly layout

- `Hung.Ads` (neutral, `autoReferenced: true`) — `GameBannerAds`/`GameInterAds`/`GameRewardAds`/`GameAppOpenAds` (the game-facing orchestration, vendor-free), the `Contracts/` provider interfaces (`IBannerAdsProvider`/`IInterstitialAdsProvider`/`IRewardedAdsProvider`), and `Doubles/NullAdsService`. Holds vendor components only as plain `MonoBehaviour` fields, cast to the provider interface at runtime — this is what lets a vendor folder be deleted without touching this assembly.
- `Hung.Ads.Integration.Max` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_MAX]`) — AppLovin MAX adapters (`BannerAds`/`InterstitialAds`/`RewardedAds`/`AppOpenAds`/`MaxInit`). `MaxInit.cs` and `AppOpenAds.cs` are fully dead code (whole-file comments / zero live callers) kept as-is, not revived by this split.
- `Hung.Ads.Integration.AdMob` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_ADMOB]`) — Google AdMob adapters on Google Mobile Ads Unity 9.1.0 (`AdMobRewarded`/`AdMobInterstitial`/`AdMobBanner`/`AdMobAdsInstaller`). `AppOpenAds.cs` is untouched (App Open ads are not routed, D6). `HUNG_ADS_ADMOB` is set in ProjectSettings as of the ads routing feature; this assembly compiles into the current build.
- `Hung.Ads.Integration.IronSource` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_IRONSOURCE]`) — the LevelPlay/ironSource mediation SDK adapters, **and** `AdsManager` itself (the `IAdsService` composition root / `Locator.Ads` owner). `AdsManager` lives here rather than in the neutral assembly because it directly drives `Unity.Services.LevelPlay` init and impression tracking — it is not a swappable per-format provider like Banner/Inter/Reward, it is this game's actual mediation SDK lifecycle owner.

Deleting the AdMob or Max folder breaks only its own integration assembly. Deleting the IronSource folder breaks ad initialization entirely (no other assembly assigns `Locator.Ads`) — that is intentional, not a defect: LevelPlay is the umbrella mediation SDK this game ships on today, not an interchangeable peer of Max/AdMob.

## Initialization order

Analytics before ads. `AnalyticsBootstrap` (`com.hung.services.analytics`, `RuntimeInitializeOnLoadMethod` `BeforeSceneLoad`) assigns `Locator.Analytics` and `Locator.RevenueSink` before any scene or `Awake` runs, so no ad callback (in particular `AdsManager.OnImpressionDataReady`) can see a null sink. `AdsManager.Awake` (`[DefaultExecutionOrder(-50)]`) assigns `Locator.Ads` and inits LevelPlay; it no longer waits for Firebase.

1. `AnalyticsBootstrap` (BeforeSceneLoad) - assigns `Locator.Analytics` and `Locator.RevenueSink`.
2. `AdsManager.Awake` - assigns `Locator.Ads`, then inits LevelPlay.
3. Ad revenue events call `Locator.RevenueSink?.OnRevenue(...)` - null-safe, never null given the order above.

## Revenue reporting

`Hung.Ads` does not reference `com.hung.services.analytics` or any analytics SDK. `AdsManager.OnImpressionDataReady` calls `Locator.RevenueSink?.OnRevenue(source, value, currency, extra)` with neutral `extra` keys `mediation` (`ironsource`), `country`, `ad_unit`, `ad_type`, `placement`. The analytics proxy routes it to backends subscribed to the `Revenue` category; each backend maps the keys to its own scheme.

## Test doubles

`Doubles/NullAdsService : IAdsService` - all-no-op, safe to assign to `Locator.Ads` in EditMode/PlayMode tests or vendor-free builds. Request APIs complete with `Unsupported` and diagnostic `null-service`; legacy rewarded wrappers invoke hidden/continuation only, not fake reward success.

## Request and pause behavior

`GameRewardAds` and `GameInterAds` now route provider callbacks through request-scoped sessions. Each `AdsShowRequest` gets one terminal `AdsShowResult`; late provider callbacks return `DuplicateIgnored` and do not invoke the caller again.

Provider-backed shows acquire an Ads `PauseLease` through `Locator.Pause` and release only that lease when the request terminates. Debug, premium/remove-ads, unavailable, unsupported, and misconfigured paths do not acquire an Ads lease. `AdsManager.OnApplicationPause` logs only and no longer resets global time scale.

`AdsLoadQueue` serializes load requests and advances when load/display paths complete. Missing serialized provider components are guarded and fail as request outcomes instead of Awake null references.

## Routed mode

Optional multi-provider routing lets rewarded/interstitial/banner serve from MAX and AdMob (alone or combined: chain, per-format, parallel, per-placement), switchable at compile time (`HUNG_ADS_MAX`/`HUNG_ADS_ADMOB`) and at runtime. The routing core is vendor-generic: a third vendor is one more `IAdsProviderInstaller` plus an asmdef, with no change to the router or routed providers. A Yandex integration was planned but is deferred and may be dropped (see compile defines table).

To enable: assign an `AdsRoutingConfig` asset (`Hung/Ads/Routing Config`) to `AdsManager.routingConfig`. When set, `AdsManager.Start` builds the routed providers and swaps the `Game*Ads` provider registry for them regardless of `ADS_TYPE`; the `providerBindings` arrays on `GameRewardAds`/`GameInterAds`/`GameBannerAds` are ignored. Empty those arrays and remove any scene MAX provider components when switching a scene to routed mode — MAX SDK callbacks are global, and a second MAX component would also receive them.

`routingConfig == null` (the default) is legacy behavior, byte-for-byte — existing scenes are unaffected.

Config shape (`AdsRoutingData`, baked into the SO):

```json
{
  "providers": [
    { "id": "max", "enabled": true, "androidRewarded": "...", "iosRewarded": "..." },
    { "id": "admob", "enabled": true, "androidRewarded": "...", "iosRewarded": "..." }
  ],
  "formats": [
    { "format": "Rewarded", "mode": "Chain", "order": ["max", "admob"] },
    { "format": "Banner", "mode": "Chain", "order": ["admob"] }
  ],
  "placements": [
    { "format": "Rewarded", "placement": "REROLL_SKILL_CARD", "mode": "Chain", "order": ["admob"] }
  ]
}
```

Provider ids are the strings `"max"`, `"admob"`, `"yandex"` (`AdsProviderId`). Chain loads one provider at a time in order; Parallel loads all at once and takes the first ready (Banner does not support Parallel — coerced to Chain with a one-time warning). A placement route overrides the format's default route for that placement only; every other placement falls back to the format route.

Remote override: assign `AdsManager.RoutingOverrideJson` (a `static Func<string>`) to return routing-override JSON — same shape as above, every section optional, enum names case-insensitive. Read on every load cycle; a malformed override or one naming a disabled/unknown provider id is rejected and the baked config keeps working (no empty route). This is a seam only — no Firebase adapter is provided.

**D5 rule:** a vendor is integrated directly (as an installed `IAdsProviderInstaller`) **or** as a MAX mediation adapter, never both — duplicate SDKs conflict. Not enforced in code; verify your MAX mediation setup before also enabling the same vendor's direct routed provider.

Compile defines:

| Define | Enables |
|---|---|
| `HUNG_ADS_MAX` | `Hung.Ads.Integration.Max` — direct MAX providers (`MaxAdsInstaller`) |
| `HUNG_ADS_ADMOB` | `Hung.Ads.Integration.AdMob` — direct AdMob providers (`AdMobAdsInstaller`) on Google Mobile Ads Unity 9.1.0 |
| `HUNG_ADS_YANDEX` | Reserved, **not implemented and no longer planned**. No `Hung.Ads.Integration.Yandex` assembly exists and the Yandex Mobile Ads SDK is not imported. `AdsProviderId.Yandex` remains only as a stable id string. Do not set this define. |

Any subset of the compiled-in defines works; a provider with no routing-config entry is skipped with a warning even if its define is compiled in.

### Routing API index

- `Hung.Ads.AdsProviderId` — provider id string constants (`Max`, `AdMob`, and the unused reserved `Yandex`).
- `Hung.Ads.AdsFormat`, `Hung.Ads.AdsRouteMode` — format/mode enums (persisted as int; never renumber).
- `Hung.Ads.AdsProviderEntry`, `Hung.Ads.AdsFormatRoute`, `Hung.Ads.AdsPlacementRoute`, `Hung.Ads.AdsRoutingData` — routing config data model.
- `Hung.Ads.AdsRoutingConfig` — `ScriptableObject` wrapper, assign to `AdsManager.routingConfig`.
- `Hung.Ads.AdsRoutingOverride.TryApply` — parses and merges a remote override JSON string.
- `Hung.Ads.AdsRoute` — a resolved route (mode + provider ids in priority order).
- `Hung.Ads.AdsRouter` — resolves routes per format/placement; reads the remote override.
- `Hung.Ads.AdsProviderSet` — installed vendor providers keyed by id.
- `Hung.Ads.IAdsProviderInstaller`, `Hung.Ads.AdsInstallers` — installer contract and compiled-in registry.
- `Hung.Ads.RoutedRewardedProvider`, `Hung.Ads.RoutedInterstitialProvider`, `Hung.Ads.RoutedBannerProvider` — composite providers implementing the existing provider interfaces.
- `Hung.Ads.AdsRoutedComposer` — builds the routed providers and a legacy-compatible `AdsProviderRegistry` from config + installers.
- `Hung.Ads.AdsManager.routingConfig` / `AdsManager.RoutingOverrideJson` — the two entry points into routed mode.
- `Hung.Ads.Integration.Max.MaxAdsInstaller`, `Hung.Ads.Integration.AdMob.AdMobAdsInstaller` — per-vendor installers.
- `Hung.Base.IRewardAds.IsCanShowFor(Placement)` — default interface method (default `false`); `GameRewardAds.IsCanShowFor` delegates to the routed provider's placement-route readiness in routed mode, or `activeProvider.IsCanShow` in legacy mode.
- `RoutedInterstitialProvider.IsCanShowFor(Placement)` — same readiness check for interstitial; `GameInterAds.Show` uses it.

### Show safety in routed mode

- **Placement-aware gate.** `GameRewardAds.Show` and `GameInterAds.Show` decide "can show" from the request's own placement route, not the format default route. Otherwise a placement whose route is not ready would pass the gate, take the pause lease, then fail inside the routed provider and leave the game paused.
- **Show lock timeout.** A routed provider blocks a second show while one is in flight. If the vendor never reports hidden / done / display-fail, the lock expires after 180 seconds (`showLockTimeout` constructor argument, `clock` is injectable for tests) instead of blocking every later show for the session.
- **AdMob init timeout.** If `MobileAds.Initialize` never calls back, each AdMob provider reports load-fail after `AdMobAdsInstaller.InitWaitTimeoutSeconds` (15) so a chain such as `[admob, max]` advances. This path needs the real SDK and is compile-verified only.
- A reward that arrives after hidden is still forwarded once. A reward from a provider that is not the current show is dropped.

## Known limitations

Device SDK ad cycles, Google/Apple sandbox behavior, and representative player builds remain candidate gates. Yandex Mobile Ads (Task 12 of the ads routing plan) is deferred and may be dropped.

Open items found by the 0.7.0 review and not yet fixed:

- A late reward from the previous show of the same vendor cannot be told apart from a reward for the next show. The reward-after-hidden case has to be accepted, so this stays ambiguous.
- Routed mode adds new MAX components in `AdsManager.Start`. MAX SDK events are static, so any MAX components left on the scene prefab also handle every event and analytics are counted twice. Remove them when assigning `routingConfig`.
- A banner that fails moves permanently to the next provider and is never retried or reset; nothing retries after the whole chain fails.
- A remote override entry that omits `enabled` re-enables a provider the baked config disabled (`enabled` defaults to true). A placement value that is an undefined number is accepted.
- The AdMob providers report only `AdsRewardOffer` to analytics. Load, show, complete, click and fail funnel events exist only for MAX.
- `AdsRouter.Resolve` allocates on every call, so polling `IsCanShowFor` from UI `Update` produces GC churn. Cache resolved routes if needed.
- `RoutedRewardedProvider` and `RoutedInterstitialProvider` duplicate route-building and subscription code.
