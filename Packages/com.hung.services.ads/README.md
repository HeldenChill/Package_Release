# Hung Services Ads

Ads service wrapper, mediation-vendor-isolated (Ph6, paper §11.2).

Contracts (`IAdsService`/`IAdsRequestService`/`IAds`/`IRewardAds`/`IInterAds`/`IBannerAds`/`IRevenueEventSink` + Locator slots + enums) live in `com.hung.base` `Runtime/Services/Contracts/Ads`.

## Assembly layout

- `Hung.Ads` (neutral, `autoReferenced: true`) — `GameBannerAds`/`GameInterAds`/`GameRewardAds`/`GameAppOpenAds` (the game-facing orchestration, vendor-free), the `Contracts/` provider interfaces (`IBannerAdsProvider`/`IInterstitialAdsProvider`/`IRewardedAdsProvider`), and `Doubles/NullAdsService`. Holds vendor components only as plain `MonoBehaviour` fields, cast to the provider interface at runtime — this is what lets a vendor folder be deleted without touching this assembly.
- `Hung.Ads.Integration.Max` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_MAX]`) — AppLovin MAX adapters (`BannerAds`/`InterstitialAds`/`RewardedAds`/`AppOpenAds`/`MaxInit`). `MaxInit.cs` and `AppOpenAds.cs` are fully dead code (whole-file comments / zero live callers) kept as-is, not revived by this split.
- `Hung.Ads.Integration.AdMob` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_ADMOB]`) — Google AdMob adapters. **Entirely dead code** (every class body is commented out) as of Ph6 — moved here for correct isolation, not revived. `HUNG_ADS_ADMOB` is not set in ProjectSettings; this assembly does not compile into the current build.
- `Hung.Ads.Integration.IronSource` (`autoReferenced: false`, `defineConstraints: [HUNG_ADS_IRONSOURCE]`) — the LevelPlay/ironSource mediation SDK adapters, **and** `AdsManager` itself (the `IAdsService` composition root / `Locator.Ads` owner). `AdsManager` lives here rather than in the neutral assembly because it directly drives `Unity.Services.LevelPlay` init and impression tracking — it is not a swappable per-format provider like Banner/Inter/Reward, it is this game's actual mediation SDK lifecycle owner.

Deleting the AdMob or Max folder breaks only its own integration assembly. Deleting the IronSource folder breaks ad initialization entirely (no other assembly assigns `Locator.Ads`) — that is intentional, not a defect: LevelPlay is the umbrella mediation SDK this game ships on today, not an interchangeable peer of Max/AdMob.

## Initialization order

Analytics before ads (Ph6 step 6). `AdsManager.Awake` (`[DefaultExecutionOrder(-50)]`) blocks on `FirebaseManager.Ins.IsAvailable` before proceeding — Firebase is Analytics' own composition root dependency. `AnalyticsManager` (`[DefaultExecutionOrder(-100)]`) must run its `Awake` first so `Locator.Analytics`/`Locator.RevenueSink` are assigned before any ad callback (in particular `AdsManager.OnImpressionDataReady`) fires. The execution-order attributes already encode this; this section documents *why* so the ordering isn't accidentally reversed.

1. `AnalyticsManager.Awake` — assigns `Locator.Analytics` and `Locator.RevenueSink`.
2. `AdsManager.Awake` — assigns `Locator.Ads`, waits on Firebase readiness, then inits LevelPlay.
3. Ad revenue events call `Locator.RevenueSink?.OnRevenue(...)` — null-safe if analytics hasn't registered yet, but should never be null given the order above.

## Revenue reporting

`Hung.Ads` no longer references `com.hung.services.analytics` or AppsFlyer directly. `AdsManager.OnImpressionDataReady` calls `Locator.RevenueSink?.OnRevenue(source, value, currency, extra)`; `AnalyticsManager` implements `IRevenueEventSink` and forwards to `AppsFlyer.logAdRevenue`, remapping the neutral `extra` keys (`country`/`ad_unit`/`ad_type`/`placement`) to AppsFlyer's own `AdRevenueScheme` constants.

## Test doubles

`Doubles/NullAdsService : IAdsService` - all-no-op, safe to assign to `Locator.Ads` in EditMode/PlayMode tests or vendor-free builds. Request APIs complete with `Unsupported` and diagnostic `null-service`; legacy rewarded wrappers invoke hidden/continuation only, not fake reward success.

## Request and pause behavior

`GameRewardAds` and `GameInterAds` now route provider callbacks through request-scoped sessions. Each `AdsShowRequest` gets one terminal `AdsShowResult`; late provider callbacks return `DuplicateIgnored` and do not invoke the caller again.

Provider-backed shows acquire an Ads `PauseLease` through `Locator.Pause` and release only that lease when the request terminates. Debug, premium/remove-ads, unavailable, unsupported, and misconfigured paths do not acquire an Ads lease. `AdsManager.OnApplicationPause` logs only and no longer resets global time scale.

`AdsLoadQueue` serializes load requests and advances when load/display paths complete. Missing serialized provider components are guarded and fail as request outcomes instead of Awake null references.

## Known limitations

Device SDK ad cycles, Google/Apple sandbox behavior, and representative player builds remain candidate gates.
