using System.Collections.Generic;

namespace Hung.Base.Editor
{
    public interface IItemIdEditorSource
    {
        IEnumerable<ItemIdEditorOption> GetOptions();
    }
}
