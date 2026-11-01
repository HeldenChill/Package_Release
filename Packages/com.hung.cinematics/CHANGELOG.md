# Changelog

All notable changes to `com.hung.cinematics` will be documented in this file.

## [0.1.0] - 2026-10-18

### Added
- `CinematicDefinition` ScriptableObject with `CameraKeys`, `TransformTracks`, and `AudioCues`.
- `CinematicBindings` configuring camera brain, capture camera, frame pose, transform and audio mappings.
- `CinematicCameraLease` exclusive camera lock overriding Cinemachine camera priority to 1000.
- `CinematicSession` lifecycle with `Start`, `Pause`, `Resume`, `Cancel`, `Tick`, and terminal state events.
- `CinematicPlayer` component (`[ExecuteAlways]`) supporting automatic Update and manual test ticking.
- Full EditMode test suite with 31/31 unit and lifecycle tests.
