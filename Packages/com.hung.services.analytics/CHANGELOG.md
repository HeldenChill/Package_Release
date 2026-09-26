# Changelog

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
