using System.Collections.Generic;
using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class GachaRulesTests
    {
        static TrackingHarness New() => new TrackingHarness(_ => new IRule[] { new GachaRule(), new GachaFeelingRule() });

        [Test]
        public void Gacha_TypePerPool()
        {
            var h = New().Launch();
            h.Do(Fact.Gacha("pet", 10, "gem", 900));
            var first = h.Last("ftu_gacha");
            Assert.AreEqual("first", first.Parameters["type"]);
            Assert.AreEqual("gem", first.Parameters["cost_type"]);
            Assert.AreEqual(900L, first.Parameters["cost_amount"]);
            h.Clock.Advance(40.0 / 60);
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            Assert.AreEqual("rush", h.Last("ftu_gacha").Parameters["type"]);
            h.Clock.Advance(10.0 / 60);
            h.Do(Fact.Gacha("item", 1, "free", 0));
            Assert.AreEqual("first", h.Last("ftu_gacha").Parameters["type"]);
            h.Clock.Advance(3);
            h.Do(Fact.Gacha("pet", 1, "ad", 0));
            Assert.AreEqual("normal", h.Last("ftu_gacha").Parameters["type"]);
            h.Clock.Advance(180);
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            Assert.AreEqual("shy", h.Last("ftu_gacha").Parameters["type"]);
        }

        [Test]
        public void Feeling_PositiveOnMonetize()
        {
            var h = New().Launch();
            h.Do(Fact.Gacha("pet", 10, "gem", 900));
            h.Do(Fact.Monetize(MonetizeKind.RewardedAd));
            var e = h.Last("ftu_gacha_feeling");
            Assert.AreEqual("positive", e.Parameters["type"]);
            Assert.AreEqual("pet", e.Parameters["pool"]);
            Assert.AreEqual(10, e.Parameters["count"]);
        }

        [Test]
        public void Feeling_NormalOnStageStart_PetUpgrade_OrNextGacha()
        {
            var h = New().Launch();
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            h.Do(Fact.StageStart(2, false));
            Assert.AreEqual("normal", h.Last("ftu_gacha_feeling").Parameters["type"]);
            h.Do(Fact.StageEnd(StageResult.Win));

            h.Clear();
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            h.Do(Fact.Feature("pet_upgrade", null));
            Assert.AreEqual("normal", h.Last("ftu_gacha_feeling").Parameters["type"]);

            h.Clear();
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            h.Do(Fact.Gacha("item", 1, "gem", 100));
            Assert.AreEqual(1, h.Names.FindAll(n => n == "ftu_gacha_feeling").Count);
        }

        [Test]
        public void Feeling_NegativeAfterLongBackground_ShortBackgroundKeepsPending()
        {
            var h = New().Launch();
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            h.Background(10);
            Assert.IsFalse(h.Names.Contains("ftu_gacha_feeling"));
            h.Background(45);
            Assert.AreEqual("negative", h.Last("ftu_gacha_feeling").Parameters["type"]);
        }

        [Test]
        public void Feeling_KilledInFtu_ResolvedNextSession_KeepsFtuPrefix()
        {
            var h = New().Launch();
            h.Do(Fact.Gacha("pet", 1, "gem", 100));
            h.Kill(120);
            Assert.AreEqual("negative", h.Last("ftu_gacha_feeling").Parameters["type"]);
            Assert.IsFalse(h.Names.Contains("gacha_feeling"));
        }
    }
}
