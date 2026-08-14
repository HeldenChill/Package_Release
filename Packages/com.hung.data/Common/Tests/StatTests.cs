using NUnit.Framework;
using SStats;

namespace Hung.Data.Tests
{
    public class StatTests
    {
        [Test]
        public void BaseValue_NoModifiers_ReturnsBaseValue()
        {
            var stat = new Stat(10f);

            Assert.AreEqual(10f, stat.Value);
        }

        [Test]
        public void FlatModifier_Adds()
        {
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(5f, StatModType.Flat));

            Assert.AreEqual(15f, stat.Value);
        }

        [Test]
        public void PercentAddModifier_MultipliesAdditively()
        {
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(0.5f, StatModType.PercentAdd));

            Assert.AreEqual(15f, stat.Value);
        }

        [Test]
        public void PercentMultModifier_MultipliesCompounding()
        {
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(0.5f, StatModType.PercentMult));

            Assert.AreEqual(15f, stat.Value);
        }

        [Test]
        public void RemoveModifier_SameInstance_Reverts()
        {
            var stat = new Stat(10f);
            var mod = new StatModifier(5f, StatModType.Flat);
            stat.AddModifier(mod);
            Assert.AreEqual(15f, stat.Value);

            bool removed = stat.RemoveModifier(mod);

            Assert.IsTrue(removed);
            Assert.AreEqual(10f, stat.Value);
        }

        [Test]
        public void RemoveModifier_DifferentInstanceWithEqualValues_NotRemoved()
        {
            // StatModifier has no Equals override, so List.Remove uses reference equality -
            // characterizing that a value-equal-but-distinct instance is NOT removed.
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(5f, StatModType.Flat));

            bool removed = stat.RemoveModifier(new StatModifier(5f, StatModType.Flat));

            Assert.IsFalse(removed);
            Assert.AreEqual(15f, stat.Value);
        }

        [Test]
        public void ModifierOrder_DefaultOrder_FlatAppliesBeforePercent()
        {
            // Default constructor sets Order = (int)Type: Flat=100 < PercentAdd=200 < PercentMult=300,
            // so with default orders the sort naturally applies Flat, then PercentAdd, then PercentMult -
            // verified by reading CalculateFinalValue's Order-based sort, not the Type itself.
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(0.5f, StatModType.PercentMult)); // added out of natural order
            stat.AddModifier(new StatModifier(5f, StatModType.Flat));

            // Sorted by Order: Flat(100) first -> (10+5)=15, then PercentMult(300) -> 15*1.5=22.5
            Assert.AreEqual(22.5f, stat.Value);
        }

        [Test]
        public void ModifierOrder_ExplicitOrderOverridesTypeDefault()
        {
            // Order, not Type, is the actual sort key - a PercentMult with a custom lower Order
            // runs before a Flat modifier with a higher Order.
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(5f, StatModType.Flat, order: 999));
            stat.AddModifier(new StatModifier(1f, StatModType.PercentMult, order: 1));

            // Sorted by Order: PercentMult(1) first -> 10*2=20, then Flat(999) -> 20+5=25
            Assert.AreEqual(25f, stat.Value);
        }

        [Test]
        public void Reset_ClearsModifiersAndValueMatchesBase_WhenReadImmediately()
        {
            // Reset() sets _value = BaseValue directly and clears modifiers, but does NOT set
            // isDirty = true. Since isDirty is already false after the Value getter ran once
            // (below) and BaseValue is unchanged, the cached _value (now == BaseValue) is what
            // Value returns - this happens to read correctly, but only because Reset()
            // coincidentally wrote the correct cached value itself, not because it invalidated
            // the cache. Characterizing actual behavior per Ph5 rule.
            var stat = new Stat(10f);
            stat.AddModifier(new StatModifier(5f, StatModType.Flat));
            Assert.AreEqual(15f, stat.Value); // forces isDirty = false, _value cached at 15

            stat.Reset();

            Assert.AreEqual(10f, stat.Value);
        }
    }
}
