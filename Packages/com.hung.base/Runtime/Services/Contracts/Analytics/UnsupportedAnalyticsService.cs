using System.Collections.Generic;

namespace Hung.Base
{
    /// <summary>
    /// Absent Analytics plugin fallback. Silent no-op: analytics is optional and called on hot paths
    /// (ads callbacks), so a missing plugin must never throw or log per call.
    /// </summary>
    public sealed class UnsupportedAnalyticsService : IAnalyticsService
    {
        /// <inheritdoc/>
        public void LogEvent(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters = null) { }
        /// <inheritdoc/>
        public void SetUserProperty(string key, string value) { }
        /// <inheritdoc/>
        public void AdsRewardOffer(Placement place) { }
        /// <inheritdoc/>
        public void AdsRewardClick(Placement place) { }
        /// <inheritdoc/>
        public void AdsRewardShow(Placement place) { }
        /// <inheritdoc/>
        public void AdsRewardShowFail(Placement place, string error) { }
        /// <inheritdoc/>
        public void AdsRewardComplete(Placement place, string type) { }
        /// <inheritdoc/>
        public void AdsRewardLoadComplete() { }
        /// <inheritdoc/>
        public void AdsRewardLoad() { }
        /// <inheritdoc/>
        public void AdsRewardLoadFail() { }
        /// <inheritdoc/>
        public void AdsInterFail(string error) { }
        /// <inheritdoc/>
        public void AdsInterLoad() { }
        /// <inheritdoc/>
        public void AdsInterShow(Placement place) { }
        /// <inheritdoc/>
        public void AdsInterClick() { }
        /// <inheritdoc/>
        public void AdsInterLoadComplete() { }
        /// <inheritdoc/>
        public void AdsInterComplete() { }
        /// <inheritdoc/>
        public void BuyIAPComplete(IAP_ITEM item, Placement placement) { }
        /// <inheritdoc/>
        public void TutorialStep(string name, int step) { }
        /// <inheritdoc/>
        public void LevelTrackEvent(LEVEL_STATE state, int level, int stars, bool firstPass, bool isFtu) { }
        /// <inheritdoc/>
        public void EarnVirtualCurrency(string name, long value, string source) { }
        /// <inheritdoc/>
        public void SpendVirtualCurrency(string name, long value, string itemName) { }
    }
}
