using System;
using System.Globalization;
using System.Linq;
using Hung.Base;
using Hung.Data.Rewards;
using Hung.Data;
namespace Hung.LiveOps.SpinWheel
{
    public sealed class SpinWheelSession : ISpinWheelService, IDisposable
    {
        private readonly RewardStateStore<SpinWheelSaveData> store;
        private readonly IRewardClaimCoordinator coordinator;
        private readonly IClock clock;
        private readonly RewardDayPolicy days;
        private readonly string profile;
        private readonly ResolvedRewardOffer[] free, ads;
        private readonly int adsLimit;
        private bool stopped, busy;
        public SpinWheelSession(RewardStateStore<SpinWheelSaveData> store, IRewardClaimCoordinator coordinator,
            IClock clock, RewardDayPolicy days, string profile, ResolvedRewardOffer[] free,
            ResolvedRewardOffer[] ads, int adsLimit)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); this.days = days;
            if (string.IsNullOrWhiteSpace(profile) || free == null || free.Length == 0 || ads == null
                || free.Any(x => x == null) || ads.Any(x => x == null) || adsLimit < 0 || (adsLimit > 0 && ads.Length == 0))
                throw new ArgumentException("Invalid wheel configuration.");
            this.profile = profile; this.free = (ResolvedRewardOffer[])free.Clone(); this.ads = (ResolvedRewardOffer[])ads.Clone(); this.adsLimit = adsLimit;
        }
        private DateTime Now { get { var now = clock.UtcNow; if (now.Kind != DateTimeKind.Utc) throw new InvalidOperationException("Clock must return UTC."); return now; } }
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        public bool AdoptLegacyPending(LegacySpinSnapshot snapshot, out string code)
        {
            code = "LEGACY_PENDING_SNAPSHOT_REQUIRED";
            if (stopped || busy || snapshot == null || snapshot.Offer == null || string.IsNullOrWhiteSpace(snapshot.Authorization)) return false;
            if (!store.Load(out var state, out code)) return false;
            if (state.bw4Pending != null || string.IsNullOrWhiteSpace(state.pendingSpinId)) { code = "LEGACY_PENDING_NOT_APPLICABLE"; return false; }
            string mode = snapshot.IsAds ? "ads" : "free";
            int ordinal = checked(state.spinOrdinal + 1);
            string expected = "spin-wheel:" + N(state.rewardDayKey) + ":" + mode + ":" + N(snapshot.Index)
                + ":" + snapshot.LegacyItem.Value + ":" + N(snapshot.LegacyQuantity) + ":" + snapshot.Authorization;
            var id = RewardClaimId.Create("spin-wheel", mode, N(state.rewardDayKey), N(ordinal), snapshot.Authorization, profile);
            if (!snapshot.LegacyItem.IsValid || snapshot.LegacyQuantity <= 0 || snapshot.Index != state.pendingSelectedIndex
                || snapshot.IsAds != state.pendingIsAds || expected != state.pendingPayloadFingerprint || id.Value != state.pendingSpinId)
            { code = "LEGACY_PENDING_SNAPSHOT_MISMATCH"; return false; }
            state.bw4Pending = PendingRewardData.Create(profile, "spin-wheel", id, id.Value, snapshot.Offer,
                state.rewardDayKey, mode, snapshot.Index, snapshot.IsAds ? checked(state.adsSpinToday + 1) : 1,
                Now.Ticks, snapshot.Authorization, ordinal);
            state.bw4Pending.fingerprint = state.pendingPayloadFingerprint; // Preserve exact historical bytes.
            return store.Save(state, out code);
        }
        private bool Read(out SpinWheelSaveData state, out string code)
        {
            state = null;
            if (stopped) { code = "REWARD_STOPPED"; return false; }
            if (!store.Load(out state, out code)) return false;
            if (!string.IsNullOrWhiteSpace(state.pendingSpinId) && state.bw4Pending == null)
            { code = "LEGACY_PENDING_SNAPSHOT_REQUIRED"; return false; }
            var currentDay = days.Resolve(Now);
            int day = currentDay.Value;
            // Legacy zero initializes identity without restoring spent allowances.
            if (state.rewardDayKey == 0)
            {
                state.rewardDayKey = day;
                state.dayOfYear = currentDay.ToUtcDate().DayOfYear;
                return store.Save(state, out code);
            }
            if (state.rewardDayKey > day) { code = "CLOCK_ROLLBACK"; return false; }
            if (state.bw4Pending == null && state.rewardDayKey < day)
            {
                state.rewardDayKey = day; state.dayOfYear = days.Resolve(Now).ToUtcDate().DayOfYear;
                state.isDoneSpinFreeToday = false; state.adsSpinToday = 0;
                return store.Save(state, out code);
            }
            return true;
        }
        public bool Initialize(out string code)
        {
            if (stopped) { code = "REWARD_STOPPED"; return false; }
            if (!Read(out var state, out code)) return false;
            if (state.bw4Pending != null && state.bw4Pending.profile != profile) { code = "REWARD_CONTEXT_MISMATCH"; return false; }
            // Retry pending claims of every feature, but only this feature's own pending claim gates startup:
            // a claim another feature can never grant must not block SpinWheel (BUG-261114-382e).
            coordinator.RecoverPending();
            if (state.bw4Pending != null)
            { var result = CompleteSpin(state.bw4Pending.claimId); if (!result.Success) { code = result.DiagnosticCode; return false; } }
            code = null; return true;
        }
        public ResolvedRewardOffer Preview(int index, bool isAds)
        {
            if (!Read(out var state, out _)) return null;
            if (state.bw4Pending != null && state.bw4Pending.index == index && (state.bw4Pending.mode == "ads") == isAds) return state.bw4Pending.Offer();
            var set = isAds ? ads : free; return index >= 0 && index < set.Length ? set[index] : null;
        }
        public SpinStartResult PrepareFreeSpin(int selectedIndex) => Prepare(selectedIndex, false, null);
        public SpinStartResult PrepareAuthorizedSpin(int selectedIndex, RewardAuthorization authorization) => Prepare(selectedIndex, true, authorization);
        private SpinStartResult Prepare(int index, bool isAds, RewardAuthorization? authorization)
        {
            if (busy || stopped) return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "REWARD_BUSY_OR_STOPPED");
            busy = true;
            try
            {
                if (!Read(out var state, out var code)) return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: code);
                if (state.bw4Pending != null)
                {
                    if (state.bw4Pending.index == index && (state.bw4Pending.mode == "ads") == isAds)
                        return new SpinStartResult(RewardGrantOutcome.IdempotentReplay, state.bw4Pending.claimId, index);
                    return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "REWARD_PENDING");
                }
                var set = isAds ? ads : free;
                if (index < 0 || index >= set.Length) return new SpinStartResult(RewardGrantOutcome.InvalidReward, diagnosticCode: "SPIN_INDEX_OUT_OF_RANGE");
                if ((!isAds && state.isDoneSpinFreeToday) || (isAds && (!state.isDoneSpinFreeToday || state.adsSpinToday >= adsLimit)))
                    return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "SPIN_UNAVAILABLE");
                if (isAds && !RewardOperation.Authorized(authorization, Placement.SPIN, Now))
                    return new SpinStartResult(RewardGrantOutcome.Unavailable, diagnosticCode: "AUTHORIZATION_REQUIRED");
                string mode = isAds ? "ads" : "free", auth = isAds ? authorization.Value.RequestId : "free";
                int ordinal = checked(state.spinOrdinal + 1);
                var id = RewardClaimId.Create("spin-wheel", mode, N(state.rewardDayKey), N(ordinal), auth, profile);
                state.bw4Pending = PendingRewardData.Create(profile, "spin-wheel", id, id.Value, set[index],
                    state.rewardDayKey, mode, index, isAds ? checked(state.adsSpinToday + 1) : 1, Now.Ticks, auth, ordinal);
                state.pendingSpinId = id.Value; state.pendingSelectedIndex = index; state.pendingIsAds = isAds;
                state.pendingPayloadFingerprint = state.bw4Pending.fingerprint;
                if (!store.Save(state, out code)) return new SpinStartResult(RewardGrantOutcome.PersistenceFailure, diagnosticCode: code);
                return new SpinStartResult(RewardGrantOutcome.Success, id.Value, index);
            }
            finally { busy = false; }
        }
        public RewardClaimResult CompleteSpin(string spinId)
        {
            if (busy || stopped) return RewardOperation.Unavailable("REWARD_BUSY_OR_STOPPED");
            busy = true;
            try
            {
                if (!Read(out var state, out var code)) return RewardOperation.Unavailable(code);
                if (state.bw4Pending == null)
                    return state.bw4ConsumedClaimId == spinId && !string.IsNullOrWhiteSpace(spinId)
                        ? new RewardClaimResult(RewardGrantOutcome.IdempotentReplay) : RewardOperation.Unavailable("SPIN_NOT_PREPARED");
                if (state.bw4Pending.claimId != spinId) return RewardOperation.Unavailable("SPIN_NOT_PREPARED");
                var result = RewardOperation.Complete(store, coordinator, profile, "spin-wheel", s => s.bw4Pending,
                    (s, p) =>
                    {
                        if (s.rewardDayKey == p.day)
                        { if (p.mode == "ads") s.adsSpinToday = Math.Max(s.adsSpinToday, p.targetCount); else s.isDoneSpinFreeToday = true; }
                        s.spinOrdinal = Math.Max(s.spinOrdinal, p.ordinal);
                        s.bw4ConsumedClaimId = p.claimId;
                    }, s => s.bw4ConsumedClaimId);
                if (!result.Success) return result;
                if (!store.Load(out state, out code)) return RewardOperation.Failure(code);
                state.bw4Pending = null; state.pendingSpinId = null; state.pendingPayloadFingerprint = null;
                if (!store.Save(state, out code)) return RewardOperation.Failure(code);
                return result;
            }
            finally { busy = false; }
        }
        public SpinAvailability Current => Read(out var s, out _) ? new SpinAvailability(!s.isDoneSpinFreeToday, s.adsSpinToday, adsLimit, NextResetUtc)
            : new SpinAvailability(false, adsLimit, adsLimit, NextResetUtc);
        public DateTime NextResetUtc => days.GetNextBoundaryUtc(Now);
        public bool IsDoneSpinFreeToday => !Current.FreeAvailable;
        public int AdsSpinToday => Current.AdsSpinToday;
        public int DayOfYear => Read(out var s, out _) ? s.dayOfYear : 0;
        public void Dispose() { stopped = true; }
    }
}
