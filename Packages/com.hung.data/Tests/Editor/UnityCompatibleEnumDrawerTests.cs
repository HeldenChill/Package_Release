using System;
using NUnit.Framework;

namespace Hung.Data.Editor.Tests
{
    public sealed class UnityCompatibleEnumDrawerTests
    {
        private enum Plain
        {
            None,
            First,
            Second
        }

        [Flags]
        private enum Flags
        {
            None = 0,
            First = 1,
            Second = 2
        }

        [Test]
        public void IsFlagsEnum_ReturnsFalseForOrdinaryEnum()
        {
            Assert.That(UnityCompatibleEnumDrawer<Plain>.IsFlagsEnum, Is.False);
        }

        [Test]
        public void IsFlagsEnum_ReturnsTrueForFlagsEnum()
        {
            Assert.That(UnityCompatibleEnumDrawer<Flags>.IsFlagsEnum, Is.True);
        }

        [Test]
        public void CastRoundTrip_PreservesCombinedFlags()
        {
            Flags expected = Flags.First | Flags.Second;
            Enum boxed = UnityCompatibleEnumDrawer<Flags>.Box(expected);

            Assert.That(UnityCompatibleEnumDrawer<Flags>.Unbox(boxed), Is.EqualTo(expected));
        }
    }
}
