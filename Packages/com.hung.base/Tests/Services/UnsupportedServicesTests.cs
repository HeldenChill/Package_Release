using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.Base.Tests
{
    public sealed class UnsupportedServicesTests
    {
        private interface IProbe { }
        private sealed class Probe : IProbe { }
        [SetUp, TearDown] public void Reset() => ServiceSlots.ResetForTests();
        private static void Expect(string member) => LogAssert.Expect(LogType.Warning,
            "[Unsupported] IAdsService." + member + " called; no ads plugin installed.");
        [Test] public void Defaults_AreNotRealOwners()
        {
            Assert.IsInstanceOf<UnsupportedAdsService>(Locator.Ads);
            Assert.IsInstanceOf<UnsupportedIapService>(Locator.IAPService);
            Assert.IsFalse(ServiceSlots.TryGet<IAdsService>(out var ads)); Assert.IsNull(ads);
            Assert.IsNull(ServiceSlots.OwnerOf<IAdsService>());
            Assert.IsFalse(ServiceSlots.TryGet<IIAPService>(out _));
        }
        [Test] public void FallbackInstall_RejectsNullOrDifferentInstanceWithoutMutation()
        {
            var first = new Probe(); ServiceSlots.SetFallback<IProbe>(first);
            Assert.DoesNotThrow(() => ServiceSlots.SetFallback<IProbe>(first));
            Assert.Throws<ArgumentNullException>(() => ServiceSlots.SetFallback<IProbe>(null));
            Assert.Throws<InvalidOperationException>(() => ServiceSlots.SetFallback<IProbe>(new Probe()));
            Assert.AreSame(first, ServiceSlots.Get<IProbe>());
        }
        [Test] public void RealOwnerWins_AndUnregisterRevealsSameDefault()
        {
            var fallback = ServiceSlots.Get<IAdsService>(); var real = new UnsupportedAdsService();
            ServiceSlots.Register<IAdsService>(real); Assert.AreSame(real, Locator.Ads);
            Assert.IsNotNull(ServiceSlots.OwnerOf<IAdsService>());
            ServiceSlots.Unregister<IAdsService>(real); Assert.AreSame(fallback, Locator.Ads);
        }
        [Test] public void RewardLegacy_CompletesHiddenOnlyOnce()
        {
            int rewards=0, hidden=0; Expect("Reward.Show");
            Locator.Ads.Reward.Show(() => rewards++, () => hidden++);
            Assert.AreEqual(0,rewards); Assert.AreEqual(1,hidden);
            Assert.IsFalse(Locator.Ads.Reward.IsCanShowFor(Placement.NONE));
        }
        [Test] public void RewardRequest_CompletesUnsupportedWithoutReward()
        {
            var id=AdsRequestId.Create("test",AdsRequestKind.Rewarded,Placement.NONE,"1");
            int count=0; AdsShowResult result=default; Expect("ShowRewarded");
            Locator.Ads.ShowRewarded(new AdsShowRequest(id), value => { count++; result=value; });
            Assert.AreEqual(1,count); Assert.AreEqual(AdsRequestOutcome.Unsupported,result.Outcome);
            Assert.IsFalse(result.RewardEarned); Assert.AreEqual(id,result.RequestId);
        }
        [Test] public void InterstitialLegacy_CompletesOnce()
        {
            int count=0; Expect("Inter.Show"); Locator.Ads.Inter.Show(() => count++,Placement.NONE);
            Assert.AreEqual(1,count);
        }
        [Test] public void InterstitialRequest_CompletesUnsupportedOnce()
        {
            int count=0; var id=AdsRequestId.Create("test",AdsRequestKind.Interstitial,Placement.NONE,"1");
            Expect("ShowInterstitial");
            Locator.Ads.ShowInterstitial(new AdsShowRequest(id), value =>
            { count++; Assert.AreEqual(AdsRequestOutcome.Unsupported,value.Outcome); Assert.IsFalse(value.RewardEarned); });
            Assert.AreEqual(1,count);
        }
        [Test] public void LoadActions_AreNonNullAndDoNotExecutePassedLoad()
        {
            bool loaded=false;
            Assert.IsNotNull(Locator.Ads.Reward._OnTriggerLoadAds); Assert.IsNotNull(Locator.Ads.Inter._OnTriggerLoadAds);
            Locator.Ads.Reward._OnTriggerLoadAds(); Locator.Ads.Reward._OnAddLoadAds(() => loaded=true);
            Locator.Ads.Inter._OnTriggerLoadAds(); Locator.Ads.Inter._OnAddLoadAds(() => loaded=true);
            Assert.IsFalse(loaded);
        }
        [Test] public void PurchaseAndRestore_FailOnly()
        {
            int successes=0,failures=0;
            Locator.IAPService.Purchase(default,()=>successes++,()=>failures++);
            Locator.IAPService.Restore(()=>successes++,()=>failures++);
            Assert.AreEqual(0,successes); Assert.AreEqual(2,failures);
        }
        [Test] public void CallbackException_PropagatesWithoutSecondInvocation()
        {
            int count=0; Expect("Reward.Show");
            Assert.Throws<InvalidOperationException>(() => Locator.Ads.Reward.Show(()=>Assert.Fail("reward"),()=>
            { count++; throw new InvalidOperationException("caller"); }));
            Assert.AreEqual(1,count);
        }
        [Test] public void Warning_IsOncePerMemberPerSessionAndResets()
        {
            Expect("Banner.Load"); Locator.Ads.Banner.Load(); Locator.Ads.Banner.Load();
            ServiceSlots.ResetForTests(); Expect("Banner.Load"); Locator.Ads.Banner.Load();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
