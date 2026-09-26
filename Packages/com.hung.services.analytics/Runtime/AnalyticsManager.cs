using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;
using AppsFlyerSDK;
using Firebase.Extensions;
using Hung.DesignPattern;

namespace Hung.Analytics
{
    using Hung.Base;
    using Hung.Common;
    using Hung.Utilities.Timer;
    [DefaultExecutionOrder(-100)]
    public class AnalyticsManager : SimpleSingleton<AnalyticsManager>, IAnalyticsService, IRevenueEventSink
    {
        bool _firstAdsSession = false;
        STimer _firstAdsSessionTimer;
        int _firstAdsSessionTime = 0;

        GameData gameData;
        GameData GameData => gameData ??= Locator.Data.GetData<GameData>();
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Locator.Analytics = this;
                Locator.RevenueSink = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(this);

            _firstAdsSessionTime = 0;
            _firstAdsSessionTimer = TimerManager.Ins.PopSTimer();
            _firstAdsSessionTimer.Start(1, () =>
            {
                _firstAdsSessionTime += 1;
            }, true);
        }

        public void AdsRewardOffer(Placement place)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("Reward_request");
            FirebaseAnalytics.LogEvent($"Reward_request_action_{place}", "placement", place.ToString());
        }
        public void AdsRewardClick(Placement place)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("Reward_click");
        }
        public void AdsRewardShow(Placement place)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent($"Reward_show_action_{place}");
            CheckFirstAdsSession(place);
        }
        public void AdsRewardShowFail(Placement place, string error)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("Reward_show_failed", new Parameter[]
            {
                    new Parameter("placement", place.ToString()),
                    new Parameter("errormsg", error),
            });
        }
        public void AdsRewardLoadFail()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"Reward_request_failed");
        }
        public void AdsRewardLoadComplete()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"Reward_request_success");
        }
        public void AdsRewardLoad()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("ad_reward_load");
        }
        public void AdsRewardComplete(Placement place, string adsType)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            // FirebaseAnalytics.LogEvent("ads_reward_complete", new Parameter[]
            // {
            //         new Parameter("placement", place.ToString()),
            //         new Parameter("adsType", adsType),
            // });
            FirebaseAnalytics.LogEvent($"Reward_finish_action_{place}");
            CheckFirstAdsSession(place);
        }

        public void AdsInterFail(string error)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("Inters_request_failed", "errormsg", error);
        }

        public void AdsInterLoad()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("Inters_request");
        }

        public void AdsInterShow(Placement place)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("inters_show", "placement", place.ToString());
            CheckFirstAdsSession(place);
        }
        public void AdsInterClick()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("inters_click");
        }

        public void AdsInterLoadComplete()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"Inters_request_success");
        }

        public void AdsInterComplete()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"inters_finish");
        }

        private void CheckFirstAdsSession(Placement place)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            if (!_firstAdsSession)
            {
                _firstAdsSession = true;
                FirebaseAnalytics.LogEvent("first_ads_session", new Parameter[]
                {
                    new Parameter("placement", place.ToString()),
                    new Parameter("duration", _firstAdsSessionTime)
                });
                _firstAdsSessionTimer.Stop();
            }
        }

        //public void ResourceSpend(BoosterType type, Resource.Placement place, int amount)
        //{
        //    if (!FirebaseManager.Ins.IsAvailable) return;

        //    FirebaseAnalytics.LogEvent("resource_spend", new Parameter[]
        //    {
        //        new Parameter("name", type.ToString()),
        //        new Parameter("placement", place.ToString()),
        //        new Parameter("value", amount)
        //    });
        //}

        public void FireUserProps()
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            FirebaseAnalytics.LogEvent("UserProps", "Level", GameData.user.normalLevelIndex.ToString());
        }

        public void Day()
        {

        }
        public void GoogleFireBaseTrackEvent(string name)
        {
            //Dictionary<string, string> eventValue = new Dictionary<string, string>();
            //eventValue.Add("af_quantity", "1");
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent(name);
        }
        public void AppsFlyerTrackEvent(string name)
        {
            //Dictionary<string, string> eventValue = new Dictionary<string, string>();
            //eventValue.Add("af_quantity", "1");
            AppsFlyer.sendEvent(name, null);
        }
        public void AppsFlyerTrackParamEvent(string name, Dictionary<string, string> param)
        {
            AppsFlyer.sendEvent(name, param);
        }

        public void LevelTrackEvent(LEVEL_STATE state, int value = 0)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            if (!GameData.IsFirstTimeUser)
            {
                switch (state)
                {
                    case LEVEL_STATE.START:
                        FirebaseAnalytics.LogEvent($"level_{value}_start");
                        break;
                    case LEVEL_STATE.COMPLETE:
                    case LEVEL_STATE.FAIL:
                        FirebaseAnalytics.LogEvent($"level_{value}_stars_{GameData.level.Star}");
                        if(value >= gameData.level.PassLevels.Count)
                        {
                            for(int i = gameData.level.PassLevels.Count; i <= value; i++)
                            {
                                gameData.level.PassLevels.Add(false);
                            }
                        }
                        if (!gameData.level.PassLevels[value])
                        {
                            gameData.level.PassLevels[value] = true;
                            FirebaseAnalytics.LogEvent($"level_{value}_pass");
                        }
                        else
                        {
                            FirebaseAnalytics.LogEvent($"level_{value}_replay_pass");
                        }
                        break;
                }
            }
            else
            {
                switch (state)
                {
                    case LEVEL_STATE.START:

                        FirebaseAnalytics.LogEvent($"ftu_level_{value}_start");
                        break;
                    case LEVEL_STATE.COMPLETE:
                    case LEVEL_STATE.FAIL:
                        FirebaseAnalytics.LogEvent($"ftu_level_{value}_star_{GameData.level.Star}");
                        if (!gameData.level.PassLevels[value])
                        {
                            gameData.level.PassLevels[value] = true;
                            FirebaseAnalytics.LogEvent($"ftu_level_{value}_pass");
                        }
                        else
                        {
                            FirebaseAnalytics.LogEvent($"ftu_level_{value}_replay_pass");
                        }
                        break;
                }
            }
        }
        public void EarnVirtualCurrency(string name, long value, string source)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            // FirebaseAnalytics.LogEvent("earn_virtual_currency", new Parameter[]
            // {
            //         new Parameter("virtual_currency_name", name),
            //         new Parameter("value", value),
            //         new Parameter("source", source)
            // });
        }
        public void SpendVirtualCurrency(string name, long value, string itemName)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;

            // FirebaseAnalytics.LogEvent("spend_virtual_currency", new Parameter[]
            // {
            //         new Parameter("virtual_currency_name", name),
            //         new Parameter("value", value),
            //         new Parameter("item_name", itemName)
            // });
        }

        public void TutorialStep(string name, int step)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"{name}_step_{step}");
        }
        public void BuyIAPComplete(IAP_ITEM item, Placement placement)
        {
            if (!FirebaseManager.Ins.IsAvailable) return;
            FirebaseAnalytics.LogEvent($"IAP_packname_{item}_Location_{placement}");
        }

        // IRevenueEventSink (Ph6): ad mediation revenue routed here via
        // Locator.RevenueSink instead of ads calling AppsFlyer directly.
        // `extra` uses vendor-neutral keys (country/ad_unit/ad_type/placement) -
        // remapped here to AppsFlyer's own AdRevenueScheme constants, which is
        // what its backend actually expects.
        private static readonly Dictionary<string, string> RevenueSchemeKeys = new Dictionary<string, string>
        {
            { "country", AdRevenueScheme.COUNTRY },
            { "ad_unit", AdRevenueScheme.AD_UNIT },
            { "ad_type", AdRevenueScheme.AD_TYPE },
            { "placement", AdRevenueScheme.PLACEMENT },
        };

        public void OnRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra = null)
        {
            AFAdRevenueData adRevenueData = new AFAdRevenueData(source, MediationNetwork.IronSource, currency, value);
            Dictionary<string, string> additionalParams = null;
            if (extra != null)
            {
                additionalParams = new Dictionary<string, string>();
                foreach (var kv in extra)
                {
                    additionalParams[RevenueSchemeKeys.TryGetValue(kv.Key, out var scheme) ? scheme : kv.Key] = kv.Value;
                }
            }
            AppsFlyer.logAdRevenue(adRevenueData, additionalParams);
        }
    }
}