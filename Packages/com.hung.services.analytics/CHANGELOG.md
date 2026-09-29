# Changelog

## [0.4.0] - 2026-09-29
### Changed (breaking)
- `AnalyticsManager`/`FirebaseManager` replaced by `AnalyticsService` (proxy) + `AnalyticsBootstrap` (BeforeSceneLoad, no prefab). `Locator.Analytics`/`Locator.RevenueSink` are always set, even with zero backends.
- SDK adapters split into define-gated assemblies: `Hung.Analytics.Integration.{Firebase,AppsFlyer,AppMetrica,GameAnalytics}` (`HUNG_ANALYTICS_*`). Core references only `Hung.Base`.
- Per-event `AnalyticsCategory` routing; per-backend subscriptions in `Resources/HungAnalyticsSettings.asset` (`Hung/Analytics/Settings`).
- Remote Config is now `FirebaseRemoteConfigCache.TryGet*` (non-blocking); blocking `GetConfigValueRemoteAsync` removed.
- `LevelTrackEvent` no longer writes `GameData`; FAIL logs `level_{n}_fail` and no longer marks the level passed.
- Removed `ftu_loading_start` (it read `GameData` from inside Firebase init). Games that want it call `Locator.Analytics.LogEvent("ftu_loading_start", AnalyticsCategory.Design)` from their loading flow.
### Added
- AppMetrica backend; currency events now actually log. GameAnalytics backend (needs `GameAnalyticsSDK.asmdef` in the SDK folder, see README).
### Fixed
- Backend exceptions no longer reach callers; Firebase callbacks run on the main thread; Messaging unsubscribes on quit; first-ads-session no longer leaks a pooled timer; ad revenue mediation network no longer hardcoded to IronSource.
### Migration
- Remove missing-script components from `AdsManager.prefab`; remove any `AppsFlyerObject` from scenes; set keys in the settings window; `GoogleFireBaseTrackEvent(x)` -> `LogEvent(x, AnalyticsCategory.Ads)`; `LevelTrackEvent(state, level)` -> `LevelTrackEvent(state, level, stars, firstPass, isFtu)` with `GameData.level.MarkPassed`.

## [0.3.7] - 2026-09-27
- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6.

## [0.3.6] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.3.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.3.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 and Data 0.10.2 for the F4-IE editor prerequisite.

## [0.3.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.3.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no analytics API or runtime behavior changed.

## [0.3.0] - 2026-07-11
- BREAKING: `Analytics` namespace renamed to `Hung.Analytics` (B1 Pass 5 namespace pass) - code now matches the asmdef's `rootNamespace`, which was already `Hung.Analytics` (pre-existing mismatch).

## [0.2.0] - 2026-07-11
- `AnalyticsManager` implements `IRevenueEventSink` (com.hung.base) and assigns `Locator.RevenueSink = this` in `Awake`, alongside `Locator.Analytics`. `OnRevenue` remaps neutral `extra` keys to AppsFlyer's `AdRevenueScheme` constants before calling `AppsFlyer.logAdRevenue` - this is now the only call site (moved from com.hung.services.ads's `AdsManager`).
- Added `Doubles/RecordingAnalyticsService : IAnalyticsService, IRevenueEventSink` (records calls for test assertions).

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (contracts moved to com.hung.base).
