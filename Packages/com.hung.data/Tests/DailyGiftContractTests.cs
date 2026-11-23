using System.Linq;
using Hung.Data.LiveOps;
using NUnit.Framework;

namespace Hung.Data.Tests
{
    public class DailyGiftContractTests
    {
        [Test]
        public void ContractAssembly_HasNoLegacyPvmTypeIdentities()
        {
            var assembly = typeof(IDailyGiftConfig).Assembly;

            Assert.That(assembly.GetType("DailyGiftDataSO"), Is.Null);
            Assert.That(assembly.GetType("DailyGiftItem"), Is.Null);
            Assert.That(assembly.GetType("Utilities.Core.Data.ICharacterDefinition"), Is.Null);
            Assert.That(assembly.GetType("Utilities.Core.Data.ICharacterStats"), Is.Null);
            Assert.That(assembly.GetType("Utilities.Core.Character.PerceptionData"), Is.Null);
        }

        [Test]
        public void ContractAssembly_HasNoPvmOrSpineAssemblyReference()
        {
            // The contracts live in Hung.Data, which uses Odin on purpose (GameplayData, PoolData are
            // SerializedScriptableObject), so Odin is not checked here. The rule that matters for adoption
            // is no game assembly and no Spine.
            string[] references = typeof(IDailyGiftConfig).Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ToArray();

            Assert.That(references, Has.None.Contains("PetVsMonster.Data"));
            Assert.That(references, Has.None.Contains("spine-csharp"));
        }
    }
}
