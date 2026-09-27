using Hung.UI.Scoping;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Hung.Base.Tests
{
    public sealed class UiScopeRegistryAllocationTests
    {
        private sealed class CountingScoped : IScopedUi
        {
            public UiScope Scope { get; set; }
            public int Exits;
            public System.Action OnExit;
            public void OnScopeExit()
            {
                Exits++;
                OnExit?.Invoke();
            }
        }

        [Test]
        public void Enter_with_bound_canvases_does_not_allocate()
        {
            var registry = new UiScopeRegistry();
            UiScopePath home = UiScopePath.Of("Home");
            UiScopePath gameplay = UiScopePath.Of("Gameplay");
            registry.Enter(home);
            var scoped = new CountingScoped { Scope = UiScope.At(home) };
            registry.Register(scoped);
            registry.Enter(gameplay);
            registry.Enter(home);

            // Both Enters do real work on every invocation (the constraint may invoke the delegate
            // more than once); a single Enter(gameplay) would early-return on repeat and pass falsely.
            NUnit.Framework.Assert.That(() =>
            {
                registry.Enter(gameplay);
                registry.Enter(home);
            }, Is.Not.AllocatingGCMemory());
            NUnit.Framework.Assert.GreaterOrEqual(scoped.Exits, 2);
        }

        [Test]
        public void Nested_enter_during_scope_exit_closes_both_levels()
        {
            var registry = new UiScopeRegistry();
            UiScopePath shop = UiScopePath.Of("Home/Shop");
            registry.Enter(shop);
            var outer = new CountingScoped { Scope = UiScope.At(UiScopePath.Of("Home")) };
            var inner = new CountingScoped { Scope = UiScope.At(shop) };
            registry.Register(outer);
            registry.Register(inner);
            outer.OnExit = () =>
            {
                registry.Unregister(outer);
                registry.Enter(UiScopePath.Of("Gameplay/Wave"));
            };
            inner.OnExit = () => registry.Unregister(inner);

            registry.Enter(UiScopePath.Of("Gameplay"));

            NUnit.Framework.Assert.AreEqual(1, outer.Exits);
            NUnit.Framework.Assert.AreEqual(1, inner.Exits, "Inner canvas closed exactly once despite re-entry.");
        }
    }
}
