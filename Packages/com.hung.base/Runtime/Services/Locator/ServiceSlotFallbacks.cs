namespace Hung.Base
{
    internal static class ServiceSlotFallbacks
    {
        internal static void Install()
        {
            ServiceSlots.SetFallback<IAdsService>(new UnsupportedAdsService());
            ServiceSlots.SetFallback<IIAPService>(new UnsupportedIapService());
            ServiceSlots.SetFallback<IAudioService>(new UnsupportedAudioService());
            var items = new UnsupportedItemCapabilities();
            ServiceSlots.SetFallback<IItemBalanceReader>(items);
            ServiceSlots.SetFallback<IItemSpendService>(items);
            ServiceSlots.SetFallback<IRewardGrantService>(items);
            ServiceSlots.SetFallback<IItemPresentationProvider>(items);
            ServiceSlots.SetFallback<IRewardPresenter>(items);
        }
    }
}
