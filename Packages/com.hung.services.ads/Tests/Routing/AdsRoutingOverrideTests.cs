using System.Collections.Generic;
using NUnit.Framework;
using Hung.Ads;
using Hung.Base;

namespace Hung.Ads.Tests.Routing
{
    public class AdsRoutingOverrideTests
    {
        static AdsRoutingData Baseline() => new AdsRoutingData
        {
            providers = new List<AdsProviderEntry>
            {
                new AdsProviderEntry { id = "max", enabled = true },
                new AdsProviderEntry { id = "admob", enabled = true },
            },
            formats = new List<AdsFormatRoute>
            {
                new AdsFormatRoute { format = AdsFormat.Rewarded, mode = AdsRouteMode.Chain, order = new List<string> { "max", "admob" } },
            },
            placements = new List<AdsPlacementRoute>(),
        };

        [Test]
        public void EmptyObject_ReturnsEqualCopy()
        {
            var baseline = Baseline();
            Assert.IsTrue(AdsRoutingOverride.TryApply(baseline, "{}", out var merged, out var error), error);
            Assert.AreNotSame(baseline, merged);
            CollectionAssert.AreEqual(new[] { "max", "admob" }, merged.formats[0].order);
        }

        [Test]
        public void ProviderEnabledFalse_DisablesOnlyThatProvider()
        {
            var baseline = Baseline();
            Assert.IsTrue(AdsRoutingOverride.TryApply(baseline, "{\"providers\":[{\"id\":\"admob\",\"enabled\":false}]}", out var merged, out _));
            Assert.IsFalse(merged.FindProvider("admob").enabled);
            Assert.IsTrue(merged.FindProvider("max").enabled);
            Assert.IsTrue(baseline.FindProvider("admob").enabled, "baseline must not be mutated");
        }

        [Test]
        public void FormatRoute_ReplacedByName_CaseInsensitive()
        {
            const string json = "{\"formats\":[{\"format\":\"rewarded\",\"mode\":\"parallel\",\"order\":[\"admob\"]}]}";
            Assert.IsTrue(AdsRoutingOverride.TryApply(Baseline(), json, out var merged, out _));
            Assert.AreEqual(AdsRouteMode.Parallel, merged.formats[0].mode);
            CollectionAssert.AreEqual(new[] { "admob" }, merged.formats[0].order);
        }

        [Test]
        public void PlacementRoute_Added()
        {
            const string json = "{\"placements\":[{\"format\":\"Rewarded\",\"placement\":\"REROLL_SKILL_CARD\",\"mode\":\"Chain\",\"order\":[\"admob\"]}]}";
            Assert.IsTrue(AdsRoutingOverride.TryApply(Baseline(), json, out var merged, out _));
            Assert.AreEqual(1, merged.placements.Count);
            Assert.AreEqual(Placement.REROLL_SKILL_CARD, merged.placements[0].placement);
        }

        [TestCase("not json")]
        [TestCase("{\"formats\":[{\"format\":\"Video\",\"mode\":\"Chain\",\"order\":[]}]}")]
        [TestCase("{\"formats\":[{\"format\":\"Rewarded\",\"mode\":\"Fastest\",\"order\":[]}]}")]
        [TestCase("{\"placements\":[{\"format\":\"Rewarded\",\"placement\":\"NOPE\",\"mode\":\"Chain\",\"order\":[]}]}")]
        public void Invalid_Rejected_WithError(string json)
        {
            Assert.IsFalse(AdsRoutingOverride.TryApply(Baseline(), json, out var merged, out var error));
            Assert.IsNotEmpty(error);
            Assert.IsNull(merged);
        }

        [Test]
        public void ProviderWithoutEnabledField_DefaultsToEnabled()
        {
            var baseline = Baseline();
            baseline.providers[1].enabled = false;
            Assert.IsTrue(AdsRoutingOverride.TryApply(baseline, "{\"providers\":[{\"id\":\"admob\"}]}", out var merged, out _));
            Assert.IsTrue(merged.FindProvider("admob").enabled);
        }
    }
}
