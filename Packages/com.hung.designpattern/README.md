# Hung DesignPattern (L0)
Shared design patterns: Pool (SimplePool, ParticlePool, GameUnit), EventBus, StateMachine, Singleton.
No dependencies.

## Scoped pooling and generic state machine

The additive scoped API is in `Hung.DesignPattern.ScopedPooling`; legacy
SimplePool, ParticlePool, PoolController, GameUnit, STATE and BaseState retain
their existing behavior. New code can pool any Component without inheriting
GameUnit or adopting a shared content enum.

| API | Responsibility |
|---|---|
| PoolScope.TryCreate / Dispose / LastDiagnostic | Bind a host-owned scene or application root, release only owned resources and report registration failure context |
| PoolScope.Register | Validate and warm one fixed group or resolver snapshot atomically |
| PoolScope.Spawn | Direct-prefab or typed unit lookup; transform precedes activation |
| PoolScope.Play | Typed particle lookup; nonlooping auto-return or explicit looping return |
| PoolScope.Return / Collect | Checkout-safe individual or group return without product cleanup |
| PoolScope.ScheduleReturn / Cancel / Tick | Scoped scaled/unscaled timing and generation-safe cancellation |
| UnitPoolId / ParticlePoolId | Exact product-defined typed string identities |
| PoolCheckout / ReturnRequest | Scope and generation-aware use/request handles |
| PoolDefinition / PoolKind / PoolLifetime / PoolClock / PoolStatus | Explicit registration, timing and failure contracts |
| IPoolCatalogResolver / PoolCatalogAsset / PoolCatalogEntry | Optional product-owned identity-to-prefab authoring |
| PoolScopeRunner | Scope-owned LateUpdate clock and destruction cleanup |
| IState<TKey> / StateMachine<TKey> / StateStatus | Product-key behavior, validation and explicit replacement/re-entry |

Direct prefab path:

```csharp
PoolScope.TryCreate(hostRoot, PoolLifetime.Scene, out var scope);
scope.Register("characters", new PoolDefinition(prefabComponent, PoolKind.Unit, warmup: 3));
var status = scope.Spawn(prefabComponent, position, rotation, null, out var checkout);
if (status == PoolStatus.Success)
    scope.ScheduleReturn(checkout, .3f, PoolClock.Unscaled, out var request);
// Product host disposes scope when its selected lifetime ends.
```

Check every result before using the checkout. A running scope ticks automatically
through its owned runner; do not also call Tick from the product host. EditMode
tests can tick deterministically. Root must remain active; application root must
already be persistent. No implicit DontDestroyOnLoad, Resources fallback, catalog
replacement, active recycling or product cleanup callbacks occur.

Spawn and Play publish a checkout only while its original scope and generation
remain current after Unity callbacks. A callback that returns or rechecks out the
instance causes `StaleCheckout`; disposal causes `DisposedScope`. Failed calls
leave the output checkout empty and do not alter a nested valid checkout or start
stale particle playback. Destroyed instances are retired from ownership indexes,
inactive queues, delayed requests and particle tracking when encountered during
spawn, return, checkout validation or timer/completion ticks.

Register rejects duplicate keys; Replace requires an existing key and leaves
the active instance unchanged. ChangeState to that key exits the active instance
and enters the replacement. Repeated running Start and stopped Stop are harmless.
Validation failures do not invoke callbacks. Product Enter/Exit exceptions propagate;
this is not callback transaction rollback. Product adapters own their subscriptions.

Consumer import and legacy retirement require the separate P1 consumer handoff
and actual product proof. Package tests do not establish that proof.
