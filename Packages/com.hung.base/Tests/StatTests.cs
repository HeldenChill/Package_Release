using System;
using System.Reflection;
using NUnit.Framework;
using SStats;

namespace Hung.Base.Tests
{
    public class StatTests
    {
        private sealed class CountingStat : Stat
        {
            public int CalculationCount;

            public CountingStat(float baseValue) : base(baseValue) { }

            protected override float CalculateFinalValue()
            {
                CalculationCount++;
                return base.CalculateFinalValue();
            }
        }

        [Test]
        public void Reset_MarksStatDirty_AndRecalculatesOnNextValue()
        {
            var stat = new CountingStat(10f);

            Assert.AreEqual(10f, stat.Value);
            Assert.AreEqual(1, stat.CalculationCount);

            stat.AddModifier(new StatModifier(5f, StatModType.Flat));
            Assert.AreEqual(15f, stat.Value);
            Assert.AreEqual(2, stat.CalculationCount);

            stat.Reset();

            Assert.AreEqual(10f, stat.Value);
            Assert.AreEqual(3, stat.CalculationCount);
        }

        [Test]
        public void GetModifiersExceptMergeFromSource_ExposesExpectedGenericApi()
        {
            MethodInfo method = typeof(Stat).GetMethod(
                "GetModifiersExceptMergeFromSource",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method);
        }
    }
}
