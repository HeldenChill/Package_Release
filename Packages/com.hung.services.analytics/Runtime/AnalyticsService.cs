using System;
using System.Collections.Generic;
using Hung.Analytics.Tracking;
using Hung.Base;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>
    /// The analytics proxy behind <c>Locator.Analytics</c> and <c>Locator.RevenueSink</c>. Each domain method
    /// becomes one named event with an <see cref="AnalyticsCategory"/> and fans out to every backend subscribed
    /// to that category. A backend exception is logged once per backend and exception type and never reaches
    /// the caller or the other backends.
    /// </summary>
    public sealed class AnalyticsService : IAnalyticsService, IRevenueEventSink
    {
        readonly struct Route
        {
            public readonly IAnalyticsBackend Backend;
            public readonly AnalyticsCategory Categories;

            public Route(IAnalyticsBackend backend, AnalyticsCategory categories)
            {
                Backend = backend;
                Categories = categories;
            }
        }

        readonly List<Route> _routes = new List<Route>();
        readonly List<Action> _held = new List<Action>();
        bool _holding;
        int _droppedWhileHolding;
        readonly HashSet<string> _reportedFailures = new HashSet<string>();
        readonly Func<float> _clock;
        readonly float _startTime;
        bool _firstAdsSessionLogged;

        /// <summary>Creates an empty proxy.</summary>
        /// <param name="clock">Seconds since startup; defaults to <c>Time.realtimeSinceStartup</c>. Tests inject it.</param>
        public AnalyticsService(Func<float> clock = null)
        {
            _clock = clock ?? (() => Time.realtimeSinceStartup);
            _startTime = _clock();
        }

        /// <summary>Number of active backends.</summary>
        public int BackendCount => _routes.Count;

        /// <summary>Adds a backend receiving events whose category intersects <paramref name="categories"/>.</summary>
        public void AddBackend(IAnalyticsBackend backend, AnalyticsCategory categories)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            _routes.Add(new Route(backend, categories));
        }

        /// <inheritdoc/>
        public void LogEvent(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (_holding)
            {
                var snapshot = CopyOf(parameters);
                Keep(() => LogEvent(name, category, snapshot));
                return;
            }
            foreach (var route in _routes)
            {
                if ((route.Categories & category) == 0) continue;
                try { route.Backend.LogEvent(name, parameters); }
                catch (Exception e) { ReportFailure(route.Backend, e); }
            }
        }

        /// <inheritdoc/>
        public void SetUserProperty(string key, string value)
        {
            if (_holding)
            {
                Keep(() => SetUserProperty(key, value));
                return;
            }
            foreach (var route in _routes)
            {
                try { route.Backend.SetUserProperty(key, value); }
                catch (Exception e) { ReportFailure(route.Backend, e); }
            }
        }

        /// <summary>Routes ad revenue to backends subscribed to <see cref="AnalyticsCategory.Revenue"/>.</summary>
        public void OnRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra = null)
        {
            if (_holding)
            {
                var snapshot = CopyOf(extra);
                Keep(() => OnRevenue(source, value, currency, snapshot));
                return;
            }
            foreach (var route in _routes)
            {
                if ((route.Categories & AnalyticsCategory.Revenue) == 0) continue;
                try { route.Backend.LogAdRevenue(source, value, currency, extra); }
                catch (Exception e) { ReportFailure(route.Backend, e); }
            }
        }

        // Event names below are byte-identical to the 0.3.x Firebase events so dashboards keep working.

        /// <inheritdoc/>
        public void AdsRewardOffer(Placement place)
        {
            Log("Reward_request", AnalyticsCategory.Ads);
            Log($"Reward_request_action_{place}", AnalyticsCategory.Ads, "placement", place.ToString());
        }

        /// <inheritdoc/>
        public void AdsRewardClick(Placement place) => Log("Reward_click", AnalyticsCategory.Ads);

        /// <inheritdoc/>
        public void AdsRewardShow(Placement place)
        {
            Log($"Reward_show_action_{place}", AnalyticsCategory.Ads);
            CheckFirstAdsSession(place);
        }

        /// <inheritdoc/>
        public void AdsRewardShowFail(Placement place, string error) =>
            LogEvent("Reward_show_failed", AnalyticsCategory.Ads, new Dictionary<string, object>
            {
                { "placement", place.ToString() }, { "errormsg", error }
            });

        /// <inheritdoc/>
        public void AdsRewardComplete(Placement place, string type)
        {
            Log($"Reward_finish_action_{place}", AnalyticsCategory.Ads);
            CheckFirstAdsSession(place);
            AnalyticsTracking.Facts.Raise(Fact.Monetize(MonetizeKind.RewardedAd));
        }

        /// <inheritdoc/>
        public void AdsRewardLoadComplete() => Log("Reward_request_success", AnalyticsCategory.Ads);
        /// <inheritdoc/>
        public void AdsRewardLoad() => Log("ad_reward_load", AnalyticsCategory.Ads);
        /// <inheritdoc/>
        public void AdsRewardLoadFail() => Log("Reward_request_failed", AnalyticsCategory.Ads);

        /// <inheritdoc/>
        public void AdsInterFail(string error) => Log("Inters_request_failed", AnalyticsCategory.Ads, "errormsg", error);
        /// <inheritdoc/>
        public void AdsInterLoad() => Log("Inters_request", AnalyticsCategory.Ads);

        /// <inheritdoc/>
        public void AdsInterShow(Placement place)
        {
            Log("inters_show", AnalyticsCategory.Ads, "placement", place.ToString());
            CheckFirstAdsSession(place);
        }

        /// <inheritdoc/>
        public void AdsInterClick() => Log("inters_click", AnalyticsCategory.Ads);
        /// <inheritdoc/>
        public void AdsInterLoadComplete() => Log("Inters_request_success", AnalyticsCategory.Ads);
        /// <inheritdoc/>
        public void AdsInterComplete() => Log("inters_finish", AnalyticsCategory.Ads);

        /// <inheritdoc/>
        public void BuyIAPComplete(IAP_ITEM item, Placement placement)
        {
            Log($"IAP_packname_{item}_Location_{placement}", AnalyticsCategory.Product);
            AnalyticsTracking.Facts.Raise(Fact.Monetize(MonetizeKind.Iap));
        }

        /// <inheritdoc/>
        public void TutorialStep(string name, int step) => Log($"{name}_step_{step}", AnalyticsCategory.Design);

        /// <inheritdoc/>
        public void LevelTrackEvent(LEVEL_STATE state, int level, int stars, bool firstPass, bool isFtu)
        {
            string prefix = isFtu ? "ftu_level_" : "level_";
            switch (state)
            {
                case LEVEL_STATE.START:
                    Log($"{prefix}{level}_start", AnalyticsCategory.Design);
                    break;
                case LEVEL_STATE.COMPLETE:
                    // FTU says "_star_", normal says "_stars_": kept as-is for 0.3.x dashboards.
                    Log(isFtu ? $"ftu_level_{level}_star_{stars}" : $"level_{level}_stars_{stars}", AnalyticsCategory.Design);
                    Log($"{prefix}{level}_{(firstPass ? "pass" : "replay_pass")}", AnalyticsCategory.Product);
                    break;
                case LEVEL_STATE.FAIL:
                    Log($"{prefix}{level}_fail", AnalyticsCategory.Design);
                    break;
            }
        }

        /// <inheritdoc/>
        public void EarnVirtualCurrency(string name, long value, string source) =>
            LogEvent("earn_virtual_currency", AnalyticsCategory.Design, new Dictionary<string, object>
            {
                { "virtual_currency_name", name }, { "value", value }, { "source", source }
            });

        /// <inheritdoc/>
        public void SpendVirtualCurrency(string name, long value, string itemName) =>
            LogEvent("spend_virtual_currency", AnalyticsCategory.Design, new Dictionary<string, object>
            {
                { "virtual_currency_name", name }, { "value", value }, { "item_name", itemName }
            });

        void CheckFirstAdsSession(Placement place)
        {
            if (_firstAdsSessionLogged) return;
            _firstAdsSessionLogged = true;
            LogEvent("first_ads_session", AnalyticsCategory.Ads, new Dictionary<string, object>
            {
                { "placement", place.ToString() }, { "duration", (int)(_clock() - _startTime) }
            });
        }

        void Log(string name, AnalyticsCategory category) => LogEvent(name, category);

        void Log(string name, AnalyticsCategory category, string key, string value) =>
            LogEvent(name, category, new Dictionary<string, object> { { key, value } });

        /// <summary>Most calls kept while holding; later ones are dropped and counted.</summary>
        public const int MaxHeldCalls = 512;

        /// <summary>True while calls are kept for a later <see cref="ReleaseHeld"/>.</summary>
        public bool IsHolding => _holding;

        /// <summary>
        /// Keeps every later event, user property and revenue call, with its values copied at call time, until
        /// <see cref="ReleaseHeld"/> or <see cref="DiscardHeld"/>. Used to wait for consent before any SDK sends.
        /// </summary>
        public void Hold() => _holding = true;

        /// <summary>Stops holding and replays the kept calls, in order, through the current backends.</summary>
        // ponytail: replays in one frame; the 512 cap bounds it. Spread across frames if a profile ever shows a hitch.
        public void ReleaseHeld()
        {
            _holding = false;
            var calls = _held.ToArray();
            _held.Clear();
            foreach (var call in calls) call();
            if (_droppedWhileHolding > 0)
                Debug.LogWarning($"[Analytics] {_droppedWhileHolding} calls over the {MaxHeldCalls} hold limit were dropped before release.");
            _droppedWhileHolding = 0;
        }

        /// <summary>Stops holding and drops the kept calls unsent (consent refused).</summary>
        public void DiscardHeld()
        {
            _holding = false;
            _held.Clear();
            _droppedWhileHolding = 0;
        }

        void Keep(Action call)
        {
            if (_held.Count < MaxHeldCalls) _held.Add(call);
            else _droppedWhileHolding++;
        }

        static Dictionary<string, T> CopyOf<T>(IReadOnlyDictionary<string, T> source)
        {
            if (source == null) return null;
            var copy = new Dictionary<string, T>(source.Count);
            foreach (var kv in source) copy[kv.Key] = kv.Value;
            return copy;
        }

        void ReportFailure(IAnalyticsBackend backend, Exception e)
        {
            bool first;
            lock (_reportedFailures) first = _reportedFailures.Add(backend.Id + "|" + e.GetType().FullName);
            if (first)
                Debug.LogError($"[Analytics] backend '{backend.Id}' threw {e.GetType().Name}: {e.Message}. Further {e.GetType().Name}s from it are silenced.");
        }
    }
}
