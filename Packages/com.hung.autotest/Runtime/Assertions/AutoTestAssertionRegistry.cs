using System;
using System.Collections.Generic;

namespace Hung.AutoTest
{
    public readonly struct AutoTestAssertionDescriptor
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public AutoTestAssertionDescriptor(string id, string displayName, string description)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
        }
    }

    /// <summary>
    /// Registry for game-specific assertion creators. The core factory handles
    /// game-agnostic assertions and falls through to this registry for everything
    /// else; game glue registers its creators at startup (see
    /// game glue). Keeps the AutoTest core free of domain types.
    /// </summary>
    public static class AutoTestAssertionRegistry
    {
        static readonly Dictionary<AutoTestAssertionType, Func<AutoTestAssertionConfig, IAutoTestAssertion>> creators = new();

        static readonly Dictionary<string, Func<AutoTestAssertionConfig, IAutoTestAssertion>> stringCreators
            = new(StringComparer.Ordinal);
        static readonly Dictionary<string, AutoTestAssertionDescriptor> descriptors
            = new(StringComparer.Ordinal);

        public static void Register(AutoTestAssertionType type, Func<AutoTestAssertionConfig, IAutoTestAssertion> creator)
        {
            creators[type] = creator;
        }

        public static IAutoTestAssertion TryCreate(AutoTestAssertionConfig config)
        {
            return creators.TryGetValue(config.type, out var creator) ? creator(config) : null;
        }

        public static void Register(
            string id,
            Func<AutoTestAssertionConfig, IAutoTestAssertion> creator,
            AutoTestAssertionDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Assertion id must not be blank.", nameof(id));
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));

            string trimmedId = id.Trim();
            if (stringCreators.ContainsKey(trimmedId))
                throw new InvalidOperationException($"Assertion id '{trimmedId}' is already registered.");

            stringCreators[trimmedId] = creator;
            descriptors[trimmedId] = descriptor;
        }

        public static IAutoTestAssertion TryCreate(string id, AutoTestAssertionConfig config)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            string trimmedId = id.Trim();
            return stringCreators.TryGetValue(trimmedId, out var creator) ? creator(config) : null;
        }

        public static IReadOnlyCollection<AutoTestAssertionDescriptor> Descriptors => descriptors.Values;

        internal static void ResetStringRegistryForTests()
        {
            stringCreators.Clear();
            descriptors.Clear();
        }
    }
}
