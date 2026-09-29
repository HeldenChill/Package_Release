using System.Collections.Generic;

namespace Hung.Base
{
    /// <summary>
    /// Game-facing analytics proxy, reached through <c>Locator.Analytics</c>. Game code calls only this;
    /// the implementation fans each call out to every active analytics SDK subscribed to the event's
    /// <see cref="AnalyticsCategory"/>. New GD tracking = a new method here (or <see cref="LogEvent"/>),
    /// never a direct SDK call.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>Generic event. Use for GD one-offs before they earn a named method.</summary>
        void LogEvent(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters = null);
        /// <summary>User-level property, sent to every active backend.</summary>
        void SetUserProperty(string key, string value);

        /// <summary>Rewarded ad offered at <paramref name="place"/>.</summary>
        void AdsRewardOffer(Placement place);
        /// <summary>Rewarded ad button clicked.</summary>
        void AdsRewardClick(Placement place);
        /// <summary>Rewarded ad displayed.</summary>
        void AdsRewardShow(Placement place);
        /// <summary>Rewarded ad failed to display.</summary>
        void AdsRewardShowFail(Placement place, string error);
        /// <summary>Rewarded ad finished; reward granted.</summary>
        void AdsRewardComplete(Placement place, string type);
        /// <summary>Rewarded ad loaded.</summary>
        void AdsRewardLoadComplete();
        /// <summary>Rewarded ad load requested.</summary>
        void AdsRewardLoad();
        /// <summary>Rewarded ad load failed.</summary>
        void AdsRewardLoadFail();

        /// <summary>Interstitial load failed.</summary>
        void AdsInterFail(string error);
        /// <summary>Interstitial load requested.</summary>
        void AdsInterLoad();
        /// <summary>Interstitial displayed.</summary>
        void AdsInterShow(Placement place);
        /// <summary>Interstitial clicked.</summary>
        void AdsInterClick();
        /// <summary>Interstitial loaded.</summary>
        void AdsInterLoadComplete();
        /// <summary>Interstitial closed.</summary>
        void AdsInterComplete();

        /// <summary>IAP purchase completed.</summary>
        void BuyIAPComplete(IAP_ITEM item, Placement placement);
        /// <summary>Tutorial step reached.</summary>
        void TutorialStep(string name, int step);

        /// <summary>
        /// Level lifecycle. Pure reporting: the caller owns pass state (use <c>GameData.LevelData.MarkPassed</c>
        /// on COMPLETE and pass its result as <paramref name="firstPass"/>; FAIL passes false).
        /// </summary>
        void LevelTrackEvent(LEVEL_STATE state, int level, int stars, bool firstPass, bool isFtu);
        /// <summary>Virtual currency earned.</summary>
        void EarnVirtualCurrency(string name, long value, string source);
        /// <summary>Virtual currency spent.</summary>
        void SpendVirtualCurrency(string name, long value, string itemName);
    }
}
