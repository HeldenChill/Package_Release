using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Hung.Cinematics
{
    public sealed class CinematicCameraLease : IDisposable
    {
        private static readonly Dictionary<CinemachineBrain, CinematicCameraLease> s_ActiveLeases =
            new Dictionary<CinemachineBrain, CinematicCameraLease>();

        private readonly CinemachineBrain _brain;
        private readonly CinemachineCamera _captureCamera;
        private readonly int _overrideId;

        private readonly Vector3 _snapshotPosition;
        private readonly Quaternion _snapshotRotation;
        private readonly LensSettings _snapshotLens;
        private readonly bool _snapshotEnabled;

        private bool _disposed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            s_ActiveLeases.Clear();
        }

        private CinematicCameraLease(
            CinemachineBrain brain,
            CinemachineCamera captureCamera,
            int overrideId,
            Vector3 snapshotPos,
            Quaternion snapshotRot,
            LensSettings snapshotLens,
            bool snapshotEnabled)
        {
            _brain = brain;
            _captureCamera = captureCamera;
            _overrideId = overrideId;
            _snapshotPosition = snapshotPos;
            _snapshotRotation = snapshotRot;
            _snapshotLens = snapshotLens;
            _snapshotEnabled = snapshotEnabled;
        }

        public static bool TryAcquire(
            CinematicBindings bindings,
            out CinematicCameraLease lease,
            out string diagnostic)
        {
            lease = null;

            if (bindings == null)
            {
                diagnostic = "CinematicBindings is null.";
                return false;
            }

            if (bindings.Brain == null)
            {
                diagnostic = "CinemachineBrain is null.";
                return false;
            }

            if (bindings.CaptureCamera == null)
            {
                diagnostic = "Capture CinemachineCamera is null.";
                return false;
            }

            if (bindings.Brain.gameObject == bindings.CaptureCamera.gameObject ||
                bindings.Brain.transform == bindings.CaptureCamera.transform)
            {
                diagnostic = "Reject: Capture camera cannot be on the output camera Transform.";
                return false;
            }

            // Prune dead or disposed leases
            var deadBrains = new List<CinemachineBrain>();
            foreach (var kvp in s_ActiveLeases)
            {
                if (kvp.Key == null || kvp.Value == null || kvp.Value._disposed)
                {
                    deadBrains.Add(kvp.Key);
                }
            }
            for (int i = 0; i < deadBrains.Count; i++)
            {
                s_ActiveLeases.Remove(deadBrains[i]);
            }

            if (s_ActiveLeases.TryGetValue(bindings.Brain, out var existing) &&
                existing != null && !existing._disposed)
            {
                diagnostic = "An active camera lease already exists for this CinemachineBrain.";
                return false;
            }

            // Snapshot existing state
            Vector3 snapshotPos = bindings.CaptureCamera.transform.position;
            Quaternion snapshotRot = bindings.CaptureCamera.transform.rotation;
            LensSettings snapshotLens = bindings.CaptureCamera.Lens;
            bool snapshotEnabled = bindings.CaptureCamera.enabled;

            // Seed capture camera from Brain.OutputCamera if available
            if (bindings.Brain.OutputCamera != null)
            {
                Vector3 seedPos = bindings.Brain.OutputCamera.transform.position;
                Quaternion seedRot = bindings.Brain.OutputCamera.transform.rotation;
                float seedFov = bindings.Brain.OutputCamera.fieldOfView;

                bindings.CaptureCamera.transform.position = seedPos;
                bindings.CaptureCamera.transform.rotation = seedRot;
                bindings.CaptureCamera.ForceCameraPosition(seedPos, seedRot);

                var lens = bindings.CaptureCamera.Lens;
                lens.FieldOfView = seedFov;
                bindings.CaptureCamera.Lens = lens;
            }

            bindings.CaptureCamera.enabled = true;

            int overrideId = bindings.Brain.SetCameraOverride(
                -1, 1000, null, bindings.CaptureCamera, 1f, 0f);

            lease = new CinematicCameraLease(
                bindings.Brain,
                bindings.CaptureCamera,
                overrideId,
                snapshotPos,
                snapshotRot,
                snapshotLens,
                snapshotEnabled);

            s_ActiveLeases[bindings.Brain] = lease;
            diagnostic = null;
            return true;
        }

        internal bool ValidateBindings(out string diagnostic)
        {
            if (_disposed)
            {
                diagnostic = "Camera lease has been disposed.";
                return false;
            }

            if (_brain == null || !_brain.isActiveAndEnabled)
            {
                diagnostic = "CinemachineBrain is missing or disabled.";
                return false;
            }

            if (_brain.OutputCamera == null || !_brain.OutputCamera.isActiveAndEnabled)
            {
                diagnostic = "Output Camera is missing or disabled.";
                return false;
            }

            if (_captureCamera == null || !_captureCamera.isActiveAndEnabled)
            {
                diagnostic = "Capture CinemachineCamera is missing or disabled.";
                return false;
            }

            diagnostic = null;
            return true;
        }

        public void Apply(Pose worldPose, float fieldOfView)
        {
            if (_disposed || _captureCamera == null) return;

            _captureCamera.transform.position = worldPose.position;
            _captureCamera.transform.rotation = worldPose.rotation;
            _captureCamera.ForceCameraPosition(worldPose.position, worldPose.rotation);

            var lens = _captureCamera.Lens;
            lens.FieldOfView = fieldOfView;
            _captureCamera.Lens = lens;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_brain != null)
            {
                s_ActiveLeases.Remove(_brain);

                if (_overrideId >= 0)
                {
                    try
                    {
                        _brain.ReleaseCameraOverride(_overrideId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CinematicCameraLease] Error releasing camera override: {ex.Message}");
                    }
                }
            }

            if (_captureCamera != null)
            {
                try
                {
                    _captureCamera.transform.position = _snapshotPosition;
                    _captureCamera.transform.rotation = _snapshotRotation;
                    _captureCamera.ForceCameraPosition(_snapshotPosition, _snapshotRotation);
                    _captureCamera.Lens = _snapshotLens;
                    _captureCamera.enabled = _snapshotEnabled;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CinematicCameraLease] Error restoring capture camera: {ex.Message}");
                }
            }
        }
    }
}

