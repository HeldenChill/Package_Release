using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.AutoTest
{
    /// <summary>
    /// Owns registration and preflight evaluation of <see cref="AutoTestRuntimeCapability"/> providers.
    /// Providers register a probe per single capability bit; the runner evaluates required
    /// capabilities before invoking a case's scenario executor.
    /// </summary>
    public static class AutoTestCapabilityRegistry
    {
        private static readonly Dictionary<AutoTestRuntimeCapability, Func<AutoTestCapabilityCheck>> probes
            = new Dictionary<AutoTestRuntimeCapability, Func<AutoTestCapabilityCheck>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            probes.Clear();
        }

        internal static void ResetForTests()
        {
            probes.Clear();
        }

        /// <summary>
        /// Registers a probe for exactly one capability bit. Throws if the capability is
        /// <see cref="AutoTestRuntimeCapability.None"/>, not a single bit, already registered, or the probe is null.
        /// </summary>
        public static void Register(AutoTestRuntimeCapability capability, Func<AutoTestCapabilityCheck> probe)
        {
            if (capability == AutoTestRuntimeCapability.None || !IsSingleBit(capability))
                throw new ArgumentException("Capability must be exactly one non-None flag.", nameof(capability));
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            if (probes.ContainsKey(capability))
                throw new InvalidOperationException("Capability already registered: " + capability);

            probes[capability] = probe;
        }

        /// <summary>
        /// Evaluates whether every bit in <paramref name="required"/> has a registered, available probe.
        /// </summary>
        public static bool TryEvaluate(AutoTestRuntimeCapability required, out string diagnostic)
        {
            if (required == AutoTestRuntimeCapability.None)
            {
                diagnostic = "No runtime capabilities required.";
                return true;
            }

            uint remaining = unchecked((uint)(int)required);
            while (remaining != 0)
            {
                uint bit = remaining & (~remaining + 1u);
                var capability = (AutoTestRuntimeCapability)unchecked((int)bit);
                remaining &= ~bit;

                if (!probes.TryGetValue(capability, out Func<AutoTestCapabilityCheck> probe))
                {
                    diagnostic = "Required capability has no provider: " + capability +
                        " (value " + bit + ")";
                    return false;
                }

                AutoTestCapabilityCheck check = probe();
                if (!check.IsAvailable)
                {
                    diagnostic = "Required capability unavailable: " + capability + "; " + check.Diagnostic;
                    return false;
                }
            }

            diagnostic = "All required runtime capabilities are available.";
            return true;
        }

        private static bool IsSingleBit(AutoTestRuntimeCapability capability)
        {
            int value = (int)capability;
            return value != 0 && (value & (value - 1)) == 0;
        }
    }
}
