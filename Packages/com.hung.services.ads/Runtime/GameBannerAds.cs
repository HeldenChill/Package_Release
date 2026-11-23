using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.Ads
{
    using Hung.Common;
    using Hung.Base.Init;
    using System;
    using Hung.Base;

    public class GameBannerAds : MonoBehaviour, IBannerAds
    {
        protected const int MAX_RETRY = 30;
        [Serializable]
        public struct ProviderBinding
        {
            public ADS_TYPE type;
            public MonoBehaviour behaviour;
        }

        [SerializeField]
        ProviderBinding[] providerBindings = new ProviderBinding[0];

        IAdsProviderRegistry registry;
        readonly List<IBannerAdsProvider> subscribed = new List<IBannerAdsProvider>();
        int currentRetry = 0;
        // What the game last asked for. Vendors (MAX) raise "loaded" on every auto-refresh, so the
        // loaded handler may only re-show a banner the game still wants on screen.
        bool wantShown;

        ADS_TYPE type;
        /// <summary>Allow banner initialization and showing.</summary>
        public bool AdsEnabled { get; set; } = true;

        public ADS_TYPE Type
        {
            get => type;
            set
            {
                type = value;
            }
        }

        private void Awake()
        {
            var built = new AdsProviderRegistry();
            for (int i = 0; i < providerBindings.Length; i++)
            {
                built.RegisterBanner(
                    providerBindings[i].type,
                    providerBindings[i].behaviour as IBannerAdsProvider);
            }
            ConfigureProviders(built);
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        public void ConfigureProviders(IAdsProviderRegistry providerRegistry)
        {
            UnsubscribeAll();
            registry = providerRegistry;

            foreach (ADS_TYPE vendor in Enum.GetValues(typeof(ADS_TYPE)))
            {
                if (registry != null
                    && registry.TryGetBanner(vendor, out var provider)
                    && provider != null
                    && !subscribed.Contains(provider))
                {
                    provider.OnAdsLoaded += OnBannerLoaded;
                    provider.OnAdsLoadFail += OnBannerLoadFail;
                    subscribed.Add(provider);
                }
            }
        }

        void UnsubscribeAll()
        {
            for (int i = 0; i < subscribed.Count; i++)
            {
                subscribed[i].OnAdsLoaded -= OnBannerLoaded;
                subscribed[i].OnAdsLoadFail -= OnBannerLoadFail;
            }
            subscribed.Clear();
        }

        bool TryGetProvider(ADS_TYPE vendor, out IBannerAdsProvider provider)
        {
            provider = null;
            return registry != null && registry.TryGetBanner(vendor, out provider) && provider != null;
        }

        public void InitBanner()
        {
            if (!AdsEnabled) return;
            wantShown = true;
            foreach (ADS_TYPE vendor in Enum.GetValues(typeof(ADS_TYPE)))
            {
                if (vendor == type) continue;
                if (TryGetProvider(vendor, out var other)) other.Destroy();
            }

            if (TryGetProvider(type, out var active)) active.InitBanner();
        }

        public void Hide()
        {
            wantShown = false;
            HideAllProviders();
        }

        void HideAllProviders()
        {
            for (int i = 0; i < subscribed.Count; i++)
            {
                subscribed[i].Hide();
            }
        }

        bool IsAdsRemoved()
        {
            if (!(AdsEntitlementsAccess.Current.IsRemoveAds || AdsEntitlementsAccess.Current.IsPremiumRemoveAds))
                return false;
            // A banner already on screen when remove-ads is granted must go away.
            HideAllProviders();
            Locator.Items?.ShowBadge(AdsEntitlementsAccess.Current.IsPremiumRemoveAds ? BaseItemIds.PremiumRemoveAds : BaseItemIds.RemoveAds);
            return true;
        }

        public void Load()
        {

        }

        public void Show()
        {
            if (!AdsEnabled) return;
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (IsAdsRemoved()) return;
                Show(Type);
                currentRetry = 0;
            }
        }

        public void Show(ADS_TYPE type)
        {
            if (!AdsEnabled) return;
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (IsAdsRemoved()) return;
                wantShown = true;
                if (TryGetProvider(type, out var provider)) provider.Show();
            }
        }

        public void Hide(ADS_TYPE type)
        {
            wantShown = false;
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (TryGetProvider(type, out var provider)) provider.Hide();
            }
        }

        public void Init()
        {
            // adMobAds.InitBanner();
        }
        void OnBannerLoaded()
        {
            if (!wantShown) return;
            Show(Type);
        }

        void OnBannerLoadFail()
        {
        }
    }
}
