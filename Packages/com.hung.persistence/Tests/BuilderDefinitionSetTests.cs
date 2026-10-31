using System;
using System.IO;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Persistence.Tests.DataComposition
{
    public sealed class BuilderDefinitionSetTests
    {
        private static PersistenceBuilder Builder(string root) => new PersistenceBuilder().WithRoot(root)
            .WithCodec(new PlainJsonSaveCodec()).WithProtector(new Sha256SaveProtector());
        [Test] public void RejectedModelDoesNotReserveItsKey()
        {
            string root = Path.Combine(Path.GetTempPath(), "DataPlan_" + Guid.NewGuid().ToString("N"));
            var builder = Builder(root).Register(Definitions.Make<Wallet>("wallet"));
            Assert.Throws<InvalidOperationException>(() => builder.Register(Definitions.Make<Wallet>("prefs")));
            builder.Register(Definitions.Make<Preferences>("prefs"));
            var service = builder.Build();
            Assert.That(service.TryGetDefinition<Wallet>("wallet", out _), Is.True);
            Assert.That(service.TryGetDefinition<Preferences>("prefs", out _), Is.True);
            Assert.That(Directory.Exists(root), Is.False);
        }
        [Test] public void RejectedAliasDoesNotReserveItsModelOrCanonicalKey()
        {
            string root = Path.Combine(Path.GetTempPath(), "DataPlan_" + Guid.NewGuid().ToString("N"));
            var builder = Builder(root).Register(Definitions.Make<Wallet>("wallet", new[] { "old" }));
            var error = Assert.Throws<InvalidOperationException>(() => builder.Register(Definitions.Make<Preferences>("prefs", new[] { "old" })));
            StringAssert.Contains("SAVE_DEFINITION_NAME_COLLISION", error.Message);
            builder.Register(Definitions.Make<Preferences>("prefs", new[] { "other-old" }));
            Assert.That(builder.Build().TryGetDefinition<Preferences>(null, out var definition), Is.True);
            Assert.That(definition.LegacyPlayerPrefsKeys, Is.EqualTo(new[] { "other-old" }));
            Assert.That(Directory.Exists(root), Is.False);
        }
        [Test] public void BuilderRejectsFileCollisionWithStableDiagnostic()
        {
            var builder = Builder("not-used").Register(Definitions.Make<Wallet>("Wallet"));
            var error = Assert.Throws<InvalidOperationException>(() => builder.Register(Definitions.Make<Preferences>("wallet")));
            StringAssert.Contains("SAVE_DEFINITION_FILE_COLLISION", error.Message);
        }
        [Test] public void BuildRevalidatesMutatedAliasMetadataBeforeStoreConstruction()
        {
            string root = Path.Combine(Path.GetTempPath(), "DataPlan_" + Guid.NewGuid().ToString("N"));
            var prefs = Definitions.Make<Preferences>("prefs", new[] { "old-prefs" });
            var builder = Builder(root).Register(Definitions.Make<Wallet>("wallet")).Register(prefs);
            ((string[])prefs.LegacyPlayerPrefsKeys)[0] = "wallet";
            Assert.Throws<ArgumentException>(() => builder.Build());
            Assert.That(Directory.Exists(root), Is.False);
        }
        [Test] public void DirectServiceRegistrationRetainsLegacyReplacementSemantics()
        {
            var service = new PersistenceService(new MemoryStore());
            service.Register(Definitions.Make<Wallet>("first"));
            service.Register(Definitions.Make<Wallet>("replacement"));
            Assert.That(service.TryGetDefinition<Wallet>("replacement", out _), Is.True);
            Assert.That(service.TryGetDefinition<Wallet>("first", out _), Is.False);
        }
    }
}
