using System;
using Hung.Base;
using Hung.Base.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class SaveDefinitionTests
    {
        [TestCase("")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("../game-data")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        public void Definition_RejectsUnsafeKeys(string key)
        {
            Assert.Throws<ArgumentException>(() => Definition(key));
        }

        [Test]
        public void Definition_RejectsInvalidInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Definition(schema: 0));
            Assert.Throws<ArgumentNullException>(() => new SaveDefinition<GameData>("game-data", 1, null, _ => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), Array.Empty<string>(), PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector(), SaveFailurePolicy.FailClosed));
            Assert.Throws<ArgumentNullException>(() => new SaveDefinition<GameData>("game-data", 1, () => new GameData(), null, Array.Empty<ISaveMigration>(), Array.Empty<string>(), PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector(), SaveFailurePolicy.FailClosed));
            Assert.Throws<ArgumentNullException>(() => new SaveDefinition<GameData>("game-data", 1, () => new GameData(), _ => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), Array.Empty<string>(), null, PersistenceTestDoubles.Protector(), SaveFailurePolicy.FailClosed));
            Assert.Throws<ArgumentNullException>(() => new SaveDefinition<GameData>("game-data", 1, () => new GameData(), _ => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), Array.Empty<string>(), PersistenceTestDoubles.Codec(), null, SaveFailurePolicy.FailClosed));
            Assert.Throws<ArgumentException>(() => new SaveDefinition<GameData>("game-data", 1, () => new GameData(), _ => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), new[] { "GameData", "GameData" }, PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector(), SaveFailurePolicy.FailClosed));
        }

        [Test]
        public void ValidDefinition_ExposesImmutableValues()
        {
            SaveDefinition<GameData> definition = Definition();

            Assert.That(definition.Key, Is.EqualTo("game-data"));
            Assert.That(definition.CurrentSchemaVersion, Is.EqualTo(1));
            Assert.That(definition.LegacyPlayerPrefsKeys, Is.EquivalentTo(new[] { GameData.SaveKey, nameof(GameData) }));
            Assert.That(definition.FailurePolicy, Is.EqualTo(SaveFailurePolicy.FailClosed));
        }

        private static SaveDefinition<GameData> Definition(string key = "game-data", int schema = 1)
        {
            return new SaveDefinition<GameData>(
                key,
                schema,
                () => new GameData(),
                _ => SaveValidationResult.Valid(),
                Array.Empty<ISaveMigration>(),
                new[] { GameData.SaveKey, nameof(GameData) },
                PersistenceTestDoubles.Codec(),
                PersistenceTestDoubles.Protector(),
                SaveFailurePolicy.FailClosed);
        }
    }
}
