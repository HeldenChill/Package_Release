using System;
using System.Collections.Generic;
using Hung.Analytics.Tracking;
using Hung.Base;
using UnityEngine;

namespace Hung.Analytics
{
    /// <summary>
    /// Composition root. Before the first scene loads it builds one <see cref="AnalyticsService"/> from the
    /// registered backends and the settings asset, then sets <c>Locator.Analytics</c> and
    /// <c>Locator.RevenueSink</c>. No prefab is needed; with zero backends the locator still holds a no-op proxy.
    /// </summary>
    public static class AnalyticsBootstrap
    {
        /// <summary>Resources path (no extension) of the settings asset.</summary>
        public const string SettingsResourcePath = "HungAnalyticsSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var settings = Resources.Load<AnalyticsSettings>(SettingsResourcePath);
            if (settings == null)
                Debug.LogWarning($"[Analytics] Resources/{SettingsResourcePath}.asset missing - using default routing. Open Hung/Analytics/Settings.");
            var service = Install(settings, AnalyticsBackends.Registered);

            var tracking = TrackingBootstrap.Install(service, Resources.Load<TrackingSettings>(TrackingSettings.ResourcePath),
                new DatabaseTrackingStateStore(), new SystemTrackingClock());
            if (tracking != null) TrackingRunner.Create(tracking);
        }

        /// <summary>
        /// Initializes each backend with its settings entry (or its default), keeps the ones that initialize,
        /// and registers the resulting proxy in the locator.
        /// </summary>
        public static AnalyticsService Install(AnalyticsSettings settings, IReadOnlyList<IAnalyticsBackend> backends, Func<float> clock = null)
        {
            var service = new AnalyticsService(clock);
            bool debugLog = settings != null && settings.debugLog;
            foreach (var backend in backends)
            {
                var entry = (settings != null ? settings.Get(backend.Id) : null) ?? AnalyticsBackendEntry.CreateDefault(backend.Id);
                try
                {
                    backend.Initialize(entry, debugLog);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Analytics] {backend.Id} init failed, backend disabled for this session: {e.Message}");
                    continue;
                }
                service.AddBackend(backend, entry.categories);
                if (debugLog) Debug.Log($"[Analytics] {backend.Id} active, categories: {entry.categories}");
            }
            Locator.Analytics = service;
            Locator.RevenueSink = service;
            return service;
        }
    }
}
