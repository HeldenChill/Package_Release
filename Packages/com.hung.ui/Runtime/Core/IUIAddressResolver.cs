using System;

namespace Hung.UI
{
    /// <summary>Supplies product-owned prefab addresses for callback-based UI loading.</summary>
    public interface IUIAddressResolver
    {
        /// <summary>Return the address for a concrete canvas type.</summary>
        string GetAddress(Type canvasType);
    }
}
