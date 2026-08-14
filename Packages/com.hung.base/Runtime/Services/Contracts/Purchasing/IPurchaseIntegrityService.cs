using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hung.Base
{
    public interface IPurchaseIntegrityService
    {
        PurchaseAvailability Availability { get; }

        event Action<PurchaseTransactionSnapshot> TransactionUpdated;

        Task<PurchaseAvailability> ConnectAsync(CancellationToken token = default);

        Task<PurchaseRequestResult> PurchaseAsync(PurchaseProductId productId, CancellationToken token = default);

        Task<PurchaseReconcileResult> ReconcileAsync(CancellationToken token = default);

        Task<PurchaseRestoreResult> RestoreAsync(CancellationToken token = default);
    }
}
