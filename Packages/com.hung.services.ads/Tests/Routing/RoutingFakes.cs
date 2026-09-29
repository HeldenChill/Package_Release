using System;
using System.Collections.Generic;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    sealed class FakeRewarded : IRewardedAdsProvider
    {
        public readonly string Name;
        public FakeRewarded(string name) { Name = name; }
        public bool Ready;
        public bool Loading;
        public int LoadCalls;
        public readonly List<Placement> Shows = new List<Placement>();
        /// <summary>When set, Load() immediately raises this outcome (true = loaded, false = fail).</summary>
        public bool? AutoLoadResult;

        public bool IsCanShow => Ready;
        public bool IsLoading => Loading;
        public void Load()
        {
            LoadCalls++;
            Loading = true;
            if (AutoLoadResult == true) RaiseLoaded();
            else if (AutoLoadResult == false) RaiseLoadFail();
        }
        public void Show(Placement placement = Placement.NONE) { Shows.Add(placement); Ready = false; }

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsReceiveReward;
        public event Action OnAdsHidden;

        public void RaiseLoaded() { Loading = false; Ready = true; OnAdsLoaded?.Invoke(); }
        public void RaiseLoadFail() { Loading = false; OnAdsLoadFail?.Invoke(); }
        public void RaiseDisplayFail() => OnAdsDisplayFail?.Invoke();
        public void RaiseReward() => OnAdsReceiveReward?.Invoke();
        public void RaiseHidden() => OnAdsHidden?.Invoke();
    }

    sealed class FakeInterstitial : IInterstitialAdsProvider
    {
        public bool Ready;
        public bool Loading;
        public int LoadCalls;
        public readonly List<Placement> Shows = new List<Placement>();
        public bool IsCanShow => Ready;
        public bool IsLoading => Loading;
        public void Load() { LoadCalls++; Loading = true; }
        public void Show(Placement placement) { Shows.Add(placement); Ready = false; }
        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public event Action OnAdsDisplayFail;
        public event Action OnAdsDone;
        public void RaiseLoaded() { Loading = false; Ready = true; OnAdsLoaded?.Invoke(); }
        public void RaiseLoadFail() { Loading = false; OnAdsLoadFail?.Invoke(); }
        public void RaiseDisplayFail() => OnAdsDisplayFail?.Invoke();
        public void RaiseDone() => OnAdsDone?.Invoke();
    }

    sealed class FakeBanner : IBannerAdsProvider
    {
        public readonly List<string> Calls = new List<string>();
        public void InitBanner() => Calls.Add("init");
        public void Show() => Calls.Add("show");
        public void Hide() => Calls.Add("hide");
        public void Destroy() => Calls.Add("destroy");
        public void Load() => Calls.Add("load");
        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;
        public void RaiseLoaded() => OnAdsLoaded?.Invoke();
        public void RaiseLoadFail() => OnAdsLoadFail?.Invoke();
    }

    static class RoutingTestData
    {
        /// <summary>Providers max/admob/yandex all enabled; one format route.</summary>
        public static AdsRoutingData With(AdsFormat format, AdsRouteMode mode, params string[] order) => new AdsRoutingData
        {
            providers = new List<AdsProviderEntry>
            {
                new AdsProviderEntry { id = "max" },
                new AdsProviderEntry { id = "admob" },
                new AdsProviderEntry { id = "yandex" },
            },
            formats = new List<AdsFormatRoute> { new AdsFormatRoute { format = format, mode = mode, order = new List<string>(order) } },
            placements = new List<AdsPlacementRoute>(),
        };
    }
}
