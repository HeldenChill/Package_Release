using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Hung.Analytics.Tests
{
    public class CoreHelpersTests
    {
        [TestCase("level_5_pass", 40, "level_5_pass")]
        [TestCase("Reward show-action", 40, "Reward_show_action")]
        [TestCase("1st_win", 40, "e_1st_win")]
        [TestCase("abcdefghij", 4, "abcd")]
        [TestCase("", 40, "unnamed")]
        [TestCase(null, 40, "unnamed")]
        public void Sanitize_ProducesFirebaseSafeName(string input, int max, string expected)
        {
            Assert.AreEqual(expected, AnalyticsText.Sanitize(input, max));
        }

        [Test]
        public void ToStringDictionary_UsesInvariantCulture_OnCommaDecimalLocale()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
            try
            {
                var result = AnalyticsText.ToStringDictionary(new Dictionary<string, object>
                {
                    { "value", 0.05 }, { "count", 3 }, { "ok", true }, { "name", "gold" }
                });
                Assert.AreEqual("0.05", result["value"]);
                Assert.AreEqual("3", result["count"]);
                Assert.AreEqual("true", result["ok"]);
                Assert.AreEqual("gold", result["name"]);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ToStringDictionary_Null_ReturnsNull()
        {
            Assert.IsNull(AnalyticsText.ToStringDictionary(null));
        }

        [Test]
        public void ToJson_WritesNumbersRaw_EscapesStrings_QuotesNonFinite()
        {
            var json = AnalyticsText.ToJson(new Dictionary<string, object>
            {
                { "placement", "Home \"A\"" }, { "value", 1.5 }, { "n", 2 }, { "bad", double.NaN }, { "flag", false }
            });
            Assert.AreEqual("{\"placement\":\"Home \\\"A\\\"\",\"value\":1.5,\"n\":2,\"bad\":\"NaN\",\"flag\":false}", json);
        }

        [Test]
        public void ToJson_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(AnalyticsText.ToJson(null));
            Assert.IsNull(AnalyticsText.ToJson(new Dictionary<string, object>()));
        }

        [Test]
        public void ApplyDefines_ReplacesOnlyAnalyticsDefines_KeepsOrder()
        {
            var result = AnalyticsBackendIds.ApplyDefines(
                new[] { "DOTWEEN", "HUNG_ANALYTICS_FIREBASE", "HUNG_ADS_MAX", "" },
                new[] { AnalyticsBackendIds.AppsFlyer, AnalyticsBackendIds.GameAnalytics });
            CollectionAssert.AreEqual(
                new[] { "DOTWEEN", "HUNG_ADS_MAX", "HUNG_ANALYTICS_APPSFLYER", "HUNG_ANALYTICS_GAMEANALYTICS" }, result);
        }

        [Test]
        public void ApplyDefines_NoneEnabled_RemovesAllAnalyticsDefines()
        {
            var result = AnalyticsBackendIds.ApplyDefines(
                new[] { "HUNG_ANALYTICS_FIREBASE", "APPMETRICA_FEATURES_ADREVENUE_APPLOVIN_V8" }, new string[0]);
            CollectionAssert.AreEqual(new[] { "APPMETRICA_FEATURES_ADREVENUE_APPLOVIN_V8" }, result);
        }

        [Test]
        public void CreateDefault_AppMetrica_ExcludesRevenue_ToAvoidDoubleCountingWithAutoAdapters()
        {
            var entry = AnalyticsBackendEntry.CreateDefault(AnalyticsBackendIds.AppMetrica);
            Assert.AreEqual(Hung.Base.AnalyticsCategory.Product, entry.categories);
        }
    }
}
