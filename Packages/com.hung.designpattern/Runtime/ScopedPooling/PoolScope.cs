using System;
using System.Collections.Generic;
using UnityEngine;
using Bucket = Hung.DesignPattern.ScopedPooling.ScopedPoolGroup.Bucket;
using Slot = Hung.DesignPattern.ScopedPooling.ScopedPoolGroup.Slot;

namespace Hung.DesignPattern.ScopedPooling
{
    /// <summary>Owns registered groups and every checkout beneath a host-owned lifetime root.</summary>
    public sealed partial class PoolScope : IDisposable
    {
        private readonly Transform root;
        private readonly Dictionary<string, ScopedPoolGroup> groups = new Dictionary<string, ScopedPoolGroup>(StringComparer.Ordinal);
        private readonly Dictionary<Component, Bucket> prefabs = new Dictionary<Component, Bucket>();
        private readonly Dictionary<string, Bucket> units = new Dictionary<string, Bucket>(StringComparer.Ordinal);
        private readonly Dictionary<string, Bucket> particles = new Dictionary<string, Bucket>(StringComparer.Ordinal);
        private readonly Dictionary<long, Slot> slots = new Dictionary<long, Slot>();
        private long nextSlot;
        private bool disposed;

        /// <summary>Actionable registration failure context; empty after successful registration.</summary>
        public string LastDiagnostic { get; private set; } = string.Empty;

        private PoolStatus Reject(PoolStatus status, string context)
        {
            LastDiagnostic = status + ": " + context;
            return status;
        }

        private PoolScope(Transform root) { this.root = root; }

        /// <summary>Validate a root whose lifetime was already arranged by its host.</summary>
        public static PoolStatus TryCreate(Transform root, PoolLifetime lifetime, out PoolScope scope)
        {
            scope = null;
            if (root == null || !root.gameObject.scene.IsValid() || !root.gameObject.scene.isLoaded)
                return PoolStatus.InvalidRoot;
            if (!root.gameObject.activeInHierarchy) return PoolStatus.InvalidRoot;
            bool persistent = root.gameObject.scene.name == "DontDestroyOnLoad";
            if ((lifetime == PoolLifetime.Application) != persistent) return PoolStatus.InvalidRoot;
            if (lifetime != PoolLifetime.Scene && lifetime != PoolLifetime.Application) return PoolStatus.InvalidConfiguration;
            scope = new PoolScope(root);
            scope.AttachRunner();
            return PoolStatus.Success;
        }

        private PoolStatus Live()
        {
            if (!disposed && root == null) Dispose();
            return disposed ? PoolStatus.DisposedScope : PoolStatus.Success;
        }

        /// <summary>Validate and warm a group atomically; definitions become fixed for this scope.</summary>
        public PoolStatus Register(string groupKey, params PoolDefinition[] definitions)
        {
            if (Live() != PoolStatus.Success) return Reject(PoolStatus.DisposedScope, "group=" + groupKey);
            if (string.IsNullOrWhiteSpace(groupKey) || definitions == null || definitions.Length == 0)
                return Reject(PoolStatus.InvalidConfiguration, "group=" + groupKey + "; expected a nonempty key and definitions");
            if (groups.ContainsKey(groupKey)) return Reject(PoolStatus.DuplicateGroup, "group=" + groupKey);
            var copy = (PoolDefinition[])definitions.Clone();
            var seenPrefabs = new HashSet<Component>();
            var seenUnits = new HashSet<string>(StringComparer.Ordinal);
            var seenParticles = new HashSet<string>(StringComparer.Ordinal);
            for (int definitionIndex = 0; definitionIndex < copy.Length; definitionIndex++)
            {
                var d = copy[definitionIndex];
                string context = "group=" + groupKey + "; entry=" + definitionIndex + "; identity=" + d?.Identity;
                if (d == null || d.Prefab == null || d.Warmup < 0 || d.HardLimit < 0 ||
                    (d.HardLimit != 0 && d.Warmup > d.HardLimit)) return Reject(PoolStatus.InvalidConfiguration, context);
                if (d.Kind != PoolKind.Unit && d.Kind != PoolKind.Particle) return Reject(PoolStatus.InvalidConfiguration, context);
                if ((d.Kind == PoolKind.Particle) != (d.Prefab is ParticleSystem)) return Reject(PoolStatus.WrongPrefabType, context);
                if (d.Kind == PoolKind.Particle && !ValidParticlePrefab((ParticleSystem)d.Prefab))
                    return Reject(PoolStatus.InvalidParticleConfiguration, context);
                if (prefabs.ContainsKey(d.Prefab) || !seenPrefabs.Add(d.Prefab)) return Reject(PoolStatus.DuplicatePrefab, context);
                if (d.Identity == null) continue;
                if (string.IsNullOrWhiteSpace(d.Identity)) return Reject(PoolStatus.InvalidIdentity, context);
                var index = d.Kind == PoolKind.Unit ? units : particles;
                var seen = d.Kind == PoolKind.Unit ? seenUnits : seenParticles;
                if (index.ContainsKey(d.Identity) || !seen.Add(d.Identity)) return Reject(PoolStatus.DuplicateIdentity, context);
            }
            ScopedPoolGroup attempt = null;
            try
            {
                attempt = new ScopedPoolGroup(root, groupKey);
                foreach (var d in copy)
                {
                    var bucket = new Bucket { Definition = d, Group = attempt };
                    attempt.Buckets.Add(bucket);
                    for (int i = 0; i < d.Warmup; i++) bucket.Inactive.Enqueue(Allocate(bucket));
                }
            }
            catch (Exception ex)
            {
                if (attempt != null)
                {
                    foreach (var b in attempt.Buckets) foreach (var s in b.All) slots.Remove(s.Id);
                    attempt.Destroy();
                }
                Debug.LogException(ex);
                return Reject(PoolStatus.AllocationFailed, "group=" + groupKey + "; " + ex.Message);
            }
            groups.Add(groupKey, attempt);
            foreach (var bucket in attempt.Buckets)
            {
                var d = bucket.Definition;
                prefabs.Add(d.Prefab, bucket);
                if (d.Identity != null) (d.Kind == PoolKind.Unit ? units : particles).Add(d.Identity, bucket);
            }
            LastDiagnostic = string.Empty;
            return PoolStatus.Success;
        }

        /// <summary>Resolve once and register a frozen product catalog snapshot without a fallback lookup.</summary>
        public PoolStatus Register(string groupKey, IPoolCatalogResolver resolver)
        {
            if (Live() != PoolStatus.Success) return Reject(PoolStatus.DisposedScope, "group=" + groupKey);
            if (resolver == null) return Reject(PoolStatus.InvalidConfiguration, "group=" + groupKey + "; null resolver");
            var status = resolver.Resolve(out var definitions);
            return status == PoolStatus.Success ? Register(groupKey, definitions) : Reject(status, "group=" + groupKey + "; resolver=" + resolver.GetType().FullName);
        }

        private Func<Component, Transform, Component> instantiate =
            (prefab, parent) => UnityEngine.Object.Instantiate(prefab, parent);

        private Slot Allocate(Bucket bucket)
        {
            var instance = instantiate(bucket.Definition.Prefab, bucket.Group.Staging);
            instance.gameObject.SetActive(false);
            var slot = new Slot { Id = ++nextSlot, Instance = instance, Bucket = bucket };
            bucket.All.Add(slot);
            slots.Add(slot.Id, slot);
            return slot;
        }

        /// <summary>Spawn directly from a registered prefab, without a catalog identity.</summary>
        public PoolStatus Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent,
            out PoolCheckout checkout) where T : Component
        {
            checkout = default;
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (prefab == null || !prefabs.TryGetValue(prefab, out var bucket)) return PoolStatus.UnknownPrefab;
            return SpawnBucket<T>(bucket, position, rotation, parent, out checkout);
        }

        /// <summary>Spawn a unit by exact scope-local product identity.</summary>
        public PoolStatus Spawn<T>(UnitPoolId id, Vector3 position, Quaternion rotation, Transform parent,
            out PoolCheckout checkout) where T : Component
        {
            checkout = default;
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (!id.IsValid) return PoolStatus.InvalidIdentity;
            if (!units.TryGetValue(id.Value, out var bucket)) return PoolStatus.UnknownIdentity;
            return SpawnBucket<T>(bucket, position, rotation, parent, out checkout);
        }

        private PoolStatus SpawnBucket<T>(Bucket bucket, Vector3 position, Quaternion rotation, Transform parent,
            out PoolCheckout checkout) where T : Component
        {
            checkout = default;
            if (!(bucket.Definition.Prefab is T)) return PoolStatus.WrongPrefabType;
            if (parent != null && parent != root && !parent.IsChildOf(root)) return PoolStatus.InvalidRoot;
            for (int i = bucket.All.Count - 1; i >= 0; i--)
                if (bucket.All[i].Instance == null) RetireDestroyedSlot(bucket.All[i]);
            Slot slot = bucket.Inactive.Count > 0 ? bucket.Inactive.Dequeue() : null;
            if (slot == null)
            {
                if (bucket.Definition.HardLimit != 0 && bucket.All.Count >= bucket.Definition.HardLimit)
                    return PoolStatus.CapacityExhausted;
                try { slot = Allocate(bucket); }
                catch (Exception ex) { Debug.LogException(ex); return PoolStatus.AllocationFailed; }
            }
            slot.Generation++;
            slot.Active = true;
            var candidateCheckout = new PoolCheckout(this, slot.Id, slot.Generation, slot.Instance);
            var instance = slot.Instance;
            instance.transform.SetParent(parent != null ? parent : bucket.Group.Container, false);
            var status = CheckoutStatus(candidateCheckout);
            if (status != PoolStatus.Success) return status;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            status = CheckoutStatus(candidateCheckout);
            if (status != PoolStatus.Success) return status;
            checkout = candidateCheckout;
            return PoolStatus.Success;
        }

        // Unity lifecycle callbacks may return, dispose or recheck out this instance synchronously.
        private PoolStatus CheckoutStatus(PoolCheckout checkout)
        {
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            return IsCurrent(checkout) ? PoolStatus.Success : PoolStatus.StaleCheckout;
        }

        internal bool IsCurrent(PoolCheckout checkout)
        {
            if (Live() != PoolStatus.Success || !ReferenceEquals(checkout.Scope, this) ||
                !slots.TryGetValue(checkout.Slot, out var slot) || slot.Generation != checkout.Generation)
                return false;
            if (slot.Instance == null)
            {
                RetireDestroyedSlot(slot);
                return false;
            }
            return slot.Active;
        }

        private void RetireDestroyedSlot(Slot slot)
        {
            if (!slots.Remove(slot.Id)) return;
            slot.Active = false;
            slot.Bucket.All.Remove(slot);
            var inactive = slot.Bucket.Inactive;
            for (int i = inactive.Count; i > 0; i--)
            {
                var candidate = inactive.Dequeue();
                if (!ReferenceEquals(candidate, slot)) inactive.Enqueue(candidate);
            }
            var checkout = new PoolCheckout(this, slot.Id, slot.Generation, slot.Instance);
            CancelForCheckout(checkout);
            StopParticle(checkout);
        }

        /// <summary>Return one checkout; stale or foreign tokens never mutate a later use.</summary>
        public PoolStatus Return(PoolCheckout checkout)
        {
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (!ReferenceEquals(checkout.Scope, this)) return PoolStatus.WrongScope;
            if (!slots.TryGetValue(checkout.Slot, out var slot) || slot.Generation != checkout.Generation)
                return PoolStatus.StaleCheckout;
            if (slot.Instance == null)
            {
                RetireDestroyedSlot(slot);
                return PoolStatus.StaleCheckout;
            }
            if (!slot.Active) return PoolStatus.AlreadyReturned;
            slot.Active = false;
            CancelForCheckout(checkout);
            StopParticle(checkout);
            if (slot.Instance == null) { RetireDestroyedSlot(slot); return PoolStatus.StaleCheckout; }
            slot.Instance.gameObject.SetActive(false);
            slot.Instance.transform.SetParent(slot.Bucket.Group.Staging, false);
            slot.Bucket.Inactive.Enqueue(slot);
            return PoolStatus.Success;
        }

        /// <summary>Return every active checkout in a selected group, without product cleanup callbacks.</summary>
        public PoolStatus Collect(string groupKey)
        {
            if (Live() != PoolStatus.Success) return PoolStatus.DisposedScope;
            if (groupKey == null || !groups.TryGetValue(groupKey, out var group)) return PoolStatus.UnknownIdentity;
            var active = new List<PoolCheckout>();
            foreach (var b in group.Buckets) foreach (var s in b.All)
                if (s.Active) active.Add(new PoolCheckout(this, s.Id, s.Generation, s.Instance));
            foreach (var token in active) Return(token);
            return PoolStatus.Success;
        }

        /// <summary>Destroy only owned resources, including tracked instances reparented by product code.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            CancelAllRequests();
            ClearParticles();
            DetachRunner();
            foreach (var group in groups.Values) group.Destroy();
            groups.Clear();
            prefabs.Clear();
            units.Clear();
            particles.Clear();
            slots.Clear();
        }

        private static bool ValidParticlePrefab(ParticleSystem prefab)
        {
            foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                if (ps.main.playOnAwake || ps.main.stopAction == ParticleSystemStopAction.Destroy ||
                    ps.main.stopAction == ParticleSystemStopAction.Disable) return false;
            return true;
        }

        partial void AttachRunner();
        partial void DetachRunner();
        partial void CancelForCheckout(PoolCheckout checkout);
        partial void CancelAllRequests();
        partial void StopParticle(PoolCheckout checkout);
        partial void ClearParticles();
        internal bool IsPending(ReturnRequest request) { bool pending = false; CheckPending(request, ref pending); return pending; }
        partial void CheckPending(ReturnRequest request, ref bool pending);
    }
}
