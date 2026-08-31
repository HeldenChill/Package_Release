using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Hung.Base;

namespace Hung.IAP.Tests
{
    public sealed class LegacyIapServiceAdapterTests
    {
        [Test]
        public void Purchase_CompletedResult_InvokesSuccess()
        {
            var integrity = new FakeIntegrityService(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED");
            var adapter = Adapter(integrity);
            bool success = false;
            bool failure = false;

            adapter.Purchase(IAP_ITEM.STARTER_PACK, () => success = true, () => failure = true);

            Assert.That(success, Is.True);
            Assert.That(failure, Is.False);
            Assert.That(integrity.LastProductId, Is.EqualTo(new PurchaseProductId("starter-pack")));
        }

        [TestCase(PurchaseRequestStatus.Unsupported)]
        [TestCase(PurchaseRequestStatus.Rejected)]
        [TestCase(PurchaseRequestStatus.Cancelled)]
        [TestCase(PurchaseRequestStatus.RetryableFailure)]
        [TestCase(PurchaseRequestStatus.Misconfigured)]
        public void Purchase_NonCompletedResult_InvokesFailure(PurchaseRequestStatus status)
        {
            var adapter = Adapter(new FakeIntegrityService(status, "not-complete"));
            bool success = false;
            bool failure = false;

            adapter.Purchase(IAP_ITEM.STARTER_PACK, () => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }

        [Test]
        public void Purchase_MissingLegacyMapping_InvokesFailureWithoutCallingIntegrity()
        {
            var integrity = new FakeIntegrityService(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED");
            var adapter = Adapter(integrity);
            bool success = false;
            bool failure = false;

            adapter.Purchase(IAP_ITEM.GOLD_PACK1, () => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
            Assert.That(integrity.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void Purchase_DebugBypassStillInvokesFailure()
        {
            var adapter = Adapter(new FakeIntegrityService(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED"), allowDebugBypass: true);
            bool success = false;
            bool failure = false;

            adapter.Purchase(IAP_ITEM.STARTER_PACK, () => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }

        [Test]
        public void Restore_SuccessOnlyWhenIntegrityRestoreSucceeds()
        {
            var integrity = new FakeIntegrityService(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED")
            {
                RestoreStatus = PurchaseAggregateStatus.Success
            };
            var adapter = Adapter(integrity);
            bool success = false;
            bool failure = false;

            adapter.Restore(() => success = true, () => failure = true);

            Assert.That(success, Is.True);
            Assert.That(failure, Is.False);
        }

        [Test]
        public void Restore_PartialOrFailureInvokesFailure()
        {
            var integrity = new FakeIntegrityService(PurchaseRequestStatus.Completed, "PURCHASE_COMPLETED")
            {
                RestoreStatus = PurchaseAggregateStatus.PartialSuccess
            };
            var adapter = Adapter(integrity);
            bool success = false;
            bool failure = false;

            adapter.Restore(() => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }

        private static LegacyIapServiceAdapter Adapter(FakeIntegrityService integrity, bool allowDebugBypass = false)
        {
            var map = new LegacyPurchaseProductMap(new[]
            {
                new LegacyPurchaseProductMap.Entry(IAP_ITEM.STARTER_PACK, new PurchaseProductId("starter-pack"))
            });

            return new LegacyIapServiceAdapter(integrity, map, allowDebugBypass);
        }

        private sealed class FakeIntegrityService : IPurchaseIntegrityService
        {
            private readonly PurchaseRequestStatus status;
            private readonly string code;

            public FakeIntegrityService(PurchaseRequestStatus status, string code)
            {
                this.status = status;
                this.code = code;
            }

            public int CallCount { get; private set; }
            public PurchaseProductId LastProductId { get; private set; }
            public PurchaseAggregateStatus RestoreStatus { get; set; } = PurchaseAggregateStatus.Failed;
            public PurchaseAvailability Availability => new PurchaseAvailability(PurchaseCapabilityState.Ready);
            public event Action<PurchaseTransactionSnapshot> TransactionUpdated;

            public Task<PurchaseAvailability> ConnectAsync(CancellationToken token = default) => Task.FromResult(Availability);

            public Task<PurchaseRequestResult> PurchaseAsync(PurchaseProductId productId, CancellationToken token = default)
            {
                CallCount++;
                LastProductId = productId;
                return Task.FromResult(new PurchaseRequestResult(status, code));
            }

            public Task<PurchaseReconcileResult> ReconcileAsync(CancellationToken token = default)
            {
                return Task.FromResult(new PurchaseReconcileResult(PurchaseAggregateStatus.NothingToProcess, null, Array.Empty<PurchaseTransactionSnapshot>()));
            }

            public Task<PurchaseRestoreResult> RestoreAsync(CancellationToken token = default)
            {
                return Task.FromResult(new PurchaseRestoreResult(RestoreStatus, "restore", Array.Empty<PurchaseTransactionSnapshot>()));
            }

            public void Raise(PurchaseTransactionSnapshot snapshot) => TransactionUpdated?.Invoke(snapshot);
        }
    }
}
