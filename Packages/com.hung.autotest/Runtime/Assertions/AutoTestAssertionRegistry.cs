using System;
using System.Collections.Generic;

namespace Hung.AutoTest
{
    /// <summary>
    /// Registry for game-specific assertion creators. The core factory handles
    /// game-agnostic assertions and falls through to this registry for everything
    /// else; game glue registers its creators at startup (see
    /// game glue). Keeps the AutoTest core free of domain types.
    /// </summary>
    public static class AutoTestAssertionRegistry
    {
        static readonly Dictionary<AutoTestAssertionType, Func<AutoTestAssertionConfig, IAutoTestAssertion>> creators = new();

        public static void Register(AutoTestAssertionType type, Func<AutoTestAssertionConfig, IAutoTestAssertion> creator)
        {
            creators[type] = creator;
        }

        public static IAutoTestAssertion TryCreate(AutoTestAssertionConfig config)
        {
            return creators.TryGetValue(config.type, out var creator) ? creator(config) : null;
        }
    }
}
