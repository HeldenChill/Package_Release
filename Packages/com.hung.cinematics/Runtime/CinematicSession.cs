using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Cinematics
{
    public readonly struct CinematicResult
    {
        public CinematicState State { get; }
        public string Diagnostic { get; }
        public Pose FinalCameraPose { get; }
        public float FinalFieldOfView { get; }

        public CinematicResult(CinematicState state, string diagnostic, Pose pose, float fov)
        {
            State = state;
            Diagnostic = diagnostic;
            FinalCameraPose = pose;
            FinalFieldOfView = fov;
        }
    }

    public sealed class CinematicSession : IDisposable
    {
        internal readonly struct TransformSnapshot
        {
            public readonly Transform Target;
            public readonly Vector3 WorldPosition;
            public readonly Quaternion WorldRotation;

            public TransformSnapshot(Transform target, Vector3 pos, Quaternion rot)
            {
                Target = target;
                WorldPosition = pos;
                WorldRotation = rot;
            }
        }

        private readonly CinematicPlayer _player;
        private readonly CinematicDefinition _definition;
        private readonly CinematicBindings _bindings;
        private readonly CinematicCameraLease _cameraLease;
        private readonly List<TransformSnapshot> _snapshots;
        private readonly bool[] _audioFired;
        private readonly List<AudioSource> _ownedAudioSources;
        private readonly Pose _frame;

        private float _elapsedTime;
        private Pose _lastCameraPose;
        private float _lastFov;

        public CinematicState State { get; private set; } = CinematicState.Prepared;
        public bool IsTerminal => State == CinematicState.Completed || State == CinematicState.Cancelled || State == CinematicState.Failed;

        public event Action<CinematicResult> Finished;

        internal CinematicSession(
            CinematicPlayer player,
            CinematicDefinition definition,
            CinematicBindings bindings,
            CinematicCameraLease lease,
            List<TransformSnapshot> snapshots)
        {
            _player = player;
            _definition = definition;
            _bindings = bindings;
            _cameraLease = lease;
            _snapshots = snapshots;
            _frame = bindings.Frame;
            _ownedAudioSources = new List<AudioSource>();

            if (definition.AudioCues != null)
            {
                _audioFired = new bool[definition.AudioCues.Length];
            }

            _lastFov = 60f;
            if (definition.CameraKeys != null && definition.CameraKeys.Length > 0)
            {
                _lastFov = definition.CameraKeys[0].FieldOfView;
            }
        }

        private bool ValidateRuntimeBindings(out string diagnostic)
        {
            diagnostic = null;
            if (_definition == null)
            {
                diagnostic = "CinematicDefinition is null.";
                return false;
            }

            if (_cameraLease == null || !_cameraLease.ValidateBindings(out diagnostic))
            {
                if (string.IsNullOrEmpty(diagnostic))
                {
                    diagnostic = "Camera lease bindings are invalid.";
                }
                return false;
            }

            if (_snapshots != null)
            {
                for (int s = 0; s < _snapshots.Count; s++)
                {
                    if (_snapshots[s].Target == null)
                    {
                        diagnostic = "Required transform was destroyed during playback.";
                        return false;
                    }
                }
            }

            diagnostic = null;
            return true;
        }

        public void Start()
        {
            if (State != CinematicState.Prepared) return;

            if (!ValidateRuntimeBindings(out string diagnostic))
            {
                FinishWith(CinematicState.Failed, diagnostic);
                return;
            }

            State = CinematicState.Playing;
            SampleAt(0f);
        }

        public void Pause()
        {
            if (State != CinematicState.Playing) return;
            State = CinematicState.Paused;
            for (int i = 0; i < _ownedAudioSources.Count; i++)
            {
                if (_ownedAudioSources[i] != null && _ownedAudioSources[i].isPlaying)
                {
                    _ownedAudioSources[i].Pause();
                }
            }
        }

        public void Resume()
        {
            if (State != CinematicState.Paused) return;

            if (!ValidateRuntimeBindings(out string diagnostic))
            {
                FinishWith(CinematicState.Failed, diagnostic);
                return;
            }

            State = CinematicState.Playing;
            for (int i = 0; i < _ownedAudioSources.Count; i++)
            {
                if (_ownedAudioSources[i] != null)
                {
                    _ownedAudioSources[i].UnPause();
                }
            }
        }

        public void Cancel()
        {
            if (IsTerminal) return;
            FinishWith(CinematicState.Cancelled, "Cancelled by caller");
        }

        public void Dispose()
        {
            Cancel();
        }

        internal void Advance(float deltaTime)
        {
            if (IsTerminal) return;

            if (!ValidateRuntimeBindings(out string diagnostic))
            {
                FinishWith(CinematicState.Failed, diagnostic);
                return;
            }

            if (State != CinematicState.Playing) return;

            _elapsedTime += deltaTime;
            float sampleTime = Mathf.Clamp(_elapsedTime, 0f, _definition.Duration);
            SampleAt(sampleTime);

            // Audio cues
            if (_definition.AudioCues != null && _bindings.Audio != null)
            {
                for (int a = 0; a < _definition.AudioCues.Length; a++)
                {
                    if (!_audioFired[a] && _elapsedTime >= _definition.AudioCues[a].Time)
                    {
                        _audioFired[a] = true;
                        var cue = _definition.AudioCues[a];
                        if (!string.IsNullOrEmpty(cue.BindingId) &&
                            _bindings.Audio.TryGetValue(cue.BindingId, out var src) &&
                            src != null && cue.Clip != null)
                        {
                            src.PlayOneShot(cue.Clip, cue.Volume);
                            if (!_ownedAudioSources.Contains(src))
                            {
                                _ownedAudioSources.Add(src);
                            }
                        }
                    }
                }
            }

            if (_elapsedTime >= _definition.Duration)
            {
                FinishWith(CinematicState.Completed, null);
            }
        }

        private void SampleAt(float time)
        {
            // Sample camera
            if (_definition.CameraKeys != null && _definition.CameraKeys.Length > 0)
            {
                SampleKeys(_definition.CameraKeys, time, out var localPos, out var localRot, out var fov);
                Vector3 worldPos = _frame.position + _frame.rotation * localPos;
                Quaternion worldRot = _frame.rotation * localRot;
                _lastCameraPose = new Pose(worldPos, worldRot);
                _lastFov = fov;
                _cameraLease?.Apply(_lastCameraPose, _lastFov);
            }

            // Sample transforms
            if (_definition.TransformTracks != null && _bindings.Transforms != null)
            {
                for (int t = 0; t < _definition.TransformTracks.Length; t++)
                {
                    var track = _definition.TransformTracks[t];
                    if (track != null && !string.IsNullOrEmpty(track.BindingId) &&
                        _bindings.Transforms.TryGetValue(track.BindingId, out var target) &&
                        target != null && track.Keys != null && track.Keys.Length > 0)
                    {
                        SampleKeys(track.Keys, time, out var tPos, out var tRot, out _);
                        target.position = _frame.position + _frame.rotation * tPos;
                        target.rotation = _frame.rotation * tRot;
                    }
                }
            }
        }

        private static void SampleKeys(
            CinematicPoseKey[] keys,
            float time,
            out Vector3 localPosition,
            out Quaternion localRotation,
            out float fieldOfView)
        {
            if (keys == null || keys.Length == 0)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                fieldOfView = 60f;
                return;
            }

            if (keys.Length == 1 || time <= keys[0].Time)
            {
                localPosition = keys[0].Position;
                localRotation = Quaternion.Euler(keys[0].EulerAngles);
                fieldOfView = keys[0].FieldOfView;
                return;
            }

            if (time >= keys[keys.Length - 1].Time)
            {
                var last = keys[keys.Length - 1];
                localPosition = last.Position;
                localRotation = Quaternion.Euler(last.EulerAngles);
                fieldOfView = last.FieldOfView;
                return;
            }

            int idx = 0;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (time >= keys[i].Time && time <= keys[i + 1].Time)
                {
                    idx = i;
                    break;
                }
            }

            var kA = keys[idx];
            var kB = keys[idx + 1];
            float dur = kB.Time - kA.Time;
            float factor = dur > 0.00001f ? Mathf.Clamp01((time - kA.Time) / dur) : 1f;

            localPosition = Vector3.Lerp(kA.Position, kB.Position, factor);
            Quaternion rotA = Quaternion.Euler(kA.EulerAngles);
            Quaternion rotB = Quaternion.Euler(kB.EulerAngles);
            localRotation = Quaternion.Slerp(rotA, rotB, factor);
            fieldOfView = Mathf.Lerp(kA.FieldOfView, kB.FieldOfView, factor);
        }

        private void FinishWith(CinematicState state, string diagnostic)
        {
            if (IsTerminal) return;
            State = state;

            // Stop owned audio
            for (int i = 0; i < _ownedAudioSources.Count; i++)
            {
                if (_ownedAudioSources[i] != null && _ownedAudioSources[i].isPlaying)
                {
                    _ownedAudioSources[i].Stop();
                }
            }

            // Restore transform snapshots
            if (_snapshots != null)
            {
                for (int s = 0; s < _snapshots.Count; s++)
                {
                    var snap = _snapshots[s];
                    if (snap.Target != null)
                    {
                        snap.Target.position = snap.WorldPosition;
                        snap.Target.rotation = snap.WorldRotation;
                    }
                }
            }

            // Release camera lease before invoking callbacks
            _cameraLease?.Dispose();

            // Clear player reference
            _player?.OnSessionFinished(this);

            var result = new CinematicResult(state, diagnostic, _lastCameraPose, _lastFov);

            // Defensive notification
            var handlers = Finished?.GetInvocationList();
            if (handlers != null)
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    try
                    {
                        ((Action<CinematicResult>)handlers[i])(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }
    }
}


