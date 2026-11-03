# Changelog

## [0.4.3] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.30.0 (UI contracts moved to com.hung.ui). No code change in this package.

## [0.4.2] - 2026-11-03

- Patch release: re-pin dependencies to com.hung.base 0.29.0 (init-flow seams). No code change in this package.

## [0.4.1] - 2026-11-02

- Patch release: re-pin dependencies to com.hung.designpattern 0.5.1 (pool statics reset on SubsystemRegistration). No code change in this package.

## [0.4.0] - 2026-10-31

- Fixed player configuration validation to match runtime Resources lookup, reject nested duplicates, exclude Editor-only configurations, and preserve named diagnostic codes.
- Removed synthetic cue injection from production smoke tests; missing mapped clips now report an inconclusive configuration gap.
- **Horizon 1 Stage D Cutover:**
  - Converted `Hung.Audio` to an automatic opt-in plugin (`services.audio`) managed via `AutomaticPluginHost`.
  - Replaced legacy `AudioManager` singleton, `AudioData` Odin asset, and `AudioManager.prefab` with `AudioPlayback`, `AudioCatalog`, and `AudioPluginConfiguration`.
  - Retired `SFX_TYPE` and `BGM_TYPE` enums in favor of typed `AudioId` values and catalog-backed lookup.
  - Decoupled `Hung.Audio` from `Hung.Data` / `GameData.setting`; settings binding is now handled externally via `IAudioService.BindSettings(IAudioSettings)`.
  - Retired `Hung.Data.asmref` and internal empty `IAudioService.cs`.
  - Removed assembly reference to `Hung.Data`, making `Hung.Audio` depend only on `Hung.Base`.

## [0.3.8] - Unreleased

- Update declared dependency versions for the tested P2-C cohort; this does not activate optional product features.

## [0.3.7] - 2026-09-27

- Dependency alignment: com.hung.base 0.22.0 -> 0.23.0; com.hung.data 0.12.5 -> 0.12.6.

## [0.3.6] - 2026-09-26
- Dependency alignment for the Base/UI release.

## [0.3.4] - 2026-08-15
- Dependency alignment: com.hung.base 0.19.3 -> 0.19.4.

## [0.3.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 and Data 0.10.2 for the F4-IE editor prerequisite.

## [0.3.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.data 0.10.0 -> 0.10.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.3.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no audio API or runtime behavior changed.

## [0.3.0] - 2026-07-11
- **Migration (B1 Pass 6):** `Audio`->`Hung.Audio` (3 files). Fixed a nested-namespace shadow bug: bare `using DesignPattern;` in `AudioManager.cs` (for `ObjectContainer`, which still squats the global `DesignPattern` namespace in com.hung.base) resolved to the sibling `Hung.DesignPattern` package once nested under `Hung.Audio` - fixed with `global::DesignPattern`. `rootNamespace` set (was empty).

## [0.2.0] - 2026-07-09
### Added
- `AudioManager.PlaySfxAtPosition(SFX_TYPE, Vector3)` - 3D one-shot SFX with pool return (adopted from FistInTheMist).

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (contracts moved to com.hung.base).
