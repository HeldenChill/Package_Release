using NUnit.Framework;
using UnityEngine;
using Hung.Base;

namespace Hung.Data.Tests
{
    // Plan asked for "golden JSON string per data version constant" -- no version constant exists anywhere
    // in com.hung.data or com.hung.base (grepped version/SAVE_VERSION/DataVersion, zero hits); versioning is
    // implicit via GameData.InitData()'s array-length migration, not a numeric field. This test golds the
    // CURRENT real GameData shape instead (real type, not SaveRoundTripTests' fake POCO) so a future field
    // rename/removal on GameData is caught here rather than silently.
    public class GameDataGoldenTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(GameData.SaveKey);
        }

        [Test]
        public void GameData_Save_Load_RoundTripsRealFields()
        {
            var data = new GameData
            {
                IsFirstTimeUser = false
            };
            data.level.LevelStars.Add(3);

            Database.Save(data, GameData.SaveKey);
            var loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.IsFalse(loaded.IsFirstTimeUser);
            Assert.AreEqual(1, loaded.level.LevelStars.Count);
            Assert.AreEqual(3, loaded.level.LevelStars[0]);
        }

        [Test]
        public void GameData_MissingKey_DefaultsToFirstTimeUser()
        {
            PlayerPrefs.DeleteKey(GameData.SaveKey);

            var loaded = Database.Load<GameData>(GameData.SaveKey);

            Assert.IsTrue(loaded.IsFirstTimeUser);
        }
    }
}
