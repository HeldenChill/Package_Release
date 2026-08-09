# Changelog

## [0.4.3] - 2026-08-09
- Fix: MiniPool now reuses inactive slots after out-of-order despawn and Collect sweeps every active object.

## [0.4.2] - 2026-07-21
- Patch release aligned with the ItemId migration package set; no design-pattern API or runtime behavior changed.

## [0.4.1] - 2026-07-14
- **Fix (Unity fake-null): `Singleton<T>.Ins` could hand back a destroyed instance forever.** The guard was `if (_instance is not null)`, a plain C# null check that bypasses Unity's overloaded `==` operator. A destroyed `MonoBehaviour` is "fake-null": `== null` is true, `is not null` is *also* true. Since `_instance` is `static`, it survives scene unload and domain-reload-disabled play sessions, so after the first destruction every `Ins` call returned the dead object — and every field access on it threw `MissingReferenceException`. Now uses `!= null`, which sees fake-null and re-runs `FindObjectOfType`.
- **Found by:** Horror1Game (B4 Wave 2) play-mode — `CameraController.Ins.CameraTransition` threw `MissingReferenceException` on a `CinemachineCamera` list whose entries had been destroyed with a prior scene. Not an adoption regression (Horror's pre-adoption copy of `Singleton` was byte-identical); the bug predates the framework split and affects every `Singleton<T>` in both games.
- **Fix (same class): `SimplePool.Root`** cached `_root` in a `static` with the same `is not null` guard — a pool root destroyed on scene unload was never re-acquired. Also `Init`/`Preload`'s `is not null` prefab/parent guards, now `!= null`.
- **Fix: `SimplePool.Preload` dereferenced `prefab.PoolType` before its own `prefab == null` guard**, making that guard unreachable (a null prefab threw `NullReferenceException` one line earlier). Reordered.
- No API change. Tests: `SingletonTests` (new, 3 cases) fails against the old `is not null` guard.

## [0.4.0] - 2026-07-14
- **BREAKING: `EVENTS.cs` deleted.** The package no longer ships concrete event structs — it supplies only the mechanism (`IEvent`, `EventBus<T>`, `EventBinding<T>`). Each game declares its own event structs in its own assembly; `EventBus<T>` is generic over `T : IEvent`, so no framework change is needed to support them.
- **Why:** surfaced by the first second-consumer adoption (Horror1Game, B4). A framework package shipping one game's event structs forces every other consumer to inherit them as dead weight, and collides when two games define the same event name with different fields (Horror's `ActiveHomeSceneEvent { bool[] flags }` vs TemplateGame's `{ bool showHome; bool showGameplay; }`). Events were never a layering problem — only enums are, since a marker interface is open to implementation from any layer while an enum is closed.
- **Migration:** of the 15 structs previously here, 9 had zero consumers repo-wide and were deleted outright (`ScoreChangeEvent`, `HighScoreChangeEvent`, `UpdateHighScoreEvent`, `BallAddEvent`, `BallShootEvent`, `LoadGameEvent`, `DestructLevelEvent`, `DespawnLevelEvent`, `LevelEndEvent`). The rest moved to their real owners:
  - `ResetAoaCapEvent`, `ResetInterCapEvent` → `com.hung.services.ads` (`Hung.Ads`), its only consumers.
  - `PiggyBankRewardClaimedEvent` → `com.hung.liveops.piggybank` (`Hung.LiveOps.PiggyBank`), its only package consumer.
  - `ActiveHomeSceneEvent`, `ReviveEvent`, `ReconstructLevelEvent` → TemplateGame `Assets/_Game/_UI/Scripts/GameEvents.cs` (`Hung.UI`) — game content, not framework.
- **Known limitation, not fixed here:** `PoolType` (`SimplePool`) and `STATE` (`StateMachine`) are still framework-owned enums, so a game cannot add its own pool types or states without editing this package. Enums are closed to extension across a layer boundary in a way `IEvent` is not. The fix is to make them type parameters (`SimplePool<TKey>`, `StateMachine<TState>`) so each game owns its own enum. Deferred to Stage C — it is a breaking change to the most-depended-on L0 package and warrants its own pass.

## [0.3.1] - 2026-07-11
- **Fix (C3 safe-dispatch, pulled forward early per user request):** `EventBus<T>.Raise` now wraps each binding invocation in try/catch (`Debug.LogException`), so one subscriber throwing no longer blocks delivery to subsequent subscribers in iteration order. Un-ignored `EventBusTests.SubscriberException_DoesNotBlockOthers` (was a Ph5 `[Ignore]`d KNOWN-GAP probe) — added `LogAssert.Expect(LogType.Exception, ...)` since the exception is now caught+logged rather than propagated.

## [0.3.0] - 2026-07-11
- **Migration (B1 Pass 1 - foundations):** namespace `DesignPattern` -> `Hung.DesignPattern` across all Runtime types (Command, IConstructor, Dispatcher, GameUnit, MiniPool, ParticlePool, PoolController, SimplePool, IMemento, IOriginator, SimpleSingleton, Singleton, BaseState, StateMachine). asmdef `rootNamespace` updated to match.
- **Migration (B1 Pass 1):** `EventBus/EventBinding/EVENTS.cs` (EventBus, EventBinding, IEvent, IEventBinding, and all concrete event structs) moved from the global namespace into `Hung.DesignPattern` — these had never had a namespace and were reachable unqualified from anywhere; consumers now need `using Hung.DesignPattern;`.
- No SerializeReference hits found for this family (verified via repo-wide grep) — plain rename, no `[MovedFrom]` needed.
- Deviation from the stageBC plan's facts table: `Base.Utilities/DevLog.cs`, `Base.Utilities/UTILITIES.cs`, and `Base.Utilities/ObjectContainer.cs` squat the pre-rename bare `Utilities`/`DesignPattern` namespaces but are physically owned by `com.hung.base` (Hung.Base assembly) — left untouched here, in scope for Pass 3 (Base family) instead. Same squat shape also found in `com.hung.character/Runtime/Trigger.cs` and two `com.hung.ui` files (bare `namespace Utilities`) — also deferred to their respective passes.

## [0.2.0] - 2026-07-07
- `PoolType` gained `SOUND_UNIT = 104` (H1 character core's SoundModule). H1's own copy had `SOUND_UNIT = 3`, which this enum already uses for `RICE_CLUSTER` — debt: when H1 adopts the package, any H1 serialized data storing pool type 3 needs remapping to 104.
- StateMachine union-merge with H1's `Utilities.StateMachine` fork (com.hung.character consumer):
  - `BaseState` gains decorator API — `Decorator` (virtual auto-prop), `Type` (virtual, defaults `STATE_TYPE.NORMAL`), `_OnAddDecorState`/`_OnRemoveDecorState` events, `AddDecorState`/`RemoveDecorState` helpers. Virtual, not abstract — existing subclasses compile unchanged.
  - New `STATE_TYPE` enum (`NONE`/`NORMAL`/`DECORATOR`).
  - `StateMachine` gains `AddDecorState`/`RemoveDecorState` and decorator-aware `ChangeState` (degrades to previous behavior when no decorators; `currentStateId` kept). `ChangeState` on unknown id now returns instead of throwing `KeyNotFoundException` (H1 behavior, strictly safer).
  - `STATE` gains H1 members at fresh values: `WALK=11, JUMP=12, IN_AIR=13, USING_SKILL=14, RUN=15, CROUCH=16, VAULT=17, TIRED=18, CROWED_CONTROL=100, KNOCK_BACK=101, SLOW=102, CHARM=103, FREEZE=105, NORMAL_ATTACK=106, TELEPORTING=107`. Existing values untouched. H1 drift on adoption: H1's `NONE=0`→`-1`, `DIE=4`→`6`, `STUN=104`→`9`, movement states renumbered — runtime-only enum, verify nothing serializes STATE.

## [0.1.0] - 2026-07-07
- Extracted from Assets/_Game (mechanical move, no code changes).
