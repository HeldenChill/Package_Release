using System;
using Hung.Base;
using Hung.Base.Composition;
using Hung.Data.Rewards;
using NUnit.Framework;
namespace Hung.Data.Tests.Bw4
{
    public sealed class RewardCompositionTests
    {
        private interface IProbe { }
        private sealed class Probe : IProbe, IDisposable { internal bool Stopped; public void Dispose() { Stopped = true; } }
        [Test] public void UnselectedFactoryDoesNoIo()
        {
            using (var f = new Bw4Fixture())
            {
                int created = 0; var binding = new RewardFeatureBinding<IProbe, Probe>(() => { created++; f.DailyStore.Load(out _, out _); return new Probe(); }, _ => new FeatureStartResult(true));
                var registration = binding.Registration("fixture.reward", true, Array.Empty<string>());
                Assert.That(registration.Id, Is.EqualTo("fixture.reward")); Assert.That(created, Is.Zero); Assert.That(f.Store.Reads, Is.Zero); Assert.That(f.Store.Writes, Is.Zero);
            }
        }
        [Test] public void HostRegistersAndStopsExactInstance()
        {
            ServiceSlots.ResetForTests();
            int created = 0; var binding = new RewardFeatureBinding<IProbe, Probe>(() => { created++; return new Probe(); }, _ => new FeatureStartResult(true));
            var host = new FeatureHost(); host.Register(binding.Registration("fixture.reward", true, Array.Empty<string>()));
            Assert.That(host.Start().ProductReady, Is.True); var first = binding.Current;
            Assert.That(ServiceSlots.Get<IProbe>(), Is.SameAs(first)); host.Dispose(); Assert.That(first.Stopped, Is.True);
            Assert.That(ServiceSlots.TryGet<IProbe>(out _), Is.False); Assert.That(created, Is.EqualTo(1));
        }
        [Test] public void ActiveOwnerPreventsFactorySideEffects()
        {
            ServiceSlots.ResetForTests(); var old = new Probe(); ServiceSlots.Register<IProbe>(old);
            int created = 0; var binding = new RewardFeatureBinding<IProbe, Probe>(() => { created++; return new Probe(); }, _ => new FeatureStartResult(true));
            var host = new FeatureHost(); host.Register(binding.Registration("fixture.reward", true, Array.Empty<string>()));
            Assert.That(host.Start().ProductReady, Is.False); Assert.That(created, Is.Zero); Assert.That(ServiceSlots.Get<IProbe>(), Is.SameAs(old));
            host.Dispose(); ServiceSlots.Unregister<IProbe>(old);
        }
        [Test] public void FailedInitializationDisposesCandidateAndLeavesSlotEmpty()
        {
            ServiceSlots.ResetForTests(); var candidate = new Probe();
            var binding = new RewardFeatureBinding<IProbe, Probe>(() => candidate, _ => new FeatureStartResult(false, "fixture.failed"));
            var host = new FeatureHost(); host.Register(binding.Registration("fixture.reward", true, Array.Empty<string>()));
            Assert.That(host.Start().ProductReady, Is.False); Assert.That(candidate.Stopped, Is.True); Assert.That(ServiceSlots.TryGet<IProbe>(out _), Is.False);
        }
        [Test] public void StaleStopCannotRemoveReplacementAndFreshSessionCanStart()
        {
            ServiceSlots.ResetForTests();
            var first = new RewardFeatureBinding<IProbe, Probe>(() => new Probe(), _ => new FeatureStartResult(true));
            var host = new FeatureHost(); host.Register(first.Registration("fixture.first", true, Array.Empty<string>()));
            Assert.That(host.Start().ProductReady, Is.True); var old = first.Current;
            var replacement = new Probe(); Assert.That(ServiceSlots.Register<IProbe>(replacement), Is.EqualTo(ServiceRegistration.Replaced));
            host.Dispose(); Assert.That(old.Stopped, Is.True); Assert.That(ServiceSlots.Get<IProbe>(), Is.SameAs(replacement));
            ServiceSlots.Unregister<IProbe>(replacement); replacement.Dispose();
            var second = new RewardFeatureBinding<IProbe, Probe>(() => new Probe(), _ => new FeatureStartResult(true));
            var next = new FeatureHost(); next.Register(second.Registration("fixture.second", true, Array.Empty<string>()));
            Assert.That(next.Start().ProductReady, Is.True); next.Dispose(); Assert.That(ServiceSlots.TryGet<IProbe>(out _), Is.False);
        }
    }
}
