# Hung Audio

Opt-in audio plugin for the Hung framework, featuring bounded voice allocation, neutral `AudioId` cue playback, and automatic lifecycle registration.

## Architecture & Lifecycle

- **Plugin Identity:** Declares `services.audio` via `Hung.Base.Composition.AutomaticPluginHost`.
- **Runtime Owner:** Spawns a single `Hung.Audio.Session` root with `AudioPlayback`, bound to `ServiceSlots.Register<IAudioService>()`.
- **Decoupled Configuration:** Loaded at runtime via `Resources.Load<AudioPluginConfiguration>("HungAudio")`, pointing to an `AudioCatalog` ScriptableObject.
- **No Direct Product Coupling:** Does not reference `GameData` or scene singletons. Mute and live volume settings are bound via `IAudioService.BindSettings(IAudioSettings)` from product-layer adapters (e.g., `LegacyAudioSettingsBinder`).
- **Retired Legacy Types:** `AudioManager`, `AudioData`, `AudioManager.prefab`, `Hung.Data.asmref`, and the enums `SFX_TYPE`/`BGM_TYPE` are retired.
- **Fallback:** When uninstalled or disabled, `Locator.Audio` resolves to `UnsupportedAudioService` in `Hung.Base` (logs once per member without allocating playback resources).

## Public API

Callers interact with `IAudioService` via `Locator.Audio`:
- `PlayBgm(AudioId id, float fadeOut = 0.3f)`
- `PlaySfx(AudioId id)`
- `PlaySfx(AudioId id, Vector3 position, float volume = 1f, float pitch = 1f)`
- `PlayLoopSfx(AudioId id, float fadeIn = 0.1f)`
- `StopSfx()`, `StopSfx(AudioId id)`, `StopLoopSfx(AudioSource source, float fadeOut = 0.1f)`
- `BindSettings(IAudioSettings settings)`, `UnbindSettings()`
