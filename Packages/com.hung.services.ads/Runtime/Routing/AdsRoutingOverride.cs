using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    using Hung.Base;

    /// <summary>
    /// Parses a remote routing override and merges it over the baked data.
    /// JSON shape (every section optional; enum names case-insensitive):
    /// {"providers":[{"id":"yandex","enabled":false}],
    ///  "formats":[{"format":"Rewarded","mode":"Chain","order":["admob","max"]}],
    ///  "placements":[{"format":"Rewarded","placement":"REROLL_SKILL_CARD","mode":"Chain","order":["yandex"]}]}
    /// Any invalid enum name or malformed JSON rejects the whole override.
    /// </summary>
    public static class AdsRoutingOverride
    {
        [Serializable] class Dto { public List<ProviderDto> providers; public List<RouteDto> formats; public List<RouteDto> placements; }
        [Serializable] class ProviderDto { public string id; public bool enabled = true; }
        [Serializable] class RouteDto { public string format; public string placement; public string mode; public List<string> order; }

        /// <summary>Merges <paramref name="json"/> over a copy of <paramref name="baseline"/>. Baseline is never mutated.</summary>
        public static bool TryApply(AdsRoutingData baseline, string json, out AdsRoutingData merged, out string error)
        {
            merged = null;
            Dto dto;
            try { dto = JsonUtility.FromJson<Dto>(json); }
            catch (Exception e) { error = "malformed json: " + e.Message; return false; }
            if (dto == null) { error = "empty json"; return false; }

            var result = baseline.Clone();

            if (dto.providers != null)
            {
                foreach (var p in dto.providers)
                {
                    var entry = p == null ? null : result.FindProvider(p.id);
                    if (entry != null) entry.enabled = p.enabled;
                }
            }

            if (dto.formats != null)
            {
                foreach (var r in dto.formats)
                {
                    if (!TryParse(r, out AdsFormat format, out AdsRouteMode mode, out error)) return false;
                    result.formats.RemoveAll(x => x.format == format);
                    result.formats.Add(new AdsFormatRoute { format = format, mode = mode, order = r.order ?? new List<string>() });
                }
            }

            if (dto.placements != null)
            {
                foreach (var r in dto.placements)
                {
                    if (!TryParse(r, out AdsFormat format, out AdsRouteMode mode, out error)) return false;
                    if (!Enum.TryParse(r.placement, true, out Placement placement))
                    {
                        error = "unknown placement '" + r.placement + "'";
                        return false;
                    }
                    result.placements.RemoveAll(x => x.format == format && x.placement == placement);
                    result.placements.Add(new AdsPlacementRoute { format = format, placement = placement, mode = mode, order = r.order ?? new List<string>() });
                }
            }

            error = null;
            merged = result;
            return true;
        }

        static bool TryParse(RouteDto r, out AdsFormat format, out AdsRouteMode mode, out string error)
        {
            mode = default;
            error = null;
            if (r == null || !Enum.TryParse(r.format, true, out format) || !Enum.IsDefined(typeof(AdsFormat), format))
            {
                format = default;
                error = "unknown format '" + r?.format + "'";
                return false;
            }
            if (!Enum.TryParse(r.mode, true, out mode) || !Enum.IsDefined(typeof(AdsRouteMode), mode))
            {
                error = "unknown mode '" + r.mode + "'";
                return false;
            }
            return true;
        }
    }
}
