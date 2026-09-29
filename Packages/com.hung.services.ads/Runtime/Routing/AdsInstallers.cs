using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    /// <summary>Creates one vendor's providers and adds them to the set. Implemented in each integration assembly.</summary>
    public interface IAdsProviderInstaller
    {
        /// <summary>Id matching an AdsProviderEntry.id, e.g. AdsProviderId.Max.</summary>
        string ProviderId { get; }

        /// <summary>Add this vendor's providers (as components on <paramref name="host"/>) for every format whose unit id is set.</summary>
        void Install(GameObject host, AdsProviderEntry entry, AdsProviderSet set);
    }

    /// <summary>
    /// Installers compiled into this build. Integration assemblies register at
    /// AfterAssembliesLoaded; cleared at SubsystemRegistration (runs first) for domain-reload-off play mode.
    /// </summary>
    public static class AdsInstallers
    {
        static readonly List<IAdsProviderInstaller> installers = new List<IAdsProviderInstaller>();

        /// <summary>Installers in registration order.</summary>
        public static IReadOnlyList<IAdsProviderInstaller> All => installers;

        /// <summary>Adds an installer; one with the same ProviderId is replaced.</summary>
        public static void Register(IAdsProviderInstaller installer)
        {
            if (installer == null || string.IsNullOrEmpty(installer.ProviderId)) return;
            installers.RemoveAll(i => i.ProviderId == installer.ProviderId);
            installers.Add(installer);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => installers.Clear();
    }
}
