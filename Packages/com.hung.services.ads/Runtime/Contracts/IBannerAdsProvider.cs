using System;

namespace Hung.Ads
{
    // Internal vendor-provider contract (paper §11.2). Implemented by exactly one
    // MonoBehaviour per mediation vendor, living in that vendor's Integration
    // assembly. Neutral Game*Ads classes hold the concrete component reference as
    // a plain MonoBehaviour (Unity can serialize/Inspector-assign that across an
    // assembly boundary) and cast to this interface at runtime - see GameBannerAds.
    public interface IBannerAdsProvider
    {
        void InitBanner();
        void Show();
        void Hide();
        void Destroy();
        void Load();

        event Action OnAdsLoaded;
        event Action OnAdsLoadFail;
    }
}
