using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class ProductionDefinitionTests
    {
        [Test]
        public void ProductionDefinitions_RegisterStableNonTypeNameKeys()
        {
            var definitions = PackageSaveDefinitions.CreateAll(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());

            Assert.That(definitions.Count, Is.EqualTo(7));
            Assert.That(definitions.Select(x => x.Key), Is.EquivalentTo(new[] { "game-data", "daily-gift", "heart", "daily-reward", "piggy-bank", "spin-wheel", "reward-integrity" }));
            Assert.That(definitions.Select(x => x.Key).Distinct().Count(), Is.EqualTo(7));
            Assert.That(definitions.All(x => x.CurrentSchemaVersion == 1), Is.True);
            Assert.That(definitions.Any(x => x.Key == nameof(GameData)), Is.False);
            Assert.That(definitions.OfType<SaveDefinition<GameData>>().Single().FailurePolicy, Is.EqualTo(SaveFailurePolicy.FailClosed));
        }

        [Test]
        public void ProductionDefinitions_RegisterIntoServiceAndResolveLegacyRequests()
        {
            var service = new PersistenceService(new InMemorySaveStore());
            PackageSaveDefinitions.RegisterAll(service, PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());

            Assert.That(service.TryGetDefinition<GameData>(GameData.SaveKey, out var gameData), Is.True);
            Assert.That(gameData.Key, Is.EqualTo("game-data"));
            Assert.That(service.TryGetDefinition<HeartSave.HeartSaveData>("HeartSaveData", out var heart), Is.True);
            Assert.That(heart.Key, Is.EqualTo("heart"));
        }
    }
}
