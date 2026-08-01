using Hung.Base;
using Hung.Data.Tests.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    public class ItemSaveTests
    {
        private DatabaseFacadeTestScope scope;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(nameof(GameData));
            PlayerPrefs.DeleteKey(GameData.SaveKey);
            scope = new DatabaseFacadeTestScope();
        }

        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
            PlayerPrefs.DeleteKey(nameof(GameData));
            PlayerPrefs.DeleteKey(GameData.SaveKey);
        }

        [Test]
        public void GameItem_RoundTripsByStableId()
        {
            ItemId gem = ItemId.Parse("pet_vs_monster.gem");
            var data = new GameData();

            Assert.IsTrue(data.InitData(new[] { BaseItemIds.Gold, gem }));
            data.ClaimItem(gem, 9);
            Database.Save(data, GameData.SaveKey);

            GameData loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.AreEqual(9, loaded.GetItemData(gem).Quantity);
        }

        [Test]
        public void InitData_AddsMissingCatalogIdsWithoutRemovingUnknownSavedIds()
        {
            ItemId gem = ItemId.Parse("pet_vs_monster.gem");
            ItemId retired = ItemId.Parse("pet_vs_monster.retired");
            var data = new GameData();
            data.InitData(new[] { BaseItemIds.Gold, retired });
            data.ClaimItem(retired, 4);

            Assert.IsFalse(data.InitData(new[] { BaseItemIds.Gold, gem }));

            Assert.AreEqual(0, data.GetItemData(gem).Quantity);
            Assert.AreEqual(4, data.GetItemData(retired).Quantity);
        }

        [Test]
        public void InitData_SecondRunIsIdempotent()
        {
            var data = new GameData();

            Assert.IsTrue(data.InitData(new[] { BaseItemIds.Gold, BaseItemIds.Heart }));
            Assert.IsFalse(data.InitData(new[] { BaseItemIds.Gold, BaseItemIds.Heart }));
        }

        [Test]
        public void RegisteredGameData_LegacyKeyResolvesCanonicalDefinition()
        {
            var oldData = new GameData();
            oldData.InitData(new[] { BaseItemIds.Gold });
            oldData.ClaimItem(BaseItemIds.Gold, 99);
            Database.Save(oldData, nameof(GameData));

            GameData loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.AreEqual(99, loaded.GetItemData(BaseItemIds.Gold).Quantity);
            Assert.That(scope.Store.Primary.ContainsKey(Hung.Data.Persistence.PackageSaveDefinitions.GameDataKey), Is.True);
        }

        [Test]
        public void GameData_PlayerPrefsLegacyImport_RetainsOriginalValue()
        {
            scope.Dispose();
            var legacyData = new GameData();
            legacyData.InitData(new[] { BaseItemIds.Gold });
            legacyData.ClaimItem(BaseItemIds.Gold, 27);
            string legacyJson = JsonConvert.SerializeObject(legacyData);
            PlayerPrefs.SetString(GameData.SaveKey, legacyJson);
            scope = new DatabaseFacadeTestScope(new Hung.Data.Persistence.PlayerPrefsLegacySaveSource());

            GameData loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.That(loaded.GetItemData(BaseItemIds.Gold).Quantity, Is.EqualTo(27));
            Assert.That(PlayerPrefs.GetString(GameData.SaveKey), Is.EqualTo(legacyJson));
            Assert.That(scope.Store.Primary.ContainsKey(Hung.Data.Persistence.PackageSaveDefinitions.GameDataKey), Is.True);
        }
    }
}
