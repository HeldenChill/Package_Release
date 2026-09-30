namespace Hung.Base
{
    /// <summary>
    /// Facts the ads module needs from the host game. The ads package never reads GameData;
    /// the host registers one implementation in <see cref="Locator.AdsEntitlements"/>.
    /// Implementations are read live on every check, so return current values.
    /// </summary>
    public interface IAdsEntitlements
    {
        /// <summary>Normal remove-ads: suppresses interstitial and banner, not rewarded.</summary>
        bool IsRemoveAds { get; }
        /// <summary>Premium remove-ads: also skips rewarded (reward auto-granted).</summary>
        bool IsPremiumRemoveAds { get; }
        /// <summary>Player progression used for start-level gates (interstitial, app open).</summary>
        int LevelIndex { get; }
    }

    /// <summary>Used when the host registers nothing: ads shown, level 0.</summary>
    public sealed class NullAdsEntitlements : IAdsEntitlements
    {
        public static readonly NullAdsEntitlements Instance = new NullAdsEntitlements();
        public bool IsRemoveAds => false;
        public bool IsPremiumRemoveAds => false;
        public int LevelIndex => 0;
    }
}
