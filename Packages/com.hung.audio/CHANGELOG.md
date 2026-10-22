# Changelog

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
