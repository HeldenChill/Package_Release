using Hung.Base;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class StableIdGrammarTests
    {
        [TestCase("base.ui.click")]
        [TestCase("a.b")]
        [TestCase("pvm.bgm.home_1")]
        [TestCase("a1.b_2.c3")]
        public void IsValid_AcceptsStrictIds(string raw) => Assert.That(StableIdGrammar.IsValid(raw), Is.True);

        [TestCase(null)]
        [TestCase("")]
        [TestCase("base")]
        [TestCase("Base.ui")]
        [TestCase("base.ui.Click")]
        [TestCase("base..click")]
        [TestCase(".base")]
        [TestCase("base.")]
        [TestCase("1base.x")]
        [TestCase("base.1x")]
        [TestCase("base.ui-click")]
        [TestCase("base.ui click")]
        [TestCase("_base.x")]
        public void IsValid_RejectsNonStrict(string raw) => Assert.That(StableIdGrammar.IsValid(raw), Is.False);

        [Test]
        public void IsValid_EnforcesMaxLength()
        {
            Assert.That(StableIdGrammar.IsValid("a." + new string('b', 118)), Is.True); // 120
            Assert.That(StableIdGrammar.IsValid("a." + new string('b', 119)), Is.False); // 121
        }

        [TestCase("base.ui.click", "base")]
        [TestCase("Gold", "")]
        [TestCase(".x", "")]
        [TestCase(null, "")]
        public void GetGroup_ReturnsFirstSegment(string raw, string expected) => Assert.That(StableIdGrammar.GetGroup(raw), Is.EqualTo(expected));

        [Test]
        public void ItemId_IsStableId() => Assert.That(BaseItemIds.Gold, Is.InstanceOf<IStableId>());
    }
}
