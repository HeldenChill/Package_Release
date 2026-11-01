using Hung.DesignPattern;

namespace Hung.LiveOps.PiggyBank
{
    /// <summary>Raised when the player claims the piggy bank reward. Carries the gold granted.</summary>
    public struct PiggyBankRewardClaimedEvent : IEvent
    {
        public int goldAmount;
    }
}
