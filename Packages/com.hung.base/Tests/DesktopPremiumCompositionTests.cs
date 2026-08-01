using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class DesktopPremiumCompositionTests
    {
        [Test]
        public async Task BaseOnlyUnsupportedIntegrityService_NeverInitializesPurchasing()
        {
            var service = new UnsupportedPurchaseIntegrityService();
            bool eventRaised = false;
            service.TransactionUpdated += _ => eventRaised = true;

            PurchaseAvailability availability = await service.ConnectAsync();
            PurchaseRequestResult purchase = await service.PurchaseAsync(new PurchaseProductId("starter-pack"));
            PurchaseReconcileResult reconcile = await service.ReconcileAsync();
            PurchaseRestoreResult restore = await service.RestoreAsync();

            Assert.That(availability.State, Is.EqualTo(PurchaseCapabilityState.Unsupported));
            Assert.That(service.Availability.State, Is.EqualTo(PurchaseCapabilityState.Unsupported));
            Assert.That(purchase.Status, Is.EqualTo(PurchaseRequestStatus.Unsupported));
            Assert.That(reconcile.Status, Is.EqualTo(PurchaseAggregateStatus.Unsupported));
            Assert.That(restore.Status, Is.EqualTo(PurchaseAggregateStatus.Unsupported));
            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public void BasePackageManifest_DoesNotDependOnUnityPurchasing()
        {
            string manifest = File.ReadAllText("Packages/com.hung.base/package.json");

            Assert.That(manifest, Does.Not.Contain("com.unity.purchasing"));
            Assert.That(manifest, Does.Not.Contain("Unity.Purchasing"));
        }

        [Test]
        public void LegacyUnsupportedIapService_FailsDeterministically()
        {
            bool success = false;
            bool failure = false;

            new UnsupportedIapService().Purchase(IAP_ITEM.STARTER_PACK, () => success = true, () => failure = true);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.True);
        }
    }
}
