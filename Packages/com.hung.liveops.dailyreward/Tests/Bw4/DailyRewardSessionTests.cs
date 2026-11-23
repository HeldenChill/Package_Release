using System;
using Hung.Base;
using Hung.Data.Rewards;
using NUnit.Framework;
using Hung.Data;
namespace Hung.LiveOps.DailyReward.Tests.Bw4
{
    public sealed class DailyRewardSessionTests
    {
        [Test] public void UnsetLegacyDayPreservesProgressThenNormalRolloverResets()
        {
            using (var f = new Bw4Fixture())
            {
                var legacy = new DailyRewardSaveData { dayOfYear = f.Clock.UtcNow.DayOfYear, currentProgress = 1 };
                Assert.That(f.Persistence.Save(Hung.Data.Persistence.PackageSaveDefinitions.DailyReward(f.Codec, f.Protector), legacy).Success, Is.True);
                var session = Session(f);
                Assert.That(session.Initialize(out _), Is.True);
                Assert.That(session.Current.Progress, Is.EqualTo(1));
                Assert.That(session.ClaimSequence(0).Outcome, Is.EqualTo(RewardGrantOutcome.IdempotentReplay));
                Assert.That(f.Grant.Published, Is.Zero);
                f.Clock.UtcNow = f.Clock.UtcNow.AddDays(1);
                Assert.That(session.Current.Progress, Is.Zero);
                Assert.That(session.ClaimSequence(0).Success, Is.True);
            }
        }
        [Test] public void OtherFeaturePermanentRecoveryFailureDoesNotBlockInitialize()
        {
            using (var f = new Bw4Fixture())
            {
                // BUG-261114-382e: an unknown item makes the grant fail for good, leaving the claim Granting.
                var poisoned = new RewardClaimRequest(new RewardClaimId("other-feature-claim"), "other-feature",
                    new[] { new RewardGrantItem(ItemId.Parse("fixture.removed"), 1) }, "other-fingerprint");
                Assert.That(f.Coordinator.Claim(poisoned).Outcome, Is.EqualTo(RewardGrantOutcome.InvalidReward));
                Assert.That(f.Coordinator.RecoverPending().FailedCount, Is.EqualTo(1));
                Assert.That(Session(f).Initialize(out var code), Is.True, code);
            }
        }
        [Test] public void UnsetLegacyDayWriteFailureRetainsProgressForRetry()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.DailyStore.Save(new DailyRewardSaveData { currentProgress = 1 }, out _), Is.True);
                f.Store.FailKey = "daily-reward";
                Assert.That(Session(f).Initialize(out _), Is.False);
                Assert.That(f.DailyStore.Load(out var unchanged, out _), Is.True);
                Assert.That(unchanged.rewardDayKey, Is.Zero);
                Assert.That(unchanged.currentProgress, Is.EqualTo(1));
                var retry = Session(f);
                Assert.That(retry.Initialize(out _), Is.True);
                Assert.That(retry.Current.Progress, Is.EqualTo(1));
            }
        }
        private static DailyRewardSession Session(Bw4Fixture f, int amount = 5, string profile = "profile-a")
            => new DailyRewardSession(f.DailyStore, f.Coordinator, f.Clock, new RewardDayPolicy(0), profile,
                Bw4Fixture.Offer(amount), new[] { Bw4Fixture.Offer(amount) }, new[] { false }, 3600);
        [Test] public void FreeGrantAndCooldownAreDurable()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True);
                Assert.That(s.ClaimFree().Success, Is.True); Assert.That(s.CanClaimFree, Is.False);
                Assert.That(Session(f).CanClaimFree, Is.False); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void PendingFreeTokenAndPayloadSurviveConfigChange()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True);
                f.Grant.Unavailable = true; Assert.That(s.ClaimFree().Success, Is.False);
                Assert.That(f.DailyStore.Load(out var before, out _), Is.True); string id = before.bw4Pending.claimId;
                f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(1); f.Grant.Unavailable = false;
                var next = Session(f, 99); Assert.That(next.PreviewFree().Items[0].Quantity, Is.EqualTo(5));
                Assert.That(next.Initialize(out _), Is.True); Assert.That(f.ReloadWallet().receipts.ContainsKey(id), Is.True);
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void CoordinatorFailureMatrixNeverDuplicatesValue()
        {
            for (int failure = 0; failure < 4; failure++) using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True);
                f.Store.FailKey = "reward-integrity"; f.Store.SkipMatches = failure;
                Assert.That(s.ClaimSequence(0).Success, Is.False, "failure=" + failure);
                Assert.That(Session(f).Initialize(out _), Is.True, "retry=" + failure);
                Assert.That(Session(f).Current.Progress, Is.EqualTo(1));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
                Assert.That(f.ReloadWallet().receipts.Count, Is.EqualTo(1));
            }
        }
        [Test] public void FeatureFailureMatrixPreservesPendingAndAbsoluteProgress()
        {
            for (int failure = 0; failure < 3; failure++) using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True);
                f.Store.FailKey = "daily-reward"; f.Store.SkipMatches = failure;
                Assert.That(s.ClaimSequence(0).Success, Is.False);
                var next = Session(f); Assert.That(next.Initialize(out _), Is.True);
                Assert.That(next.ClaimSequence(0).Success, Is.True);
                Assert.That(next.Current.Progress, Is.EqualTo(1)); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void PendingOldDaySettlesBeforeReset()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); f.Grant.Unavailable = true;
                Assert.That(s.ClaimSequence(0).Success, Is.False); f.Clock.UtcNow = f.Clock.UtcNow.AddDays(1); f.Grant.Unavailable = false;
                Assert.That(Session(f).Initialize(out _), Is.True);
                Assert.That(Session(f).Current.Progress, Is.Zero); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void ProfileMismatchDoesNotRecoverOrGrant()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); f.Grant.Unavailable = true;
                Assert.That(s.ClaimFree().Success, Is.False); f.Grant.Unavailable = false;
                Assert.That(Session(f, profile: "profile-b").Initialize(out var code), Is.False);
                Assert.That(code, Is.EqualTo("REWARD_CONTEXT_MISMATCH")); Assert.That(f.Grant.Published, Is.Zero);
            }
        }
        [Test] public void StoppedAndInvalidRequestsCannotGrant()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True);
                Assert.That(s.ClaimSequence(9).Outcome, Is.EqualTo(RewardGrantOutcome.InvalidReward));
                s.Dispose(); Assert.That(s.ClaimFree().Outcome, Is.EqualTo(RewardGrantOutcome.Unavailable)); Assert.That(f.Grant.Published, Is.Zero);
            }
        }
        [Test] public void PendingSlotPreviewSurvivesShorterConfiguration()
        {
            using (var f = new Bw4Fixture())
            {
                var s = new DailyRewardSession(f.DailyStore, f.Coordinator, f.Clock, new RewardDayPolicy(0), "profile-a",
                    Bw4Fixture.Offer(), new[] { Bw4Fixture.Offer(), Bw4Fixture.Offer() }, new[] { false, false }, 3600);
                Assert.That(s.Initialize(out _), Is.True); Assert.That(s.ClaimSequence(0).Success, Is.True);
                f.Grant.Unavailable = true; Assert.That(s.ClaimSequence(1).Success, Is.False); f.Grant.Unavailable = false;
                var shorter = Session(f, 99); Assert.That(shorter.PreviewSequence(1).Items[0].Quantity, Is.EqualTo(5));
                Assert.That(shorter.Initialize(out _), Is.True); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(10));
            }
        }
    }
}
