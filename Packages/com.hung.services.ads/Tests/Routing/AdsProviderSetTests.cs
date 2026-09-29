using NUnit.Framework;
using UnityEngine;
using Hung.Ads;

namespace Hung.Ads.Tests.Routing
{
    public class AdsProviderSetTests
    {
        [TearDown] public void TearDown() => AdsInstallers.Clear();

        [Test]
        public void Has_ReflectsFormatSpecificRegistration()
        {
            var set = new AdsProviderSet();
            set.AddRewarded("max", new FakeRewarded("max"));
            Assert.IsTrue(set.Has("max", AdsFormat.Rewarded));
            Assert.IsFalse(set.Has("max", AdsFormat.Interstitial));
            Assert.IsFalse(set.Has("admob", AdsFormat.Rewarded));
            Assert.IsFalse(set.Has(null, AdsFormat.Rewarded));
        }

        [Test]
        public void Add_NullOrEmpty_Ignored()
        {
            var set = new AdsProviderSet();
            set.AddRewarded("", new FakeRewarded("x"));
            set.AddRewarded("max", null);
            CollectionAssert.IsEmpty(set.AllRewarded);
        }

        [Test]
        public void Installers_RegisterSameIdTwice_LastWins()
        {
            var a = new StubInstaller("max");
            var b = new StubInstaller("max");
            AdsInstallers.Register(a);
            AdsInstallers.Register(b);
            Assert.AreEqual(1, AdsInstallers.All.Count);
            Assert.AreSame(b, AdsInstallers.All[0]);
        }

        sealed class StubInstaller : IAdsProviderInstaller
        {
            public StubInstaller(string id) { ProviderId = id; }
            public string ProviderId { get; }
            public void Install(GameObject host, AdsProviderEntry entry, AdsProviderSet set) { }
        }
    }
}
