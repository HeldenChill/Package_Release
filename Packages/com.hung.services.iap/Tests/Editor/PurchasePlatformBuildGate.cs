using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Hung.IAP.EditorTests
{
    public sealed class PurchasePlatformBuildGateTests
    {
        [TestCase(BuildTarget.Android, true)]
        [TestCase(BuildTarget.iOS, true)]
        [TestCase(BuildTarget.StandaloneWindows64, false)]
        [TestCase(BuildTarget.StandaloneWindows, false)]
        public void IsMobilePurchaseTarget_ReturnsExpectedPolicy(BuildTarget target, bool expected)
        {
            Assert.That(PurchasePlatformBuildGate.IsMobilePurchaseTarget(target), Is.EqualTo(expected));
        }

        [Test]
        public void DesktopPremiumGate_RequiresBaseButNotIapPackage()
        {
            string baseManifest = File.ReadAllText("Packages/com.hung.base/package.json");
            string iapManifest = File.ReadAllText("Packages/com.hung.services.iap/package.json");

            Assert.That(baseManifest, Does.Not.Contain("com.unity.purchasing"));
            Assert.That(iapManifest, Does.Contain("com.unity.purchasing"));
            Assert.That(PurchasePlatformBuildGate.IsMobilePurchaseTarget(BuildTarget.StandaloneWindows64), Is.False);
        }

        [Test]
        public void MobileF2PGate_RequiresIapPackageAndUnityPurchasing()
        {
            string iapManifest = File.ReadAllText("Packages/com.hung.services.iap/package.json");

            Assert.That(PurchasePlatformBuildGate.IsMobilePurchaseTarget(BuildTarget.Android), Is.True);
            Assert.That(PurchasePlatformBuildGate.IsMobilePurchaseTarget(BuildTarget.iOS), Is.True);
            Assert.That(iapManifest, Does.Contain("\"com.hung.base\""));
            Assert.That(iapManifest, Does.Contain("\"com.hung.data\""));
            Assert.That(iapManifest, Does.Contain("\"com.unity.purchasing\""));
        }
    }

    public static class PurchasePlatformBuildGate
    {
        public static bool IsMobilePurchaseTarget(BuildTarget target)
        {
            return target == BuildTarget.Android || target == BuildTarget.iOS;
        }
    }
}
