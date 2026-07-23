using System.IO;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    [TestFixture]
    internal sealed class EnergyServiceFactoryTests
    {
        private string _tempStatePath;

        [SetUp]
        public void SetUp()
        {
            _tempStatePath = Path.Combine(Path.GetTempPath(), $"energy-factory-test-{System.Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempStatePath))
            {
                File.Delete(_tempStatePath);
            }
        }

        [Test]
        public void CreateLocal_NullConfig_ReturnsFailureWithNoService()
        {
            EnergyCreationResult result = EnergyServiceFactory.CreateLocal(null, _tempStatePath);

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Service);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Test]
        public void CreateLocal_InvalidConfig_ReturnsFailureWithNoService()
        {
            EnergyConfigSO so = ScriptableObject_CreateInvalid();

            EnergyCreationResult result = EnergyServiceFactory.CreateLocal(so, _tempStatePath);

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Service);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Test]
        public void CreateLocal_ValidConfig_ReturnsReadyServiceWithInitialBalances()
        {
            EnergyConfigSO so = ScriptableObject_CreateValid();

            EnergyCreationResult result = EnergyServiceFactory.CreateLocal(so, _tempStatePath);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Service);
            EnergySnapshot snapshot = result.Service.Current;
            Assert.AreEqual(so.initialRenewable, snapshot.RenewableAmount);
            Assert.AreEqual(so.initialBonus, snapshot.BonusAmount);
            Assert.AreEqual(so.renewableMax, snapshot.RenewableMax);
        }

        private static EnergyConfigSO ScriptableObject_CreateInvalid()
        {
            EnergyConfigSO so = UnityEngine.ScriptableObject.CreateInstance<EnergyConfigSO>();
            so.renewableMax = 0; // invalid: must be > 0
            so.regenerationIntervalSeconds = 60f;
            so.runCost = 1;
            so.initialRenewable = 5;
            so.initialBonus = 0;
            so.transactionRetentionCapacity = 10;
            return so;
        }

        private static EnergyConfigSO ScriptableObject_CreateValid()
        {
            EnergyConfigSO so = UnityEngine.ScriptableObject.CreateInstance<EnergyConfigSO>();
            so.renewableMax = 5;
            so.regenerationIntervalSeconds = 60f;
            so.runCost = 1;
            so.initialRenewable = 5;
            so.initialBonus = 2;
            so.transactionRetentionCapacity = 10;
            return so;
        }
    }
}
