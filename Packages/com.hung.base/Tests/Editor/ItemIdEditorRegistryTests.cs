using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Editor;
using NUnit.Framework;
using Sirenix.OdinInspector.Editor;

namespace Hung.Base.Editor.Tests
{
    public sealed class ItemIdEditorRegistryTests
    {
        [Test]
        public void Drawer_IsTheSingleGlobalItemIdDrawerOwner()
        {
            Assert.That(typeof(ItemIdDrawer).BaseType,
                Is.EqualTo(typeof(OdinValueDrawer<ItemId>)));

            Type[] owners = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.Name == "ItemIdDrawer" &&
                    type.BaseType == typeof(OdinValueDrawer<ItemId>))
                .ToArray();
            Assert.That(owners, Has.Length.EqualTo(1));
            Assert.That(owners[0].Assembly.GetName().Name, Is.EqualTo("Hung.Base.Editor"));
        }

        [Test]
        public void Registry_MergesBaseAndProviderRowsDeterministically()
        {
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[]
            {
                new RowsSource(new ItemIdEditorOption(ItemId.Parse("zeta.last"), "Last", "zeta")),
                new RowsSource(new ItemIdEditorOption(ItemId.Parse("alpha.first"), "First", "alpha"))
            });

            string[] ids = registry.GetOptions().Select(option => option.Id.Value).ToArray();

            Assert.That(ids[0], Is.EqualTo("alpha.first"));
            Assert.That(ids[ids.Length - 1], Is.EqualTo("zeta.last"));
            Assert.That(ids, Does.Contain("base.gold"));
            Assert.That(ids, Does.Contain("base.heart"));
        }

        [Test]
        public void Registry_DiscoversPublicStaticItemIdDefinitionsWithoutCatalog()
        {
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[0]);

            Assert.That(registry.GetOptions().Select(option => option.Id.Value),
                Does.Contain("base.gold"));
        }

        [Test]
        public void Registry_DeduplicatesIdenticalMetadataAndReportsConflicts()
        {
            ItemId id = ItemId.Parse("demo.value");
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[]
            {
                new RowsSource(new ItemIdEditorOption(id, "Value", "demo")),
                new RowsSource(new ItemIdEditorOption(id, "Value", "demo")),
                new RowsSource(new ItemIdEditorOption(id, "Other", "demo"))
            });

            Assert.That(registry.GetOptions().Count(option => option.Id == id), Is.EqualTo(1));
            Assert.That(registry.Conflicts.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(registry.Conflicts[0].Id, Is.EqualTo(id));
        }

        [Test]
        public void Registry_InvalidatesItsCache()
        {
            int calls = 0;
            var registry = new ItemIdEditorRegistry(() =>
            {
                calls++;
                return new IItemIdEditorSource[]
                {
                    new RowsSource(new ItemIdEditorOption(BaseItemIds.Gold, "Gold", "base"))
                };
            });

            registry.GetOptions();
            registry.GetOptions();
            registry.Invalidate();
            registry.GetOptions();

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void Registry_ExcludesDefinitionsFromTestAssemblies()
        {
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[0]);

            Assert.That(registry.GetOptions().Select(option => option.Id.Value),
                Does.Not.Contain("test.only"));
        }

        [Test]
        public void Registry_PreservesUnknownValuesAndValidatesExplicitCustomId()
        {
            ItemId unknown = ItemId.Parse("future.unreleased");
            var registry = new ItemIdEditorRegistry(new IItemIdEditorSource[0]);

            Assert.That(registry.IsUnknown(unknown), Is.True);
            Assert.That(registry.TryParseCustomId(unknown.Value, out ItemId preserved), Is.True);
            Assert.That(preserved.Value, Is.EqualTo(unknown.Value));
            Assert.That(registry.TryParseCustomId("not valid", out _), Is.False);
        }

        private sealed class RowsSource : IItemIdEditorSource
        {
            private readonly ItemIdEditorOption[] rows;

            public RowsSource(params ItemIdEditorOption[] rows)
            {
                this.rows = rows;
            }

            public IEnumerable<ItemIdEditorOption> GetOptions()
            {
                return rows;
            }
        }

        public static readonly ItemId TestOnlyDefinition = ItemId.Parse("test.only");
    }
}
