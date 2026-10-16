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
    /// With <see cref="AnalyticsSettings.deferInitialization"/> on, the proxy holds every call and no SDK starts
    /// until the game calls <see cref="StartDeferred"/> (after consent) or <see cref="DiscardDeferred"/>.
    /// </summary>
    public static class AnalyticsBootstrap
    {
        /// <summary>Resources path (no extension) of the settings asset.</summary>
        public const string SettingsResourcePath = "HungAnalyticsSettings";

        static readonly HashSet<string> _startedBackendIds = new HashSet<string>();
        static readonly HashSet<string> _readyBackendIds = new HashSet<string>();
        static readonly HashSet<string> _identityBackendIds = new HashSet<string>();
        static int _diagnosticGeneration;

        /// <summary>True when this launch's backend Initialize returned successfully. Not a delivery receipt.</summary>
        public static bool IsBackendStarted(string id) => _startedBackendIds.Contains(id);
        /// <summary>True when the backend reports local SDK readiness for this launch. Not dashboard delivery.</summary>
        public static bool IsBackendReady(string id) => _readyBackendIds.Contains(id);
        /// <summary>True when this launch's identity setter completed for the backend; exposes no raw identity.</summary>
        public static bool IsIdentityApplied(string id) => _identityBackendIds.Contains(id);

        /// <summary>Creates a readiness callback bound to this startup generation; late callbacks from an old play are ignored.</summary>
        public static Action<bool> ReadySignal(string backendId)
        {
            int generation = _diagnosticGeneration;
            return identityApplied =>
            {
                if (generation != _diagnosticGeneration) return;
                _readyBackendIds.Add(backendId);
                if (identityApplied) _identityBackendIds.Add(backendId);
            };
        }

        static void ResetDiagnostics()
        {
            _diagnosticGeneration++;
            _startedBackendIds.Clear();
            _readyBackendIds.Clear();
            _identityBackendIds.Clear();
        }

        static AnalyticsService _deferred;
        static AnalyticsSettings _deferredSettings;
        static IReadOnlyList<IAnalyticsBackend> _deferredBackends;

        /// <summary>
        /// The launch identity passed to <see cref="StartDeferred"/>; null in automatic mode, before the start, or when
        /// identity failed. Backends read it inside <see cref="IAnalyticsBackend.Initialize"/>.
        /// </summary>
        public static string LaunchUserId { get; private set; }

        /// <summary>True while a deferred proxy waits for <see cref="StartDeferred"/> or <see cref="DiscardDeferred"/>.</summary>
        public static bool IsDeferred => _deferred != null;

        // Domain reload is off in consuming projects: statics survive between plays.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ResetDiagnostics();
            _deferred = null;
            _deferredSettings = null;
            _deferredBackends = null;
            LaunchUserId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var settings = Resources.Load<AnalyticsSettings>(SettingsResourcePath);
            if (settings == null)
                Debug.LogWarning($"[Analytics] Resources/{SettingsResourcePath}.asset missing - using default routing. Open Hung/Analytics/Settings.");
            var service = settings != null && settings.deferInitialization
                ? InstallDeferred(settings, AnalyticsBackends.Registered)
                : Install(settings, AnalyticsBackends.Registered);

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
            ResetDiagnostics();
            var service = new AnalyticsService(clock);
            AddBackends(service, settings, backends);
            Locator.Analytics = service;
            Locator.RevenueSink = service;
            return service;
        }

        /// <summary>
        /// Registers a holding proxy in the locator and starts no SDK. Calls made before <see cref="StartDeferred"/>
        /// are kept (bounded) and replayed in order once the backends start.
        /// </summary>
        public static AnalyticsService InstallDeferred(AnalyticsSettings settings, IReadOnlyList<IAnalyticsBackend> backends, Func<float> clock = null)
        {
            ResetDiagnostics();
            var service = new AnalyticsService(clock);
            service.Hold();
            _deferred = service;
            _deferredSettings = settings;
            _deferredBackends = backends;
            LaunchUserId = null;
            Locator.Analytics = service;
            Locator.RevenueSink = service;
            return service;
        }

        /// <summary>
        /// Starts the deferred backends with <paramref name="launchUserId"/> (null when identity failed), then replays the
        /// held calls. Returns false when nothing is deferred (automatic mode, or already started or discarded).
        /// </summary>
        public static bool StartDeferred(string launchUserId)
        {
            if (_deferred == null) return false;
            var service = _deferred;
            var settings = _deferredSettings;
            var backends = _deferredBackends;
            _deferred = null;
            _deferredSettings = null;
            _deferredBackends = null;
            LaunchUserId = launchUserId;
            AddBackends(service, settings, backends);
            service.ReleaseHeld();
            return true;
        }

        /// <summary>Consent refused: drops the held calls and starts no SDK. Later calls reach no backend.</summary>
        public static void DiscardDeferred()
        {
            if (_deferred == null) return;
            _deferred.DiscardHeld();
            _deferred = null;
            _deferredSettings = null;
            _deferredBackends = null;
        }

        static void AddBackends(AnalyticsService service, AnalyticsSettings settings, IReadOnlyList<IAnalyticsBackend> backends)
        {
            if (backends == null) return;
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
                _startedBackendIds.Add(backend.Id);
                if (debugLog) Debug.Log($"[Analytics] {backend.Id} active, categories: {entry.categories}");
            }
        }
    }
}
