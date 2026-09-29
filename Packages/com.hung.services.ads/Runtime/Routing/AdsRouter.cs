using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>
    /// Resolves which providers serve a format/placement: placement override, else format route,
    /// minus providers that are disabled, unknown, or not installed. Reads the remote override on
    /// every call (parsed once per distinct JSON string); a rejected override falls back to baseline.
    /// </summary>
    public sealed class AdsRouter
    {
        readonly AdsRoutingData baseline;
        readonly Func<string, AdsFormat, bool> isInstalled;
        readonly Func<string> overrideJson;
        string cachedJson;
        AdsRoutingData cachedData;
        bool warnedBannerParallel;
        bool warnedWideParallel;

        public AdsRouter(AdsRoutingData baseline, Func<string, AdsFormat, bool> isInstalled, Func<string> overrideJson = null)
        {
            this.baseline = baseline ?? new AdsRoutingData();
            this.isInstalled = isInstalled ?? ((id, f) => false);
            this.overrideJson = overrideJson;
        }

        AdsRoutingData Current
        {
            get
            {
                string json = overrideJson?.Invoke();
                if (string.IsNullOrEmpty(json)) return baseline;
                if (json == cachedJson) return cachedData;
                cachedJson = json;
                if (AdsRoutingOverride.TryApply(baseline, json, out var merged, out var error))
                {
                    cachedData = merged;
                }
                else
                {
                    Debug.LogWarning("[Ads Routing] Override rejected, using baked config: " + error);
                    cachedData = baseline;
                }
                return cachedData;
            }
        }

        /// <summary>Route for this format and placement. Never null; may be empty.</summary>
        public AdsRoute Resolve(AdsFormat format, Placement placement) => Resolve(Current, format, placement);

        /// <summary>Index 0 = format default route; then each placement route of this format (not for banner).</summary>
        public IReadOnlyList<AdsRoute> PreloadRoutes(AdsFormat format)
        {
            var data = Current;
            var routes = new List<AdsRoute> { Resolve(data, format, Placement.NONE) };
            if (format == AdsFormat.Banner) return routes;
            foreach (var p in data.placements)
            {
                if (p != null && p.format == format) routes.Add(Build(data, format, p.mode, p.order));
            }
            return routes;
        }

        AdsRoute Resolve(AdsRoutingData data, AdsFormat format, Placement placement)
        {
            if (placement != Placement.NONE && format != AdsFormat.Banner)
            {
                var p = data.placements.Find(x => x != null && x.format == format && x.placement == placement);
                if (p != null) return Build(data, format, p.mode, p.order);
            }
            var f = data.formats.Find(x => x != null && x.format == format);
            return f == null ? AdsRoute.Empty : Build(data, format, f.mode, f.order);
        }

        AdsRoute Build(AdsRoutingData data, AdsFormat format, AdsRouteMode mode, List<string> order)
        {
            if (format == AdsFormat.Banner && mode == AdsRouteMode.Parallel)
            {
                if (!warnedBannerParallel) Debug.LogWarning("[Ads Routing] Banner does not support Parallel; using Chain.");
                warnedBannerParallel = true;
                mode = AdsRouteMode.Chain;
            }

            var ids = new List<string>();
            if (order != null)
            {
                foreach (var id in order)
                {
                    if (string.IsNullOrEmpty(id) || ids.Contains(id)) continue;
                    var entry = data.FindProvider(id);
                    if (entry == null || !entry.enabled || !isInstalled(id, format)) continue;
                    ids.Add(id);
                }
            }

            if (format == AdsFormat.Interstitial && mode == AdsRouteMode.Parallel && ids.Count > 2 && !warnedWideParallel)
            {
                warnedWideParallel = true;
                Debug.LogWarning("[Ads Routing] Parallel interstitial route with more than 2 providers holds several full-screen ads in memory.");
            }
            return ids.Count == 0 ? AdsRoute.Empty : new AdsRoute(mode, ids);
        }
    }
}
