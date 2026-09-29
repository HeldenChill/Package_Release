using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>
    /// Rewarded provider that routes across installed vendor providers per AdsRouter.
    /// Forwards show-time events only from the child it is currently showing, and each at most
    /// once per show; reward is accepted even when the vendor raises it after hidden.
    /// </summary>
    public sealed class RoutedRewardedProvider : IRewardedAdsProvider, IDisposable
    {
        readonly AdsRouter router;
        readonly AdsProviderSet set;
        readonly RoutedLoadCycle<IRewardedAdsProvider> cycle;
        readonly List<Action> unsubscribe = new List<Action>();

        readonly Func<float> clock;
        readonly float showLockTimeout;

        IRewardedAdsProvider current;
        bool rewardedThisShow;
        bool closedThisShow = true;
        float showStartedAt;

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsReceiveReward;
        public event Action OnAdsHidden;

        /// <param name="clock">Seconds source for the show-lock timeout; defaults to realtime.</param>
        /// <param name="showLockTimeout">Seconds after which a show whose closing event never arrived stops blocking new shows.</param>
        public RoutedRewardedProvider(AdsRouter router, AdsProviderSet set, Func<float> clock = null, float showLockTimeout = 180f)
        {
            this.router = router;
            this.set = set;
            this.clock = clock ?? (() => Time.realtimeSinceStartup);
            this.showLockTimeout = showLockTimeout;
            cycle = new RoutedLoadCycle<IRewardedAdsProvider>(c => c.IsCanShow, c => c.IsLoading, c => c.Load());
            cycle.DefaultLoaded += () => OnAdsLoaded?.Invoke();
            cycle.DefaultFailed += () => OnAdsLoadFail?.Invoke();
            foreach (var child in set.AllRewarded) Subscribe(child);
        }

        public bool IsLoading => cycle.IsRunning;

        /// <summary>A provider in the format default route is ready.</summary>
        public bool IsCanShow => FirstReady(Placement.NONE) != null;

        /// <summary>A provider in this placement's route is ready.</summary>
        public bool IsCanShowFor(Placement placement) => FirstReady(placement) != null;

        public void Load()
        {
            if (cycle.IsRunning) return;
            var routes = new List<(AdsRouteMode, IReadOnlyList<IRewardedAdsProvider>)>();
            foreach (var route in router.PreloadRoutes(AdsFormat.Rewarded)) routes.Add((route.Mode, Children(route)));
            cycle.Start(routes);
        }

        public void Show(Placement placement = Placement.NONE)
        {
            if (current != null && !closedThisShow)
            {
                if (clock() - showStartedAt < showLockTimeout)
                {
                    Debug.LogWarning("[Ads Routing] Rewarded Show ignored: an ad is already showing.");
                    return;
                }
                Debug.LogWarning("[Ads Routing] Rewarded show lock expired: the previous show never reported hidden or display fail.");
                closedThisShow = true;
            }
            var child = FirstReady(placement);
            if (child == null)
            {
                OnAdsDisplayFail?.Invoke();
                return;
            }
            current = child;
            rewardedThisShow = false;
            closedThisShow = false;
            showStartedAt = clock();
            child.Show(placement);
        }

        public void Dispose()
        {
            foreach (var u in unsubscribe) u();
            unsubscribe.Clear();
        }

        IRewardedAdsProvider FirstReady(Placement placement)
        {
            foreach (var id in router.Resolve(AdsFormat.Rewarded, placement).ProviderIds)
            {
                if (set.TryGetRewarded(id, out var c) && c.IsCanShow) return c;
            }
            return null;
        }

        IReadOnlyList<IRewardedAdsProvider> Children(AdsRoute route)
        {
            var list = new List<IRewardedAdsProvider>();
            foreach (var id in route.ProviderIds)
            {
                if (set.TryGetRewarded(id, out var c)) list.Add(c);
            }
            return list;
        }

        void Subscribe(IRewardedAdsProvider c)
        {
            Action loaded = () => cycle.OnChildLoaded(c);
            Action loadFail = () => cycle.OnChildLoadFailed(c);
            Action reward = () =>
            {
                if (!ReferenceEquals(current, c) || rewardedThisShow) { Stale("reward"); return; }
                rewardedThisShow = true;
                OnAdsReceiveReward?.Invoke();
            };
            Action hidden = () =>
            {
                if (!ReferenceEquals(current, c) || closedThisShow) { Stale("hidden"); return; }
                closedThisShow = true;
                OnAdsHidden?.Invoke();
            };
            Action displayFail = () =>
            {
                if (!ReferenceEquals(current, c) || closedThisShow) { Stale("display fail"); return; }
                closedThisShow = true;
                OnAdsDisplayFail?.Invoke();
            };

            c.OnAdsLoaded += loaded;
            c.OnAdsLoadFail += loadFail;
            c.OnAdsReceiveReward += reward;
            c.OnAdsHidden += hidden;
            c.OnAdsDisplayFail += displayFail;
            unsubscribe.Add(() =>
            {
                c.OnAdsLoaded -= loaded;
                c.OnAdsLoadFail -= loadFail;
                c.OnAdsReceiveReward -= reward;
                c.OnAdsHidden -= hidden;
                c.OnAdsDisplayFail -= displayFail;
            });
        }

        static void Stale(string what) => Debug.Log("[Ads Routing] Dropped rewarded " + what + " from a provider that is not the current show.");
    }
}
