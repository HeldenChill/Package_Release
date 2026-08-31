using System;
using Hung.Base;

namespace Hung.IAP
{
    public sealed class LegacyIapServiceAdapter : IIAPService
    {
        private readonly IPurchaseIntegrityService integrityService;
        private readonly LegacyPurchaseProductMap productMap;
        private readonly bool allowDebugBypass;

        public LegacyIapServiceAdapter(
            IPurchaseIntegrityService integrityService,
            LegacyPurchaseProductMap productMap,
            bool allowDebugBypass = false)
        {
            this.integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
            this.productMap = productMap ?? throw new ArgumentNullException(nameof(productMap));
            this.allowDebugBypass = allowDebugBypass;
        }

        public async void Purchase(IAP_ITEM item, Action onPuchaseCompleted, Action onPurchaseFail = null, Placement placement = Placement.NONE)
        {
            if (allowDebugBypass)
            {
                onPurchaseFail?.Invoke();
                return;
            }

            if (!productMap.TryGet(item, out PurchaseProductId productId))
            {
                onPurchaseFail?.Invoke();
                return;
            }

            try
            {
                PurchaseRequestResult result = await integrityService.PurchaseAsync(productId);
                if (result.Status == PurchaseRequestStatus.Completed && string.Equals(result.Code, "PURCHASE_COMPLETED", StringComparison.Ordinal))
                    onPuchaseCompleted?.Invoke();
                else
                    onPurchaseFail?.Invoke();
            }
            catch (Exception)
            {
                onPurchaseFail?.Invoke();
            }
        }

        public async void Restore(Action onRestoreComplete, Action onRestoreFail)
        {
            try
            {
                PurchaseRestoreResult result = await integrityService.RestoreAsync();
                if (result.Status == PurchaseAggregateStatus.Success)
                    onRestoreComplete?.Invoke();
                else
                    onRestoreFail?.Invoke();
            }
            catch (Exception)
            {
                onRestoreFail?.Invoke();
            }
        }
    }
}
