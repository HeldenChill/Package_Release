using System.Threading.Tasks;

namespace Hung.UI
{
    /// <summary>Optional addressed canvas acquisition contract with typed failure reporting.</summary>
    public interface IUIAcquisitionService
    {
        /// <summary>Acquire and cache a canvas by an explicit address.</summary>
        Task<UIAcquireResult<T>> AcquireAsync<T>(string address) where T : UICanvas;
    }
}
