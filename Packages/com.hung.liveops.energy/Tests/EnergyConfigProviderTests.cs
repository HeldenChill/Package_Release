using NUnit.Framework;
using UnityEngine;

namespace Hung.LiveOps.Energy.Tests
{
    internal sealed class EnergyConfigProviderTests
    {
        private static EnergyConfigSO CreateSO(
            int renewableMax = 100,
            float regenerationIntervalSeconds = 60,
            int runCost = 10,
            int initialRenewable = 100,
            int initialBonus = 0,
            int transactionRetentionCapacity = 20)
        {
            EnergyConfigSO so = ScriptableObject.CreateInstance<EnergyConfigSO>();
            so.renewableMax = renewableMax;
            so.regenerationIntervalSeconds = regenerationIntervalSeconds;
            so.runCost = runCost;
            so.initialRenewable = initialRenewable;
            so.initialBonus = initialBonus;
            so.transactionRetentionCapacity = transactionRetentionCapacity;
            return so;
        }

        [Test]
        public void GetConfig_ValidSO_ReturnsMatchingConfigAndNonEmptyVersion()
        {
            EnergyConfigSO so = CreateSO(renewableMax: 150, regenerationIntervalSeconds: 30, runCost: 5, initialRenewable: 120, initialBonus: 3, transactionRetentionCapacity: 50);
            IEnergyConfigProvider provider = new LocalEnergyConfigProvider(so);

            EnergyConfigResult result = provider.GetConfig();

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Config);
            Assert.AreEqual(150, result.Config.RenewableMax);
            Assert.AreEqual(30d, result.Config.RegenerationInterval.TotalSeconds);
            Assert.AreEqual(5, result.Config.RunCost);
            Assert.AreEqual(120, result.Config.InitialRenewable);
            Assert.AreEqual(3, result.Config.InitialBonus);
            Assert.AreEqual(50, result.Config.TransactionRetentionCapacity);
            Assert.IsFalse(string.IsNullOrEmpty(result.Version));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetConfig_DifferentRenewableMax_ProducesDifferentVersionStrings()
        {
            EnergyConfigSO soA = CreateSO(renewableMax: 100);
            EnergyConfigSO soB = CreateSO(renewableMax: 50);

            EnergyConfigResult resultA = new LocalEnergyConfigProvider(soA).GetConfig();
            EnergyConfigResult resultB = new LocalEnergyConfigProvider(soB).GetConfig();

            Assert.IsTrue(resultA.Success);
            Assert.IsTrue(resultB.Success);
            Assert.AreNotEqual(resultA.Version, resultB.Version);

            Object.DestroyImmediate(soA);
            Object.DestroyImmediate(soB);
        }

        [Test]
        public void GetConfig_NullSO_ReturnsFailureWithoutThrowing()
        {
            IEnergyConfigProvider provider = new LocalEnergyConfigProvider(null);

            EnergyConfigResult result = default;
            Assert.DoesNotThrow(() => result = provider.GetConfig());

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Config);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Test]
        public void GetConfig_InvalidSOFields_ReturnsFailureWithoutThrowing()
        {
            EnergyConfigSO so = CreateSO(renewableMax: 0);
            IEnergyConfigProvider provider = new LocalEnergyConfigProvider(so);

            EnergyConfigResult result = default;
            Assert.DoesNotThrow(() => result = provider.GetConfig());

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Config);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void FixedEnergyConfigProvider_ReturnsSuppliedConfigAndVersion()
        {
            EnergyConfig config = new EnergyConfig(100, System.TimeSpan.FromSeconds(60), 10, 100, 0, 20);
            IEnergyConfigProvider provider = new FixedEnergyConfigProvider(config, "fixed-v1");

            EnergyConfigResult result = provider.GetConfig();

            Assert.IsTrue(result.Success);
            Assert.AreSame(config, result.Config);
            Assert.AreEqual("fixed-v1", result.Version);
        }
    }
}
