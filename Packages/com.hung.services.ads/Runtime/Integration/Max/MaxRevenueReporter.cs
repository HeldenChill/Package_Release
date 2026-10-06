using System.Collections.Generic;
using Hung.Base;
using UnityEngine;

namespace Hung.Ads.Integration.Max
{
    /// <summary>
    /// Forwards MAX impression revenue (every format) to <c>Locator.RevenueSink</c>, the same seam the
    /// IronSource path uses. Subscribes once per process; MAX callbacks fire for any ad unit.
    /// </summary>
    public static class MaxRevenueReporter
    {
        static bool subscribed;

        public static void EnsureSubscribed()
        {
            if (subscribed) return;
            subscribed = true;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += Report;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += Report;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += Report;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += Report;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += Report;
        }

        static void Report(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null || adInfo.Revenue <= 0) return;
            Debug.Log($"[MAX]: AdRevenuePaid {adInfo.AdFormat} {adInfo.NetworkName} {adInfo.Revenue}");

            var extra = new Dictionary<string, string>
            {
                { "mediation", "max" },
                { "country", MaxSdk.GetSdkConfiguration().CountryCode },
                { "ad_unit", adUnitId },
                { "ad_type", adInfo.AdFormat },
                { "placement", adInfo.Placement }
            };
            Locator.RevenueSink?.OnRevenue(adInfo.NetworkName, adInfo.Revenue, "USD", extra);
        }
    }
}
