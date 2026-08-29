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

        GameData gameData;
        GameData GameData => gameData ??= Locator.Data.GetData<GameData>();
        ADS_TYPE type;
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
            foreach (ADS_TYPE vendor in Enum.GetValues(typeof(ADS_TYPE)))
            {
                if (vendor == type) continue;
                if (TryGetProvider(vendor, out var other)) other.Destroy();
            }

            if (TryGetProvider(type, out var active)) active.InitBanner();
        }

        public void Hide()
        {
            for (int i = 0; i < subscribed.Count; i++)
            {
                subscribed[i].Hide();
            }
        }

        public void Load()
        {

        }

        public void Show()
        {
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (GameData.IsRemoveAds() || GameData.IsPremiumRemoveAds())
                {
                    Locator.Items.ShowBadge(GameData.IsPremiumRemoveAds() ? BaseItemIds.PremiumRemoveAds : BaseItemIds.RemoveAds);
                    return;
                }
                Show(Type);
                currentRetry = 0;
            }
        }

        public void Show(ADS_TYPE type)
        {
            if (!(DebugManager.Ins && !DebugManager.Ins.IsShowAds))
            {
                if (GameData.IsRemoveAds() || GameData.IsPremiumRemoveAds())
                {
                    Locator.Items.ShowBadge(GameData.IsPremiumRemoveAds() ? BaseItemIds.PremiumRemoveAds : BaseItemIds.RemoveAds);
                    return;
                }
                if (TryGetProvider(type, out var provider)) provider.Show();
            }
        }

        public void Hide(ADS_TYPE type)
        {
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
            Show(Type);
        }

        void OnBannerLoadFail()
        {
        }
    }
}
