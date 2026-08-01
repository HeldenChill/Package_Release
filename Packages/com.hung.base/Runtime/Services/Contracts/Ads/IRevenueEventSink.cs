using System.Collections.Generic;

namespace Hung.Base
{
    // Ph6 (paper §11.2): ads reports revenue through this sink instead of
    // referencing an attribution/analytics SDK directly. `extra` carries the
    // vendor-supplied enrichment fields (country/ad unit/ad type/placement)
    // that the previous direct AppsFlyer.logAdRevenue call passed as
    // additionalParams - optional so callers with no enrichment data can omit it.
    public interface IRevenueEventSink
    {
        void OnRevenue(string source, double value, string currency, IReadOnlyDictionary<string, string> extra = null);
    }
}
