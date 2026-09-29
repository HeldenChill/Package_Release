using System.Collections.Generic;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>
    /// Registry the define-gated integration assemblies add themselves to at
    /// <c>AfterAssembliesLoaded</c>. Cleared at <c>SubsystemRegistration</c> so a disabled domain reload
    /// never registers a backend twice.
    /// </summary>
    public static class AnalyticsBackends
    {
        static readonly List<IAnalyticsBackend> registered = new List<IAnalyticsBackend>();

        /// <summary>Backends compiled into this build.</summary>
        public static IReadOnlyList<IAnalyticsBackend> Registered => registered;

        /// <summary>Adds <paramref name="backend"/> unless one with the same id is already registered.</summary>
        public static void Register(IAnalyticsBackend backend)
        {
            if (backend == null || registered.Exists(b => b.Id == backend.Id)) return;
            registered.Add(backend);
        }

        /// <summary>Removes every registered backend.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => registered.Clear();
    }
}
