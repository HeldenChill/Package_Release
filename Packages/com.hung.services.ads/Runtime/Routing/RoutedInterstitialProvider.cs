using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>Interstitial provider routed across installed vendors; show-time events only from the current show, once each.</summary>
    public sealed class RoutedInterstitialProvider : IInterstitialAdsProvider, IDisposable
    {
        readonly AdsRouter router;
        readonly AdsProviderSet set;
        readonly RoutedLoadCycle<IInterstitialAdsProvider> cycle;
        readonly List<Action> unsubscribe = new List<Action>();
        readonly Func<float> clock;
        readonly float showLockTimeout;

        IInterstitialAdsProvider current;
        bool closedThisShow = true;
        float showStartedAt;

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsDone;

        /// <param name="clock">Seconds source for the show-lock timeout; defaults to realtime.</param>
        /// <param name="showLockTimeout">Seconds after which a show whose closing event never arrived stops blocking new shows.</param>
        public RoutedInterstitialProvider(AdsRouter router, AdsProviderSet set, Func<float> clock = null, float showLockTimeout = 180f)
        {
            this.router = router;
            this.set = set;
            this.clock = clock ?? (() => Time.realtimeSinceStartup);
            this.showLockTimeout = showLockTimeout;
            cycle = new RoutedLoadCycle<IInterstitialAdsProvider>(c => c.IsCanShow, c => c.IsLoading, c => c.Load());
            cycle.DefaultLoaded += () => OnAdsLoaded?.Invoke();
            cycle.DefaultFailed += () => OnAdsLoadFail?.Invoke();
            foreach (var child in set.AllInterstitial) Subscribe(child);
        }

        public bool IsLoading => cycle.IsRunning;
        public bool IsCanShow => FirstReady(Placement.NONE) != null;

        /// <summary>A provider in this placement's route is ready.</summary>
        public bool IsCanShowFor(Placement placement) => FirstReady(placement) != null;

        public void Load()
        {
            if (cycle.IsRunning) return;
            var routes = new List<(AdsRouteMode, IReadOnlyList<IInterstitialAdsProvider>)>();
            foreach (var route in router.PreloadRoutes(AdsFormat.Interstitial))
            {
                var list = new List<IInterstitialAdsProvider>();
                foreach (var id in route.ProviderIds) if (set.TryGetInterstitial(id, out var c)) list.Add(c);
                routes.Add((route.Mode, list));
            }
            cycle.Start(routes);
        }

        public void Show(Placement placement)
        {
            if (current != null && !closedThisShow)
            {
                if (clock() - showStartedAt < showLockTimeout)
                {
                    Debug.LogWarning("[Ads Routing] Interstitial Show ignored: an ad is already showing.");
                    return;
                }
                Debug.LogWarning("[Ads Routing] Interstitial show lock expired: the previous show never reported done or display fail.");
                closedThisShow = true;
            }
            var child = FirstReady(placement);
            if (child == null)
            {
                OnAdsDisplayFail?.Invoke();
                return;
            }
            current = child;
            closedThisShow = false;
            showStartedAt = clock();
            child.Show(placement);
        }

        public void Dispose()
        {
            foreach (var u in unsubscribe) u();
            unsubscribe.Clear();
        }

        IInterstitialAdsProvider FirstReady(Placement placement)
        {
            foreach (var id in router.Resolve(AdsFormat.Interstitial, placement).ProviderIds)
            {
                if (set.TryGetInterstitial(id, out var c) && c.IsCanShow) return c;
            }
            return null;
        }

        void Subscribe(IInterstitialAdsProvider c)
        {
            Action loaded = () => cycle.OnChildLoaded(c);
            Action loadFail = () => cycle.OnChildLoadFailed(c);
            Action done = () => Close(c, OnAdsDone);
            Action displayFail = () => Close(c, OnAdsDisplayFail);
            c.OnAdsLoaded += loaded;
            c.OnAdsLoadFail += loadFail;
            c.OnAdsDone += done;
            c.OnAdsDisplayFail += displayFail;
            unsubscribe.Add(() =>
            {
                c.OnAdsLoaded -= loaded;
                c.OnAdsLoadFail -= loadFail;
                c.OnAdsDone -= done;
                c.OnAdsDisplayFail -= displayFail;
            });
        }

        void Close(IInterstitialAdsProvider c, Action raise)
        {
            if (!ReferenceEquals(current, c) || closedThisShow) return;
            closedThisShow = true;
            raise?.Invoke();
        }
    }
}
