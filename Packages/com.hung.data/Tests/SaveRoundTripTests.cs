using NUnit.Framework;
using UnityEngine;
using Hung.Data.Tests.Persistence;

namespace Hung.Data.Tests
{
    public class SaveRoundTripTests
    {
        private DatabaseFacadeTestScope scope;

        private class SaveRoundTripTestData
        {
            public int Value;
            public string Name;
        }

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(nameof(SaveRoundTripTestData));
            scope = new DatabaseFacadeTestScope();
        }

        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
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
            var loaded = Database.Load<SaveRoundTripTestData>();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.Value);
            Assert.That(scope.Store.Primary.ContainsKey(nameof(SaveRoundTripTestData)), Is.True);
            Assert.That(PlayerPrefs.HasKey(nameof(SaveRoundTripTestData)), Is.False);
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
