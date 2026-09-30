using UnityEngine;

namespace Hung.Ads
{
    /// <summary>
    /// Ad pacing counters owned by the ads module. Session-only by design: they reset every
    /// launch (and every Editor play with domain reload off) and are never persisted.
    /// </summary>
    public static class AdsSessionCounters
    {
        public static int WatchedAds;
        public static int PlayGameAds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { WatchedAds = 0; PlayGameAds = 0; }
    }
}
