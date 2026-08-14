using System.Collections.Generic;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public class RewardGrantReceiptTests
    {
        [Test]
        public void InitData_RepairsMissingRewardGrantReceipts()
        {
            var data = new GameData();
            data.user.PurchasedItems = new List<IAP_ITEM>();
            data.user.ItemDatas = new[] { new GameData.ItemData { ItemId = BaseItemIds.Gold } };
            data.user.RewardGrantReceipts = null;

            data.InitData(new[] { BaseItemIds.Gold });

            Assert.That(data.user.RewardGrantReceipts, Is.Not.Null);
            Assert.That(data.user.RewardGrantReceipts, Is.Empty);
        }
    }
}
