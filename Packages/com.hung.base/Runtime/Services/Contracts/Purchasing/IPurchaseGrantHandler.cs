using System.Threading;
using System.Threading.Tasks;

namespace Hung.Base
{
    public interface IPurchaseGrantHandler
    {
        Task<PurchaseGrantStatus> GrantAsync(PurchaseGrantRequest request, CancellationToken token = default);
    }
}
