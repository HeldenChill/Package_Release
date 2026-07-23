using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests
{
    // Database lives in com.hung.base (global namespace), not com.hung.data itself -
    // adjusted from the plan's assumed package location during Ph5 execution.
    // Database.Save/Load persists to PlayerPrefs keyed by typeof(T).Name - tests must
    // clean up PlayerPrefs in TearDown to avoid cross-run pollution on the same machine.
    public class SaveRoundTripTests
    {
        private class SaveRoundTripTestData
        {
            public int Value;
            public string Name;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(nameof(SaveRoundTripTestData));
        }

        [Test]
        public void Save_Load_RoundTripsEqualData()
        {
            var data = new SaveRoundTripTestData { Value = 7, Name = "hello" };

            Database.Save(data);
            var loaded = Database.Load<SaveRoundTripTestData>();

            Assert.AreEqual(7, loaded.Value);
            Assert.AreEqual("hello", loaded.Name);
        }

        [Test]
        public void Load_MissingKey_ReturnsNewDefaultAndPersistsIt()
        {
            // Characterizing the actual self-healing behavior: a miss does not throw or
            // return null, it constructs a default instance, saves it, and returns it.
            PlayerPrefs.DeleteKey(nameof(SaveRoundTripTestData));

            var loaded = Database.Load<SaveRoundTripTestData>();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.Value);
            Assert.IsTrue(PlayerPrefs.HasKey(nameof(SaveRoundTripTestData)), "Load on a miss must persist the default it created");
        }

        [Test]
        public void Mutate_Save_Load_ReflectsChange()
        {
            var data = new SaveRoundTripTestData { Value = 1 };
            Database.Save(data);

            data.Value = 99;
            Database.Save(data);
            var loaded = Database.Load<SaveRoundTripTestData>();

            Assert.AreEqual(99, loaded.Value);
        }
    }
}
