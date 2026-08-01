using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public class TimeAndRewardContractTests
    {
        [TestCase("2026-07-28T03:59:59Z", 240, 20260727)]
        [TestCase("2026-07-28T04:00:00Z", 240, 20260728)]
        public void RewardDayPolicy_UsesUtcBoundary(string utc, int offset, int expected)
        {
            var policy = new RewardDayPolicy(offset);

            Assert.AreEqual(new RewardDayKey(expected), policy.Resolve(DateTime.Parse(utc).ToUniversalTime()));
        }

        [Test]
        public void RewardClaimId_IsStableAndProfileScoped()
        {
            RewardClaimId a = RewardClaimId.Create("daily-gift", "normal", "2026-W31", "4", "gift-v3", "profile-a");
            RewardClaimId replay = RewardClaimId.Create("daily-gift", "normal", "2026-W31", "4", "gift-v3", "profile-a");
            RewardClaimId other = RewardClaimId.Create("daily-gift", "normal", "2026-W31", "4", "gift-v3", "profile-b");

            Assert.AreEqual(a, replay);
            Assert.AreNotEqual(a, other);
        }

        [Test]
        public void RewardGrantItem_RejectsInvalidQuantity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewardGrantItem(BaseItemIds.Gold, 0));
        }

        [Test]
        public void RewardAuthorization_RequiresUtcCompletion()
        {
            Assert.Throws<ArgumentException>(() => new RewardAuthorization(
                "request-a",
                Placement.DAILY_REWARD,
                DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                "provider-evidence"));
        }

        [Test]
        public void RewardClaimRequest_RejectsEmptyPayload()
        {
            Assert.Throws<ArgumentException>(() => new RewardClaimRequest(
                new RewardClaimId("claim-a"),
                "daily-gift",
                new List<RewardGrantItem> { new RewardGrantItem(BaseItemIds.Gold, 1) },
                ""));
        }
    }
}
