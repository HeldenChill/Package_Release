using System;
using Hung.Base;
using Hung.Data.Rewards;
using NUnit.Framework;
using Hung.Data;
namespace Hung.LiveOps.SpinWheel.Tests.Bw4
{
    public sealed class SpinWheelSessionTests
    {
        [Test] public void UnsetLegacyDayPreservesAllowancesThenNormalRolloverResets()
        {
            using (var f = new Bw4Fixture())
            {
                var legacy = new SpinWheelSaveData { dayOfYear = f.Clock.UtcNow.DayOfYear, isDoneSpinFreeToday = true, adsSpinToday = 2 };
                Assert.That(f.Persistence.Save(Hung.Data.Persistence.PackageSaveDefinitions.SpinWheel(f.Codec, f.Protector), legacy).Success, Is.True);
                var session = Session(f);
                Assert.That(session.Initialize(out _), Is.True);
                Assert.That(session.Current.FreeAvailable, Is.False);
                Assert.That(session.AdsSpinToday, Is.EqualTo(2));
                Assert.That(session.PrepareFreeSpin(0).Success, Is.False);
                f.Clock.UtcNow = f.Clock.UtcNow.AddDays(1);
                Assert.That(session.Current.FreeAvailable, Is.True);
                Assert.That(session.AdsSpinToday, Is.Zero);
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
        [Test] public void UnsetLegacyDayWriteFailureRetainsAllowancesForRetry()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.SpinStore.Save(new SpinWheelSaveData { isDoneSpinFreeToday = true, adsSpinToday = 2 }, out _), Is.True);
                f.Store.FailKey = "spin-wheel";
                Assert.That(Session(f).Initialize(out _), Is.False);
                Assert.That(f.SpinStore.Load(out var unchanged, out _), Is.True);
                Assert.That(unchanged.rewardDayKey, Is.Zero);
                Assert.That(unchanged.isDoneSpinFreeToday, Is.True);
                Assert.That(unchanged.adsSpinToday, Is.EqualTo(2));
                var retry = Session(f);
                Assert.That(retry.Initialize(out _), Is.True);
                Assert.That(retry.Current.FreeAvailable, Is.False);
                Assert.That(retry.AdsSpinToday, Is.EqualTo(2));
            }
        }
        private static SpinWheelSession Session(Bw4Fixture f, int amount = 5, string profile = "profile-a")
            => new SpinWheelSession(f.SpinStore, f.Coordinator, f.Clock, new RewardDayPolicy(0), profile,
                new[] { Bw4Fixture.Offer(amount) }, new[] { Bw4Fixture.Offer(amount) }, 2);
        [Test] public void PreparedPayloadSurvivesChangedConfiguration()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var prepared = s.PrepareFreeSpin(0); Assert.That(prepared.Success, Is.True);
                Assert.That(Session(f, 99).Preview(0, false).Items[0].Quantity, Is.EqualTo(5));
                Assert.That(Session(f, 99).CompleteSpin(prepared.SpinId).Success, Is.True);
                Assert.That(Session(f).CompleteSpin(prepared.SpinId).Outcome, Is.EqualTo(RewardGrantOutcome.IdempotentReplay));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void PendingPreparationCannotBeOverwritten()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var first = s.PrepareFreeSpin(0);
                Assert.That(s.PrepareFreeSpin(0).SpinId, Is.EqualTo(first.SpinId));
                Assert.That(s.PrepareFreeSpin(1).Success, Is.False); Assert.That(f.Grant.Published, Is.Zero);
            }
        }
        [Test] public void FeatureSaveFailureMatrixIsRetrySafe()
        {
            for (int failure = 0; failure < 2; failure++) using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var prepared = s.PrepareFreeSpin(0);
                f.Store.FailKey = "spin-wheel"; f.Store.SkipMatches = failure;
                Assert.That(s.CompleteSpin(prepared.SpinId).Success, Is.False);
                Assert.That(Session(f).Initialize(out _), Is.True); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
                Assert.That(f.SpinStore.Load(out var state, out _), Is.True); Assert.That(state.spinOrdinal, Is.EqualTo(1));
            }
        }
        [Test] public void FailedPrepareNeverStartsValue()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); f.Store.FailKey = "spin-wheel";
                Assert.That(s.PrepareFreeSpin(0).Success, Is.False); Assert.That(f.Grant.Published, Is.Zero);
                Assert.That(f.SpinStore.Load(out var state, out _), Is.True); Assert.That(state.bw4Pending, Is.Null);
            }
        }
        [Test] public void FinalizedWriteFailureDoesNotDoubleIncrementAds()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var free = s.PrepareFreeSpin(0); Assert.That(s.CompleteSpin(free.SpinId).Success, Is.True);
                var auth = new RewardAuthorization("ad-1", Placement.SPIN, f.Clock.UtcNow, "provider-proof"); var ads = s.PrepareAuthorizedSpin(0, auth);
                f.Store.FailKey = "reward-integrity"; f.Store.SkipMatches = 3;
                Assert.That(s.CompleteSpin(ads.SpinId).Success, Is.False); Assert.That(Session(f).Initialize(out _), Is.True);
                Assert.That(f.SpinStore.Load(out var state, out _), Is.True); Assert.That(state.adsSpinToday, Is.EqualTo(1)); Assert.That(state.spinOrdinal, Is.EqualTo(2));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(10));
            }
        }
        [Test] public void DayResetDoesNotLosePendingValueOrChargeNewAllowance()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var prepared = s.PrepareFreeSpin(0);
                f.Clock.UtcNow = f.Clock.UtcNow.AddDays(1); Assert.That(s.CompleteSpin(prepared.SpinId).Success, Is.True);
                Assert.That(s.Current.FreeAvailable, Is.True); Assert.That(s.Current.AdsSpinToday, Is.Zero);
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void LegacyPendingWithoutSnapshotIsPreservedAndUnavailable()
        {
            using (var f = new Bw4Fixture())
            {
                var old = new SpinWheelSaveData { pendingSpinId = "old-id", pendingPayloadFingerprint = "old-bytes", pendingSelectedIndex = 2, rewardDayKey = new RewardDayPolicy(0).Resolve(f.Clock.UtcNow).Value };
                Assert.That(f.Persistence.Save(Hung.Data.Persistence.PackageSaveDefinitions.SpinWheel(f.Codec, f.Protector), old).Success, Is.True);
                var next = Session(f); Assert.That(next.Initialize(out var code), Is.False); Assert.That(code, Is.EqualTo("LEGACY_PENDING_SNAPSHOT_REQUIRED"));
                Assert.That(f.SpinStore.Load(out var after, out _), Is.True); Assert.That(after.pendingSpinId, Is.EqualTo("old-id")); Assert.That(after.pendingPayloadFingerprint, Is.EqualTo("old-bytes")); Assert.That(f.Grant.Published, Is.Zero);
            }
        }
        [Test] public void AuthorizationAndProfileMismatchFailClosed()
        {
            using (var f = new Bw4Fixture())
            {
                var s = Session(f); Assert.That(s.Initialize(out _), Is.True); var free = s.PrepareFreeSpin(0);
                Assert.That(Session(f, profile: "profile-b").Initialize(out _), Is.False); Assert.That(f.Grant.Published, Is.Zero);
                Assert.That(s.CompleteSpin(free.SpinId).Success, Is.True);
                var invalid = new RewardAuthorization("a", Placement.DAILY_REWARD, f.Clock.UtcNow, "proof");
                Assert.That(s.PrepareAuthorizedSpin(0, invalid).Success, Is.False);
            }
        }
        [Test] public void VerifiedLegacySnapshotKeepsOriginalIdAndFingerprint()
        {
            using (var f = new Bw4Fixture())
            {
                int day = new RewardDayPolicy(0).Resolve(f.Clock.UtcNow).Value;
                string id = RewardClaimId.Create("spin-wheel", "free", day.ToString(System.Globalization.CultureInfo.InvariantCulture), "1", "free", "profile-a").Value;
                string fingerprint = "spin-wheel:" + day.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":free:0:fixture.coins:5:free";
                var old = new SpinWheelSaveData { rewardDayKey = day, pendingSpinId = id, pendingPayloadFingerprint = fingerprint };
                Assert.That(f.Persistence.Save(Hung.Data.Persistence.PackageSaveDefinitions.SpinWheel(f.Codec, f.Protector), old).Success, Is.True);
                var s = Session(f, 99);
                Assert.That(s.AdoptLegacyPending(new LegacySpinSnapshot(ItemId.Parse("fixture.coins"), 6, 0, false, "free", Bw4Fixture.Offer()), out _), Is.False);
                Assert.That(s.AdoptLegacyPending(new LegacySpinSnapshot(ItemId.Parse("fixture.coins"), 5, 0, false, "free", Bw4Fixture.Offer()), out _), Is.True);
                Assert.That(s.CompleteSpin(id).Success, Is.True);
                Assert.That(f.ReloadWallet().receipts[id], Is.EqualTo(fingerprint)); Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
    }
}
