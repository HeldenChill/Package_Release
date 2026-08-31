using Hung.DesignPattern;

namespace Hung.Ads
{
    /// <summary>Raised to reset the app-open ad frequency cap.</summary>
    public struct ResetAoaCapEvent : IEvent { }

    /// <summary>Raised to reset the interstitial ad frequency cap.</summary>
    public struct ResetInterCapEvent : IEvent { }
}
