using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Hung.Ads.Integration.AdMob
{
    using Hung.Ads;

    /// <summary>Initialises the Google Mobile Ads SDK once and registers AdMob providers for routed ads.</summary>
    public sealed class AdMobAdsInstaller : IAdsProviderInstaller
    {
        static bool initStarted;
        static Action pending;

        /// <summary>Seconds a provider waits for SDK init before reporting load fail, so a routed chain can move on.</summary>
        public const float InitWaitTimeoutSeconds = 15f;

        /// <summary>True after MobileAds.Initialize completed.</summary>
        public static bool Initialized { get; private set; }

        /// <summary>Runs <paramref name="action"/> now if initialised, else after init completes.</summary>
        public static void WhenInitialized(Action action)
        {
            if (Initialized) { action?.Invoke(); return; }
            pending += action;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            initStarted = false;
            Initialized = false;
            pending = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Register() => AdsInstallers.Register(new AdMobAdsInstaller());

        public string ProviderId => AdsProviderId.AdMob;

        public void Install(GameObject host, AdsProviderEntry entry, AdsProviderSet set)
        {
            if (!initStarted)
            {
                initStarted = true;
                MobileAds.RaiseAdEventsOnUnityMainThread = true;
                MobileAds.Initialize(_ =>
                {
                    Initialized = true;
                    var run = pending;
                    pending = null;
                    run?.Invoke();
                });
            }

            string unit = entry.UnitFor(AdsFormat.Rewarded);
            if (unit != null)
            {
                var p = host.AddComponent<AdMobRewarded>();
                p.SetAdUnitId(unit);
                set.AddRewarded(ProviderId, p);
            }

            unit = entry.UnitFor(AdsFormat.Interstitial);
            if (unit != null)
            {
                var p = host.AddComponent<AdMobInterstitial>();
                p.SetAdUnitId(unit);
                set.AddInterstitial(ProviderId, p);
            }

            unit = entry.UnitFor(AdsFormat.Banner);
            if (unit != null)
            {
                var p = host.AddComponent<AdMobBanner>();
                p.SetAdUnitId(unit);
                set.AddBanner(ProviderId, p);
            }
        }
    }
}
