using System.Collections.Generic;

namespace Hung.Analytics
{
    using Hung.Base;

    // Ph6 test double (paper §17.7): records every call instead of hitting
    // Firebase/AppsFlyer, so EditMode/PlayMode tests can assert "X was tracked"
    // without a real analytics SDK. Also implements IRevenueEventSink so ads-
    // side revenue-sink tests don't need the real AnalyticsManager either.
    public readonly struct RecordedEvent
    {
        public readonly string Name;
        public readonly object[] Args;
        public RecordedEvent(string name, params object[] args)
        {
            Name = name;
            Args = args;
        }
    }

    public class RecordingAnalyticsService : IAnalyticsService, IRevenueEventSink
    {
        public List<RecordedEvent> Events { get; } = new List<RecordedEvent>();

        void Record(string name, params object[] args) => Events.Add(new RecordedEvent(name, args));

        public void AdsRewardOffer(Placement place) => Record(nameof(AdsRewardOffer), place);
        public void AdsRewardClick(Placement place) => Record(nameof(AdsRewardClick), place);
        public void AdsRewardShow(Placement place) => Record(nameof(AdsRewardShow), place);
        public void AdsRewardShowFail(Placement place, string error) => Record(nameof(AdsRewardShowFail), place, error);
        public void AdsRewardComplete(Placement place, string type) => Record(nameof(AdsRewardComplete), place, type);
        public void AdsRewardLoadComplete() => Record(nameof(AdsRewardLoadComplete));
        public void AdsRewardLoad() => Record(nameof(AdsRewardLoad));
        public void AdsRewardLoadFail() => Record(nameof(AdsRewardLoadFail));

        public void AdsInterFail(string error) => Record(nameof(AdsInterFail), error);
        public void AdsInterLoad() => Record(nameof(AdsInterLoad));
        public void AdsInterShow(Placement place) => Record(nameof(AdsInterShow), place);
        public void AdsInterClick() => Record(nameof(AdsInterClick));
        public void AdsInterLoadComplete() => Record(nameof(AdsInterLoadComplete));
        public void AdsInterComplete() => Record(nameof(AdsInterComplete));
        public void BuyIAPComplete(IAP_ITEM item, Placement placement) => Record(nameof(BuyIAPComplete), item, placement);
        public void TutorialStep(string name, int step) => Record(nameof(TutorialStep), name, step);

        public void FireUserProps() => Record(nameof(FireUserProps));
        public void Day() => Record(nameof(Day));
        public void GoogleFireBaseTrackEvent(string name) => Record(nameof(GoogleFireBaseTrackEvent), name);
        public void AppsFlyerTrackParamEvent(string name, Dictionary<string, string> param) => Record(nameof(AppsFlyerTrackParamEvent), name, param);
        public void LevelTrackEvent(LEVEL_STATE state, int value = 0) => Record(nameof(LevelTrackEvent), state, value);
        public void EarnVirtualCurrency(string name, long value, string source) => Record(nameof(EarnVirtualCurrency), name, value, source);
        public void SpendVirtualCurrency(string name, long value, string itemName) => Record(nameof(SpendVirtualCurrency), name, value, itemName);

        public void OnRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra = null)
            => Record(nameof(OnRevenue), source, value, currency, extra);
    }
}
