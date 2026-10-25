using System;
using Hung.Base;
using NUnit.Framework;

namespace Hung.Base.Tests.Services
{
    public sealed class FocusedItemContractTests
    {
        private sealed class TestOwner : IAudioSettings, IItemBalanceReader, IItemSpendService
        {
            private readonly ItemId id = new ItemId("test.gold");
            public int Quantity = 5;
            public int LockQuantity = 2;
            public bool CommitSucceeds = true;
            public bool BgmMuted;
            public bool SfxMuted;
            public int SuccessfulEffects;
            public bool IsBgmMuted => BgmMuted;
            public bool IsSfxMuted => SfxMuted;

            public ItemBalanceStatus GetBalance(ItemId itemId, out ItemBalance balance)
            {
                balance = default;
                if (!itemId.IsValid) return ItemBalanceStatus.InvalidItem;
                if (itemId != id) return ItemBalanceStatus.UnknownItem;
                balance = new ItemBalance(id, Quantity, LockQuantity);
                return ItemBalanceStatus.Found;
            }

            public ItemSpendResult Spend(ItemId itemId, int quantity)
            {
                if (!itemId.IsValid || quantity <= 0)
                    return new ItemSpendResult(ItemSpendOutcome.InvalidRequest);
                if (itemId != id) return new ItemSpendResult(ItemSpendOutcome.UnknownItem);
                // Test product policy: locks reserve quantity; no generic contract mandates it.
                if (quantity > Quantity - LockQuantity)
                    return new ItemSpendResult(ItemSpendOutcome.InsufficientBalance);
                int candidate = checked(Quantity - quantity);
                if (!CommitSucceeds)
                    return new ItemSpendResult(ItemSpendOutcome.PersistenceFailure, "TEST_COMMIT_FAILED");
                Quantity = candidate;
                SuccessfulEffects++;
                return new ItemSpendResult(ItemSpendOutcome.Success);
            }
        }

        private sealed class QueryOnly : IItemBalanceReader
        {
            public ItemBalanceStatus GetBalance(ItemId id, out ItemBalance balance)
            {
                balance = default;
                return ItemBalanceStatus.Unavailable;
            }
        }

        [Test]
        public void DefaultResultsDoNotMeanSuccess()
        {
            Assert.That(default(ItemBalanceStatus), Is.EqualTo(ItemBalanceStatus.Unavailable));
            Assert.That(default(ItemSpendResult).Outcome, Is.EqualTo(ItemSpendOutcome.Unavailable));
        }

        [Test]
        public void SnapshotIsDetachedAndZeroIsFound()
        {
            var owner = new TestOwner();
            var id = new ItemId("test.gold");
            Assert.That(owner.GetBalance(id, out ItemBalance first), Is.EqualTo(ItemBalanceStatus.Found));
            owner.Quantity = 0;
            Assert.That(first.Quantity, Is.EqualTo(5));
            Assert.That(first.LockQuantity, Is.EqualTo(2));
            Assert.That(owner.GetBalance(id, out ItemBalance second), Is.EqualTo(ItemBalanceStatus.Found));
            Assert.That(second.Quantity, Is.Zero);
        }

        [Test]
        public void InvalidAndUnknownQueriesReturnDefault()
        {
            var owner = new TestOwner();
            Assert.That(owner.GetBalance(default, out ItemBalance invalid), Is.EqualTo(ItemBalanceStatus.InvalidItem));
            Assert.That(invalid.ItemId.IsValid, Is.False);
            Assert.That(owner.GetBalance(new ItemId("test.unknown"), out ItemBalance unknown), Is.EqualTo(ItemBalanceStatus.UnknownItem));
            Assert.That(unknown.Quantity, Is.Zero);
        }

        [Test]
        public void QueryOnlyDoesNotAdvertiseSpend()
        {
            object adapter = new QueryOnly();
            Assert.That(adapter, Is.Not.InstanceOf<IItemSpendService>());
        }

        [Test]
        public void SpendRejectsInvalidUnknownAndReservedBalance()
        {
            var owner = new TestOwner();
            var id = new ItemId("test.gold");
            Assert.That(owner.Spend(default, 1).Outcome, Is.EqualTo(ItemSpendOutcome.InvalidRequest));
            Assert.That(owner.Spend(id, 0).Outcome, Is.EqualTo(ItemSpendOutcome.InvalidRequest));
            Assert.That(owner.Spend(id, -1).Outcome, Is.EqualTo(ItemSpendOutcome.InvalidRequest));
            Assert.That(owner.Spend(new ItemId("test.unknown"), 1).Outcome, Is.EqualTo(ItemSpendOutcome.UnknownItem));
            Assert.That(owner.Spend(id, int.MaxValue).Outcome, Is.EqualTo(ItemSpendOutcome.InsufficientBalance));
            Assert.That(owner.Spend(id, 4).Outcome, Is.EqualTo(ItemSpendOutcome.InsufficientBalance));
            Assert.That(owner.Quantity, Is.EqualTo(5));
            Assert.That(owner.SuccessfulEffects, Is.Zero);
        }

        [Test]
        public void CommitPrecedesPublicationAndEffects()
        {
            var owner = new TestOwner { CommitSucceeds = false };
            var id = new ItemId("test.gold");
            Assert.That(owner.Spend(id, 2).Outcome, Is.EqualTo(ItemSpendOutcome.PersistenceFailure));
            Assert.That(owner.Quantity, Is.EqualTo(5));
            Assert.That(owner.SuccessfulEffects, Is.Zero);
            owner.CommitSucceeds = true;
            Assert.That(owner.Spend(id, 2).Outcome, Is.EqualTo(ItemSpendOutcome.Success));
            Assert.That(owner.Quantity, Is.EqualTo(3));
            Assert.That(owner.SuccessfulEffects, Is.EqualTo(1));
        }

        [Test]
        public void AudioSettingsAreLiveFocusedFacts()
        {
            var owner = new TestOwner();
            IAudioSettings settings = owner;
            owner.BgmMuted = true;
            owner.SfxMuted = true;
            Assert.That(settings.IsBgmMuted, Is.True);
            Assert.That(settings.IsSfxMuted, Is.True);
            Assert.That(typeof(IAudioSettings).GetProperties().Length, Is.EqualTo(2));
        }
    }
}
