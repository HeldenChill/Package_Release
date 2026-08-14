using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests.Persistence
{
    public class LegacyImportTests
    {
        [Test]
        public void Service_ImportsLegacyGameDataWithoutDeletingPlayerPrefsText()
        {
            string json = Resources.Load<TextAsset>("Persistence/Fixtures/GameData.item-id-v1.raw").text;
            var legacy = new DictionaryLegacySaveSource();
            legacy.Set(GameData.SaveKey, json);
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store, legacy);
            SaveDefinition<GameData> definition = PackageSaveDefinitions.GameData(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());

            LoadResult<GameData> result = service.Load(definition);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Source, Is.EqualTo(SaveDataSource.LegacyPlayerPrefs));
            Assert.That(result.Recovery, Is.EqualTo(SaveRecoveryState.Migrated));
            Assert.That(store.Primary.ContainsKey(PackageSaveDefinitions.GameDataKey), Is.True);
            Assert.That(legacy.TryRead(GameData.SaveKey, out string preserved), Is.True);
            Assert.That(preserved, Is.EqualTo(json));
            Assert.That(result.Value.GetItemData(ItemId.Parse("pet_vs_monster.gem")).Quantity, Is.EqualTo(45));
        }

        [Test]
        public void Service_PrimaryWinsOverLegacy()
        {
            string json = Resources.Load<TextAsset>("Persistence/Fixtures/GameData.item-id-v1.raw").text;
            var legacy = new DictionaryLegacySaveSource();
            legacy.Set(GameData.SaveKey, json);
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store, legacy);
            SaveDefinition<GameData> definition = PackageSaveDefinitions.GameData(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());
            var canonical = definition.CreateDefault();
            canonical.IsFirstTimeUser = true;
            service.Save(definition, canonical);

            LoadResult<GameData> result = service.Load(definition);

            Assert.That(result.Source, Is.EqualTo(SaveDataSource.Primary));
            Assert.That(result.Value.IsFirstTimeUser, Is.True);
        }
    }
}
