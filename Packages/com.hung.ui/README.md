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
- `UILocator` — static service resolver (`UILocator.Service`)
- `IUIService` — core UI service contract
- `UICanvas` — base canvas class with lifecycle hooks, active state, and back-stack integration
- `UIToggle` — toggle component with `SetText` label support
- `UIBackStack` — navigation back-stack history
- `UITransition` — transition base class
- `Hung.UI.Scoping` (`UiScope`, `UiScopePath`, `UiScopeRegistry`, `IScopedUi`) — hierarchical canvas lifetime scoping

Public addressed acquisition types: `IUIAsyncPrefabProvider`, `IUIAddressResolver`, `UIPrefabLease`, `AddressablesUIPrefabProvider`, `AsyncCanvasRegistry`, `UIAcquireResult<T>`, and `UIAcquireFailure`. `UIManager.SetAsyncProvider` and `UIManager.AcquireAsync<T>` are the facade entry points.

`UIManager.CloseUIDirectly`, `IsContain` and `IsInBackStack` implement the `IUIService` back-stack queries. `UIButton.Tf`/`RectTf` expose the serialized `tf`; `UIButton.DefaultClickSfx` (static, default true) and the overridable `PlaysClickSfx` control click SFX. `UIPositionAnim.waitFrame` delays only the first play by 5 frames; SHOW/HIDE reverse mid-flight. `UIAnim.AbandonCurrent()` and `MaskRaycastImage.maskRectTfs` are protected adapter seams. `UIScaleAnim.Propertys` is public (0.7.1). `UIAlphaAnim` loops authored IDLE data and `Stop()` releases its running state.

`UIItem.OnIconSpriteAssigned(Sprite)` is the protected extension point for product icon presentation, such as trim-aware scaling. `UIItem.anims` is protected (0.7.2) for product subclasses.

## Known limitations / sharp edges

- EditMode tests cover transition parameters, animation queueing, and canvas lifecycle. PlayMode tests cover addressed manager destruction and one existing popup prefab. Other prefab and player behavior still require consumer verification.

## Samples

None.


## Horizon 1 UI-C0 conformance contract (2026-10-28)

The existing UIManager, UICanvas, UIBackStack, transitions and provider/resolver APIs remain authoritative.
No new UI framework, package dependency, serialized field, product canvas or public API is introduced.
At SubsystemRegistration the UI package resets UICanvas.BackPushPolicy to OnOpen,
UIButton.DefaultClickSfx to true, and clears UISCanvas.AnyOpened observers. Products reapply their
chosen policy and subscriptions afterward, in BeforeSceneLoad or ordered scene composition before
the first open/input. Do not subscribe during another unordered SubsystemRegistration callback.
An ordinary canvas close or manager destruction does not clear the process-wide event.

UI has no Unsupported fallback. Optional UI actions use ServiceSlots.TryGet<IUIService>; required
composition reports an explicit initialization failure before allowing UI interaction. Do not create
a fake successful Open or a fallback back stack. Existing Locator.UI may be null by this approved exception.

The package default pushes on Open. A product retaining Setup-then-Open selects ExplicitSetup and
routes both interface and concrete calls through its qualified adapter. A UIManager-typed reference
to an adapter with a hidden OpenUI method bypasses that method; census and reroute those callers.
Keep the existing private package Awake/OnDestroy lifecycle intact in subclasses.

Resources and addressed loading remain distinct. GetUIAsync without an address resolver uses the
Resources path and may callback immediately. AcquireAsync does not open the acquired inactive canvas.
Addressables remains a mandatory compile/package dependency. A fake provider does not prove a real
Addressables catalog. Successful prefab leases remain owned until cache/manager disposal, not close.

UIItem receives resolved presentation; inventory/reward mutations and product gameplay remain outside
widgets. Product adapters own camera, raycast and banner policy. Retain GUIDs/serialized bindings only
after exact inheritance/field/asset verification in each consumer cohort.

Implementation acceptance evidence: Docs/reports/horizon1-boundaries/service-architecture/ui-p3/README.md.
The path is the executor's required report location, not a claim that execution has occurred. UI-C0 is
package qualification only. BumpHero W6, ForestBloom stage 5, BW7, release/player/device evidence,
TemplateGame Data integration and final BW8 remain separate. Historical results elsewhere in this
document are not fresh UI-C0 evidence.

### UI-C0 review repair ? 2026-10-28

The standalone repeat-play witness is now Editor-only opt-in through the owned
`Horizon1.UiC0.RepeatArmed` protocol. Ordinary selection ignores it without seeding UI state;
only the explicitly armed proof retains its sentinel across plays. Actual per-entry reload flags
are written to unique evidence files. The normal PlayMode selector uses exact fixture names and
its evidence comparison requires the exact three added normal cases. Original producer evidence
and frozen payloads remain historical; never reapply them over authorized repairs. Fresh repair
results and raw scene/settings/policy/proof cleanup observations are recorded in
`Docs/reports/horizon1-boundaries/service-architecture/ui-p3/review/fixes/README.md`.
