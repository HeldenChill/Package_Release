using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    public interface IAnalyticsService
    {
        public abstract void AdsRewardOffer(Placement place);
        public abstract void AdsRewardClick(Placement place);
        public abstract void AdsRewardShow(Placement place);
        public abstract void AdsRewardShowFail(Placement place, string error);
        public abstract void AdsRewardComplete(Placement place, string type);
        public abstract void AdsRewardLoadComplete();
        public abstract void AdsRewardLoad();
        public abstract void AdsRewardLoadFail();

        public abstract void AdsInterFail(string error);
        public abstract void AdsInterLoad();
        public abstract void AdsInterShow(Placement place);
        public abstract void AdsInterClick();
        public abstract void AdsInterLoadComplete();
        public abstract void AdsInterComplete();
        public abstract void BuyIAPComplete(IAP_ITEM item, Placement placement);
        public abstract void TutorialStep(string name, int step);


        //public void ResourceSpend(BoosterType type, Resource.Placement place, int amount)
        //{
        //    if (!isFirebaseInit) return;

        //    FirebaseAnalytics.LogEvent("resource_spend", new Parameter[]
        //    {
        //        new Parameter("name", type.ToString()),
        //        new Parameter("placement", place.ToString()),
        //        new Parameter("value", amount)
        //    });
        //}

        public abstract void FireUserProps();

        public abstract void Day();
        public abstract void GoogleFireBaseTrackEvent(string name);

        public abstract void AppsFlyerTrackParamEvent(string name, Dictionary<string, string> param);

        public abstract void LevelTrackEvent(LEVEL_STATE state, int value = 0);
        public abstract void EarnVirtualCurrency(string name, long value, string source);
        public abstract void SpendVirtualCurrency(string name, long value, string itemName);
    }
}
