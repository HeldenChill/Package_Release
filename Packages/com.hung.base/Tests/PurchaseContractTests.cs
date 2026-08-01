using System;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class PurchaseContractTests
    {
        [TestCase("starter-pack")]
        [TestCase("gold.pack_1")]
        public void PurchaseProductId_AcceptsCanonicalValues(string value)
        {
            Assert.That(new PurchaseProductId(value).Value, Is.EqualTo(value));
        }

        [TestCase("")]
        [TestCase("Starter-Pack")]
        [TestCase("gold pack")]
        [TestCase("gold/pack")]
        [TestCase("gold.pack!")]
        public void PurchaseProductId_RejectsInvalidValues(string value)
        {
            Assert.Throws<ArgumentException>(() => new PurchaseProductId(value));
        }

        [Test]
        public void PurchaseProductId_UsesOrdinalEquality()
        {
            Assert.That(new PurchaseProductId("starter-pack"), Is.EqualTo(new PurchaseProductId("starter-pack")));
            Assert.That(new PurchaseProductId("starter-pack"), Is.Not.EqualTo(new PurchaseProductId("starter_pack")));
        }

        [Test]
        public void PurchaseProductId_RejectsOver120Characters()
        {
            string value = new string('a', 121);

            Assert.Throws<ArgumentException>(() => new PurchaseProductId(value));
        }

        [Test]
        public void UnsupportedIapService_NeverReportsSuccess()
        {
            bool success = false;
            bool failure = false;

            new UnsupportedIapService().Purchase(IAP_ITEM.STARTER_PACK, () => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }

        [Test]
        public void UnsupportedIapService_RestoreReportsFailure()
        {
            bool success = false;
            bool failure = false;

            new UnsupportedIapService().Restore(() => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }
    }
}
