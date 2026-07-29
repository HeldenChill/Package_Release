using System;

namespace Hung.Base
{
    [Obsolete("Use IPurchaseIntegrityService.")]
    public interface IIAPService
    {
        public void Purchase(IAP_ITEM item, Action onPuchaseCompleted, Action onPurchaseFail = null, Placement placement = Placement.NONE);
        public void Restore(Action onRestoreComplete, Action onRestoreFail);
    }
}


