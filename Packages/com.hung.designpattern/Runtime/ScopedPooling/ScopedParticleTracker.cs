using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern.ScopedPooling
{
    internal sealed class ScopedParticleTracker
    {
        private readonly List<PoolCheckout> tracked = new List<PoolCheckout>();

        internal void Add(PoolCheckout checkout) => tracked.Add(checkout);

        internal void Remove(PoolCheckout checkout) =>
            tracked.RemoveAll(t => t.Slot == checkout.Slot && t.Generation == checkout.Generation);

        internal void Clear() => tracked.Clear();

        internal void Tick(PoolScope scope)
        {
            foreach (var checkout in tracked.ToArray())
            {
                if (!checkout.IsValid) { Remove(checkout); continue; }
                var particle = (ParticleSystem)checkout.Instance;
                bool alive = false;
                foreach (var ps in particle.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps.IsAlive(false)) { alive = true; break; }
                if (!alive) scope.Return(checkout);
            }
        }
    }

    public sealed partial class PoolScope
    {
        private ScopedParticleTracker particleTracker;

        /// <summary>Play a scoped effect; automatic return requires an entirely nonlooping particle tree.</summary>
        public PoolStatus Play(ParticlePoolId id, Vector3 position, Quaternion rotation, Transform parent,
            bool autoReturn, out PoolCheckout checkout)
        {
            checkout = default;
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (!id.IsValid) return PoolStatus.InvalidIdentity;
            if (!particles.TryGetValue(id.Value, out var bucket)) return PoolStatus.UnknownIdentity;
            return PlayBucket(bucket, position, rotation, parent, autoReturn, out checkout);
        }

        /// <summary>Play a registered particle prefab directly without adopting catalog lookup.</summary>
        public PoolStatus Play(ParticleSystem prefab, Vector3 position, Quaternion rotation, Transform parent,
            bool autoReturn, out PoolCheckout checkout)
        {
            checkout = default;
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (prefab == null || !prefabs.TryGetValue(prefab, out var bucket)) return PoolStatus.UnknownPrefab;
            return PlayBucket(bucket, position, rotation, parent, autoReturn, out checkout);
        }

        private PoolStatus PlayBucket(ScopedPoolGroup.Bucket bucket, Vector3 position, Quaternion rotation,
            Transform parent, bool autoReturn, out PoolCheckout checkout)
        {
            checkout = default;
            if (autoReturn)
                foreach (var ps in bucket.Definition.Prefab.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps.main.loop) return PoolStatus.InvalidParticleConfiguration;
            var status = SpawnBucket<ParticleSystem>(bucket, position, rotation, parent, out checkout);
            if (status != PoolStatus.Success) return status;
            ((ParticleSystem)checkout.Instance).Play(true);
            status = CheckoutStatus(checkout);
            if (status != PoolStatus.Success)
            {
                checkout = default;
                return status;
            }
            if (autoReturn)
            {
                if (particleTracker == null) particleTracker = new ScopedParticleTracker();
                particleTracker.Add(checkout);
            }
            return PoolStatus.Success;
        }

        partial void StopParticle(PoolCheckout checkout)
        {
            particleTracker?.Remove(checkout);
            if (checkout.Instance is ParticleSystem particle && particle != null)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        partial void ClearParticles() => particleTracker?.Clear();
        partial void TickParticles() => particleTracker?.Tick(this);
    }
}
