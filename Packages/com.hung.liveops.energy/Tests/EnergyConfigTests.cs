using System;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    internal sealed class EnergyConfigTests
    {
        private static EnergyConfig BuildValid(
            int renewableMax = 100,
            double regenerationIntervalSeconds = 60,
            int runCost = 10,
            int initialRenewable = 100,
            int initialBonus = 0,
            int transactionRetentionCapacity = 20)
        {
            return new EnergyConfig(
                renewableMax,
                TimeSpan.FromSeconds(regenerationIntervalSeconds),
                runCost,
                initialRenewable,
                initialBonus,
                transactionRetentionCapacity);
        }

        [Test]
        public void Constructor_ValidValues_Succeeds()
        {
            Assert.DoesNotThrow(() => BuildValid());
        }

        [Test]
        public void Constructor_RenewableMaxZero_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(renewableMax: 0));
            StringAssert.Contains("renewableMax", ex.Message);
        }

        [Test]
        public void Constructor_RenewableMaxNegative_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuildValid(renewableMax: -1));
        }

        [Test]
        public void Constructor_RegenerationIntervalZero_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(regenerationIntervalSeconds: 0));
            StringAssert.Contains("regenerationInterval", ex.Message);
        }

        [Test]
        public void Constructor_RegenerationIntervalNegative_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuildValid(regenerationIntervalSeconds: -5));
        }

        [Test]
        public void Constructor_RunCostNegative_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(runCost: -1));
            StringAssert.Contains("runCost", ex.Message);
        }

        [Test]
        public void Constructor_RunCostZero_Succeeds()
        {
            Assert.DoesNotThrow(() => BuildValid(runCost: 0));
        }

        [Test]
        public void Constructor_InitialRenewableNegative_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(initialRenewable: -1));
            StringAssert.Contains("initialRenewable", ex.Message);
        }

        [Test]
        public void Constructor_InitialBonusNegative_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(initialBonus: -1));
            StringAssert.Contains("initialBonus", ex.Message);
        }

        [Test]
        public void Constructor_TransactionRetentionCapacityZero_ThrowsActionableMessage()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BuildValid(transactionRetentionCapacity: 0));
            StringAssert.Contains("transactionRetentionCapacity", ex.Message);
        }

        [Test]
        public void Constructor_TransactionRetentionCapacityNegative_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuildValid(transactionRetentionCapacity: -1));
        }

        [Test]
        public void ComputeVersion_SameValues_ProducesSameVersion()
        {
            EnergyConfig a = BuildValid();
            EnergyConfig b = BuildValid();

            Assert.AreEqual(a.ComputeVersion(), b.ComputeVersion());
        }

        [Test]
        public void ComputeVersion_DifferentRenewableMax_ProducesDifferentVersion()
        {
            EnergyConfig a = BuildValid(renewableMax: 100);
            EnergyConfig b = BuildValid(renewableMax: 50);

            Assert.AreNotEqual(a.ComputeVersion(), b.ComputeVersion());
        }
    }
}
