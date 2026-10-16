using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using NUnit.Framework;

namespace Hung.Ads.Tests
{
    public sealed class VideoAdsAnalyticsTests
    {
        readonly List<KeyValuePair<string, Dictionary<string, object>>> _events = new List<KeyValuePair<string, Dictionary<string, object>>>();
        float _now;

        [SetUp]
        public void SetUp()
        {
            _events.Clear();
            _now = 0f;
            VideoAdsAnalytics.ResetForPlay();
            VideoAdsAnalytics.Sink = (name, args) =>
                _events.Add(new KeyValuePair<string, Dictionary<string, object>>(name, args.ToDictionary(p => p.Key, p => p.Value)));
            VideoAdsAnalytics.HasConnection = () => true;
            VideoAdsAnalytics.Clock = () => _now;
        }

        [TearDown]
        public void TearDown() => VideoAdsAnalytics.ResetForPlay();

        string[] Names() => _events.Select(e => e.Key).ToArray();
        string[] Results() => _events.Select(e => (string)e.Value["result"]).ToArray();

        static AdsShowRequest Request(string n) =>
            new AdsShowRequest(AdsRequestId.Create("video-ads-test", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, n));

        [TestCase(true, "watched")]
        [TestCase(false, "canceled")]
        public void Rewarded_ShowThenHide_SendsAvailableStartedWatchInOrder(bool earnReward, string watchResult)
        {
            var go = new UnityEngine.GameObject(nameof(VideoAdsAnalyticsTests));
            try
            {
                var provider = new FakeRewardedProvider { IsCanShow = true };
                var ads = Create(go, provider);

                ads.Show(Request("1"), _ => { });
                if (earnReward) provider.EarnReward();
                provider.Hide();

                CollectionAssert.AreEqual(new[] { "video_ads_available", "video_ads_started", "video_ads_watch" }, Names());
                CollectionAssert.AreEqual(new[] { "success", "success", watchResult }, Results());
                foreach (var e in _events)
                {
                    Assert.AreEqual("rewarded", e.Value["ad_type"]);
                    Assert.AreEqual("daily_reward", e.Value["placement"]);
                    Assert.AreEqual(true, e.Value["connection"]);
                    Assert.AreEqual(4, e.Value.Count, "Only ad_type, placement, result and connection.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rewarded_HideWithoutReward_SendsCanceled()
        {
            var go = new UnityEngine.GameObject(nameof(VideoAdsAnalyticsTests));
            try
            {
                var provider = new FakeRewardedProvider { IsCanShow = true };
                var ads = Create(go, provider);

                ads.Show(Request("1"), _ => { });
                provider.Hide();
                provider.Hide();

                Assert.AreEqual(1, Names().Count(n => n == "video_ads_watch"), "Watch is sent once per started display.");
                Assert.AreEqual("canceled", Results().Last());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rewarded_DisplayFailure_CompletesFailedWatchOnceAndAllowsNextShow()
        {
            var go = new UnityEngine.GameObject(nameof(VideoAdsAnalyticsTests));
            try
            {
                var provider = new FakeRewardedProvider { IsCanShow = true };
                var ads = Create(go, provider);
                int completions = 0;
                AdsShowResult observed = default;
                ads.Show(Request("failed"), result => { completions++; observed = result; });
                provider.FailDisplay();
                Assert.AreEqual(1, completions, "First display failure must finish immediately, without Hidden.");
                provider.FailDisplay();
                provider.Hide();

                CollectionAssert.AreEqual(new[] { "success", "success", "failed" }, Results());
                Assert.AreEqual(1, completions);
                Assert.AreEqual(AdsRequestOutcome.Failed, observed.Outcome);
                Assert.IsFalse(observed.IsEarnedReward);
                provider.IsCanShow = true;
                ads.Show(Request("next"), _ => completions++);
                provider.EarnReward();
                provider.Hide();
                Assert.AreEqual(2, completions);
                CollectionAssert.AreEqual(new[] { "success", "success", "failed", "success", "success", "watched" }, Results());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rewarded_Unavailable_IsSuppressedFor60SecondsPerPlacement()
        {
            var go = new UnityEngine.GameObject(nameof(VideoAdsAnalyticsTests));
            try
            {
                var provider = new FakeRewardedProvider { IsCanShow = false };
                var ads = Create(go, provider);

                ads.Show(Request("a"), _ => { });
                _now = 10f;
                ads.Show(Request("b"), _ => { });
                _now = 61f;
                ads.Show(Request("c"), _ => { });

                CollectionAssert.AreEqual(new[] { "video_ads_available", "video_ads_available" }, Names());
                CollectionAssert.AreEqual(new[] { "not_available", "not_available" }, Results());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rewarded_SuccessClearsSuppression()
        {
            VideoAdsAnalytics.Available(VideoAdsAnalytics.Rewarded, Placement.DAILY_REWARD, false);
            VideoAdsAnalytics.Available(VideoAdsAnalytics.Rewarded, Placement.DAILY_REWARD, true);
            _now = 5f;
            VideoAdsAnalytics.Available(VideoAdsAnalytics.Rewarded, Placement.DAILY_REWARD, false);

            CollectionAssert.AreEqual(new[] { "not_available", "success", "not_available" }, Results());
        }

        [Test]
        public void Interstitial_NotAvailable_IsNeverSuppressed_AndReportsConnection()
        {
            VideoAdsAnalytics.HasConnection = () => false;

            VideoAdsAnalytics.Available(VideoAdsAnalytics.Interstitial, Placement.IN_GAME, false);
            VideoAdsAnalytics.Available(VideoAdsAnalytics.Interstitial, Placement.IN_GAME, false);

            CollectionAssert.AreEqual(new[] { "not_available", "not_available" }, Results());
            Assert.AreEqual("interstitial", _events[0].Value["ad_type"]);
            Assert.AreEqual("in_game", _events[0].Value["placement"]);
            Assert.AreEqual(false, _events[0].Value["connection"]);
        }

        [TestCase(AdsRequestOutcome.Completed, "watched")]
        [TestCase(AdsRequestOutcome.Skipped, "canceled")]
        [TestCase(AdsRequestOutcome.Failed, "failed")]
        [TestCase(AdsRequestOutcome.Unavailable, "failed")]
        public void WatchResult_MapsOutcome(AdsRequestOutcome outcome, string expected)
        {
            var id = AdsRequestId.Create("video-ads-test", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "map");
            Assert.AreEqual(expected, VideoAdsAnalytics.WatchResult(new AdsShowResult(id, outcome)));
        }

        [Test]
        public void SinkException_IsSwallowed()
        {
            VideoAdsAnalytics.Sink = (name, args) => throw new InvalidOperationException("backend down");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("video_ads_started analytics failed"));
            Assert.DoesNotThrow(() => VideoAdsAnalytics.Started(VideoAdsAnalytics.Rewarded, Placement.DAILY_REWARD));
        }

        static GameRewardAds Create(UnityEngine.GameObject go, FakeRewardedProvider provider)
        {
            var ads = go.AddComponent<GameRewardAds>();
            var registry = new AdsProviderRegistry();
            registry.RegisterRewarded(ADS_TYPE.MAX, provider);
            ads.ConfigureProviders(registry);
            return ads;
        }

        sealed class FakeRewardedProvider : IRewardedAdsProvider
        {
            public bool IsCanShow { get; set; }
            public bool IsLoading { get; private set; }
            public void Show(Placement placement = Placement.NONE) { IsCanShow = false; }
            public void Load() { IsLoading = true; }
            public void EarnReward() => OnAdsReceiveReward?.Invoke();
            public void Hide() => OnAdsHidden?.Invoke();
            public void FailDisplay() => OnAdsDisplayFail?.Invoke();
            public event Action OnAdsReceiveReward;
            public event Action OnAdsHidden;
#pragma warning disable 67
            public event Action OnAdsLoaded;
            public event Action OnAdsLoadFail;
            public event Action OnAdsDisplayFail;
#pragma warning restore 67
        }
    }
}
