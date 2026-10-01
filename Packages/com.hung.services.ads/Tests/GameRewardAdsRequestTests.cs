using NUnit.Framework;
using Hung.Base;

namespace Hung.Ads.Tests
{
    public sealed class GameRewardAdsRequestTests
    {
        [Test]
        public void HiddenAfterReward_CompletesEarnedRewardOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "1"));
            AdsShowResult observed = default;
            controller.TryBegin(request, result => observed = result, out var context, out _);
            var session = new RewardedRequestSession(context);

            session.OnRewardEarned("provider-reward");
            session.OnHidden();
            session.OnDisplayFailed();

            Assert.AreEqual(AdsRequestOutcome.Completed, observed.Outcome);
            Assert.IsTrue(observed.IsEarnedReward);
            Assert.AreEqual("provider-reward", observed.ProviderEvidence);
        }

        [Test]
        public void HiddenWithoutReward_CompletesSkippedOnce()
        {
            var controller = new AdsRequestController();
            var request = new AdsShowRequest(AdsRequestId.Create("reward", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "2"));
            AdsShowResult observed = default;
            controller.TryBegin(request, result => observed = result, out var context, out _);

            new RewardedRequestSession(context).OnHidden();

            Assert.AreEqual(AdsRequestOutcome.Skipped, observed.Outcome);
            Assert.IsFalse(observed.IsEarnedReward);
            Assert.IsTrue(observed.ShouldContinueFlow);
        }
    }

    public sealed class GameRewardAdsReloadTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void NextLoad_WaitsForHidden_AndSecondAdCanShow(bool earnReward)
        {
            var go = new UnityEngine.GameObject(nameof(GameRewardAdsReloadTests));
            try
            {
                var ads = go.AddComponent<GameRewardAds>();
                var provider = new LifecycleProvider();
                var registry = new AdsProviderRegistry();
                registry.RegisterRewarded(ADS_TYPE.MAX, provider);
                ads.ConfigureProviders(registry);
                int completions = 0;
                AdsShowResult first = default;
                var request = new AdsShowRequest(AdsRequestId.Create("reload-test", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "first"));
                ads.Show(request, result => { first = result; completions++; });

                if (earnReward) provider.EarnReward();
                Assert.AreEqual(0, provider.LoadCalls, "MAX refuses preload while the current ad is showing.");
                provider.Hide();
                Assert.AreEqual(1, completions);
                Assert.AreEqual(earnReward, first.IsEarnedReward);
                Assert.AreEqual(1, provider.LoadCalls, "Closing must preload even when no reward was earned.");
                Assert.AreEqual(0, provider.RejectedLoads);

                provider.CompleteLoad();
                ads.Show(new AdsShowRequest(AdsRequestId.Create("reload-test", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "second")), _ => completions++);
                Assert.AreEqual(2, provider.ShowCalls);
                provider.EarnReward();
                provider.Hide();
                Assert.AreEqual(2, completions);
                Assert.AreEqual(2, provider.LoadCalls);
                Assert.AreEqual(0, provider.RejectedLoads);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        sealed class LifecycleProvider : IRewardedAdsProvider
        {
            public bool IsCanShow { get; private set; } = true;
            public bool IsLoading { get; private set; }
            public int LoadCalls { get; private set; }
            public int ShowCalls { get; private set; }
            public int RejectedLoads { get; private set; }
            bool showing;
            public void Show(Placement placement = Placement.NONE) { ShowCalls++; showing = true; IsCanShow = false; }
            public void Load() { LoadCalls++; IsLoading = true; if (showing) RejectedLoads++; }
            public void CompleteLoad() { IsLoading = false; IsCanShow = true; }
            public void EarnReward() => OnAdsReceiveReward?.Invoke();
            public void Hide() { showing = false; OnAdsHidden?.Invoke(); }
            public event System.Action OnAdsReceiveReward;
            public event System.Action OnAdsHidden;
#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsLoadFail;
            public event System.Action OnAdsDisplayFail;
#pragma warning restore 67
        }
    }


    public sealed class AdsFormatEnableTests
    {
        [Test]
        public void ManagerFormatSwitches_DisableLoadingAndCompleteSkippedRequests()
        {
            var go = new UnityEngine.GameObject(nameof(AdsFormatEnableTests));
            var previous = Locator.Ads;
            try
            {
                var reward = go.AddComponent<GameRewardAds>();
                var inter = go.AddComponent<GameInterAds>();
                typeof(GameInterAds).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(inter, null);
                var banner = go.AddComponent<GameBannerAds>();
                var managerType = typeof(GameRewardAds).Assembly.GetType("Hung.Ads.AdsManager");
                var manager = go.AddComponent(managerType);
                Set(manager, "rewardBehaviour", reward);
                Set(manager, "interBehaviour", inter);
                Set(manager, "bannerBehaviour", banner);
                Set(manager, "reward", reward);
                Set(manager, "inter", inter);
                Set(manager, "banner", banner);
                foreach (var name in new[] { "rewardedAdsEnabled", "interstitialAdsEnabled", "bannerAdsEnabled" })
                    Assert.IsTrue((bool)managerType.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(manager), "Existing assets keep formats enabled by default.");
                Set(manager, "rewardedAdsEnabled", false);
                Set(manager, "interstitialAdsEnabled", false);
                Set(manager, "bannerAdsEnabled", false);
                managerType.GetMethod("ApplyFormatSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(manager, null);

                var provider = new CountingProvider();
                var registry = new AdsProviderRegistry();
                registry.RegisterRewarded(ADS_TYPE.MAX, provider);
                registry.RegisterInterstitial(ADS_TYPE.MAX, provider);
                registry.RegisterBanner(ADS_TYPE.MAX, provider);
                reward.ConfigureProviders(registry);
                inter.ConfigureProviders(registry);
                banner.ConfigureProviders(registry);
                reward.Type = ADS_TYPE.MAX;
                inter.Type = ADS_TYPE.MAX;
                inter.Load();
                banner.InitBanner();
                banner.Show();

                int completed = 0;
                ((IAdsRequestService)manager).ShowRewarded(new AdsShowRequest(AdsRequestId.Create("disabled", AdsRequestKind.Rewarded, Placement.DAILY_REWARD, "reward")), r => { Assert.AreEqual(AdsRequestOutcome.Skipped, r.Outcome); Assert.IsFalse(r.IsEarnedReward); Assert.AreEqual("ads-disabled", r.DiagnosticCode); completed++; });
                ((IAdsRequestService)manager).ShowInterstitial(new AdsShowRequest(AdsRequestId.Create("disabled", AdsRequestKind.Interstitial, Placement.IN_GAME, "inter")), r => { Assert.AreEqual(AdsRequestOutcome.Skipped, r.Outcome); Assert.IsTrue(r.ShouldContinueFlow); Assert.AreEqual("ads-disabled", r.DiagnosticCode); completed++; });
                // Simulate late completion/failure callbacks: disabled formats must not restart loads.
                provider.Fail();
                provider.Hidden();
                Assert.AreEqual(2, completed);
                Assert.AreEqual(0, provider.LoadCalls);
                Assert.AreEqual(0, provider.ShowCalls);
                Assert.AreEqual(0, provider.BannerInitCalls);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); Locator.Ads = previous; }
        }

        static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing format switch: " + name);
            field.SetValue(target, value);
        }

        sealed class CountingProvider : IRewardedAdsProvider, IInterstitialAdsProvider, IBannerAdsProvider
        {
            public bool IsCanShow => false;
            public bool IsLoading => false;
            public int LoadCalls;
            public int ShowCalls;
            public int BannerInitCalls;
            public void Load() { LoadCalls++; }
            public void Show(Placement placement = Placement.NONE) { ShowCalls++; }
            public void Show() { ShowCalls++; }
            public void InitBanner() { BannerInitCalls++; }
            public void Hide() { }
            public void Destroy() { }
            public void Fail() => OnAdsLoadFail?.Invoke();
            public void Hidden() { OnAdsHidden?.Invoke(); OnAdsDone?.Invoke(); }
            public event System.Action OnAdsLoadFail;
            public event System.Action OnAdsHidden;
            public event System.Action OnAdsDone;
#pragma warning disable 67
            public event System.Action OnAdsLoaded;
            public event System.Action OnAdsDisplayFail;
            public event System.Action OnAdsReceiveReward;
#pragma warning restore 67
        }
    }

}
