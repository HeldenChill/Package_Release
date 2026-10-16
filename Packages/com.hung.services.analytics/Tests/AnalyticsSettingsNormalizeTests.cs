using Hung.Base;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Analytics.Tests
{
    public class AnalyticsSettingsNormalizeTests
    {
        [Test]
        public void NormalizeCategories_ClearsUndefinedBits_AndKeepsDefinedOnes()
        {
            var settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
            try
            {
                settings.backends.Clear();
                settings.backends.Add(new AnalyticsBackendEntry("everything", (AnalyticsCategory)(-1)));
                settings.backends.Add(new AnalyticsBackendEntry("adsrevenue", AnalyticsCategory.Ads | AnalyticsCategory.Revenue));
                settings.backends.Add(new AnalyticsBackendEntry("stray", (AnalyticsCategory)(2 | 64)));
                settings.backends.Add(null);

                Assert.IsTrue(settings.NormalizeCategories());

                Assert.AreEqual(AnalyticsCategory.All, settings.backends[0].categories);
                Assert.AreEqual(AnalyticsCategory.Ads | AnalyticsCategory.Revenue, settings.backends[1].categories);
                Assert.AreEqual(AnalyticsCategory.Product, settings.backends[2].categories);
                Assert.IsFalse(settings.NormalizeCategories(), "Second pass has nothing left to change.");
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
