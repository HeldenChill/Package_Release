using System;

namespace Hung.IAP
{
    using Hung.Base;

    // Ph6 test double (paper §17.7, §9.13-2): all-no-op IIAPService. Every
    // purchase/restore call fires the failure path - deliberately never
    // fires onPuchaseCompleted, so a test or vendor-free build can't be
    // silently misread as having actually granted an item. See the
    // package README's transaction-state section for why a false success
    // here would be worse than a false failure.
    public class NullIapService : IIAPService
    {
        public void Purchase(IAP_ITEM item, Action onPuchaseCompleted, Action onPurchaseFail = null, Placement placement = Placement.NONE)
            => onPurchaseFail?.Invoke();

        public void Restore(Action onRestoreComplete, Action onRestoreFail)
            => onRestoreFail?.Invoke();
    }
}
