using System;
using System.Collections.Generic;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>Banner provider routed as a chain across installed vendors. One banner view exists at a time.</summary>
    public sealed class RoutedBannerProvider : IBannerAdsProvider, IDisposable
    {
        readonly AdsRouter router;
        readonly AdsProviderSet set;
        readonly List<Action> unsubscribe = new List<Action>();
        readonly List<IBannerAdsProvider> chain = new List<IBannerAdsProvider>();
        int index;
        bool initialized;

        public event Action OnAdsLoaded;
        public event Action OnAdsLoadFail;

        public RoutedBannerProvider(AdsRouter router, AdsProviderSet set)
        {
            this.router = router;
            this.set = set;
            foreach (var child in set.AllBanner) Subscribe(child);
        }

        IBannerAdsProvider Current => initialized && index < chain.Count ? chain[index] : null;

        public void InitBanner()
        {
            chain.Clear();
            foreach (var id in router.Resolve(AdsFormat.Banner, Placement.NONE).ProviderIds)
            {
                if (set.TryGetBanner(id, out var c)) chain.Add(c);
            }
            index = 0;
            initialized = true;
            Current?.InitBanner();
        }

        public void Load()
        {
            if (!initialized) InitBanner();
            var c = Current;
            if (c == null)
            {
                initialized = false;
                OnAdsLoadFail?.Invoke();
                return;
            }
            c.Load();
        }

        public void Show() => Current?.Show();
        public void Hide() => Current?.Hide();

        public void Destroy()
        {
            Current?.Destroy();
            initialized = false;
        }

        public void Dispose()
        {
            foreach (var u in unsubscribe) u();
            unsubscribe.Clear();
        }

        void Subscribe(IBannerAdsProvider c)
        {
            Action loaded = () => { if (ReferenceEquals(Current, c)) OnAdsLoaded?.Invoke(); };
            Action loadFail = () => { if (ReferenceEquals(Current, c)) Advance(); };
            c.OnAdsLoaded += loaded;
            c.OnAdsLoadFail += loadFail;
            unsubscribe.Add(() =>
            {
                c.OnAdsLoaded -= loaded;
                c.OnAdsLoadFail -= loadFail;
            });
        }

        void Advance()
        {
            chain[index].Destroy();
            index++;
            var next = Current;
            if (next == null)
            {
                initialized = false;
                OnAdsLoadFail?.Invoke();
                return;
            }
            next.InitBanner();
            next.Load();
        }
    }
}
