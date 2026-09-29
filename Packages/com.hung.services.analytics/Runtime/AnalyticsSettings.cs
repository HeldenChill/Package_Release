using System.Collections.Generic;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>
    /// Analytics configuration, loaded from <c>Resources/HungAnalyticsSettings.asset</c>.
    /// Edit it through <c>Hung/Analytics/Settings</c>, which also writes the scripting defines.
    /// </summary>
    [CreateAssetMenu(menuName = "Hung/Analytics Settings", fileName = AnalyticsBootstrap.SettingsResourcePath)]
    public sealed class AnalyticsSettings : ScriptableObject
    {
        /// <summary>Verbose SDK and routing logs.</summary>
        public bool debugLog;

        /// <summary>One row per backend id.</summary>
        public List<AnalyticsBackendEntry> backends = AnalyticsBackendEntry.CreateDefaults();

        /// <summary>Entry for <paramref name="id"/>, or null.</summary>
        public AnalyticsBackendEntry Get(string id) => backends?.Find(e => e != null && e.id == id);
    }
}
