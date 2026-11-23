# Changelog

## [0.2.0] - 2026-11-14
### Changed
- `CinematicPlayer.TryPrepare` only validates; the camera lease and transform snapshots are taken in `CinematicSession.Start` (BUG-261114-ce05, owner option B). A prepared session no longer freezes the gameplay camera, and finishing restores bound transforms to their pose at `Start`. A lease conflict found at `Start` fails the session with the lease diagnostic. Ticks before `Start` leave the session `Prepared`.
### Added
- `CinematicCameraLease.CanAcquire(bindings, out diagnostic)` checks lease availability without taking it.

﻿# Changelog

All notable changes to `com.hung.cinematics` will be documented in this file.

## [0.1.0] - 2026-10-18

### Added
- `CinematicDefinition` ScriptableObject with `CameraKeys`, `TransformTracks`, and `AudioCues`.
- `CinematicBindings` configuring camera brain, capture camera, frame pose, transform and audio mappings.
- `CinematicCameraLease` exclusive camera lock overriding Cinemachine camera priority to 1000.
- `CinematicSession` lifecycle with `Start`, `Pause`, `Resume`, `Cancel`, `Tick`, and terminal state events.
- `CinematicPlayer` component (`[ExecuteAlways]`) supporting automatic Update and manual test ticking.
- Full EditMode test suite with 31/31 unit and lifecycle tests.
