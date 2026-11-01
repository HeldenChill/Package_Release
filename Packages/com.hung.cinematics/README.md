# Hung.Cinematics

Layer 4 standalone package for deterministic cinematic playback and exclusive camera leasing in Unity.

## Responsibilities
- **CinematicDefinition**: ScriptableObject asset containing camera pose tracks, transform tracks, and timed audio cues.
- **CinematicBindings**: Configures brain, dedicated capture camera, root coordinate frame, and object mappings.
- **CinematicCameraLease**: Exclusive camera override (Priority 1000) that cleanly restores camera state upon completion or disposal.
- **CinematicSession & CinematicPlayer**: Deterministic time sampling with manual `Tick(float deltaTime)` or automatic component Update.
- **Total Separation**: Zero dependencies on gameplay logic, enemy AI, inventory, player stats, or death controllers.

## Session States
```
Prepared -> Playing <-> Paused
               |
               v
     Completed / Failed / Cancelled
```

## Camera Lease Contract
- `CinematicCameraLease.TryAcquire(CinematicBindings bindings, out CinematicCameraLease lease, out string error)`: Atomically validates brain/camera availability and claims priority 1000 override.
- `lease.Apply(Pose worldPose, float fieldOfView)`: Directly updates camera transform and FOV.
- `lease.Dispose()`: Immediately restores camera priority, enabled state, and frees the reservation.

## Pose Interpolation Rules
- Positions are interpolated linearly between keyframes.
- Rotations use normalized quaternion spherical linear interpolation (Slerp).
- Track evaluation transforms local keyframe offsets relative to the origin coordinate frame (`Frame`).

## Sample Usage
```csharp
var bindings = new CinematicBindings
{
    Brain = brain,
    CaptureCamera = captureCam,
    Frame = new Pose(camPos, yawRotation),
    Transforms = new Dictionary<string, Transform> { { "attacker-visual", attackerModel } }
};

if (cinematicPlayer.TryPrepare(definition, bindings, out var session, out string err))
{
    session.Finished += result => {
        Debug.Log($"Cinematic finished with state: {result.State}");
    };
    session.Start();
}
```
