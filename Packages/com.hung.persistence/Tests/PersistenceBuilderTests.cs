using System;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Hung.Persistence.Tests
{
    /// <summary>
    /// Gate G5: composition builder. Product-free - see plan §9.1's governing rule.
    /// </summary>
    [TestFixture]
    public class PersistenceBuilderTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "HungPersistenceBuilderTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        private PersistenceBuilder NewBuilder() => new PersistenceBuilder()
            .WithRoot(Path.Combine(tempRoot, "saves"))
            .WithCodec(new PlainJsonSaveCodec())
            .WithProtector(new Sha256SaveProtector());

        private static SaveDefinition<SampleState> Definition(string key) => new SaveDefinition<SampleState>(
            key: key,
            currentSchemaVersion: 1,
            createDefault: () => new SampleState { Count = -1 },
            validate: _ => SaveValidationResult.Valid(),
            migrations: new ISaveMigration[] { new IdentityMigration() },
            legacyPlayerPrefsKeys: Array.Empty<string>(),
            codec: new PlainJsonSaveCodec(),
            protector: new Sha256SaveProtector(),
            failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

        // ---- Fake product definition: registers, round-trips, stays isolated ----

        [Test]
        public void Register_FakeProductDefinition_RoundTripsAndStaysIsolated()
        {
            PersistenceService service = NewBuilder()
                .Register(Definition("fake-product-slot"))
                .Build();

            SaveDefinition<SampleState> definition = Definition("fake-product-slot");
            SaveResult save = service.Save(definition, new SampleState { Count = 7 });
            Assert.IsTrue(save.Success);

            LoadResult<SampleState> load = service.Load(definition);
            Assert.IsTrue(load.Success);
            Assert.AreEqual(7, load.Value.Count);

            // Isolation: a second, unrelated definition/type registered on the same builder
            // instance does not leak into or collide with this one.
            PersistenceService serviceTwo = NewBuilder()
                .Register(Definition("other-fake-slot"))
                .Build();
            Assert.IsFalse(serviceTwo.TryGetDefinition<SampleState>("fake-product-slot", out _));
        }

        // ---- Duplicate key / duplicate model type ----

        [Test]
        public void Register_DuplicateKey_ThrowsActionableError()
        {
            var builder = NewBuilder().Register(Definition("dup-slot"));
            var otherTypeDefinition = new SaveDefinition<OtherSampleState>(
                key: "dup-slot",
                currentSchemaVersion: 1,
                createDefault: () => new OtherSampleState(),
                validate: _ => SaveValidationResult.Valid(),
                migrations: new ISaveMigration[] { new IdentityMigration() },
                legacyPlayerPrefsKeys: Array.Empty<string>(),
                codec: new PlainJsonSaveCodec(),
                protector: new Sha256SaveProtector(),
                failurePolicy: SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Register(otherTypeDefinition));
            StringAssert.Contains("dup-slot", ex.Message);
        }

        [Test]
        public void Register_DuplicateModelType_ThrowsActionableError()
        {
            var builder = NewBuilder().Register(Definition("slot-a"));

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Register(Definition("slot-b")));
            StringAssert.Contains("SampleState", ex.Message);
        }

        // ---- Missing composition: actionable error, no silent fallback ----

        [Test]
        public void Build_MissingRoot_ThrowsActionableError()
        {
            var builder = new PersistenceBuilder()
                .WithCodec(new PlainJsonSaveCodec())
                .WithProtector(new Sha256SaveProtector());

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            StringAssert.Contains("root", ex.Message.ToLowerInvariant());
        }

        [Test]
        public void Build_MissingCodec_ThrowsActionableError()
        {
            var builder = new PersistenceBuilder()
                .WithRoot(tempRoot)
                .WithProtector(new Sha256SaveProtector());

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            StringAssert.Contains("codec", ex.Message.ToLowerInvariant());
        }

        [Test]
        public void Build_MissingProtector_ThrowsActionableError()
        {
            var builder = new PersistenceBuilder()
                .WithRoot(tempRoot)
                .WithCodec(new PlainJsonSaveCodec());

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            StringAssert.Contains("protector", ex.Message.ToLowerInvariant());
        }

        [Test]
        public void WithHmacProtection_AndWithProtector_Combined_ThrowsActionableError()
        {
            var builder = new PersistenceBuilder()
                .WithProtector(new Sha256SaveProtector());

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => builder.WithHmacProtection(new FakeKeyProvider(), "purpose"));
            StringAssert.Contains("protector", ex.Message.ToLowerInvariant());
        }

        [Test]
        public void WithHmacProtection_KeyProviderFails_PropagatesDiagnosticCode()
        {
            var builder = new PersistenceBuilder();
            PersistenceException ex = Assert.Throws<PersistenceException>(
                () => builder.WithHmacProtection(new FailingKeyProvider(), "purpose"));
            Assert.AreEqual("SAVE_KEY_UNAVAILABLE", ex.DiagnosticCode);
        }

        // ---- Helpers ----

        private sealed class SampleState
        {
            public int Count { get; set; }
        }

        private sealed class OtherSampleState
        {
            public string Name { get; set; }
        }

        private sealed class IdentityMigration : ISaveMigration
        {
            public int FromVersion => 0;
            public int ToVersion => 1;
            public JObject Migrate(JObject source) => source;
        }

        private sealed class FakeKeyProvider : ISecretKeyProvider
        {
            public SaveSecretKeyResult GetOrCreateKey(string purpose) => new SaveSecretKeyResult(true, new byte[32]);
        }

        private sealed class FailingKeyProvider : ISecretKeyProvider
        {
            public SaveSecretKeyResult GetOrCreateKey(string purpose) => new SaveSecretKeyResult(false, null, "SAVE_KEY_UNAVAILABLE");
        }
    }
}
