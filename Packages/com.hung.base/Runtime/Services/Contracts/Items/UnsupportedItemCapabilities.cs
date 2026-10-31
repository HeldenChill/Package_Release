using System.Collections.Generic;
namespace Hung.Base
{
    public sealed class UnsupportedItemCapabilities : IItemBalanceReader, IItemSpendService,
        IRewardGrantService, IItemPresentationProvider, IRewardPresenter
    {
        public ItemBalanceStatus GetBalance(ItemId id, out ItemBalance balance)
        { balance = default; return ItemBalanceStatus.Unavailable; }
        public ItemSpendResult Spend(ItemId id, int quantity) => new ItemSpendResult(ItemSpendOutcome.Unavailable, "ITEMS_UNAVAILABLE");
        public RewardGrantResult Grant(RewardClaimId id, IReadOnlyList<RewardGrantItem> items, string fingerprint)
            => new RewardGrantResult(RewardGrantOutcome.Unavailable, "ITEMS_UNAVAILABLE");
        public ItemPresentationStatus GetPresentation(ItemId id, out ItemPresentation presentation)
        { presentation = default; return ItemPresentationStatus.Unavailable; }
        public RewardPresentationOutcome Present(RewardPresentationRequest request) => RewardPresentationOutcome.Unavailable;
    }
}
