using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Hung.LiveOps.Energy.Tests
{
    internal sealed class LocalEnergyStateStoreTests
    {
        private string _tempDir;
        private string _filePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "EnergyStoreTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _filePath = Path.Combine(_tempDir, "energy-state.json");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (string file in Directory.GetFiles(_tempDir))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_tempDir, true);
            }
            catch (Exception) { /* best-effort cleanup */ }
        }

        private static EnergyState MakeState()
        {
            return new EnergyState
            {
                SchemaVersion = EnergyStateMapper.CurrentSchemaVersion,
                RenewableAmount = 3,
                BonusAmount = 5,
                RegenerationAnchorUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UnlimitedUntilUtc = null,
                LatestObservedUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                AppliedConfigSnapshot = "cfg-v1"
            };
        }

        [Test]
        public void Load_NoFile_ReturnsNotFound()
        {
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);

            EnergyStateLoadResult result = store.Load();

            Assert.AreEqual(EnergyStateLoadStatus.NotFound, result.Status);
            Assert.IsNull(result.State);
        }

        [Test]
        public void SaveThenLoad_RoundTripsState()
        {
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);
            EnergyState original = MakeState();

            bool saved = store.Save(original);
            EnergyStateLoadResult result = store.Load();

            Assert.IsTrue(saved);
            Assert.AreEqual(EnergyStateLoadStatus.Loaded, result.Status);
            Assert.AreEqual(original.RenewableAmount, result.State.RenewableAmount);
            Assert.AreEqual(original.BonusAmount, result.State.BonusAmount);
            Assert.AreEqual(original.RegenerationAnchorUtc, result.State.RegenerationAnchorUtc);
            Assert.AreEqual(original.AppliedConfigSnapshot, result.State.AppliedConfigSnapshot);
        }

        [Test]
        public void Load_MalformedJson_ReturnsCorruptAndQuarantines()
        {
            File.WriteAllText(_filePath, "{ not valid json ][");
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);

            EnergyStateLoadResult result = store.Load();

            Assert.AreEqual(EnergyStateLoadStatus.Corrupt, result.Status);
            string[] quarantineFiles = Directory.GetFiles(_tempDir, "*.corrupt-*.json");
            Assert.AreEqual(1, quarantineFiles.Length);
            Assert.AreEqual("{ not valid json ][", File.ReadAllText(quarantineFiles[0]));
            Assert.IsTrue(File.Exists(_filePath), "Original corrupt file must not be deleted.");
        }

        [Test]
        public void Load_UnsupportedSchemaVersion_ReturnsUnsupportedVersionAndQuarantines()
        {
            EnergyStateDto dto = new EnergyStateDto { schemaVersion = 999 };
            string json = UnityEngine.JsonUtility.ToJson(dto);
            File.WriteAllText(_filePath, json);
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);

            EnergyStateLoadResult result = store.Load();

            Assert.AreEqual(EnergyStateLoadStatus.UnsupportedVersion, result.Status);
            string[] quarantineFiles = Directory.GetFiles(_tempDir, "*.corrupt-*.json");
            Assert.AreEqual(1, quarantineFiles.Length);
        }

        [Test]
        public void Load_MultipleCorruptLoads_ProduceDistinctQuarantineFiles()
        {
            File.WriteAllText(_filePath, "not json at all");
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);

            store.Load();
            store.Load();
            store.Load();

            string[] quarantineFiles = Directory.GetFiles(_tempDir, "*.corrupt-*.json");
            Assert.AreEqual(3, quarantineFiles.Length);
            Assert.AreEqual(3, quarantineFiles.Distinct().Count());
        }

        [Test]
        public void Save_ToUnwritableDestination_ReturnsFalseWithoutThrowing()
        {
            // Use a file as a "directory" component to force an IO failure.
            string blockerFile = Path.Combine(_tempDir, "blocker.txt");
            File.WriteAllText(blockerFile, "x");
            string badPath = Path.Combine(blockerFile, "energy-state.json");
            LocalEnergyStateStore store = new LocalEnergyStateStore(badPath);

            bool result = store.Save(MakeState());

            Assert.IsFalse(result);
        }

        [Test]
        public void Save_MissingParentDirectory_CreatesDirectoryAndSucceeds()
        {
            string nestedPath = Path.Combine(_tempDir, "nested", "subdir", "energy-state.json");
            LocalEnergyStateStore store = new LocalEnergyStateStore(nestedPath);

            bool saved = store.Save(MakeState());

            Assert.IsTrue(saved);
            Assert.IsTrue(File.Exists(nestedPath));
        }

        [Test]
        public void Load_WithStrayTempFile_IgnoresItAndCleansItUp()
        {
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);
            store.Save(MakeState());
            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, "garbage from a crashed save");

            EnergyStateLoadResult result = store.Load();

            Assert.AreEqual(EnergyStateLoadStatus.Loaded, result.Status);
            Assert.IsFalse(File.Exists(tempPath), "Stray .tmp file should be cleaned up by Load.");
        }

        [Test]
        public void Save_FailureAfterPriorSuccess_LeavesPreviousFileIntact()
        {
            LocalEnergyStateStore store = new LocalEnergyStateStore(_filePath);
            store.Save(MakeState());
            string originalContent = File.ReadAllText(_filePath);

            File.SetAttributes(_filePath, FileAttributes.ReadOnly);
            try
            {
                EnergyState secondState = MakeState();
                secondState.RenewableAmount = 999;

                bool saved = store.Save(secondState);

                Assert.IsFalse(saved);
                Assert.AreEqual(originalContent, File.ReadAllText(_filePath));
            }
            finally
            {
                File.SetAttributes(_filePath, FileAttributes.Normal);
            }
        }
    }
}
