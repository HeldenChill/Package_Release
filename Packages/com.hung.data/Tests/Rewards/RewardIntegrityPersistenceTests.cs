using System.Linq;
using Hung.Base;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using Hung.Data.Tests.Persistence;
using NUnit.Framework;

namespace Hung.Data.Tests.Rewards
{
    public class RewardIntegrityPersistenceTests
    {
        [Test]
        public void RewardIntegrity_RoundTrip_PreservesPendingClaim()
        {
            RewardIntegrityStateData state = RewardIntegrityStateData.CreateDefault();
            state.latestObservedUtcTicks = UtcTicks(2026, 7, 27, 10);
            state.claims.Add(RewardClaimRecordData.Prepared("claim-a", "daily-gift", "hash-a", state.latestObservedUtcTicks));
            state.claims[0].items.Add(new RewardGrantItemData(BaseItemIds.Gold.Value, 5));

            LoadResult<RewardIntegrityStateData> loaded = RoundTrip(state);

            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Value.claims.Single().claimId, Is.EqualTo("claim-a"));
            Assert.That(loaded.Value.claims.Single().items.Single().itemId, Is.EqualTo(BaseItemIds.Gold.Value));
        }

        [Test]
        public void RewardIntegrity_RejectsDuplicateClaimIds()
        {
            RewardIntegrityStateData state = RewardIntegrityStateData.CreateDefault();
            state.claims.Add(RewardClaimRecordData.Prepared("claim-a", "daily-gift", "hash-a", UtcTicks(2026, 7, 27, 10)));
            state.claims.Add(RewardClaimRecordData.Prepared("claim-a", "daily-reward", "hash-b", UtcTicks(2026, 7, 27, 11)));

            SaveDefinition<RewardIntegrityStateData> definition = PackageSaveDefinitions.RewardIntegrity(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());

            Assert.That(definition.Validate(state).Success, Is.False);
        }

        [Test]
        public void RewardIntegrity_RejectsNonFinalizedRecordWithoutPayloadItems()
        {
            RewardIntegrityStateData state = RewardIntegrityStateData.CreateDefault();
            state.claims.Add(RewardClaimRecordData.Prepared("claim-a", "daily-gift", "hash-a", UtcTicks(2026, 7, 27, 10)));

            SaveDefinition<RewardIntegrityStateData> definition = PackageSaveDefinitions.RewardIntegrity(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());

            Assert.That(definition.Validate(state).Success, Is.False);
        }

        private static LoadResult<RewardIntegrityStateData> RoundTrip(RewardIntegrityStateData state)
        {
            var service = new PersistenceService(new InMemorySaveStore());
            SaveDefinition<RewardIntegrityStateData> definition = PackageSaveDefinitions.RewardIntegrity(PersistenceTestDoubles.Codec(), PersistenceTestDoubles.Protector());
            Assert.That(service.Save(definition, state).Success, Is.True);
            return service.Load(definition);
        }

        private static long UtcTicks(int year, int month, int day, int hour) =>
            new System.DateTime(year, month, day, hour, 0, 0, System.DateTimeKind.Utc).Ticks;
    }
}
