using System;

namespace Hung.Ads
{
    using Hung.Base;

    // See IBannerAdsProvider for the pattern this contract follows.
    public interface IInterstitialAdsProvider
    {
        bool IsCanShow { get; }
        bool IsLoading { get; }
        void Load();
        void Show(Placement placement);

        event Action OnAdsLoaded;
        event Action OnAdsLoadFail;
        event Action OnAdsDisplayFail;
        event Action OnAdsDone;
    }
}
