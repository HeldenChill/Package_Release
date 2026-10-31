using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
namespace Hung.Data.Tests.Bw4
{
    public sealed class RewardCoordinatorSafetyTests
    {
        [Test] public void SameFingerprintChangedPayloadConflicts()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.Coordinator.Claim(f.Request()).Success, Is.True);
                Assert.That(f.Coordinator.Claim(f.Request(amount: 9)).Outcome, Is.EqualTo(RewardGrantOutcome.Conflict));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void RequestCopiesInputAndRejectsDefault()
        {
            var items = new List<RewardGrantItem> { new RewardGrantItem(ItemId.Parse("fixture.coins"), 5) };
            var request = new RewardClaimRequest(new RewardClaimId("claim"), "f", items, "fp"); items.Clear();
            Assert.That(request.Items.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new RewardClaimRequest(default, "f", request.Items, "fp"));
            Assert.Throws<ArgumentException>(() => new RewardClaimRequest(new RewardClaimId("c"), "f", new[] { default(RewardGrantItem) }, "fp"));
        }
        [Test] public void RecoverySaveFailureIsNotReportedRecovered()
        {
            using (var f = new Bw4Fixture())
            {
                f.Grant.Unavailable = true; Assert.That(f.Coordinator.Claim(f.Request()).Success, Is.False);
                f.Grant.Unavailable = false; f.Store.FailKey = "reward-integrity";
                var report = f.Coordinator.RecoverPending();
                Assert.That(report.RecoveredCount, Is.Zero); Assert.That(report.FailedCount, Is.EqualTo(1));
                Assert.That(f.Coordinator.RecoverPending().FailedCount, Is.Zero);
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void ReceiptSurvivesCoordinatorFailureAndRestart()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.Persistence.Save(PackageSaveDefinitions.RewardIntegrity(f.Codec, f.Protector), RewardIntegrityStateData.CreateDefault()).Success, Is.True);
                f.Store.FailKey = "reward-integrity"; f.Store.SkipMatches = 2;
                Assert.That(f.Coordinator.Claim(f.Request()).Success, Is.False);
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
                var result = f.MakeCoordinator().Claim(f.Request()); Assert.That(result.Success, Is.True);
                Assert.That(f.ReloadWallet().receipts.Count, Is.EqualTo(1)); Assert.That(f.Grant.Published, Is.EqualTo(1));
            }
        }
        [Test] public void RealPromotionFailureRetainsBackupAndRetry()
        {
            using (var f = new Bw4Fixture())
            {
                Assert.That(f.Persistence.Save(f.WalletDefinition, new Wallet()).Success, Is.True);
                f.Files.FailPromotionKey = "fixture-wallet";
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), f.Request().Items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.PersistenceFailure));
                Assert.That(f.Grant.Published, Is.Zero);
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(f.Root, "backup", "fixture-wallet.save")), Is.True);
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), f.Request().Items, "fp").SuccessOutcome(), Is.True);
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.EqualTo(5));
            }
        }
        [Test] public void MixedUnknownGrantDoesNotPublishOrCommit()
        {
            using (var f = new Bw4Fixture())
            {
                var items = new[] { new RewardGrantItem(ItemId.Parse("fixture.coins"), 5), new RewardGrantItem(ItemId.Parse("fixture.unknown"), 1) };
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.InvalidReward));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.Zero); Assert.That(f.Grant.Published, Is.Zero);
            }
        }
        [Test] public void UnknownSavedRowsAreRetainedAndWalletSaveFailurePublishesNothing()
        {
            using (var f = new Bw4Fixture())
            {
                var state = new Wallet(); state.balances.Add("retired.item", 17);
                Assert.That(f.Persistence.Save(f.WalletDefinition, state).Success, Is.True);
                f.Store.FailKey = "fixture-wallet";
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), f.Request().Items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.PersistenceFailure));
                Assert.That(f.ReloadWallet().balances["fixture.coins"], Is.Zero); Assert.That(f.Grant.Published, Is.Zero);
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), f.Request().Items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.Success));
                Assert.That(f.ReloadWallet().balances["retired.item"], Is.EqualTo(17));
                Assert.That(f.Grant.Grant(new RewardClaimId("c"), f.Request().Items, "fp").Outcome, Is.EqualTo(RewardGrantOutcome.IdempotentReplay));
                Assert.That(f.ReloadWallet().receipts["c"], Is.EqualTo("fp")); Assert.That(f.Grant.Published, Is.EqualTo(1));
            }
        }
    }
    internal static class GrantAssertions
    { internal static bool SuccessOutcome(this RewardGrantResult result) => result.Outcome == RewardGrantOutcome.Success || result.Outcome == RewardGrantOutcome.IdempotentReplay; }
}
