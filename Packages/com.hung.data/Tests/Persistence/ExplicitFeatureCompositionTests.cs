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
    public sealed class ExplicitFeatureCompositionTests
    {
        public sealed class Receipt
        {
            public string Claim;
            public string Fingerprint;
        }

        public sealed class ProductState
        {
            public bool Initialized;
            public bool BgmMuted;
            public bool SfxMuted;
            public int Progression = 7;
            public int SeedCount;
            public Dictionary<string, int> Balances = new Dictionary<string, int>(StringComparer.Ordinal);
            public List<Receipt> Receipts = new List<Receipt>();
        }

        public sealed class OtherState { }

        private sealed class TrackingLegacy : ILegacySaveSource
        {
            public readonly List<string> Reads = new List<string>();
            public bool TryRead(string key, out string json)
            {
                Reads.Add(key);
                json = null;
                return false;
            }
        }

        // Product example owns policy; the package coordinator only invokes it.
        private sealed class Session : IAudioSettings, IRewardGrantService
        {
            private readonly IPersistenceService service;
            private readonly SaveDefinition<ProductState> definition;
            public ProductState State { get; private set; }
            public int SessionResetCount { get; private set; }
            public bool IsBgmMuted => State.BgmMuted;
            public bool IsSfxMuted => State.SfxMuted;

            public Session(IPersistenceService service, SaveDefinition<ProductState> definition)
            {
                this.service = service;
                this.definition = definition;
            }

            public FeatureStartResult Start()
            {
                LoadResult<ProductState> loaded = service.Load(definition);
                if (!loaded.Success) return new FeatureStartResult(false, loaded.DiagnosticCode);
                ProductState candidate = Clone(loaded.Value);
                bool changed = false;
                // In this test product's schema, Initialized=false is a durable
                // incomplete first-run marker, including after failed seed commits.
                // This is not a rule for existing consumer/legacy GameData saves.
                if (!candidate.Initialized)
                {
                    candidate.Initialized = true;
                    candidate.SeedCount++;
                    candidate.Balances["test.gold"] = 10;
                    changed = true;
                }
                if (!candidate.Balances.ContainsKey("test.new"))
                {
                    candidate.Balances.Add("test.new", 0);
                    changed = true;
                }
                if (changed)
                {
                    SaveResult saved = service.Save(definition, candidate);
                    if (!saved.Success) return new FeatureStartResult(false, saved.DiagnosticCode);
                }
                State = candidate;
                SessionResetCount++;
                return new FeatureStartResult(true);
            }

            public bool SetMute(bool bgmMuted, bool sfxMuted)
            {
                ProductState candidate = Clone(State);
                candidate.BgmMuted = bgmMuted;
                candidate.SfxMuted = sfxMuted;
                if (!service.Save(definition, candidate).Success) return false;
                State = candidate;
                return true;
            }

            public RewardGrantResult Grant(RewardClaimId id, IReadOnlyList<RewardGrantItem> items, string fingerprint)
            {
                if (State == null) return new RewardGrantResult(RewardGrantOutcome.Unavailable);
                if (string.IsNullOrWhiteSpace(id.Value) || items == null || items.Count == 0 || string.IsNullOrWhiteSpace(fingerprint))
                    return new RewardGrantResult(RewardGrantOutcome.InvalidReward);
                Receipt previous = State.Receipts.FirstOrDefault(x => string.Equals(x.Claim, id.Value, StringComparison.Ordinal));
                if (previous != null)
                    return new RewardGrantResult(string.Equals(previous.Fingerprint, fingerprint, StringComparison.Ordinal)
                        ? RewardGrantOutcome.IdempotentReplay : RewardGrantOutcome.Conflict);

                ProductState candidate = Clone(State);
                foreach (RewardGrantItem item in items)
                {
                    if (!item.ItemId.IsValid || item.Quantity <= 0 || !candidate.Balances.TryGetValue(item.ItemId.Value, out int balance))
                        return new RewardGrantResult(RewardGrantOutcome.InvalidReward);
                    try { candidate.Balances[item.ItemId.Value] = checked(balance + item.Quantity); }
                    catch (OverflowException) { return new RewardGrantResult(RewardGrantOutcome.InvalidReward); }
                }
                candidate.Receipts.Add(new Receipt { Claim = id.Value, Fingerprint = fingerprint });
                if (!service.Save(definition, candidate).Success)
                    return new RewardGrantResult(RewardGrantOutcome.PersistenceFailure);
                State = candidate;
                return new RewardGrantResult(RewardGrantOutcome.Success);
            }

            private static ProductState Clone(ProductState source)
            {
                return new ProductState
                {
                    Initialized = source.Initialized,
                    BgmMuted = source.BgmMuted,
                    SfxMuted = source.SfxMuted,
                    Progression = source.Progression,
                    SeedCount = source.SeedCount,
                    Balances = new Dictionary<string, int>(source.Balances, StringComparer.Ordinal),
                    Receipts = source.Receipts.Select(x => new Receipt { Claim = x.Claim, Fingerprint = x.Fingerprint }).ToList()
                };
            }
        }

        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "HungP2A-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            string full = Path.GetFullPath(root);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("HungP2A-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing cleanup outside the isolated test root.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        private static SaveDefinition<ProductState> Definition(string key = "p2-product", string[] aliases = null)
        {
            return new SaveDefinition<ProductState>(key, 1, () => new ProductState(),
                state => state != null && state.Balances != null && state.Receipts != null
                    ? SaveValidationResult.Valid() : SaveValidationResult.Invalid("TEST_INVALID_STATE"),
                Array.Empty<ISaveMigration>(), aliases ?? new[] { "p2-product-legacy" },
                new PlainJsonSaveCodec(), new Sha256SaveProtector(), SaveFailurePolicy.FailClosed);
        }

        private static SaveDefinition<OtherState> Other(string key, params string[] aliases)
        {
            return new SaveDefinition<OtherState>(key, 1, () => new OtherState(),
                state => SaveValidationResult.Valid(), Array.Empty<ISaveMigration>(), aliases,
                new PlainJsonSaveCodec(), new Sha256SaveProtector(), SaveFailurePolicy.FailClosed);
        }

        // Product composition example. This is deliberately not a new runtime registry.
        // Call this before constructing a builder, key provider or physical store.
        private static void Preflight(IReadOnlyList<SaveDefinition> selected)
        {
            var types = new HashSet<Type>();
            var names = new Dictionary<string, SaveDefinition>(StringComparer.Ordinal);
            foreach (SaveDefinition definition in selected)
            {
                if (definition == null || !types.Add(definition.ModelType))
                    throw new ArgumentException("Duplicate or invalid selected model type.");
                var ownNames = new List<string> { definition.Key };
                ownNames.AddRange(definition.LegacyPlayerPrefsKeys);
                foreach (string name in ownNames)
                {
                    if (names.TryGetValue(name, out SaveDefinition owner) && !ReferenceEquals(owner, definition))
                        throw new ArgumentException("Selected save name has multiple owners: " + name);
                    // Own canonical key may also be its own alias. Duplicate aliases
                    // within one definition already fail SaveDefinition construction.
                    names[name] = definition;
                }
            }
        }

        [Test]
        public void BuilderRegistersOnlySelectedModelAndTouchesOnlyItsLegacyAliases()
        {
            SaveDefinition<ProductState> definition = Definition();
            Preflight(new SaveDefinition[] { definition });
            var legacy = new TrackingLegacy();
            PersistenceService service = new PersistenceBuilder().WithRoot(root)
                .WithCodec(definition.Codec).WithProtector(definition.Protector)
                .WithLegacySource(legacy).Register(definition).Build();
            Assert.That(service.TryGetDefinition<ProductState>(null, out _), Is.True);
            Assert.That(service.TryGetDefinition<GameData>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<DailyGiftDbModel>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<HeartSave.HeartSaveData>(null, out _), Is.False);
            Assert.That(service.TryGetDefinition<RewardIntegrityStateData>(null, out _), Is.False);
            Assert.That(service.Load(definition).Success, Is.True);
            Assert.That(legacy.Reads, Is.EqualTo(new[] { "p2-product-legacy" }));
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(Path.GetFileName(files[0]), Is.EqualTo("p2-product.save"));
        }

        [Test]
        public void InvalidDefinitionSelectionFailsBeforeCompositionSideEffects()
        {
            SaveDefinition<ProductState> definition = Definition();
            int composeCalls = 0;
            Action<IReadOnlyList<SaveDefinition>> compose = selected =>
            {
                Preflight(selected);
                composeCalls++;
                Directory.CreateDirectory(root);
            };
            Assert.Throws<ArgumentException>(() => compose(new SaveDefinition[] { definition, Definition("another-key") }));
            Assert.Throws<ArgumentException>(() => compose(new SaveDefinition[] { definition, Other("p2-product") }));
            Assert.Throws<ArgumentException>(() => compose(new SaveDefinition[] { definition, Other("other", "p2-product-legacy") }));
            Assert.Throws<ArgumentException>(() => compose(new SaveDefinition[] { definition, Other("p2-product-legacy") }));
            Assert.That(composeCalls, Is.Zero);
            Assert.That(Directory.Exists(root), Is.False);
        }

        [Test]
        public void EmptyFeatureSelectionNeedsNoPersistenceComposition()
        {
            using var host = new FeatureHost();
            Assert.That(host.Start().ProductReady, Is.True);
            Assert.That(Directory.Exists(root), Is.False);
        }

        [Test]
        public void ExistingStateAndUnknownItemsSurviveNormalizationWithoutReseeding()
        {
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store);
            SaveDefinition<ProductState> definition = Definition();
            var existing = new ProductState { Initialized = true, BgmMuted = true, Progression = 42, SeedCount = 1 };
            existing.Balances.Add("test.gold", 0);
            existing.Balances.Add("future.unknown", 99);
            existing.Receipts.Add(new Receipt { Claim = "old", Fingerprint = "fp-old" });
            Assert.That(service.Save(definition, existing).Success, Is.True);
            var session = new Session(service, definition);
            using var host = new FeatureHost();
            host.Register(new FeatureRegistration("state", true, Array.Empty<string>(), session.Start, () => { }));
            Assert.That(host.Start().ProductReady, Is.True);
            host.Start();
            Assert.That(session.State.Balances["test.gold"], Is.Zero);
            Assert.That(session.State.Balances["future.unknown"], Is.EqualTo(99));
            Assert.That(session.State.Balances["test.new"], Is.Zero);
            Assert.That(session.State.Receipts.Single().Claim, Is.EqualTo("old"));
            Assert.That(session.State.Progression, Is.EqualTo(42));
            Assert.That(session.State.SeedCount, Is.EqualTo(1));
            Assert.That(session.SessionResetCount, Is.EqualTo(1));
            int writes = store.WriteCount;
            Assert.That(service.Load(definition).Value.BgmMuted, Is.True);
            Assert.That(service.Load(definition).Value.Progression, Is.EqualTo(42));
            Assert.That(store.WriteCount, Is.EqualTo(writes));
        }

        [Test]
        public void FailedFirstRunCommitPublishesNothingAndFreshHostRetriesOnce()
        {
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store);
            SaveDefinition<ProductState> definition = Definition();
            // Real load persists a default before product-specific first-run policy.
            Assert.That(service.Load(definition).Success, Is.True);
            store.FailNextWrite = true;
            var failed = new Session(service, definition);
            using (var first = new FeatureHost())
            {
                first.Register(new FeatureRegistration("state", true, Array.Empty<string>(), failed.Start, () => { }));
                Assert.That(first.Start().ProductReady, Is.False);
                Assert.That(failed.State, Is.Null);
                Assert.That(failed.SessionResetCount, Is.Zero);
            }
            Assert.That(service.Load(definition).Value.Initialized, Is.False);
            var retry = new Session(service, definition);
            using var second = new FeatureHost();
            second.Register(new FeatureRegistration("state", true, Array.Empty<string>(), retry.Start, () => { }));
            Assert.That(second.Start().ProductReady, Is.True);
            Assert.That(retry.State.Balances["test.gold"], Is.EqualTo(10));
            Assert.That(service.Load(definition).Value.SeedCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedDefaultCreationCannotPublishReadiness()
        {
            var store = new InMemorySaveStore { FailNextWrite = true };
            var session = new Session(new PersistenceService(store), Definition());
            using var host = new FeatureHost();
            host.Register(new FeatureRegistration("state", true, Array.Empty<string>(), session.Start, () => { }));
            Assert.That(host.Start().ProductReady, Is.False);
            Assert.That(session.State, Is.Null);
            Assert.That(store.Primary, Is.Empty);
        }

        [Test]
        public void LiveSettingsAdapterFollowsCommittedRootAndRejectsFailedCandidate()
        {
            var store = new InMemorySaveStore();
            var session = new Session(new PersistenceService(store), Definition());
            Assert.That(session.Start().Success, Is.True);
            IAudioSettings view = session;
            ProductState old = session.State;
            Assert.That(session.SetMute(true, false), Is.True);
            Assert.That(view.IsBgmMuted, Is.True);
            Assert.That(old.BgmMuted, Is.False);
            store.FailNextWrite = true;
            Assert.That(session.SetMute(false, true), Is.False);
            Assert.That(view.IsBgmMuted, Is.True);
            Assert.That(view.IsSfxMuted, Is.False);
        }

        [Test]
        public void GrantAndReceiptCommitTogetherAndReplaySurvivesReload()
        {
            var store = new InMemorySaveStore();
            var service = new PersistenceService(store);
            SaveDefinition<ProductState> definition = Definition();
            var session = new Session(service, definition);
            Assert.That(session.Start().Success, Is.True);
            var claim = new RewardClaimId("test-claim");
            var items = new[] { new RewardGrantItem(new ItemId("test.gold"), 3) };
            store.FailNextWrite = true;
            Assert.That(session.Grant(claim, items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.PersistenceFailure));
            Assert.That(session.State.Balances["test.gold"], Is.EqualTo(10));
            Assert.That(session.State.Receipts, Is.Empty);
            Assert.That(service.Load(definition).Value.Receipts, Is.Empty);
            Assert.That(session.Grant(claim, items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.Success));
            int writes = store.WriteCount;
            var reloaded = new Session(service, definition);
            Assert.That(reloaded.Start().Success, Is.True);
            Assert.That(reloaded.Grant(claim, items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.IdempotentReplay));
            Assert.That(reloaded.Grant(claim, items, "other").Outcome, Is.EqualTo(RewardGrantOutcome.Conflict));
            Assert.That(reloaded.State.Balances["test.gold"], Is.EqualTo(13));
            Assert.That(reloaded.State.Receipts.Count, Is.EqualTo(1));
            Assert.That(store.WriteCount, Is.EqualTo(writes));
        }

        [Test]
        public void ProtectedCorruptPrimaryAndNoBackupFailsClosed()
        {
            var store = new InMemorySaveStore();
            SaveDefinition<ProductState> definition = Definition();
            store.Primary.Add(definition.Key, new byte[] { 1, 2, 3 });
            var session = new Session(new PersistenceService(store), definition);
            using var host = new FeatureHost();
            host.Register(new FeatureRegistration("state", true, Array.Empty<string>(), session.Start, () => { }));
            Assert.That(host.Start().ProductReady, Is.False);
            Assert.That(session.State, Is.Null);
            Assert.That(store.QuarantinedBytes.Count, Is.EqualTo(1));
            Assert.That(store.WriteCount, Is.Zero);
        }
    }
}
