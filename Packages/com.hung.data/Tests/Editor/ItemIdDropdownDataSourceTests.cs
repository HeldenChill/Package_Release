using System.Linq;
using Hung.Base;
using NUnit.Framework;

namespace Hung.Data.Editor.Tests
{
    public sealed class ItemIdDropdownDataSourceTests
    {
        [Test]
        public void Build_DeduplicatesAndSortsOrdinally()
        {
            ItemId[] input =
            {
                ItemId.Parse("pet_vs_monster.bear"),
                ItemId.Parse("base.gold"),
                ItemId.Parse("pet_vs_monster.bear")
            };

            ItemIdDropdownOption[] result = ItemIdDropdownDataSource.Build(input).ToArray();

            Assert.That(result.Select(option => option.Id.Value), Is.EqualTo(new[]
            {
                "base.gold",
                "pet_vs_monster.bear"
            }));
        }

        [Test]
        public void Build_PreservesFullIdAsSearchText()
        {
            ItemIdDropdownOption option = ItemIdDropdownDataSource.Build(new[]
            {
                ItemId.Parse("pet_vs_monster.egg-splosion")
            }).Single();

            Assert.That(option.SearchText, Is.EqualTo("pet_vs_monster.egg-splosion"));
            Assert.That(option.GroupPath, Is.EqualTo("pet_vs_monster"));
        }

        [Test]
        public void GetOptions_ReusesCacheUntilInvalidated()
        {
            int scans = 0;
            var source = new ItemIdDropdownDataSource(() =>
            {
                scans++;
                return new[] { ItemId.Parse("base.gold") };
            });

            source.GetOptions();
            source.GetOptions();
            source.Invalidate();
            source.GetOptions();

            Assert.That(scans, Is.EqualTo(2));
        }

        [Test]
        public void FindSelectedIndex_ReturnsExactMatchAndMissingSentinel()
        {
            ItemIdDropdownOption[] options = ItemIdDropdownDataSource.Build(new[]
            {
                ItemId.Parse("base.gold"),
                ItemId.Parse("base.heart")
            }).ToArray();

            Assert.That(
                ItemIdDropdownDataSource.FindSelectedIndex(options, ItemId.Parse("base.heart")),
                Is.EqualTo(1));
            Assert.That(
                ItemIdDropdownDataSource.FindSelectedIndex(options, ItemId.Parse("missing.raw")),
                Is.EqualTo(-1));
        }
    }
}
