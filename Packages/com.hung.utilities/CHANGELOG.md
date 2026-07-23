# Changelog

## [0.2.1] - 2026-07-21
- Patch release aligned with the ItemId migration package set; no utilities API or runtime behavior changed.

## [0.2.0] - 2026-07-11
- **Migration (B1 Pass 1 - foundations):** namespace `Utilities` -> `Hung.Utilities` (7 Runtime files: LinearRegression, MainThreadDispatcher, EdgeHelpers, MathHelper, SRandom, PosYToSortingOrder, VFXController); `Utilities.Timer` -> `Hung.Utilities.Timer` (STimer.cs, TimerManager.cs — the plan's assumed `Utilities.STimer` namespace never existed in code; actual namespace was always `Utilities.Timer`, asmdef `rootNamespace` corrected to match). `LitJson`/`MEC`/`UnityEngine.UI.Extensions` (ThirdParty) left untouched.
- `VFXObject.cs`/`VFXColor.cs` had no namespace at all (reachable unqualified) — wrapped into `Hung.Utilities` for B1 target-root compliance; zero external consumers found, so no consumer fix needed.
- No SerializeReference hits for this family — plain rename, no `[MovedFrom]` needed.
- **Compile-gap found+fixed:** nested `using Utilities;`/`using DesignPattern;` inside `namespace Hung.Utilities.Input { ... }` silently resolved to the sibling `Hung.Utilities`/`Hung.DesignPattern` namespace instead of the intended global one (C# enclosing-namespace lookup shadows a bare simple name when an ancestor scope has a nested namespace of that same name) — fixed by moving the two base-owned-type usings (`PlayerInput.cs`, `LineDrawInput.cs`, still consuming global `DevLog`/`UTILITIES`) to file-scope with `global::Utilities` qualification. Same shadowing risk applies to any future pass nesting a bare stray-namespace `using` inside a `Hung.X` namespace block matching that name.

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (mechanical move, no code changes).
