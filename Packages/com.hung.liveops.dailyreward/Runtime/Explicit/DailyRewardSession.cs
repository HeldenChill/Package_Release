using System;
using System.Globalization;
using System.Linq;
using Hung.Base;
using Hung.Data.Rewards;
using Hung.Data;
namespace Hung.LiveOps.DailyReward
{
    public sealed class DailyRewardSession : IDailyRewardService, IDisposable
    {
        private readonly RewardStateStore<DailyRewardSaveData> store;
        private readonly IRewardClaimCoordinator coordinator;
        private readonly IClock clock;
        private readonly RewardDayPolicy days;
        private readonly string profile;
        private readonly ResolvedRewardOffer free;
        private readonly ResolvedRewardOffer[] sequence;
        private readonly bool[] ads;
        private readonly int cooldown;
        private bool stopped, busy;
        public DailyRewardSession(RewardStateStore<DailyRewardSaveData> store, IRewardClaimCoordinator coordinator,
            IClock clock, RewardDayPolicy days, string profile, ResolvedRewardOffer free,
            ResolvedRewardOffer[] sequence, bool[] ads, int cooldownSeconds)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); this.days = days;
            if (string.IsNullOrWhiteSpace(profile) || free == null || sequence == null || sequence.Length == 0
                || sequence.Any(x => x == null) || ads == null || ads.Length != sequence.Length || cooldownSeconds < 0)
                throw new ArgumentException("Invalid DailyReward configuration.");
            this.profile = profile; this.free = free; this.sequence = (ResolvedRewardOffer[])sequence.Clone();
            this.ads = (bool[])ads.Clone(); cooldown = cooldownSeconds;
        }
        private DateTime Now { get { var now = clock.UtcNow; if (now.Kind != DateTimeKind.Utc) throw new InvalidOperationException("Clock must return UTC."); return now; } }
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        public bool Initialize(out string code)
        {
            if (stopped) { code = "REWARD_STOPPED"; return false; }
            if (!Read(out var state, out code)) return false;
            if (state.bw4Pending != null && state.bw4Pending.profile != profile) { code = "REWARD_CONTEXT_MISMATCH"; return false; }
            // Retry pending claims of every feature, but only this feature's own pending claim gates startup:
            // a claim another feature can never grant must not block DailyReward (BUG-261114-382e).
            coordinator.RecoverPending();
            if (state.bw4Pending != null)
            {
                var result = Complete(); if (!result.Success) { code = result.DiagnosticCode; return false; }
            }
            code = null; return true;
        }
        private bool Read(out DailyRewardSaveData state, out string code)
        {
            state = null;
            if (stopped) { code = "REWARD_STOPPED"; return false; }
            if (!store.Load(out state, out code)) return false;
            var currentDay = days.Resolve(Now);
            int day = currentDay.Value;
            // Legacy zero means an unset day identity, not an elapsed reward day.
            // Match the legacy manager: initialize identity without renewing eligibility.
            if (state.rewardDayKey == 0)
            {
                state.rewardDayKey = day;
                state.dayOfYear = currentDay.ToUtcDate().DayOfYear;
                return store.Save(state, out code);
            }
            if (state.rewardDayKey > day) { code = "CLOCK_ROLLBACK"; return false; }
            // Settle old-day pending work before resetting eligibility.
            if (state.bw4Pending == null && state.rewardDayKey < day)
            { state.rewardDayKey = day; state.dayOfYear = days.Resolve(Now).ToUtcDate().DayOfYear; state.currentProgress = 0; return store.Save(state, out code); }
            return true;
        }
        public ResolvedRewardOffer PreviewFree()
        {
            if (!Read(out var state, out _)) return null;
            return state.bw4Pending != null && state.bw4Pending.mode == "free" ? state.bw4Pending.Offer() : free;
        }
        public ResolvedRewardOffer PreviewSequence(int slot)
        {
            if (!Read(out var state, out _) || slot < 0) return null;
            if (state.bw4Pending != null && state.bw4Pending.mode == "sequence" && state.bw4Pending.index == slot)
                return state.bw4Pending.Offer();
            return slot < sequence.Length ? sequence[slot] : null;
        }
        public RewardClaimResult ClaimFree() => Guard(() => Claim(-1, null));
        public RewardClaimResult ClaimSequence(int slot, RewardAuthorization? authorization = null) => Guard(() => slot < 0
            ? new RewardClaimResult(RewardGrantOutcome.InvalidReward, "SLOT_OUT_OF_RANGE") : Claim(slot, authorization));
        private RewardClaimResult Guard(Func<RewardClaimResult> operation)
        {
            if (stopped || busy) return RewardOperation.Unavailable("REWARD_BUSY_OR_STOPPED");
            busy = true; try { return operation(); } finally { busy = false; }
        }
        private RewardClaimResult Claim(int slot, RewardAuthorization? authorization)
        {
            if (!Read(out var state, out var code)) return RewardOperation.Failure(code);
            string mode = slot < 0 ? "free" : "sequence";
            if (state.bw4Pending != null)
            {
                if (state.bw4Pending.mode != mode || (slot >= 0 && state.bw4Pending.index != slot)) return RewardOperation.Unavailable("REWARD_PENDING");
                return Complete();
            }
            if (slot >= sequence.Length || slot < -1) return new RewardClaimResult(RewardGrantOutcome.InvalidReward, "SLOT_OUT_OF_RANGE");
            if (slot < 0 && GetFreeCooldownRemaining() > TimeSpan.Zero) return RewardOperation.Unavailable("FREE_COOLDOWN");
            if (slot >= 0 && state.currentProgress > slot) return new RewardClaimResult(RewardGrantOutcome.IdempotentReplay);
            if (slot >= 0 && state.currentProgress < slot) return RewardOperation.Unavailable("SLOT_LOCKED");
            if (slot >= 0 && ads[slot] && !RewardOperation.Authorized(authorization, Placement.DAILY_REWARD, Now))
                return RewardOperation.Unavailable("AUTHORIZATION_REQUIRED");
            string auth = slot >= 0 && ads[slot] ? authorization.Value.RequestId : "free";
            // Only allocated once; a failed pending save performs no grant.
            long ticks = Now.Ticks;
            string token = slot < 0 ? ticks.ToString(CultureInfo.InvariantCulture) : N(slot);
            string definition = slot < 0 ? "daily-reward-free" : auth;
            var id = RewardClaimId.Create("daily-reward", mode, N(state.rewardDayKey), token, definition, profile);
            var offer = slot < 0 ? free : sequence[slot];
            state.bw4Pending = PendingRewardData.Create(profile, "daily-reward", id, id.Value, offer,
                state.rewardDayKey, mode, Math.Max(0, slot), slot < 0 ? 1 : slot + 1, ticks, auth);
            if (!store.Save(state, out code)) return RewardOperation.Failure(code);
            return Complete();
        }
        private RewardClaimResult Complete()
        {
            var result = RewardOperation.Complete(store, coordinator, profile, "daily-reward", s => s.bw4Pending,
                (s, p) =>
                {
                    if (p.mode == "free")
                    { s.lastFreeClaimUtcTicks = Math.Max(s.lastFreeClaimUtcTicks, p.preparedUtcTicks); s.lastFreeClaimTime = (int)new DateTime(p.preparedUtcTicks, DateTimeKind.Utc).TimeOfDay.TotalSeconds; }
                    else if (s.rewardDayKey == p.day) s.currentProgress = Math.Max(s.currentProgress, p.targetCount);
                    s.bw4ConsumedClaimId = p.claimId;
                }, s => s.bw4ConsumedClaimId);
            if (!result.Success) return result;
            if (!store.Load(out var state, out var code)) return RewardOperation.Failure(code);
            state.bw4Pending = null;
            if (!store.Save(state, out code)) return RewardOperation.Failure(code);
            return result;
        }
        public DailyRewardSnapshot Current
        {
            get
            {
                if (!Read(out var s, out _)) return new DailyRewardSnapshot(default, 0, null, TimeSpan.MaxValue);
                var slots = Enumerable.Range(0, sequence.Length).Select(i => new DailyRewardSlotSnapshot(i, s.currentProgress > i, s.currentProgress >= i, ads[i])).ToArray();
                return new DailyRewardSnapshot(new RewardDayKey(s.rewardDayKey), s.currentProgress, slots, GetFreeCooldownRemaining());
            }
        }
        public TimeSpan GetFreeCooldownRemaining()
        {
            if (!Read(out var s, out _)) return TimeSpan.MaxValue;
            if (s.lastFreeClaimUtcTicks <= 0) return TimeSpan.Zero;
            var left = new DateTime(s.lastFreeClaimUtcTicks, DateTimeKind.Utc).AddSeconds(cooldown) - Now;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
        public void Reconcile() { Read(out _, out _); }
        public int GetProgress => Current.Progress;
        public int GetLastFreeClaimTime => Read(out var s, out _) ? s.lastFreeClaimTime : 0;
        public bool CanClaimFree => !stopped && GetFreeCooldownRemaining() <= TimeSpan.Zero;
        public void Dispose() { stopped = true; }
    }
}
