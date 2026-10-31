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
            // An unconfigured manager does not claim the slot; Base supplies Unsupported.
        }

        public void ConfigureLegacyFacade(IPurchaseIntegrityService integrityService, LegacyPurchaseProductMap productMap)
        {
            if (ServiceSlots.TryGet<IIAPService>(out var current) && !ReferenceEquals(current, legacyFacade))
                throw new InvalidOperationException("IAP slot is already owned by another installation.");
            var candidate = new LegacyIapServiceAdapter(integrityService, productMap, debugBypassFailsClosed);
            var result = ServiceSlots.Register<IIAPService>(candidate);
            if (result == ServiceRegistration.RefusedDuplicateOwner || result == ServiceRegistration.RefusedNull)
                throw new InvalidOperationException("IAP slot registration was refused.");
            legacyFacade = candidate;
        }

        private void OnDestroy()
        {
            if (legacyFacade != null) ServiceSlots.Unregister<IIAPService>(legacyFacade);
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
