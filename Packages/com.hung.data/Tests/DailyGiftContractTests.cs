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
        public void ContractAssembly_HasNoPvmOrOdinAssemblyReference()
        {
            string[] references = typeof(IDailyGiftConfig).Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ToArray();

            Assert.That(references, Has.None.Contains("PetVsMonster.Data"));
            Assert.That(references, Has.None.Contains("Sirenix.OdinInspector.Attributes"));
            Assert.That(references, Has.None.Contains("spine-csharp"));
        }
    }
}
