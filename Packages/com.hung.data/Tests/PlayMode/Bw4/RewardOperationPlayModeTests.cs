using Hung.Base;
using Hung.Data.Rewards;
using NUnit.Framework;
namespace Hung.Data.PlayModeTests.Bw4
{
    public sealed class RewardOperationPlayModeTests
    {
        private static int entries;
        [Test] public void RestartAndReplayRetainOneValueCommit()
        {
            using (var f = new Bw4Fixture())
            {
                int entry = ++entries;
                UnityEngine.Debug.Log("BW4_ENTRY=" + entry + ";ROOT=" + f.Root);
                Assert.That(f.DailyStore.Load(out var state, out _), Is.True);
                int day = new RewardDayPolicy(0).Resolve(f.Clock.UtcNow).Value;
                state.bw4Pending = PendingRewardData.Create("profile-a", "fixture-feature", new RewardClaimId("claim"), "claim", Bw4Fixture.Offer(), day, "sequence", 0, 1, f.Clock.UtcNow.Ticks, "free");
                Assert.That(f.DailyStore.Save(state, out _), Is.True);
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var result = RewardOperation.Complete(f.DailyStore, f.MakeCoordinator(), "profile-a", "fixture-feature",
                        s => s.bw4Pending, (s, p) => { s.currentProgress = p.targetCount; s.bw4ConsumedClaimId = p.claimId; }, s => s.bw4ConsumedClaimId);
                    Assert.That(result.Success, Is.True);
                }
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
                Assert.That(f.ReloadWallet().receipts.Count, Is.EqualTo(1));
                Assert.That(f.DailyStore.Load(out state, out _), Is.True); Assert.That(state.currentProgress, Is.EqualTo(1));
            }
        }
    }
}
