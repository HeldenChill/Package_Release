# Changelog

## [0.5.3] - 2026-08-11
- Dependency-only patch: align Base 0.19.2 for the F4-IE editor prerequisite.

## [0.5.2] - 2026-08-09
- Dependency-only patch: align exact package constraints for the approved F3B propagation; no runtime or API behavior changed.
- Dependency alignment: com.hung.base 0.19.0 -> 0.19.1; com.hung.designpattern 0.4.2 -> 0.4.3; com.hung.utilities 0.2.1 -> 0.2.2.

## [0.5.1] - 2026-07-21
- Dependency-only patch aligning package declarations with the ItemId migration release; no UI API or runtime behavior changed.

## [0.5.0] - 2026-07-15
- **`UIManager.GetUIAsync<T>(Action<T>)`** implements the new `IUIService` member (see `com.hung.base` 0.13.0). Default path is synchronous: it invokes the callback immediately with `GetUI<T>()`.
- **New extension seam: `partial void TryGetUIAsyncOverride<T>(Action<T> onComplete, ref bool handled)`.** A game whose canvases are not resolvable by the synchronous `CanvasRegistry` (Addressables, asset bundles) implements this in its own `partial UIManager` and sets `handled = true`; otherwise `GetUIAsync` falls back to the sync path. Uses a `ref bool` rather than a `bool` return because a `partial void` cannot return a value.

## [0.4.0] - 2026-07-14
### Changed
- `UIManager` is now `public partial class`. A game adds its own UI helpers - screen-space math, ad-banner insets, an Addressables-backed canvas loader - from a `Hung.UI.asmref` folder instead of forking the manager. Same extension seam `com.hung.data` uses for `DataManager`. No API change for existing consumers.
### Why
Wave 2 of the Horror1Game adoption (B4): Horror's `UIManager` fork carried five game-specific helpers that have no place in `IUIService`, and adopting the package would otherwise have meant losing them.

## [0.3.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 6 - liveops+ui+tutorial+audio+tools):** `Base.UI` (15 files) merged with bare `UI` (47 files repo-wide, incl. Assets/_Game/_UI game-side content sharing this package's namespace) into one `Hung.UI` namespace - no real type-name collisions (one nested-`Propertys`-class false positive, checked before merging). `rootNamespace` `Base.UI`->`Hung.UI`.
- Squat cleanup: `DetectMouse.cs`/`FontMaterialProp.cs` (bare pre-rename `Utilities` namespace, deferred from Pass 1)->`Hung.Utilities`; `UIDropdown.cs`/`UIDropdownShow.cs` (`Hung.Utilitys` typo namespace, found during Pass 5)->`Hung.UI`.
- Migration map: code with `using Base.UI;` or bare `using UI;` on any of the merged files must switch to `using Hung.UI;`.

## [0.2.0] - 2026-07-11
### Changed
- **Migration (B1 Pass 3 - base, dependency):** `com.hung.base` renamed `Base`->`Hung.Base`. Two files in this package that squatted the bare `Base` namespace (`UISButton.cs`, `MaskRaycastImage.cs`) renamed alongside it, `Base`->`Hung.Base`. This package's own `Base.UI` squat intentionally NOT renamed this pass (Pass 6's territory).
### Fixed
- `UICanvas.cs`, `UIButton.cs`, `UIManager.cs` (`Base.UI`) used base-family symbols (`Locator`, `SFX_TYPE`, `IUIService`) unqualified via implicit access to the old bare-`Base` enclosing namespace; broke when `Base` became `Hung.Base` - added explicit `using Hung.Base;` to each.

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game/_UI (mechanical move, GUIDs preserved). Framework files: Base/ widgets, UIManager, BasePopup, generic state-button components, Anim files (reborn as UITransition impls in wave 2b Task 4a), RecyclableScrollRect Main/ (Demo/ deleted), UIParticleSystem. UICanvas + UIToggle moved into com.hung.base instead (IUIService/InitCanvas constrain on them there). Game-side leftover (`_UI/Scripts` named canvases, game components, `UI_ANIM.cs`, Debug_* canvases) stays in Assets as assembly `Hung.UI.Game`. `_UI/Base/Hung.Base.asmref` retired.
### PvM absorption verdicts

| File | PvM delta | Verdict | Reason |
|---|---|---|---|
| UIToggle.cs | adds `IsOn` getter | MERGE | zero-dependency, useful read accessor |
| UIDropdown.cs | none (whitespace only) | no-op | identical |
| UIAnim.cs (base) | centralizes `state = ANIM.NONE` reset into `OnAnimExit`; adds opt-in `isAnimQueue`/`animQueue` (defers `Play` calls that arrive mid-animation); adds static `CopyRectTransform` helper; drops decorative unused `[Button]` on the virtual `Play(ANIM)` stub | MERGE | strict DRY/robustness improvement, opt-in (default false), zero behavior change for existing callers |
| UIPositionAnim.cs | null-guards `StartTf`/`EndTf` with error log instead of NRE; supports the new anim queue; `SetupBaseData` auto-generates Start/End/Region anchor RectTransforms instead of leaving them for manual wiring | MERGE | depends on UIAnim merge above; guards a real crash (destroyed anchor references) and removes manual anchor setup |
| BasePopup.cs | none (whitespace only) | no-op | identical |
| TopCanvas.cs | `UpdateUIHeart`/unlimited-heart branch commented out; uses PvM-specific `GameData.ItemData.Stackable.Quantity` shape instead of `.Quantity` | REJECT | regression — PvM fork removed working logic and diverged onto a PvM-only data shape; not applicable to Package_Repo's `GameData` |

- Asmref/asmdef fallout from claiming the `Hung.UI` name for the package: every consumer that referenced the OLD monolithic Hung.UI (by name-string OR by its preserved file GUID `eb17b1cd92d58324986c35a6da748f08`) now resolves to `Hung.UI.Game` instead, since that's the renamed file. Fixed by adding an explicit `"Hung.UI"` package reference alongside wherever framework types (UICanvas is in Base now; UISCanvas/BasePopup/UIManager are in the package) are actually used: `Hung.Items`, `Hung.Game`, `Hung.Subsystem.PiggyBank`, `Hung.Tutorial` asmdefs, plus 3 subsystem `UI/Hung.UI.asmref` injection points (SpinWheel/DailyReward/PiggyBank were name-based, not GUID — repointed to the Hung.UI.Game GUID). `Hung.Tool.asmdef` needed BOTH `Hung.UI` (UISCanvas/BasePopup) and `Hung.UI.Game` (UIPack/ListItemShow — game-side types its UIFlow importer inspects by reflection; known coupling, unchanged from pre-wave-2b).
- Two-tier `UICanvas` lifecycle: `Open()`/`Close()` framework-owned, subclasses override `OnOpen(object)`/`OnClose()`/`OnBackKey()`; `CanClose()` veto hook added for `ShowContentPopup`'s cooldown guard; `Show()`/`Hide()` stay virtual (tier-2 raw visibility). ~29 game consumer files migrated.
- `UITransition` abstraction (`PlayIntro`/`PlayOutro`/`Interrupt`): `UIAnim` implements it over the existing DOTween `Play(ANIM)` machinery; `UISCanvasTransition` adapts `UISCanvas`'s richer multi-child-anim close orchestration. Resolved lazily via `UICanvas.EnsureTransition()` (virtual-dispatch hook on first `Open`/`Close`), not `Awake()` — avoids C# method-hiding breaking the self-add for ~25 subclasses that declare their own non-`override` `Awake()`.
- 0 compile errors: Hung.Base, Hung.UI.Game, Hung.Tutorial, Assembly-CSharp.
