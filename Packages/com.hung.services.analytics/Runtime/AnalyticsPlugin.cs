using System;
using System.Collections.Generic;
using Hung.Base.Composition;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>Declares automatic analytics startup to AutomaticPluginHost when every startup gate allows it.</summary>
    internal static class AnalyticsPlugin
    {
        internal const string FeatureId = "services.analytics";

        /// <summary>Editor suppression first, so a suppressed Editor session reads no profile.</summary>
        internal static bool Evaluate(bool editorSuppressed, AutomaticStartupProfile legacy, PluginStartupProfile plugin) =>
            !editorSuppressed &&
            AutomaticStartupPolicy.Evaluate(legacy, AutomaticStartupKind.LegacyAnalytics) &&
            PluginStartupPolicy.Evaluate(plugin, FeatureId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Declare()
        {
            bool start;
            try
            {
                start = !AnalyticsBootstrap.EditorAutomaticStartupSuppressed &&
                    AutomaticStartupPolicy.ShouldStart(AutomaticStartupKind.LegacyAnalytics) &&
                    PluginStartupPolicy.ShouldStart(FeatureId);
            }
            catch (Exception error)
            {
                Debug.LogError("[Analytics] automatic startup policy failed: " + error.Message);
                start = false;
            }
            if (!start)
            {
                AnalyticsBootstrap.SuppressedAutomaticBootCount++;
                return;
            }
            AutomaticPluginHost.Collect(CreateRegistration());
        }

        internal static FeatureRegistration CreateRegistration()
        {
            AnalyticsService started = null;
            return new FeatureRegistration(FeatureId, false, Array.Empty<string>(),
                () => { started = AnalyticsBootstrap.StartAutomatic(); return new FeatureStartResult(true); },
                () => { var service = started; started = null; AnalyticsBootstrap.Uninstall(service); });
        }
    }
}

