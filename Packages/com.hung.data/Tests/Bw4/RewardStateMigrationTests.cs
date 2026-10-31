using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Hung.Data.Rewards;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace Hung.Data.Tests.Bw4
{
    public sealed class RewardStateMigrationTests
    {
        [Test] public void NonNullPendingAndConsumedIdentityRoundTripThroughBothSelectedDefinitions()
        {
            using (var f = new Bw4Fixture())
            {
                int day = new Hung.Base.RewardDayPolicy(0).Resolve(f.Clock.UtcNow).Value;
                var daily = new DailyRewardSaveData {
                    bw4Pending = PendingRewardData.Create("profile-a", "daily-reward", new Hung.Base.RewardClaimId("daily"), "daily", Bw4Fixture.Offer(), day, "free", 0, 1, f.Clock.UtcNow.Ticks, "free"),
                    bw4ConsumedClaimId = "daily-consumed"
                };
                var spin = new SpinWheelSaveData {
                    bw4Pending = PendingRewardData.Create("profile-a", "spin-wheel", new Hung.Base.RewardClaimId("spin"), "spin", Bw4Fixture.Offer(), day, "free", 0, 1, f.Clock.UtcNow.Ticks, "free", 1),
                    bw4ConsumedClaimId = "spin-consumed"
                };
                Assert.That(f.DailyStore.Save(daily, out _), Is.True);
                Assert.That(f.SpinStore.Save(spin, out _), Is.True);
                Assert.That(f.DailyStore.Load(out var readDaily, out _), Is.True);
                Assert.That(f.SpinStore.Load(out var readSpin, out _), Is.True);
                Assert.That(readDaily.bw4Pending.claimId, Is.EqualTo("daily"));
                Assert.That(readDaily.bw4Pending.fingerprint, Is.EqualTo(daily.bw4Pending.fingerprint));
                Assert.That(readDaily.bw4ConsumedClaimId, Is.EqualTo("daily-consumed"));
                Assert.That(readSpin.bw4Pending.claimId, Is.EqualTo("spin"));
                Assert.That(readSpin.bw4Pending.items[0].quantity, Is.EqualTo(5));
                Assert.That(readSpin.bw4ConsumedClaimId, Is.EqualTo("spin-consumed"));
            }
        }
        [Test] public void SchemaOneDailyProgressAndTimestampsSurvive()
        {
            using (var f = new Bw4Fixture())
            {
                var state = new DailyRewardSaveData { currentProgress = 4, dayOfYear = 204, lastFreeClaimTime = 123456, lastFreeClaimUtcTicks = 123 };
                Assert.That(f.Persistence.Save(PackageSaveDefinitions.DailyReward(f.Codec, f.Protector), state).Success, Is.True);
                Assert.That(f.DailyStore.Load(out var after, out _), Is.True);
                Assert.That(after.currentProgress, Is.EqualTo(4)); Assert.That(after.lastFreeClaimTime, Is.EqualTo(123456));
                Assert.That(after.lastFreeClaimUtcTicks, Is.EqualTo(123)); Assert.That(after.bw4Pending, Is.Null);
                var envelope = SaveEnvelope.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(f.Root, "primary", "daily-reward.save")));
                Assert.That(envelope.PayloadSchemaVersion, Is.EqualTo(2));
            }
        }
        [Test] public void MigrationPreservesUnknownJsonAndLegacyPendingBytes()
        {
            using (var f = new Bw4Fixture())
            {
                var migration = Bw4RewardSaveDefinitions.SpinWheel(f.Codec, f.Protector).Migrations[1];
                var old = JObject.Parse("{\"pendingSpinId\":\"old\",\"pendingPayloadFingerprint\":\"exact:bytes\",\"extension\":{\"x\":9}}");
                var next = migration.Migrate(old);
                Assert.That((string)next["pendingPayloadFingerprint"], Is.EqualTo("exact:bytes"));
                Assert.That((int)next["extension"]["x"], Is.EqualTo(9)); Assert.That(old["bw4Pending"], Is.Null);
            }
        }
        [Test] public void SelectedSchemaTwoDefinitionsAreFailClosedAndCollideWithLegacy()
        {
            using (var f = new Bw4Fixture())
            {
                var next = Bw4RewardSaveDefinitions.SpinWheel(f.Codec, f.Protector);
                Assert.That(next.FailurePolicy, Is.EqualTo(SaveFailurePolicy.FailClosed));
                Assert.Throws<System.ArgumentException>(() => SaveDefinitionSet.Validate(new SaveDefinition[] { next, PackageSaveDefinitions.SpinWheel(f.Codec, f.Protector) }));
                Assert.That(next.Validate(new SpinWheelSaveData { bw4Pending = new PendingRewardData() }).Success, Is.False);
            }
        }
        [Test] public void TypedRoundTripUnknownMembersIsNotPromised()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.DailyStore.Load(out var original, out _), Is.True);
                original.currentProgress = 7; Assert.That(f.DailyStore.Save(original, out _), Is.True); original.currentProgress = 99;
                Assert.That(f.DailyStore.Load(out var next, out _), Is.True); Assert.That(next.currentProgress, Is.EqualTo(7));
            }
        }
    }
}
