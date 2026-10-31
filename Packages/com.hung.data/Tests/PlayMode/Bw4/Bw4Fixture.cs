using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Hung.Data.Rewards;
using NUnit.Framework;
namespace Hung.Data.PlayModeTests.Bw4
{
    internal sealed class Bw4Fixture : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "HungBw4PlanTests", Guid.NewGuid().ToString("N"));
        internal readonly TestClock Clock = new TestClock();
        internal readonly FaultFiles Files = new FaultFiles();
        internal FaultStore Store;
        internal readonly PlainJsonSaveCodec Codec = new PlainJsonSaveCodec();
        internal readonly Sha256SaveProtector Protector = new Sha256SaveProtector();
        internal PersistenceService Persistence;
        internal SaveDefinition<Wallet> WalletDefinition;
        internal WalletGrant Grant;
        internal RewardClaimCoordinator Coordinator;
        internal Bw4Fixture()
        {
            Store = new FaultStore(new FileSaveStore(Root, Files));
            Persistence = new PersistenceService(Store);
            WalletDefinition = new SaveDefinition<Wallet>("fixture-wallet", 1, () => new Wallet(),
                x => x != null && x.balances != null && x.receipts != null ? SaveValidationResult.Valid() : SaveValidationResult.Invalid("WALLET_INVALID"),
                null, null, Codec, Protector, SaveFailurePolicy.FailClosed);
            Grant = new WalletGrant(this); Coordinator = MakeCoordinator();
        }
        internal RewardClaimCoordinator MakeCoordinator() => new RewardClaimCoordinator(Clock, new RewardDayPolicy(0), "profile-a", Persistence,
            PackageSaveDefinitions.RewardIntegrity(Codec, Protector), Grant);
        internal RewardStateStore<DailyRewardSaveData> DailyStore => new RewardStateStore<DailyRewardSaveData>(Persistence, Bw4RewardSaveDefinitions.DailyReward(Codec, Protector));
        internal RewardStateStore<SpinWheelSaveData> SpinStore => new RewardStateStore<SpinWheelSaveData>(Persistence, Bw4RewardSaveDefinitions.SpinWheel(Codec, Protector));
        internal static ResolvedRewardOffer Offer(int amount = 5, string key = "fixture.offer") => new RewardOfferDefinition {
            key = key, revision = "1", items = new List<RewardLine> { new RewardLine("fixture.coins", amount) }
        }.Resolve(x => x.Value == "fixture.coins");
        internal RewardClaimRequest Request(string id = "claim", int amount = 5, string fingerprint = "old-fingerprint")
            => new RewardClaimRequest(new RewardClaimId(id), "fixture-feature", new[] { new RewardGrantItem(ItemId.Parse("fixture.coins"), amount) }, fingerprint);
        internal Wallet ReloadWallet() => new PersistenceService(new FileSaveStore(Root)).Load(WalletDefinition).Value;
        public void Dispose()
        {
            string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "HungBw4PlanTests")) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Root);
            if (!target.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Cleanup boundary.");
            if (Directory.Exists(target)) Directory.Delete(target, true);
        }
    }
    [Serializable] internal sealed class Wallet
    {
        public Dictionary<string, int> balances = new Dictionary<string, int> { { "fixture.coins", 0 } };
        public Dictionary<string, string> receipts = new Dictionary<string, string>();
    }
    internal sealed class WalletGrant : IRewardGrantService
    {
        private readonly Bw4Fixture fixture;
        internal int Published;
        internal bool Unavailable;
        internal WalletGrant(Bw4Fixture fixture) { this.fixture = fixture; }
        public RewardGrantResult Grant(RewardClaimId id, IReadOnlyList<RewardGrantItem> items, string fingerprint)
        {
            if (Unavailable) return new RewardGrantResult(RewardGrantOutcome.Unavailable);
            var loaded = fixture.Persistence.Load(fixture.WalletDefinition);
            if (!loaded.Success) return new RewardGrantResult(RewardGrantOutcome.PersistenceFailure, loaded.DiagnosticCode);
            var candidate = RewardStateStore<Wallet>.Copy(loaded.Value);
            if (candidate.receipts.TryGetValue(id.Value, out var old)) return new RewardGrantResult(old == fingerprint ? RewardGrantOutcome.IdempotentReplay : RewardGrantOutcome.Conflict);
            try
            {
                foreach (var item in RewardPayload.Canonical(items))
                {
                    if (!candidate.balances.ContainsKey(item.ItemId.Value)) return new RewardGrantResult(RewardGrantOutcome.InvalidReward);
                    candidate.balances[item.ItemId.Value] = checked(candidate.balances[item.ItemId.Value] + item.Quantity);
                }
            }
            catch (Exception) { return new RewardGrantResult(RewardGrantOutcome.InvalidReward); }
            candidate.receipts.Add(id.Value, fingerprint);
            var saved = fixture.Persistence.Save(fixture.WalletDefinition, candidate);
            if (!saved.Success) return new RewardGrantResult(RewardGrantOutcome.PersistenceFailure, saved.DiagnosticCode);
            Published++; return new RewardGrantResult(RewardGrantOutcome.Success);
        }
    }
    internal sealed class TestClock : IClock
    { public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc); }
    internal sealed class FaultStore : ISaveStore
    {
        private readonly ISaveStore inner;
        internal string FailKey;
        internal int SkipMatches;
        internal int Reads, Writes;
        internal FaultStore(ISaveStore inner) { this.inner = inner; }
        public SaveStoreReadResult ReadPrimary(string key) { Reads++; return inner.ReadPrimary(key); }
        public SaveStoreReadResult ReadBackup(string key) { Reads++; return inner.ReadBackup(key); }
        public SaveStoreWriteResult Write(string key, byte[] bytes)
        {
            Writes++;
            if (key == FailKey && SkipMatches-- <= 0) { FailKey = null; return SaveStoreWriteResult.Failed("INJECTED_SAVE_FAILURE"); }
            return inner.Write(key, bytes);
        }
        public SaveStoreWriteResult RestoreBackup(string key) => inner.RestoreBackup(key);
        public SaveStoreWriteResult QuarantinePrimary(string key, byte[] bytes, string reason) => inner.QuarantinePrimary(key, bytes, reason);
        public SaveStoreWriteResult QuarantineBackup(string key, byte[] bytes, string reason) => inner.QuarantineBackup(key, bytes, reason);
    }
    internal sealed class FaultFiles : IFileSaveOperations
    {
        private readonly SystemFileSaveOperations inner = new SystemFileSaveOperations();
        internal string FailPromotionKey;
        public void CreateDirectory(string path) => inner.CreateDirectory(path);
        public bool FileExists(string path) => inner.FileExists(path);
        public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
        public Stream CreateNew(string path) => inner.CreateNew(path);
        public void Copy(string source, string destination, bool overwrite) => inner.Copy(source, destination, overwrite);
        public void Delete(string path) => inner.Delete(path);
        public void Move(string source, string destination)
        {
            if (Path.GetFileName(destination) == FailPromotionKey + ".save") { FailPromotionKey = null; throw new IOException("Injected promotion failure."); }
            inner.Move(source, destination);
        }
    }
}
