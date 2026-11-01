# Hung Tutorial

## Purpose

Tutorial graph runtime: state machine driving a `TutorialGraphSO`/`TutorialDataSO` graph, coordinate resolver for pointing at world/UI targets, and hand/callout/video UI components. Ships with a graph editor window for authoring tutorial flows.

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.2.0, `com.hung.utilities` 0.1.0, `com.hung.base` 0.7.0, `com.hung.data` 0.3.0, `com.hung.ui` 0.1.0

## Prerequisites

- Sirenix Odin Inspector (attribute-only, declared in asmdef)
- DOTween (Asset Store)
- TextMeshPro (`Unity.TextMeshPro`, Unity 6 built-in)

## Quick start

Author a `TutorialGraphSO` via the graph editor window, register it with `TutorialRegistry`, and drive playback through `TutorialManager` — `TutorialStateMachine` advances `TutorialStepContext`s, `TutorialCoordinateResolver` resolves each step's target, and `TutorialUIController` shows the corresponding hand/callout/video UI.

## Public API index

- `TutorialManager` — top-level facade for starting/advancing tutorials
- `TutorialGraphSO` / `TutorialDataSO` — graph + step data ScriptableObjects
- `TutorialStateMachine` / `TutorialStepContext` — playback state machine
- `TutorialCoordinateResolver` — resolves world/UI target coordinates per step
- `TutorialRegistry` — registers/looks up available tutorial graphs
- `TutorialUIController` — drives hand/callout/video UI for the active step
- `Trigger.cs` — collider-trigger event relay into the tutorial flow

## Known limitations / sharp edges

- The graph editor window (`Runtime/Editor/TutorialGraphEditorWindow.cs`) currently lives under `Runtime/` — Stage A Ph3 Task 3.6 splits it into a proper `Editor/` assembly; until that lands, whole-file `#if UNITY_EDITOR` guards are the (non-conformant, tracked) status quo.
- Broken node-reference validation for the graph asset needs Unity asset loading and is deferred to the Ph5/C2 AutoTest validator wrapper, not the filesystem-only python validator.
- No automated tests exist yet for this package.

## Samples

None.
