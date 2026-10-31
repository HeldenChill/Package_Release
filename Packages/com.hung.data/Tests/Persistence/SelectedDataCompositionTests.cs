using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hung.Base;
using Hung.Base.Composition;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Persistence
{
    public sealed class SelectedDataCompositionTests
    {
        public sealed class FictionalState { public int Balance; public List<string> Receipts = new List<string>(); }
        private sealed class Keys : ISecretKeyProvider
        {
            public int Calls;
            public SaveSecretKeyResult GetOrCreateKey(string purpose) { Calls++; return new SaveSecretKeyResult(true, new byte[32]); }
        }
        private sealed class Session
        {
            public bool Available = true;
            public void RequireAvailable() { if (!Available) throw new InvalidOperationException("FIXTURE_OWNER_STOPPED"); }
        }
        private sealed class UnusedPorts : ILegacySaveSource, IFileSaveOperations
        {
            public int Calls;
            private Exception Unexpected() { Calls++; return new InvalidOperationException("UNSELECTED_PORT_USED"); }
            public bool TryRead(string key, out string json) { json = null; throw Unexpected(); }
            public void CreateDirectory(string path) { throw Unexpected(); }
            public bool FileExists(string path) { throw Unexpected(); }
            public byte[] ReadAllBytes(string path) { throw Unexpected(); }
            public Stream CreateNew(string path) { throw Unexpected(); }
            public void Copy(string source, string destination, bool overwrite) { throw Unexpected(); }
            public void Move(string source, string destination) { throw Unexpected(); }
            public void Delete(string path) { throw Unexpected(); }
        }
        private static SaveDefinition<FictionalState> Definition(ISaveCodec codec, ISaveProtector protector) =>
            new SaveDefinition<FictionalState>("fixture-state", 1, () => new FictionalState(), _ => SaveValidationResult.Valid(),
                Array.Empty<ISaveMigration>(), Array.Empty<string>(), codec, protector, SaveFailurePolicy.FailClosed);
        [Test] public void UnselectedFeatureDoesNotResolveKeyOrCreateStore()
        {
            var keys = new Keys();
            var ports = new UnusedPorts(); int defaultCalls = 0;
            string root = Path.Combine(Path.GetTempPath(), "HungDataPlan_" + Guid.NewGuid().ToString("N"));
            Func<PersistenceService> compose = () =>
            {
                var builder = new PersistenceBuilder().WithRoot(root).WithCodec(new PlainJsonSaveCodec())
                    .WithLegacySource(ports).WithFileOperations(ports).WithHmacProtection(keys, "fixture-purpose");
                return builder.Register(new SaveDefinition<FictionalState>("unused", 1, () => { defaultCalls++; return new FictionalState(); },
                    _ => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), new[] { "unused-old" }, builder.Codec, builder.Protector, SaveFailurePolicy.FailClosed)).Build();
            };
            using (var host = new FeatureHost()) { Assert.That(host.Start().ProductReady, Is.True); }
            Assert.That(keys.Calls, Is.Zero);
            Assert.That(ports.Calls, Is.Zero); Assert.That(defaultCalls, Is.Zero);
            Assert.That(Directory.Exists(root), Is.False);
            GC.KeepAlive(compose);
        }
        [Test] public void ExplicitGraphHasNoAggregateOrUnselectedFeatureDefinition()
        {
            var codec = new PlainJsonSaveCodec(); var protector = new Sha256SaveProtector();
            var definition = Definition(codec, protector);
            SaveDefinitionSet.Validate(new SaveDefinition[] { definition });
            string root = Path.Combine(Path.GetTempPath(), "HungDataPlan_" + Guid.NewGuid().ToString("N"));
            var service = new PersistenceBuilder().WithRoot(root).WithCodec(codec).WithProtector(protector).Register(definition).Build();
            Assert.That(service.TryGetDefinition<FictionalState>(null, out _), Is.True);
            Assert.That(service.TryGetDefinition<GameData>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<DailyGiftDbModel>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<HeartSave.HeartSaveData>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<RewardIntegrityStateData>(null, out _), Is.False);
            Assert.That(Directory.Exists(root), Is.False);
        }
        [Test] public void RequiredLoadFailureBlocksDependentAndPublishesNoOwner()
        {
            var store = new InMemorySaveStore { FailNextWrite = true };
            var service = new PersistenceService(store);
            var definition = Definition(new PlainJsonSaveCodec(), new Sha256SaveProtector());
            Session owner = null; int dependent = 0;
            using (var host = new FeatureHost())
            {
                host.Register(new FeatureRegistration("fixture.state", true, Array.Empty<string>(), () =>
                {
                    var loaded = service.Load(definition);
                    if (!loaded.Success) return new FeatureStartResult(false, loaded.DiagnosticCode);
                    owner = new Session(); return new FeatureStartResult(true);
                }, () => { if (owner != null) owner.Available = false; }));
                host.Register(new FeatureRegistration("fixture.reader", true, new[] { "fixture.state" }, () => { dependent++; return new FeatureStartResult(true); }, () => { }));
                var report = host.Start();
                Assert.That(report.ProductReady, Is.False);
                Assert.That(report.Features.Single(x => x.Id == "fixture.reader").State, Is.EqualTo(FeatureState.Blocked));
                Assert.That(owner, Is.Null); Assert.That(dependent, Is.Zero);
            }
        }
        [Test] public void OptionalFailureDoesNotPreventIndependentRequiredFeatureReadiness()
        {
            using (var host = new FeatureHost())
            {
                host.Register(new FeatureRegistration("fixture.optional", false, Array.Empty<string>(), () => new FeatureStartResult(false, "FIXTURE_OPTIONAL_FAILED"), () => { }));
                host.Register(new FeatureRegistration("fixture.required", true, Array.Empty<string>(), () => new FeatureStartResult(true), () => { }));
                Assert.That(host.Start().ProductReady, Is.True);
            }
        }
        [Test] public void NewHostUsesNewOwnerAndOldBorrowedReaderRejectsUse()
        {
            Session previous = null;
            for (int session = 0; session < 2; session++)
            {
                Session current = null;
                using (var host = new FeatureHost())
                {
                    host.Register(new FeatureRegistration("fixture.state", true, Array.Empty<string>(), () => { current = new Session(); return new FeatureStartResult(true); }, () => current.Available = false));
                    Assert.That(host.Start().ProductReady, Is.True);
                    current.RequireAvailable();
                    if (previous != null) { Assert.That(current, Is.Not.SameAs(previous)); Assert.Throws<InvalidOperationException>(() => previous.RequireAvailable()); }
                }
                Assert.Throws<InvalidOperationException>(() => current.RequireAvailable());
                previous = current;
            }
        }
    }
}
