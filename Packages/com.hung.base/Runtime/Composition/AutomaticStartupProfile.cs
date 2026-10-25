using System;
using UnityEngine;

namespace Hung.Base.Composition
{
    /// <summary>Only automatic legacy startup; explicit composition is unaffected.</summary>
    public enum AutomaticStartupKind { LegacyPersistence = 0, LegacyAnalytics = 1 }

    /// <summary>Product-owned Resources policy. Packages ship no instance.</summary>
    [CreateAssetMenu(menuName = "Hung/Composition/Automatic startup profile")]
    public sealed class AutomaticStartupProfile : ScriptableObject
    {
        /// <summary>Allow the legacy Data factory's automatic BeforeSceneLoad installation.</summary>
        public bool legacyPersistence = true;
        /// <summary>Allow legacy Analytics automatic boot; explicit installation remains available.</summary>
        public bool legacyAnalytics = true;
    }

    /// <summary>Reads the product's optional startup profile without caching a play-session decision.</summary>
    public static class AutomaticStartupPolicy
    {
        /// <summary>Unique Resources path without an extension; absence enables both legacy hooks.</summary>
        public const string ResourcePath = "HungAutomaticStartup";

        /// <summary>No cached selection survives subsequent play sessions.</summary>
        public static bool ShouldStart(AutomaticStartupKind kind) =>
            Evaluate(Resources.Load<AutomaticStartupProfile>(ResourcePath), kind);

        /// <summary>Absence preserves legacy startup; unknown selectors fail explicitly.</summary>
        public static bool Evaluate(AutomaticStartupProfile profile, AutomaticStartupKind kind)
        {
            switch (kind)
            {
                case AutomaticStartupKind.LegacyPersistence:
                    return profile == null || profile.legacyPersistence;
                case AutomaticStartupKind.LegacyAnalytics:
                    return profile == null || profile.legacyAnalytics;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
