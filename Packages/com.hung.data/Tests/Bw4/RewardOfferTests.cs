using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hung.Base;
using Hung.Data.Rewards;
using NUnit.Framework;
namespace Hung.Data.Tests.Bw4
{
    public sealed class RewardOfferTests
    {
        private static RewardOfferDefinition Def(params RewardLine[] lines) => new RewardOfferDefinition { key = "offer", revision = "r1", items = lines.ToList() };
        [Test] public void MergeSortAndDetach()
        {
            var def = Def(new RewardLine("fixture.z", 2), new RewardLine("fixture.a", 1), new RewardLine("fixture.z", 3));
            var offer = def.Resolve(_ => true); def.items[0].quantity = 99; def.items.Clear();
            Assert.That(offer.Items.Select(x => x.ItemId.Value), Is.EqualTo(new[] { "fixture.a", "fixture.z" }));
            Assert.That(offer.Items.Select(x => x.Quantity), Is.EqualTo(new[] { 1, 5 }));
            Assert.Throws<NotSupportedException>(() => ((IList<RewardGrantItem>)offer.Items)[0] = default);
        }
        [Test] public void EmptyRejected() { Assert.Throws<ArgumentException>(() => Def().Resolve(_ => true)); }
        [Test] public void InvalidIdRejected() { Assert.Throws<ArgumentException>(() => Def(new RewardLine("", 1)).Resolve(_ => true)); }
        [Test] public void ZeroRejected() { Assert.Throws<ArgumentOutOfRangeException>(() => Def(new RewardLine("fixture.a", 0)).Resolve(_ => true)); }
        [Test] public void NegativeRejected() { Assert.Throws<ArgumentOutOfRangeException>(() => Def(new RewardLine("fixture.a", -1)).Resolve(_ => true)); }
        [Test] public void UnknownRejected() { Assert.Throws<ArgumentException>(() => Def(new RewardLine("fixture.a", 1)).Resolve(_ => false)); }
        [Test] public void OverflowRejected() { Assert.Throws<OverflowException>(() => Def(new RewardLine("fixture.a", int.MaxValue), new RewardLine("fixture.a", 1)).Resolve(_ => true)); }
        [Test] public void DefaultPayloadRejected() { Assert.Throws<ArgumentException>(() => RewardPayload.Canonical(new[] { default(RewardGrantItem) })); }
        [Test] public void FingerprintStableAcrossCultureAndOrder()
        {
            var old = CultureInfo.CurrentCulture;
            try
            {
                var a = Def(new RewardLine("fixture.z", 4), new RewardLine("fixture.a", 2)).Resolve(_ => true);
                var b = Def(new RewardLine("fixture.a", 2), new RewardLine("fixture.z", 4)).Resolve(_ => true);
                string expected = RewardFingerprintV1.Create("p", "f", "o", a);
                Assert.That(expected, Is.EqualTo("bw4:v1:99060cc4d051a25ef96bbb598e2427e60bcaf7e16b05b093f88d6412fe83e4a5"));
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                Assert.That(RewardFingerprintV1.Create("p", "f", "o", b), Is.EqualTo(expected));
                Assert.That(expected, Does.StartWith("bw4:v1:"));
                Assert.That(RewardFingerprintV1.Create("other", "f", "o", b), Is.Not.EqualTo(expected));
            }
            finally { CultureInfo.CurrentCulture = old; }
        }
        [Test] public void LegacyFingerprintBytesUnchanged()
        { Assert.That(RewardFingerprint.NormalizeItems(new[] { new RewardGrantItem(ItemId.Parse("fixture.z"), 3), new RewardGrantItem(ItemId.Parse("fixture.a"), 2) }), Is.EqualTo("fixture.a:2|fixture.z:3")); }
        [Test] public void TranslationRequiresExplicitHostEntry()
        {
            var map = new LegacyRewardTranslation(); var id = ItemId.Parse("base.all_booster");
            Assert.Throws<ArgumentException>(() => map.Resolve(id, 1));
            var offer = Bw4Fixture.Offer(); map.Add(id, 1, offer); Assert.That(map.Resolve(id, 1), Is.SameAs(offer));
        }
        [Test] public void TwoProductsNeedNoSharedConstants()
        {
            var a = Def(new RewardLine("forest.seed", 3)).Resolve(x => x.Value.StartsWith("forest.", StringComparison.Ordinal));
            var b = Def(new RewardLine("factory.gear", 7)).Resolve(x => x.Value.StartsWith("factory.", StringComparison.Ordinal));
            Assert.That(a.Items[0].ItemId.Value, Is.EqualTo("forest.seed")); Assert.That(b.Items[0].Quantity, Is.EqualTo(7));
        }
    }
}
