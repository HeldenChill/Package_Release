using System.Collections.Generic;
using System.Linq;
using Hung.Base;

public static class RewardFingerprint
{
    public static string NormalizeItems(IReadOnlyList<RewardGrantItem> items)
    {
        return string.Join("|", items
            .OrderBy(x => x.ItemId.Value, System.StringComparer.Ordinal)
            .Select(x => x.ItemId.Value + ":" + x.Quantity));
    }
}
