using System;
using Hung.UI.Scoping;
using NUnit.Framework;

namespace Hung.Base.Tests
{
    public sealed class UiScopeTests
    {
        private sealed class FakeCanvas : IScopedUi
        {
            private readonly UiScopeRegistry registry;

            public FakeCanvas(UiScopeRegistry registry, UiScope scope)
            {
                this.registry = registry;
                Scope = scope;
            }

            public UiScope Scope { get; }
            public int CloseCount { get; private set; }
            public Action Exiting { get; set; }

            public void OnScopeExit()
            {
                registry.Unregister(this);
                CloseCount++;
                Exiting?.Invoke();
            }
        }

        [Test]
        public void Scope_path_compares_segments_not_prefixes()
        {
            Assert.IsTrue(UiScopePath.Of("Home/Shop").IsAtOrUnder(UiScopePath.Of("Home")));
            Assert.IsFalse(UiScopePath.Of("HomeExtra").IsAtOrUnder(UiScopePath.Of("Home")));
        }

        [Test]
        public void Reopening_popup_rebinds_it_to_current_phase()
        {
            var registry = new UiScopeRegistry();
            var popup = new FakeCanvas(registry, UiScope.At(UiScopePath.Of("Gameplay")));

            registry.Enter(UiScopePath.Of("Gameplay/Trader"));
            registry.Register(popup);
            registry.Enter(UiScopePath.Of("Gameplay/Defense"));
            Assert.AreEqual(1, popup.CloseCount);

            registry.Register(popup);
            registry.Enter(UiScopePath.Of("Gameplay/Trader"));
            Assert.AreEqual(2, popup.CloseCount);
        }

        [Test]
        public void Multiple_popups_can_unregister_while_scope_changes()
        {
            var registry = new UiScopeRegistry();
            registry.Enter(UiScopePath.Of("Home"));
            var a = new FakeCanvas(registry, UiScope.At(UiScopePath.Of("Home")));
            var b = new FakeCanvas(registry, UiScope.At(UiScopePath.Of("Home")));
            registry.Register(a);
            registry.Register(b);

            Assert.DoesNotThrow(() => registry.Enter(UiScopePath.Of("Gameplay")));
            Assert.AreEqual(1, a.CloseCount);
            Assert.AreEqual(1, b.CloseCount);
        }

        [Test]
        public void Global_canvas_survives_scope_change()
        {
            var registry = new UiScopeRegistry();
            var popup = new FakeCanvas(registry, UiScope.Global);
            registry.Register(popup);

            registry.Enter(UiScopePath.Of("Home"));
            registry.Enter(UiScopePath.Of("Gameplay"));

            Assert.AreEqual(0, popup.CloseCount);
        }

        [Test]
        public void Nested_scope_change_does_not_skip_outer_closures()
        {
            var registry = new UiScopeRegistry();
            registry.Enter(UiScopePath.Of("Home"));
            var first = new FakeCanvas(registry, UiScope.At(UiScopePath.Of("Home")));
            var second = new FakeCanvas(registry, UiScope.At(UiScopePath.Of("Home")));
            first.Exiting = () => registry.Enter(UiScopePath.Of("Home"));
            registry.Register(first);
            registry.Register(second);

            registry.Enter(UiScopePath.Of("Gameplay"));

            Assert.AreEqual(1, first.CloseCount);
            Assert.AreEqual(1, second.CloseCount);
        }
    }
}
