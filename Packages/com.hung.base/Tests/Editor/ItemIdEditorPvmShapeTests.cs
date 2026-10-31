using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Editor;
using NUnit.Framework;

namespace Hung.Base.Editor.Tests
{
    public sealed class ItemIdEditorPvmShapeTests
    {
        private sealed class PvmShapeSource : IItemIdEditorSource
        {
            private readonly ItemId id;
            public PvmShapeSource(string raw) { id = ItemId.Parse(raw); }
            public IEnumerable<ItemIdEditorOption> GetOptions() { yield return new ItemIdEditorOption(id, id.Value, ""); }
        }

        [Test]
        public void ExplicitLegacySource_MergesWithBaseIds()
        {
            var r = new ItemIdEditorRegistry(new IItemIdEditorSource[] { new PvmShapeSource("Gold") });
            Assert.That(r.GetOptions().Any(o => o.Id == ItemId.Parse("Gold")), Is.True);
            Assert.That(r.GetOptions().Any(o => o.Id == BaseItemIds.Gold), Is.True);
        }

        [Test]
        public void Shared_PvmCallShapes()
        {
            ItemIdEditorRegistry.Shared.Invalidate();
            Assert.That(ItemIdEditorRegistry.Shared.GetOptions().Any(o => o.Id == BaseItemIds.Gold), Is.True);
            Assert.That(ItemIdEditorRegistry.Shared.IsUnknown(ItemId.Parse("future.pvm_item")), Is.True);
            Assert.That(ItemIdEditorRegistry.Shared.TryParseCustomId("future.pvm_item", out ItemId parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(ItemId.Parse("future.pvm_item")));
            Assert.That(ItemIdEditorRegistry.Shared.TryParseCustomId("not valid", out _), Is.False);
        }

        [Test]
        public void RegisterSource_LegacySourceTwice_AddsOnce()
        {
            var source = new PvmShapeSource("pvmshape.once");
            try
            {
                ItemIdEditorRegistry.RegisterSource(source);
                ItemIdEditorRegistry.RegisterSource(source);
                Assert.That(ItemIdEditorRegistry.Shared.GetOptions().Count(o => o.Id.Value == "pvmshape.once"), Is.EqualTo(1));
                Assert.That(ItemIdEditorRegistry.Shared.Conflicts.Any(c => c.Id.Value == "pvmshape.once"), Is.False);
            }
            finally { Assert.That(ItemIdEditorRegistry.UnregisterSourceForTests(source), Is.True); }
            Assert.That(ItemIdEditorRegistry.Shared.GetOptions().Any(o => o.Id.Value == "pvmshape.once"), Is.False);
        }

        [Test]
        public void ItemIdDrawer_KeepsExactPvmCheckedShape()
        {
            Type baseType = typeof(ItemIdDrawer).BaseType;
            Assert.That(baseType.IsGenericType && baseType.GetGenericTypeDefinition().FullName == "Sirenix.OdinInspector.Editor.OdinValueDrawer`1", Is.True);
            Assert.That(baseType.GetGenericArguments()[0], Is.EqualTo(typeof(ItemId)));
            Assert.That(typeof(ItemIdDrawer).FullName, Is.EqualTo("Hung.Base.Editor.ItemIdDrawer"));
        }

        [TestCase("Hung.Base.Editor.ItemIdDropdownOption")]
        [TestCase("Hung.Base.Editor.ItemIdAdvancedDropdown")]
        public void DeadTypes_AreRemoved(string fullName) =>
            Assert.That(typeof(ItemIdDrawer).Assembly.GetType(fullName), Is.Null);
    }
}
