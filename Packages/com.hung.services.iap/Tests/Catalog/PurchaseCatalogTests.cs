using System;
using NUnit.Framework;
using Hung.Base;

namespace Hung.IAP.Tests
{
    public sealed class PurchaseCatalogTests
    {
        [Test]
        public void Create_RejectsDuplicateLogicalIds()
        {
            var first = Entry("starter-pack", googlePlayId: "starter_google");
            var second = Entry("starter-pack", appleAppStoreId: "starter_apple");

            Assert.Throws<InvalidOperationException>(() => new PurchaseCatalog(new[] { first, second }));
        }

        [Test]
        public void Create_RejectsDuplicateStoreIdsWithinSameStore()
        {
            var first = Entry("starter-pack", googlePlayId: "same_google");
            var second = Entry("gold-pack", googlePlayId: "same_google");

            Assert.Throws<InvalidOperationException>(() => new PurchaseCatalog(new[] { first, second }));
        }

        [Test]
        public void Create_AllowsSameIdAcrossDifferentStores()
        {
            var first = Entry("starter-pack", googlePlayId: "same_store_id");
            var second = Entry("gold-pack", appleAppStoreId: "same_store_id");

            Assert.DoesNotThrow(() => new PurchaseCatalog(new[] { first, second }));
        }

        [Test]
        public void TryGet_ReturnsFalseForDisabledProducts()
        {
            var catalog = new PurchaseCatalog(new[] { Entry("starter-pack", enabled: false) });

            Assert.That(catalog.TryGet(new PurchaseProductId("starter-pack"), out _), Is.False);
        }

        [Test]
        public void TryResolveStoreId_UsesStoreNameAndIgnoresDisabledProducts()
        {
            var catalog = new PurchaseCatalog(new[]
            {
                Entry("starter-pack", googlePlayId: "starter_google", appleAppStoreId: "starter_apple"),
                Entry("disabled-pack", googlePlayId: "disabled_google", enabled: false)
            });

            Assert.That(catalog.TryResolveStoreId(PurchaseStoreNames.GooglePlay, "starter_google", out var google), Is.True);
            Assert.That(google.ProductId, Is.EqualTo(new PurchaseProductId("starter-pack")));
            Assert.That(catalog.TryResolveStoreId(PurchaseStoreNames.AppleAppStore, "starter_apple", out var apple), Is.True);
            Assert.That(apple.ProductId, Is.EqualTo(new PurchaseProductId("starter-pack")));
            Assert.That(catalog.TryResolveStoreId(PurchaseStoreNames.GooglePlay, "disabled_google", out _), Is.False);
        }

        [Test]
        public void Create_RejectsSubscriptionsUntilSupported()
        {
            var entry = Entry("vip-sub", type: PurchaseProductType.Subscription);

            Assert.Throws<NotSupportedException>(() => new PurchaseCatalog(new[] { entry }));
        }

        [Test]
        public void LegacyMap_RequiresExplicitEntries()
        {
            var map = new LegacyPurchaseProductMap(new[]
            {
                new LegacyPurchaseProductMap.Entry(IAP_ITEM.STARTER_PACK, new PurchaseProductId("starter-pack"))
            });

            Assert.That(map.TryGet(IAP_ITEM.STARTER_PACK, out var id), Is.True);
            Assert.That(id, Is.EqualTo(new PurchaseProductId("starter-pack")));
            Assert.That(map.TryGet(IAP_ITEM.GOLD_PACK1, out _), Is.False);
        }

        [Test]
        public void LegacyMap_RejectsConflictingEntries()
        {
            var entries = new[]
            {
                new LegacyPurchaseProductMap.Entry(IAP_ITEM.STARTER_PACK, new PurchaseProductId("starter-pack")),
                new LegacyPurchaseProductMap.Entry(IAP_ITEM.STARTER_PACK, new PurchaseProductId("other-pack"))
            };

            Assert.Throws<InvalidOperationException>(() => new LegacyPurchaseProductMap(entries));
        }

        private static PurchaseCatalogEntry Entry(
            string productId,
            PurchaseProductType type = PurchaseProductType.Consumable,
            string googlePlayId = null,
            string appleAppStoreId = null,
            string editorTestId = null,
            bool enabled = true,
            int catalogVersion = 1)
        {
            return new PurchaseCatalogEntry(
                new PurchaseProductId(productId),
                type,
                googlePlayId,
                appleAppStoreId,
                editorTestId,
                enabled,
                catalogVersion);
        }
    }
}
