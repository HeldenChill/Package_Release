using System;
using System.IO;
using System.Text;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public class FileSaveStoreTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "comhung-store-tests-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }

        [Test]
        public void Store_WritesPrimaryAndPreservesPreviousPrimaryAsBackup()
        {
            var store = new FileSaveStore(root);

            Assert.That(store.Write("game-data", Bytes("v1")).Success, Is.True);
            Assert.That(Text(store.ReadPrimary("game-data").Content.ToArray()), Is.EqualTo("v1"));

            Assert.That(store.Write("game-data", Bytes("v2")).Success, Is.True);

            Assert.That(Text(store.ReadPrimary("game-data").Content.ToArray()), Is.EqualTo("v2"));
            Assert.That(Text(store.ReadBackup("game-data").Content.ToArray()), Is.EqualTo("v1"));
        }

        [Test]
        public void Store_RestoresBackupAndQuarantinesExactBytes()
        {
            var store = new FileSaveStore(root);
            store.Write("game-data", Bytes("v1"));
            store.Write("game-data", Bytes("v2"));

            Assert.That(store.RestoreBackup("game-data").Success, Is.True);
            Assert.That(Text(store.ReadPrimary("game-data").Content.ToArray()), Is.EqualTo("v1"));

            Assert.That(store.QuarantinePrimary("game-data", Bytes("bad"), "SAVE_PRIMARY_CORRUPT").Success, Is.True);
            string quarantine = Directory.GetFiles(Path.Combine(root, "quarantine"))[0];
            Assert.That(Text(File.ReadAllBytes(quarantine)), Is.EqualTo("bad"));
        }

        [Test]
        public void Store_RejectsInvalidKeys()
        {
            var store = new FileSaveStore(root);
            Assert.Throws<ArgumentException>(() => store.ReadPrimary("../game-data"));
        }

        private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
        private static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes);
    }
}
