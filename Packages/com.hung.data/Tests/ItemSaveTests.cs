using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    public class ItemSaveTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(nameof(GameData));
            PlayerPrefs.DeleteKey(GameData.SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
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
        public void ExplicitSaveKey_DoesNotReadOldGameDataKey()
        {
            var oldData = new GameData();
            oldData.InitData(new[] { BaseItemIds.Gold });
            oldData.ClaimItem(BaseItemIds.Gold, 99);
            Database.Save(oldData, nameof(GameData));

            GameData loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.IsTrue(loaded.IsFirstTimeUser);
            loaded.InitData(new[] { BaseItemIds.Gold });
            Assert.AreEqual(0, loaded.GetItemData(BaseItemIds.Gold).Quantity);
        }
    }
}
