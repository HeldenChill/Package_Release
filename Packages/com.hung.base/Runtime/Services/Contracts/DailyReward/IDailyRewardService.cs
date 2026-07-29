using System;

namespace Hung.Base
{
    public readonly struct DailyRewardSlotSnapshot
    {
        public DailyRewardSlotSnapshot(int index, bool claimed, bool unlocked, bool requiresAuthorization)
        {
            Index = index;
            Claimed = claimed;
            Unlocked = unlocked;
            RequiresAuthorization = requiresAuthorization;
        }

        public int Index { get; }
        public bool Claimed { get; }
        public bool Unlocked { get; }
        public bool RequiresAuthorization { get; }
    }

    public readonly struct DailyRewardSnapshot
    {
        public DailyRewardSnapshot(RewardDayKey day, int progress, DailyRewardSlotSnapshot[] slots, TimeSpan freeCooldownRemaining)
        {
            Day = day;
            Progress = progress;
            Slots = slots ?? Array.Empty<DailyRewardSlotSnapshot>();
            FreeCooldownRemaining = freeCooldownRemaining;
        }

        public RewardDayKey Day { get; }
        public int Progress { get; }
        public DailyRewardSlotSnapshot[] Slots { get; }
        public TimeSpan FreeCooldownRemaining { get; }
        public bool CanClaimFree => FreeCooldownRemaining <= TimeSpan.Zero;
    }

    public interface IDailyRewardService 
    {
        DailyRewardSnapshot Current { get; }
        RewardClaimResult ClaimSequence(int slot, RewardAuthorization? authorization = null);
        RewardClaimResult ClaimFree();
        TimeSpan GetFreeCooldownRemaining();
        void Reconcile();

        public int GetProgress{ get; }
        public int GetLastFreeClaimTime{ get; }
        public bool CanClaimFree{ get; }
    }
}
