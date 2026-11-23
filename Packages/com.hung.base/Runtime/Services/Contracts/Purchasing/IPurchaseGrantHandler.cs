using System.Threading;
using System.Threading.Tasks;

namespace Hung.Base
{
    /// <summary>
    /// Applies the goods of a validated purchase. Called by the IAP integrity service on the thread that
    /// started the purchase (the Unity main thread for game code).
    /// </summary>
    public interface IPurchaseGrantHandler
    {
        /// <summary>
        /// Grants <paramref name="request"/>. Must be idempotent per <see cref="PurchaseGrantRequest.TransactionId"/>:
        /// a transaction already granted returns <see cref="PurchaseGrantStatus.AlreadyGranted"/> and changes nothing.
        /// The service also skips transactions its ledger already marks granted, but a crash between the grant and
        /// the ledger write can still replay one.
        /// </summary>
        Task<PurchaseGrantStatus> GrantAsync(PurchaseGrantRequest request, CancellationToken token = default);
    }
}
