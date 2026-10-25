using NUnit.Framework;
using Hung.DesignPattern.ScopedPooling;

namespace Hung.DesignPattern.Tests
{
    public class ScopedPoolContractTests
    {
        [Test]
        public void Identity_IsExactAndTyped()
        {
            Assert.AreNotEqual(new UnitPoolId("plant"), new UnitPoolId("Plant"));
            Assert.AreNotEqual(new UnitPoolId("plant"), new UnitPoolId(" plant"));
            Assert.AreNotEqual((object)new UnitPoolId("plant"), new ParticlePoolId("plant"));
            Assert.IsFalse(default(UnitPoolId).IsValid);
            Assert.IsFalse(new ParticlePoolId("  ").IsValid);
        }

        [Test]
        public void DefaultCheckout_IsNotUsable()
        {
            Assert.IsFalse(default(PoolCheckout).IsValid);
            Assert.IsFalse(default(ReturnRequest).IsValid);
        }
    }
}
