# Changelog

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
