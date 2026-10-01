# Changelog

## [0.8.1] - 2026-10-01
### Fixed
- Rewarded preload waits until the hidden callback, after request completion. Loading from reward-earned while MAX was still showing rejected the load and left the next ad stuck loading. Closing without earning a reward also preloads the next ad.
- Interstitial request completion and next preload now run in the same main-thread callback.
- Optional analytics telemetry no longer throws when no analytics service is registered.
### Added
- AdsManager has independent Rewarded Ads Enabled, Interstitial Ads Enabled, and Banner Ads Enabled Inspector switches, all defaulting to true for existing scenes/prefabs. Disabled formats do not load, retry, initialize banners, or show; fullscreen requests complete Skipped with diagnostic `ads-disabled` and never grant a reward.

## [0.8.0] - 2026-09-30
### Changed
- The ads module no longer reads or writes `GameData`. Remove-ads, premium remove-ads and level index come from `Locator.AdsEntitlements` (read live each check; unregistered = `NullAdsEntitlements`: ads shown, level 0). Fixes the rewarded/interstitial/banner NRE when the host's `GameData` type is not `Hung.Base.GameData`.
- `watchingAdsCount` / `playGameAdsCount` moved to the package-owned `AdsSessionCounters`: session-only, reset every launch, no longer persisted in the save. **Hosts must register an `IAdsEntitlements`** to keep remove-ads working.
- Requires `com.hung.base` 0.26.0.

## [0.7.3] - 2026-09-30
### Fixed
- NullReferenceException in `GameRewardAds.OnAdsReceiveReward` / `GameInterAds` after an ad finished when the host scene has no `MainThreadDispatcher`: the reward was never granted. Callbacks now enqueue on the dispatcher when present and run inline otherwise (MAX already raises them on the main thread).
- Rewarded ad finished but the reward callback never fired when a `MainThreadDispatcher` exists: the reward was queued to the next frame while the hidden callback ran inline and completed the request as Skipped first. Hidden now goes through the same queue, so reward is always marked before completion.

## [0.7.2] - 2026-09-30
### Fixed
- Rewarded and interstitial ads never loaded when the host did not assign `_OnAddLoadAds`: nothing called `LoadAds`, so `MaxSdk.IsRewardedAdReady` always returned false ("was not requested"). `GameRewardAds` / `GameInterAds` now default `_OnAddLoadAds` to an immediate load; hosts can still override it with their own queue.

## [0.7.1] - 2026-09-29
### Changed
- Custom `af_*` ad events use `Locator.Analytics.LogEvent(name, AnalyticsCategory.Ads)` (base 0.25.0).
- IronSource revenue `extra` carries `mediation = ironsource`.
- Dropped the `com.hung.services.analytics` dependency and the `Hung.Analytics` reference from `Hung.Ads.Integration.IronSource`; removed the IronSource loading wait on `FirebaseManager` and the unused `AnalyticsManager` field.
- `AdsManager.prefab`: removed the missing-script `AnalyticsManager` component and the nested `AppsFlyerObject` (it double-initialized AppsFlyer next to `AppsFlyerBackend`). Shipped in `release-2026.09.29.3`.

## [0.7.0] - 2026-09-29
### Added
- Multi-provider ads routing: `AdsRouter`, `AdsRoutingConfig`/`AdsRoutingData`/`AdsRoutingOverride`, `AdsProviderSet`, `IAdsProviderInstaller`/`AdsInstallers`, `RoutedLoadCycle`, `RoutedRewardedProvider`, `RoutedInterstitialProvider`, `RoutedBannerProvider`, `AdsRoutedComposer`. Lets rewarded/interstitial/banner serve from MAX, AdMob and Yandex, alone or combined (chain, per-format, parallel, per-placement), switchable at compile time and at runtime. See README "Routed mode".
- `AdMobRewarded`/`AdMobInterstitial`/`AdMobBanner`/`AdMobAdsInstaller` — AdMob integration rewritten on Google Mobile Ads Unity 9.1.0 (was fully dead/commented-out code).
- `MaxAdsInstaller` and `SetAdUnitId(string)` on the MAX rewarded/interstitial/banner providers, for routed mode.
- `AdsManager.routingConfig` (optional `AdsRoutingConfig` field) and `AdsManager.RoutingOverrideJson` (static remote-override seam).
- Requires `com.hung.base` `0.24.0` (adds `IRewardAds.IsCanShowFor(Placement)`).

- `RoutedInterstitialProvider.IsCanShowFor(Placement)`.

### Fixed (found in review before release)
- `GameRewardAds.Show` / `GameInterAds.Show` gated on the format default route while the routed provider showed the placement route. A placement route that was not ready took the pause lease, failed inside the routed provider and left the request open. Both now gate on the request's placement.
- Routed reward/interstitial providers could block every later show for the session if a vendor never reported hidden / done / display-fail. The lock now expires after 180 seconds (`showLockTimeout`, injectable `clock`).
- AdMob providers waited forever for SDK init and never reported load-fail, stalling a chain. They now report load-fail after 15 seconds and skip deferred loads on destroyed components.

### Changed
- None for legacy mode: `routingConfig == null` is byte-for-byte the existing behavior.

### Known gaps
- Yandex Mobile Ads integration is deferred and may be dropped. No `Hung.Ads.Integration.Yandex` assembly exists; `HUNG_ADS_YANDEX` is reserved and unused.
- Open review items (late reward across shows, duplicate MAX components, banner retry, override `enabled` default, AdMob analytics parity, route allocation) are listed in the README under Known limitations.

## [0.6.6] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6; com.hung.services.analytics 0.3.6 -> 0.3.7.

## [0.6.5] - 2026-09-26
- Dependency alignment for the Base/UI release.
- Declare the existing Analytics assembly dependency for standalone consumers.

## [0.6.3] - 2026-08-31
### Fixed
- Dependency alignment: com.hung.data 0.12.2 -> 0.12.3.
- `Locator.Items` is now null-guarded at all four ads badge call sites (`GameRewardAds`, `GameInterAds`, `GameBannerAds` x2). Showing a rewarded ad while the host had not yet assigned `Locator.Items` threw `NullReferenceException` from `GameRewardAds.Show` and aborted the ad request. The `?.` form matches the idiom already used by every `Locator.Items` call site in the liveops packages.

## [0.5.3] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.5.2] - 2026-08-11
- Dependency-only patch: align Base 0.19.2, Data 0.10.2, and Analytics 0.3.3 for the F4-IE editor prerequisite.

## [0.5.1] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.services.analytics 0.3.1 -> 0.3.2; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.5.0] - 2026-07-28
### Added
- Added request-scoped rewarded and interstitial sessions backed by `AdsRequestController`, with exactly-once terminal completion.
- Added `AdsLoadQueue` for deterministic load draining and queue advancement.

### Changed
- `AdsManager` now implements the asynchronous `IAdsRequestService` facade from `IAdsService`.
- `GameRewardAds` and `GameInterAds` preserve legacy callback wrappers while using request-scoped result contracts internally.
- `NullAdsService` request APIs report `Unsupported` with diagnostic `null-service`; legacy null rewarded ads no longer fabricate reward success.
- Ads provider shows acquire an Ads pause lease and release only that lease on terminal completion.

### Fixed
- Removed `AdsManager` resume-time global time-scale reset, so Ads no longer clears unrelated popup/tutorial/gameplay pauses.
- Added provider null guards and fixed the IronSource rewarded loading check to read the IronSource provider.

## [0.4.1] - 2026-07-21
- Aligned package dependencies and serialized manager data with the ItemId migration release; no ads API changed.

## [0.4.0] - 2026-07-14
- Adopted `ResetAoaCapEvent` and `ResetInterCapEvent` (new `Runtime/AdsEvents.cs`, namespace `Hung.Ads`). They previously lived in `com.hung.designpattern`'s `EVENTS.cs`, which was deleted in designpattern 0.4.0 — this package's `GameAppOpenAds`/`GameInterAds`/`GameRewardAds` were their only consumers, so ownership moves here.
- No behavior change. Consumers already in `namespace Hung.Ads` need no `using` update; any outside consumer must now `using Hung.Ads;` instead of `using Hung.DesignPattern;` for these two types.

## [0.3.0] - 2026-07-11
- BREAKING: `Ads`/`Ads.Max`/`Ads.IronSource`/`Ads.AdMob` namespaces renamed to `Hung.Ads`/`Hung.Ads.Integration.Max`/`Hung.Ads.Integration.IronSource`/`Hung.Ads.Integration.AdMob` (B1 Pass 5 namespace pass). `rootNamespace` updated on all 4 asmdefs to match.

## [0.2.0] - 2026-07-11
- BREAKING (internal restructure): split into `Hung.Ads` (neutral) + `Hung.Ads.Integration.{Max,AdMob,IronSource}` (vendor-isolated, `autoReferenced: false`, gated by `HUNG_ADS_MAX`/`HUNG_ADS_ADMOB`/`HUNG_ADS_IRONSOURCE` defines). `AdsManager` moved into the IronSource integration assembly (it owns the LevelPlay SDK lifecycle, not a swappable per-format provider).
- `GameBannerAds`/`GameInterAds`/`GameRewardAds` now hold vendor components as plain `MonoBehaviour` fields cast to new `IBannerAdsProvider`/`IInterstitialAdsProvider`/`IRewardedAdsProvider` contracts (`Runtime/Contracts/`) at runtime, instead of referencing `Ads.Max`/`Ads.IronSource` types directly. Vendor payload data (`MaxSdkBase.AdInfo`/`LevelPlayAdInfo`/etc) dropped from the provider events - confirmed unused by every consumer before removal.
- Removed direct `Hung.Analytics`/`AppsFlyer`/`Firebase.*.dll`/`MaxSdk.Scripts`/`Unity.LevelPlay`/`GoogleMobileAds.*.dll` references from neutral `Hung.Ads` (the Firebase DLLs were dead weight - only `FirebaseManager.Ins`, a `Hung.Analytics` type, was ever used, and that moved with `AdsManager`).
- Ad revenue now reported via `Locator.RevenueSink?.OnRevenue(...)` instead of a direct `AppsFlyer.logAdRevenue` call.
- Added `Doubles/NullAdsService : IAdsService` (all no-op).
- AdMob adapters confirmed fully dead code (every class body commented out) - moved as-is, not revived.

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (contracts moved to com.hung.base).
