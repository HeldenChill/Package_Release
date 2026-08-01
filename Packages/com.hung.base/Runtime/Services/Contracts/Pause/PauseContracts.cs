using System;

namespace Hung.Base
{
    public enum PauseLeaseKind { Gameplay = 1, Popup = 2, Tutorial = 3, Ads = 4, Application = 5, Debug = 6 }

    public interface ITimeScale
    {
        float Scale { get; set; }
    }

    public sealed class UnityTimeScale : ITimeScale
    {
        public float Scale { get => UnityEngine.Time.timeScale; set => UnityEngine.Time.timeScale = value; }
    }

    public interface IPauseService
    {
        bool IsPaused { get; }
        int ActiveLeaseCount { get; }
        bool Acquire(PauseLease lease);
        bool Release(PauseLeaseId id);
        void ReleaseOwner(string owner);
    }

    public readonly struct PauseLeaseId : IEquatable<PauseLeaseId>
    {
        public PauseLeaseId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Pause lease id is required.", nameof(value));
            Value = value;
        }

        public string Value { get; }

        public static PauseLeaseId Create(PauseLeaseKind kind, string owner, string nonce)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Owner is required.", nameof(owner));
            if (string.IsNullOrWhiteSpace(nonce)) throw new ArgumentException("Nonce is required.", nameof(nonce));
            return new PauseLeaseId($"{(int)kind}|{owner.Trim()}|{nonce.Trim()}");
        }

        public bool Equals(PauseLeaseId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PauseLeaseId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public readonly struct PauseLease
    {
        public PauseLease(PauseLeaseId id, PauseLeaseKind kind, string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Owner is required.", nameof(owner));
            Id = id;
            Kind = kind;
            Owner = owner.Trim();
        }

        public PauseLeaseId Id { get; }
        public PauseLeaseKind Kind { get; }
        public string Owner { get; }
    }
}
