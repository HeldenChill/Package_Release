using System;
using Hung.Base;
using Hung.DesignPattern;
using UnityEngine;

namespace Hung.IAP
{
    public class IAPManager : Singleton<IAPManager>
    {
        [SerializeField]
        private bool debugBypassFailsClosed;

        private IIAPService legacyFacade;

        protected void Awake()
        {
            Locator.IAPService = legacyFacade ?? new NullIapService();
        }

        public void ConfigureLegacyFacade(IPurchaseIntegrityService integrityService, LegacyPurchaseProductMap productMap)
        {
            legacyFacade = new LegacyIapServiceAdapter(integrityService, productMap, debugBypassFailsClosed);
            Locator.IAPService = legacyFacade;
        }

        [Obsolete("Use ConfigureLegacyFacade with IPurchaseIntegrityService composition.")]
        public void Purchase(IAP_ITEM item, Action onPuchaseCompleted, Action onPurchaseFail = null, Placement placement = Placement.NONE)
        {
            (legacyFacade ?? Locator.IAPService ?? new NullIapService()).Purchase(item, onPuchaseCompleted, onPurchaseFail, placement);
        }

        [Obsolete("Use ConfigureLegacyFacade with IPurchaseIntegrityService composition.")]
        public void Restore(Action onRestoreComplete, Action onRestoreFail)
        {
            (legacyFacade ?? Locator.IAPService ?? new NullIapService()).Restore(onRestoreComplete, onRestoreFail);
        }
    }
}
