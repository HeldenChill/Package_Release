# Changelog

## [0.2.1] - 2026-11-25
### Fixed
- Unity 6.6 compile: replace `Object.GetInstanceID()` (CS0619 in 6.6) with `GetHashCode()` for the random seed.

## [0.2.0] - 2026-11-14
### Added
- `Blackboard.ResetToDefaults()` restores every key to its graph default or agent override.
- `AIAgent.ResetRuntimeState()` prepares a pooled agent for reuse: aborts the active path and resets the blackboard (BUG-261114-9450, owner option A).

﻿# Changelog

## [0.1.0] - unreleased
- AI Graph Editor menu moved from Window > Hung > AI Graph Editor to Tools > Character > AI Graph Editor (replaces the removed BrainGraph Editor entry).
- Behavior-tree runtime: AIGraphAsset, AIGraphRunner, stateful node lifecycle, conditional aborts.
- Typed per-agent blackboard with per-agent overrides.
- Core nodes: composites, decorators, conditions, actions, movement through IAIMover.
- AIAgent component; optional NavMeshMover.
- Hung.AI.Editor: GraphView graph editor (undo/redo, copy/paste, search window, groups, sticky notes, minimap, align, blackboard panel, node inspector, validation badges, missing-node placeholders), live runtime debugger, AIGraphAsset and AIAgent inspectors.
