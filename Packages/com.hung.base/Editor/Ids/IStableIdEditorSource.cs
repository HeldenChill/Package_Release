using System.Collections.Generic;
using Hung.Base;

namespace Hung.Base.Editor
{
    /// <summary>Source provider for options displayed in a typed stable identifier dropdown.</summary>
    /// <typeparam name="TId">The identifier struct type.</typeparam>
    public interface IStableIdEditorSource<TId> where TId : struct, IStableId
    {
        /// <summary>Gets the available identifier editor options provided by this source.</summary>
        /// <returns>A sequence of editor options.</returns>
        IEnumerable<StableIdEditorOption<TId>> GetOptions();
    }
}
