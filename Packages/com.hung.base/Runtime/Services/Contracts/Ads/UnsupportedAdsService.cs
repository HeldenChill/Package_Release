using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
    /// <summary>Absent Ads plugin: no rewards, truthful Unsupported outcomes and finite continuations.</summary>
    public sealed class UnsupportedAdsService : IAdsService
    {
        private readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);
        private readonly Format appOpen;
        private readonly Format banner;
        private readonly RewardFormat reward;
        private readonly InterFormat inter;

        /// <summary>Creates defaults with warning state owned by this session.</summary>
        public UnsupportedAdsService()
        {
            appOpen = new Format(this, "AppOpen");
            banner = new Format(this, "Banner");
            reward = new RewardFormat(this);
            inter = new InterFormat(this);
        }
        /// <inheritdoc/>
        public IAds AppOpen => appOpen;
        /// <inheritdoc/>
        public IAds Banner => banner;
        /// <inheritdoc/>
        public IRewardAds Reward => reward;
        /// <inheritdoc/>
        public IInterAds Inter => inter;
        /// <inheritdoc/>
        public ADS_TYPE Type { get => default; set => Warn("Type"); }
        /// <inheritdoc/>
        public void ShowRewarded(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            Warn("ShowRewarded");
            onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Unsupported,
                false, "ADS_PLUGIN_ABSENT"));
        }
        /// <inheritdoc/>
        public void ShowInterstitial(AdsShowRequest request, Action<AdsShowResult> onCompleted)
        {
            Warn("ShowInterstitial");
            onCompleted?.Invoke(new AdsShowResult(request.RequestId, AdsRequestOutcome.Unsupported,
                false, "ADS_PLUGIN_ABSENT"));
        }
        private void Warn(string member)
        {
            if (warned.Add(member)) Debug.LogWarning("[Unsupported] IAdsService." + member + " called; no ads plugin installed.");
        }
        private class Format : IAds
        {
            protected readonly UnsupportedAdsService Owner;
            private readonly string name;
            internal Format(UnsupportedAdsService owner, string name) { Owner = owner; this.name = name; }
            public ADS_TYPE Type { get => default; set => Owner.Warn(name + ".Type"); }
            public void Show() => Owner.Warn(name + ".Show");
            public void Hide() => Owner.Warn(name + ".Hide");
            public void Load() => Owner.Warn(name + ".Load");
        }
        private sealed class RewardFormat : Format, IRewardAds
        {
            internal RewardFormat(UnsupportedAdsService owner) : base(owner, "Reward") { }
            public Action _OnTriggerLoadAds => () => { };
            public Action<Action> _OnAddLoadAds => _ => { };
            public bool IsCanShowFor(Placement placement) => false;
            public void Show(Action rewardCallBack, Action hiddenCallBack = null, Placement placement = Placement.NONE)
            {
                Owner.Warn("Reward.Show");
                hiddenCallBack?.Invoke();
            }
        }
        private sealed class InterFormat : Format, IInterAds
        {
            internal InterFormat(UnsupportedAdsService owner) : base(owner, "Inter") { }
            public Action _OnTriggerLoadAds => () => { };
            public Action<Action> _OnAddLoadAds => _ => { };
            public void Show(Action callback, Placement placement)
            {
                Owner.Warn("Inter.Show");
                callback?.Invoke();
            }
        }
    }
}
