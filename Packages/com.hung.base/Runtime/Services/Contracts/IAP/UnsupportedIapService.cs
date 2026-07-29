namespace Hung.Base
{
    public sealed class UnsupportedIapService : IIAPService
    {
        public void Purchase(IAP_ITEM item, System.Action onPuchaseCompleted, System.Action onPurchaseFail = null, Placement placement = Placement.NONE)
        {
            onPurchaseFail?.Invoke();
        }

        public void Restore(System.Action onRestoreComplete, System.Action onRestoreFail)
        {
            onRestoreFail?.Invoke();
        }
    }
}
