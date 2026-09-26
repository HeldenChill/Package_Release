# Hung Services Analytics

Analytics wrapper (AppsFlyer + Firebase adapters).

Contracts (`IAnalyticsService` + Locator slot + enums) live in `com.hung.base` `Runtime/Services/Contracts/Analytics`. Requires AppsFlyer/Firebase SDKs in the consumer. Assembly renamed Hung.Analystics -> Hung.Analytics (typo fix). PvM's live-hardened variant = future merge-in (uses PvM Base fork APIs).

## Initialization order (Ph6 step 6)

`AnalyticsManager` (`[DefaultExecutionOrder(-100)]`) must initialize before `com.hung.services.ads`'s `AdsManager` (`[DefaultExecutionOrder(-50)]`) — ads blocks on `FirebaseManager.Ins.IsAvailable` in its own startup and calls `Locator.RevenueSink` for every ad-revenue impression, both of which this package owns. See `com.hung.services.ads/README.md`'s Initialization order section for the full sequence.

## Revenue sink

`AnalyticsManager` implements `IRevenueEventSink` (`com.hung.base`) alongside `IAnalyticsService`, and assigns `Locator.RevenueSink = this` in `Awake` next to `Locator.Analytics = this`. `OnRevenue(source, value, currency, extra)` remaps the neutral `extra` dictionary keys (`country`/`ad_unit`/`ad_type`/`placement`) to AppsFlyer's `AdRevenueScheme` constants before calling `AppsFlyer.logAdRevenue` — this is the only place that call happens now; `com.hung.services.ads` no longer references AppsFlyer or this package's assembly directly.

## Test doubles

`Doubles/RecordingAnalyticsService : IAnalyticsService, IRevenueEventSink` — records every call (method name + args) into an `Events` list instead of hitting Firebase/AppsFlyer, for asserting "X was tracked" in EditMode/PlayMode tests.

## Known limitations

No tests yet (`has_tests: false` in the catalog).
