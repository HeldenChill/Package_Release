using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>Stable string ids for ad providers, used by routing config and installers.</summary>
    public static class AdsProviderId
    {
        public const string Max = "max";
        public const string AdMob = "admob";
        public const string Yandex = "yandex";
    }

    /// <summary>Ad format a route applies to. Persisted as int in assets: never renumber.</summary>
    public enum AdsFormat { Rewarded = 0, Interstitial = 1, Banner = 2 }

    /// <summary>Chain loads one provider at a time in order; Parallel loads all at once. Persisted as int: never renumber.</summary>
    public enum AdsRouteMode { Chain = 0, Parallel = 1 }

    /// <summary>Runtime switch and ad unit ids for one provider.</summary>
    [Serializable]
    public sealed class AdsProviderEntry
    {
        public string id;
        public bool enabled = true;
        public string androidRewarded;
        public string iosRewarded;
        public string androidInterstitial;
        public string iosInterstitial;
        public string androidBanner;
        public string iosBanner;

        /// <summary>Ad unit id for this format on the current platform, or null when unset.</summary>
        public string UnitFor(AdsFormat format)
        {
#if UNITY_IOS
            string value = format == AdsFormat.Rewarded ? iosRewarded
                : format == AdsFormat.Interstitial ? iosInterstitial : iosBanner;
#else
            string value = format == AdsFormat.Rewarded ? androidRewarded
                : format == AdsFormat.Interstitial ? androidInterstitial : androidBanner;
#endif
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    /// <summary>Default route for one format.</summary>
    [Serializable]
    public sealed class AdsFormatRoute
    {
        public AdsFormat format;
        public AdsRouteMode mode;
        public List<string> order = new List<string>();
    }

    /// <summary>Route override for one placement of one format (rewarded / interstitial only).</summary>
    [Serializable]
    public sealed class AdsPlacementRoute
    {
        public AdsFormat format;
        public Placement placement;
        public AdsRouteMode mode;
        public List<string> order = new List<string>();
    }

    /// <summary>Complete routing configuration: providers, per-format routes, per-placement overrides.</summary>
    [Serializable]
    public sealed class AdsRoutingData
    {
        public List<AdsProviderEntry> providers = new List<AdsProviderEntry>();
        public List<AdsFormatRoute> formats = new List<AdsFormatRoute>();
        public List<AdsPlacementRoute> placements = new List<AdsPlacementRoute>();

        /// <summary>Entry with this id, or null.</summary>
        public AdsProviderEntry FindProvider(string id) => providers.Find(p => p != null && p.id == id);

        /// <summary>Deep copy.</summary>
        public AdsRoutingData Clone() => JsonUtility.FromJson<AdsRoutingData>(JsonUtility.ToJson(this));
    }

    /// <summary>A resolved route: mode plus the provider ids that are enabled and installed, in priority order.</summary>
    public sealed class AdsRoute
    {
        public static readonly AdsRoute Empty = new AdsRoute(AdsRouteMode.Chain, Array.Empty<string>());

        public AdsRoute(AdsRouteMode mode, IReadOnlyList<string> providerIds)
        {
            Mode = mode;
            ProviderIds = providerIds;
        }

        public AdsRouteMode Mode { get; }
        public IReadOnlyList<string> ProviderIds { get; }
        public bool IsEmpty => ProviderIds.Count == 0;
    }
}
