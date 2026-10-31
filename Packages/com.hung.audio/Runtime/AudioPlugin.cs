using System;
using Hung.Base;
using Hung.Base.Composition;
using UnityEngine;

namespace Hung.Audio
{
    /// <summary>Declares and owns one optional application Audio session.</summary>
    public static class AudioPlugin
    {
        /// <summary>Stable automatic feature identity.</summary>
        public const string FeatureId = "services.audio";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Declare()
        {
            try
            {
                if (PluginStartupPolicy.ShouldStart(FeatureId)) AutomaticPluginHost.Collect(CreateRegistration());
            }
            catch (Exception error) { Debug.LogError("[Audio] startup declaration failed: " + error.Message); }
        }
        /// <summary>Creates an instance-owned registration; explicit loader supports isolated host tests.</summary>
        public static FeatureRegistration CreateRegistration(Func<AudioPluginConfiguration[]> load = null)
        {
            GameObject root = null;
            AudioPlayback core = null;
            AudioServiceAdapter service = null;
            void Stop()
            {
                var previous = service; service = null;
                if (previous != null) ServiceSlots.Unregister<IAudioService>(previous);
                core?.Dispose(); core = null;
                var previousRoot = root; root = null;
                if (previousRoot != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(previousRoot);
                    else UnityEngine.Object.DestroyImmediate(previousRoot);
                }
            }
            return new FeatureRegistration(FeatureId, false, Array.Empty<string>(), () =>
            {
                if (ServiceSlots.TryGet<IAudioService>(out _)) return new FeatureStartResult(false, "AUDIO_OWNER_PRESENT");
                AudioPluginConfiguration config;
                try
                {
                    var matches = load != null ? load() : Resources.LoadAll<AudioPluginConfiguration>("HungAudio");
                    if (matches == null || matches.Length == 0) return new FeatureStartResult(false, "AUDIO_CONFIG_MISSING");
                    if (matches.Length != 1) return new FeatureStartResult(false, "AUDIO_CONFIG_DUPLICATE");
                    config = matches[0];
                    if (config == null || config.Catalog == null) return new FeatureStartResult(false, "AUDIO_CATALOG_MISSING");
                    if (config.MaxSfxVoices < 1 || config.MaxSfxVoices > 128) return new FeatureStartResult(false, "AUDIO_VOICE_CAP_INVALID");
                    config.Catalog.BuildIndex();
                }
                catch (Exception error)
                {
                    Debug.LogError("[Audio] catalog validation failed: " + error.Message);
                    return new FeatureStartResult(false, "AUDIO_CATALOG_INVALID");
                }
                try
                {
                    root = new GameObject("Hung.Audio.Session"); root.SetActive(false);
                    core = root.AddComponent<AudioPlayback>(); core.Initialize(config.Catalog, null, config.MaxSfxVoices);
                    service = new AudioServiceAdapter(core);
                    if (ServiceSlots.TryGet<IAudioService>(out _)) { Stop(); return new FeatureStartResult(false, "AUDIO_OWNER_PRESENT"); }
                    var result = ServiceSlots.Register<IAudioService>(service);
                    if (result != ServiceRegistration.Registered) { Stop(); return new FeatureStartResult(false, "AUDIO_REGISTER_FAILED"); }
                    if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(root);
                    root.SetActive(true);
                    return new FeatureStartResult(true);
                }
                catch (Exception error)
                {
                    Stop(); Debug.LogError("[Audio] initialization failed: " + error.Message);
                    return new FeatureStartResult(false, "AUDIO_INITIALIZE_FAILED");
                }
            }, Stop);
        }
    }
}
