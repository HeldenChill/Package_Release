# Hung UI

## Purpose

UI framework: `UIManager` facade, `UISCanvas`/`UICanvasComponent` lifecycle, canvas transitions, and reusable widgets (buttons, dropdowns, popups).

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.2.0, `com.hung.utilities` 0.1.0, `com.hung.base` 0.7.0

## Prerequisites

- Sirenix Odin Inspector (attribute-only, declared in asmdef)
- DOTween (Asset Store)
- TextMeshPro (`Unity.TextMeshPro`, Unity 6 built-in)

## Quick start

Register canvases with `CanvasRegistry`, open/close them through `UIManager`, and derive `BasePopup` for modal content — `UISCanvasTransition` handles show/hide animation.

## Public API index

- `UIManager` — top-level facade for opening/closing/tracking UI canvases
- `UISCanvas` / `UICanvasComponent` — canvas lifecycle base
- `BasePopup` — modal popup base class
- `CanvasRegistry` — canvas registration/lookup
- `IUIPrefabProvider` / `ResourcesPrefabProvider` — pluggable prefab resolution (swap for Addressables, etc.)
- `UISButton` / `UIButtonComponent` — button widgets
- `UIDropdown` — dropdown widget
- `UISCanvasTransition` — show/hide transition driver

## Known limitations / sharp edges

- No automated tests exist yet for this package.

## Samples

None.
