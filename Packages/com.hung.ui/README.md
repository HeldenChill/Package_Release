# Hung UI

## Current UI transition contract

`UISCanvasTransition` coordinates a canvas's effects. A root `UIAnim` does not replace the composite transition; an explicitly authored non-animation `UITransition` remains supported. `UISCanvas` starts SHOW once, then reports `IsFullyShown` and `FullyShown` after all configured SHOW effects complete. Closing waits for all configured HIDE effects, or completes immediately when none apply. Authored raycast blockers release input during hide and return on reopen. The old serialized `closeButton` remains supported alongside `closeBtns`; `CloseButtons` exposes both for tutorial controls.

`UIAnim` queues a distinct HIDE requested during SHOW and drops queued work on interrupt. Delayed animation callbacks carry a generation token so an interrupted effect cannot complete a later operation. EditMode tests cover transition parameters, request queueing, and canvas open/close coordination. A PlayMode smoke covers an existing `NoWifiPopup` prefab; broader consumer and player behavior still need verification.

## Addressed canvas acquisition

`UIManager.AcquireAsync<T>(address)` returns a typed `UIAcquireResult<T>` and uses `AddressablesUIPrefabProvider` by default. Call `SetAsyncProvider(IUIAsyncPrefabProvider)` before the first acquisition to supply a game provider. Add an `IUIAddressResolver` through the two-argument `SetAsyncProvider` overload to route the existing callback-shaped `GetUIAsync<T>(Action<T>)` through addressed acquisition; the callback receives the canvas or null once. Requests for the same canvas type and address share one in-flight load; a different address for an already loading or loaded type returns `AddressMismatch`. The manager retains successful prefab leases until it is destroyed. Without a resolver, existing `GetUI<T>()` and callback-shaped `GetUIAsync<T>(Action<T>)` remain Resources-based.

## Purpose

UI framework: `UIManager` facade, `UISCanvas`/`UICanvasComponent` lifecycle, canvas transitions, and reusable widgets (buttons, dropdowns, popups).

## Layer + dependencies

- Layer: `4`
- Dependencies: `com.hung.designpattern` 0.4.4, `com.hung.utilities` 0.2.2, `com.hung.base` 0.21.1, Addressables 1.22.3 minimum

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

Public addressed acquisition types: `IUIAsyncPrefabProvider`, `IUIAddressResolver`, `UIPrefabLease`, `AddressablesUIPrefabProvider`, `AsyncCanvasRegistry`, `UIAcquireResult<T>`, and `UIAcquireFailure`. `UIManager.SetAsyncProvider` and `UIManager.AcquireAsync<T>` are the facade entry points.

`UIItem.OnIconSpriteAssigned(Sprite)` is the protected extension point for product icon presentation, such as trim-aware scaling.

## Known limitations / sharp edges

- EditMode tests cover transition parameters, animation queueing, and canvas lifecycle. PlayMode tests cover addressed manager destruction and one existing popup prefab. Other prefab and player behavior still require consumer verification.

## Samples

None.
