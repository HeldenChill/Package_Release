using System;
using UnityEngine;

namespace Hung.DesignPattern.ScopedPooling
{
    /// <summary>Status outcomes for pool scope operations.</summary>
    public enum PoolStatus
    {
        /// <summary>Operation succeeded.</summary>
        Success,
        /// <summary>Item was already returned to the pool.</summary>
        AlreadyReturned,
        /// <summary>Specified pool identity is invalid, empty, or whitespace.</summary>
        InvalidIdentity,
        /// <summary>Pool configuration or parameter is invalid.</summary>
        InvalidConfiguration,
        /// <summary>A pool group with the specified key is already registered in this scope.</summary>
        DuplicateGroup,
        /// <summary>A definition with the specified identity is already registered.</summary>
        DuplicateIdentity,
        /// <summary>The specified prefab is already registered in this scope.</summary>
        DuplicatePrefab,
        /// <summary>The identity was not found in this scope.</summary>
        UnknownIdentity,
        /// <summary>The prefab was not found in this scope.</summary>
        UnknownPrefab,
        /// <summary>Prefab component type does not match requested component type.</summary>
        WrongPrefabType,
        /// <summary>Hard limit reached and capacity is exhausted.</summary>
        CapacityExhausted,
        /// <summary>Lifetime root is invalid, unparented, or outside expected persistence context.</summary>
        InvalidRoot,
        /// <summary>Scope has been disposed and cannot perform operations.</summary>
        DisposedScope,
        /// <summary>Checkout token or handle does not belong to this scope.</summary>
        WrongScope,
        /// <summary>Checkout token is stale due to generation mismatch or deactivation.</summary>
        StaleCheckout,
        /// <summary>Requested return delay is negative, infinite, or NaN.</summary>
        InvalidDelay,
        /// <summary>Delayed return request is stale or cancelled.</summary>
        StaleRequest,
        /// <summary>Instantiation or warmup allocation failed.</summary>
        AllocationFailed,
        /// <summary>Particle configuration is invalid for pooling or playback mode.</summary>
        InvalidParticleConfiguration
    }

    /// <summary>Lifetime boundary for a pool scope.</summary>
    public enum PoolLifetime
    {
        /// <summary>Scope bounded by scene lifetime; destroyed upon scene unload.</summary>
        Scene,
        /// <summary>Scope bounded by application lifetime; persistent across scene loads.</summary>
        Application
    }

    /// <summary>Kind of pooled object.</summary>
    public enum PoolKind
    {
        /// <summary>Standard unit component pool.</summary>
        Unit,
        /// <summary>Particle system pool.</summary>
        Particle
    }

    /// <summary>Clock used for delayed return timing.</summary>
    public enum PoolClock
    {
        /// <summary>Scaled game time (Time.deltaTime).</summary>
        Scaled,
        /// <summary>Unscaled real time (Time.unscaledDeltaTime).</summary>
        Unscaled
    }

    /// <summary>Typed exact ordinal identity for pooled units.</summary>
    public readonly struct UnitPoolId : IEquatable<UnitPoolId>
    {
        /// <summary>Raw string identifier.</summary>
        public string Value { get; }
        /// <summary>Whether the identity is non-null and not whitespace.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        /// <summary>Initializes a new UnitPoolId.</summary>
        public UnitPoolId(string value) { Value = value; }
        /// <summary>Compares two identities for ordinal equality.</summary>
        public bool Equals(UnitPoolId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        /// <summary>Compares object for equality.</summary>
        public override bool Equals(object obj) => obj is UnitPoolId other && Equals(other);
        /// <summary>Calculates hash code from ordinal string.</summary>
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        /// <summary>Returns string representation.</summary>
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Typed exact ordinal identity for pooled particles.</summary>
    public readonly struct ParticlePoolId : IEquatable<ParticlePoolId>
    {
        /// <summary>Raw string identifier.</summary>
        public string Value { get; }
        /// <summary>Whether the identity is non-null and not whitespace.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        /// <summary>Initializes a new ParticlePoolId.</summary>
        public ParticlePoolId(string value) { Value = value; }
        /// <summary>Compares two identities for ordinal equality.</summary>
        public bool Equals(ParticlePoolId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        /// <summary>Compares object for equality.</summary>
        public override bool Equals(object obj) => obj is ParticlePoolId other && Equals(other);
        /// <summary>Calculates hash code from ordinal string.</summary>
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        /// <summary>Returns string representation.</summary>
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Immutable pool configuration definition.</summary>
    public sealed class PoolDefinition
    {
        /// <summary>Target prefab component.</summary>
        public Component Prefab { get; }
        /// <summary>Pool category kind.</summary>
        public PoolKind Kind { get; }
        /// <summary>Optional product-scoped identity.</summary>
        public string Identity { get; }
        /// <summary>Preloaded instance count.</summary>
        public int Warmup { get; }
        /// <summary>Maximum active instance limit; 0 means uncapped.</summary>
        public int HardLimit { get; }
        /// <summary>Initializes a new pool definition.</summary>
        public PoolDefinition(Component prefab, PoolKind kind, string identity = null,
            int warmup = 0, int hardLimit = 0)
        { Prefab = prefab; Kind = kind; Identity = identity; Warmup = warmup; HardLimit = hardLimit; }
    }

    /// <summary>Resolves catalog configurations into immutable definitions.</summary>
    public interface IPoolCatalogResolver
    {
        /// <summary>Resolves definitions from authoring catalog.</summary>
        PoolStatus Resolve(out PoolDefinition[] definitions);
    }

    /// <summary>Checked-out pool instance token tracking scope and generation.</summary>
    public readonly struct PoolCheckout
    {
        internal readonly PoolScope Scope;
        internal readonly long Slot;
        internal readonly ulong Generation;
        /// <summary>Checked-out component instance.</summary>
        public Component Instance { get; }
        /// <summary>Whether this checkout is valid and active in its scope.</summary>
        public bool IsValid => Scope != null && Scope.IsCurrent(this);
        internal PoolCheckout(PoolScope scope, long slot, ulong generation, Component instance)
        { Scope = scope; Slot = slot; Generation = generation; Instance = instance; }
    }

    /// <summary>Handle to a scheduled delayed return request.</summary>
    public readonly struct ReturnRequest
    {
        internal readonly PoolScope Scope;
        internal readonly long Id;
        /// <summary>Whether this request is pending in its scope.</summary>
        public bool IsValid => Scope != null && Scope.IsPending(this);
        internal ReturnRequest(PoolScope scope, long id) { Scope = scope; Id = id; }
    }
}
