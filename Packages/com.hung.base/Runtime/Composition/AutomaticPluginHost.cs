using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base.Composition
{
    /// <summary>Collects explicit plugin declarations, then delegates ordered startup to FeatureHost.</summary>
    public static class AutomaticPluginHost
    {
        private static readonly List<FeatureRegistration> declarations = new List<FeatureRegistration>();
        private static FeatureHost host;
        private static FeatureBootReport failedReport;
        private static bool frozen;
        private static bool operating;
        private static bool stopped;

        /// <summary>Collects a declaration before boot; resubmitting the same object is idempotent.</summary>
        public static void Collect(FeatureRegistration declaration)
        {
            if (declaration == null) throw new ArgumentNullException(nameof(declaration));
            if (frozen || stopped) throw new InvalidOperationException("Plugin collection is closed.");
            foreach (var existing in declarations) if (ReferenceEquals(existing, declaration)) return;
            declarations.Add(declaration);
        }
        /// <summary>Returns actual host state after boot, including shutdown state.</summary>
        public static FeatureBootReport GetReport()
        {
            if (!frozen) throw new InvalidOperationException("Automatic plugin startup has not run.");
            if (failedReport != null) return failedReport;
            return host.GetReport();
        }
        /// <summary>Stops initialized plugins once in reverse dependency order.</summary>
        public static void Shutdown()
        {
            if (operating) throw new InvalidOperationException("Automatic plugin host operation is already in progress.");
            if (stopped) return;
            stopped = true;
            host?.Dispose();
        }
        private static FeatureBootReport StartCollected()
        {
            if (operating) throw new InvalidOperationException("Automatic plugin host operation is already in progress.");
            if (stopped) throw new InvalidOperationException("Automatic plugin host has stopped.");
            if (frozen) return GetReport();
            frozen = true;
            operating = true;
            try
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var duplicates = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var declaration in declarations)
                    if (!ids.Add(declaration.Id)) duplicates.Add(declaration.Id);
                if (duplicates.Count != 0)
                {
                    string detail = string.Join(", ", duplicates);
                    failedReport = new FeatureBootReport(false, Array.Empty<FeatureReport>(),
                        new[] { new FeatureDiagnostic("automatic.plugins", "PLUGIN_DUPLICATE_IDS", detail) });
                    throw new InvalidOperationException("Duplicate plugin feature IDs: " + detail + ".");
                }
                declarations.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id,b.Id));
                host = new FeatureHost();
                foreach (var declaration in declarations) host.Register(declaration);
                return host.Start();
            }
            finally { operating = false; }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlay()
        {
            Shutdown();
            declarations.Clear(); host = null; failedReport = null;
            frozen = false; operating = false; stopped = false;
            ServiceSlots.ResetForTests();
            PluginStartupPolicy.ResetSession();
            Application.quitting -= Shutdown;
            Application.quitting += Shutdown;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            try { StartCollected(); }
            catch (Exception error) { Debug.LogError("[AutomaticPluginHost] " + error.Message); }
        }
    }
}
