using System;
using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using Hung.Base.Editor;
using NUnit.Framework;

namespace Hung.Base.Editor.Tests
{
    public sealed class SampleAudioIds
    {
        public static readonly AudioId Field = AudioId.Parse("demo.sfx.field");
        public static AudioId Property => AudioId.Parse("demo.sfx.property");
        private static readonly AudioId Hidden = AudioId.Parse("demo.sfx.hidden");
        public static readonly ItemId Other = ItemId.Parse("demo.other");
    }

    public sealed class ThrowingAudioIds
    {
        public static AudioId Broken => throw new InvalidOperationException("boom");
    }

    public sealed class StableIdEditorRegistryTests
    {
        public static readonly AudioId TestOnlyDefinition = AudioId.Parse("test.only");

        private sealed class RowsSource : IStableIdEditorSource<AudioId>
        {
            private readonly StableIdEditorOption<AudioId>[] rows;
            public RowsSource(params StableIdEditorOption<AudioId>[] rows) => this.rows = rows;
            public IEnumerable<StableIdEditorOption<AudioId>> GetOptions() => rows;
        }

        [Test]
        public void Build_MergesAndOrdersByGroupThenValue()
        {
            var s1 = new RowsSource(new StableIdEditorOption<AudioId>(AudioId.Parse("zeta.last"), "Last", "zeta"));
            var s2 = new RowsSource(new StableIdEditorOption<AudioId>(AudioId.Parse("alpha.first"), "First", "alpha"));
            var options = StableIdEditorRegistry<AudioId>.Build(new[] { s1, s2 }, out _);
            Assert.That(options[0].Id.Value, Is.EqualTo("alpha.first"));
            Assert.That(options[options.Count - 1].Id.Value, Is.EqualTo("zeta.last"));
        }

        [Test]
        public void Build_DeduplicatesIdenticalRowsAndReportsConflicts()
        {
            var s1 = new RowsSource(
                new StableIdEditorOption<AudioId>(AudioId.Parse("demo.value"), "Value", "demo"),
                new StableIdEditorOption<AudioId>(AudioId.Parse("demo.value"), "Value", "demo"),
                new StableIdEditorOption<AudioId>(AudioId.Parse("demo.value"), "Other", "demo"));
            var options = StableIdEditorRegistry<AudioId>.Build(new[] { s1 }, out var conflicts);
            Assert.That(options.Count(o => o.Id.Value == "demo.value"), Is.EqualTo(1));
            Assert.That(conflicts.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(conflicts[0].Id.Value, Is.EqualTo("demo.value"));
        }

        [Test]
        public void Conflicts_RebuildsAfterDefinitionScanInvalidation()
        {
            bool conflicting = false;
            var registry = new StableIdEditorRegistry<AudioId>(() => new[] { new RowsSource(
                new StableIdEditorOption<AudioId>(BaseAudioIds.UiClick, "Click", "base"),
                new StableIdEditorOption<AudioId>(BaseAudioIds.UiClick, conflicting ? "Other" : "Click", "base")) });
            Assert.That(registry.Conflicts.Count, Is.Zero);
            conflicting = true;
            StableIdDefinitionScan.Invalidate();
            Assert.That(registry.Conflicts.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOptions_CachesUntilInvalidate()
        {
            int calls = 0;
            var reg = new StableIdEditorRegistry<AudioId>(() =>
            {
                calls++;
                return new[] { new RowsSource(new StableIdEditorOption<AudioId>(BaseAudioIds.UiClick, "Click", "base")) };
            });

            reg.GetOptions();
            reg.GetOptions();
            reg.Invalidate();
            reg.GetOptions();
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void Shared_ExcludesTestAssemblyDefinitions()
        {
            var options = StableIdEditorRegistry<AudioId>.Shared.GetOptions();
            Assert.That(options.Any(o => o.Id.Value == "test.only"), Is.False);
            Assert.That(options.Any(o => o.Id.Value == "demo.sfx.field"), Is.False);
            Assert.That(options.Any(o => o.Id.Value == "demo.sfx.property"), Is.False);
        }

        [Test]
        public void ConstantsSource_YieldsPublicStaticFieldsAndProperties()
        {
            var src = new StableIdConstantsSource<AudioId>(typeof(SampleAudioIds));
            var opts = src.GetOptions().ToArray();
            Assert.That(opts.Length, Is.EqualTo(2));
            Assert.That(opts.Any(o => o.Id.Value == "demo.sfx.field" && o.Label == "Field" && o.GroupPath == "demo"), Is.True);
            Assert.That(opts.Any(o => o.Id.Value == "demo.sfx.property" && o.Label == "Property" && o.GroupPath == "demo"), Is.True);
        }

        [Test]
        public void ConstantsSource_ReadsStaticClasses()
        {
            var src = new StableIdConstantsSource<AudioId>(typeof(BaseAudioIds));
            Assert.That(src.GetOptions().Count(), Is.EqualTo(6));
        }

        [Test]
        public void RegisterSource_SameInstanceTwice_IsIdempotent()
        {
            var opt = new StableIdEditorOption<AudioId>(AudioId.Parse("demo.register.once"), "Once", "demo");
            var src = new RowsSource(opt);
            try
            {
                StableIdEditorRegistry<AudioId>.RegisterSource(src);
                StableIdEditorRegistry<AudioId>.RegisterSource(src);
                Assert.That(StableIdEditorRegistry<AudioId>.Shared.GetOptions().Count(o => o.Id.Value == "demo.register.once"), Is.EqualTo(1));
                Assert.That(StableIdEditorRegistry<AudioId>.Shared.Conflicts.Any(c => c.Id.Value == "demo.register.once"), Is.False);
            }
            finally
            {
                Assert.That(StableIdEditorRegistry<AudioId>.UnregisterSourceForTests(src), Is.True);
            }
            Assert.That(StableIdEditorRegistry<AudioId>.Shared.GetOptions().Any(o => o.Id.Value == "demo.register.once"), Is.False);
        }

        [Test]
        public void Shared_ContainsBaseAudioIds()
        {
            var options = StableIdEditorRegistry<AudioId>.Shared.GetOptions();
            Assert.That(options.Any(o => o.Id == BaseAudioIds.UiClick && o.Label == "UiClick" && o.GroupPath == "base"), Is.True);
            Assert.That(options.Any(o => o.Id == BaseAudioIds.UiPopupShow), Is.True);
            Assert.That(options.Any(o => o.Id == BaseAudioIds.UiPopupHide), Is.True);
            Assert.That(options.Any(o => o.Id == BaseAudioIds.ItemClaim), Is.True);
            Assert.That(options.Any(o => o.Id == BaseAudioIds.SpinEnd), Is.True);
            Assert.That(options.Any(o => o.Id == BaseAudioIds.SpinWheel), Is.True);
        }

        [Test]
        public void IsUnknown_ValidUnknownTrue_KnownFalse_DefaultFalse()
        {
            Assert.That(StableIdEditorRegistry<AudioId>.Shared.IsUnknown(AudioId.Parse("future.sfx.x")), Is.True);
            Assert.That(StableIdEditorRegistry<AudioId>.Shared.IsUnknown(BaseAudioIds.UiClick), Is.False);
            Assert.That(StableIdEditorRegistry<AudioId>.Shared.IsUnknown(default), Is.False);
        }

        [Test]
        public void Scan_SkipsMemberWhoseGetterThrows()
        {
            var defs = new List<StableIdDefinitionScan.Definition>();
            var srcs = new List<Type>();
            Assert.DoesNotThrow(() => StableIdDefinitionScan.Collect(new[] { typeof(ThrowingAudioIds), typeof(SampleAudioIds) }, defs, srcs));
            Assert.That(defs.Any(d => d.Id.Equals(AudioId.Parse("demo.sfx.field"))), Is.True);
            Assert.That(defs.Any(d => d.Id.Equals(AudioId.Parse("demo.sfx.property"))), Is.True);
            Assert.That(defs.Any(d => d.IdType == typeof(ItemId) && d.Id.Equals(ItemId.Parse("demo.other"))), Is.True);
            Assert.That(defs.Any(d => d.MemberName == "Broken"), Is.False);
        }

        [Test]
        public void Scan_SkipsStaticClasses()
        {
            var defs = new List<StableIdDefinitionScan.Definition>();
            var srcs = new List<Type>();
            StableIdDefinitionScan.Collect(new[] { typeof(BaseAudioIds) }, defs, srcs);
            Assert.That(defs.Count, Is.EqualTo(0));
        }

        [Test]
        public void Registry_DoesNotMixIdTypes()
        {
            var options = StableIdEditorRegistry<AudioId>.Shared.GetOptions();
            Assert.That(options.Any(o => o.Id.Value == "base.gold"), Is.False);
        }
    }
}
