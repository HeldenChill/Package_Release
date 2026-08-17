using System;
using System.IO;
using System.Text;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    [TestFixture]
    public class FileLegacySaveSourceTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        [Test]
        public void TryRead_FilePresent_ReturnsTrueAndContent()
        {
            string path = Path.Combine(tempRoot, "slot-a");
            File.WriteAllText(path, "{\"v\":1}");
            var source = new FileLegacySaveSource(tempRoot);

            bool found = source.TryRead("slot-a", out string json);

            Assert.IsTrue(found);
            Assert.AreEqual("{\"v\":1}", json);
        }

        [Test]
        public void TryRead_FileAbsent_ReturnsFalse()
        {
            var source = new FileLegacySaveSource(tempRoot);

            bool found = source.TryRead("does-not-exist", out string json);

            Assert.IsFalse(found);
            Assert.IsNull(json);
        }

        [Test]
        public void TryRead_MalformedContent_ReturnsRawTextAndLeavesFileUntouched()
        {
            string path = Path.Combine(tempRoot, "slot-b");
            const string malformed = "{not valid json";
            File.WriteAllText(path, malformed);
            var source = new FileLegacySaveSource(tempRoot);

            bool found = source.TryRead("slot-b", out string json);

            Assert.IsTrue(found);
            Assert.AreEqual(malformed, json);
            Assert.AreEqual(malformed, File.ReadAllText(path));
        }

        [Test]
        public void TryRead_UnreadableFile_ReturnsFalseWithoutThrowing()
        {
            var source = new FileLegacySaveSource(tempRoot, new ThrowingFileSaveOperations());

            bool found = source.TryRead("any-key", out string json);

            Assert.IsFalse(found);
            Assert.IsNull(json);
        }

        [Test]
        public void TryRead_SuccessfulRead_IsByteIdenticalToSourceFile()
        {
            string path = Path.Combine(tempRoot, "slot-c");
            byte[] original = Encoding.UTF8.GetBytes("{\"currencies\":[1,2,3]}");
            File.WriteAllBytes(path, original);
            var source = new FileLegacySaveSource(tempRoot);

            source.TryRead("slot-c", out string json);

            CollectionAssert.AreEqual(original, Encoding.UTF8.GetBytes(json));
        }

        private sealed class ThrowingFileSaveOperations : IFileSaveOperations
        {
            public void CreateDirectory(string path) { }
            public bool FileExists(string path) => true;
            public byte[] ReadAllBytes(string path) => throw new IOException("simulated read failure");
            public Stream CreateNew(string path) => throw new NotSupportedException();
            public void Copy(string sourcePath, string destinationPath, bool overwrite) { }
            public void Move(string sourcePath, string destinationPath) { }
            public void Delete(string path) { }
        }
    }
}
