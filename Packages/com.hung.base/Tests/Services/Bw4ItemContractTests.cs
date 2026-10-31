using System;
using System.Collections.Generic;
using Hung.Base;
using NUnit.Framework;
namespace Hung.Base.Tests
{
    public sealed class Bw4ItemContractTests
    {
        [Test] public void DefaultsAreNonNullUnavailableAndUnowned()
        {
            ServiceSlots.ResetForTests();
            Assert.That(ServiceSlots.Get<IItemBalanceReader>().GetBalance(default, out var balance), Is.EqualTo(ItemBalanceStatus.Unavailable));
            Assert.That(balance.Quantity, Is.Zero);
            Assert.That(ServiceSlots.Get<IItemSpendService>().Spend(default, 1).Outcome, Is.EqualTo(ItemSpendOutcome.Unavailable));
            Assert.That(ServiceSlots.Get<IRewardGrantService>().Grant(default, null, null).Outcome, Is.EqualTo(RewardGrantOutcome.Unavailable));
            Assert.That(ServiceSlots.Get<IItemPresentationProvider>().GetPresentation(default, out _), Is.EqualTo(ItemPresentationStatus.Unavailable));
            Assert.That(ServiceSlots.Get<IRewardPresenter>().Present(null), Is.EqualTo(RewardPresentationOutcome.Unavailable));
            Assert.That(ServiceSlots.TryGet<IRewardGrantService>(out _), Is.False); Assert.That(ServiceSlots.OwnerOf<IRewardGrantService>(), Is.Null);
        }
        [Test] public void PresentationRequestIsDetachedAndOriginIsPerRequest()
        {
            var items = new List<RewardGrantItem> { new RewardGrantItem(ItemId.Parse("fixture.coins"), 2) };
            var request = new RewardPresentationRequest(items); items.Clear();
            Assert.That(request.Items.Count, Is.EqualTo(1)); Assert.That(request.Origin.HasValue, Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<RewardGrantItem>)request.Items)[0] = default);
        }
    }
}
