using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hung.Base
{
    public sealed class UnsupportedPurchaseIntegrityService : IPurchaseIntegrityService
    {
        public const string UnsupportedCode = "PURCHASE_UNSUPPORTED";

        public PurchaseAvailability Availability => new PurchaseAvailability(PurchaseCapabilityState.Unsupported, UnsupportedCode);

        public event Action<PurchaseTransactionSnapshot> TransactionUpdated;

        public Task<PurchaseAvailability> ConnectAsync(CancellationToken token = default)
        {
            return Task.FromResult(Availability);
        }

        public Task<PurchaseRequestResult> PurchaseAsync(PurchaseProductId productId, CancellationToken token = default)
        {
            return Task.FromResult(new PurchaseRequestResult(PurchaseRequestStatus.Unsupported, UnsupportedCode));
        }

        public Task<PurchaseReconcileResult> ReconcileAsync(CancellationToken token = default)
        {
            return Task.FromResult(new PurchaseReconcileResult(PurchaseAggregateStatus.Unsupported, UnsupportedCode, Array.Empty<PurchaseTransactionSnapshot>()));
        }

        public Task<PurchaseRestoreResult> RestoreAsync(CancellationToken token = default)
        {
            return Task.FromResult(new PurchaseRestoreResult(PurchaseAggregateStatus.Unsupported, UnsupportedCode, Array.Empty<PurchaseTransactionSnapshot>()));
        }
    }
}
