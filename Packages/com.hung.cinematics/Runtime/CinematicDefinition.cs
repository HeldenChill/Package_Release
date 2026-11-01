using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Cinematics
{
    public enum CinematicState
    {
        Prepared,
        Playing,
        Paused,
        Completed,
        Cancelled,
        Failed
    }

    [Serializable]
    public struct CinematicPoseKey
    {
        public float Time;
        public Vector3 Position;
        public Vector3 EulerAngles;
        public float FieldOfView;
    }

    [Serializable]
    public sealed class CinematicTransformTrack
    {
        public string BindingId;
        public CinematicPoseKey[] Keys;
    }

    [Serializable]
    public struct CinematicAudioCue
    {
        public float Time;
        public string BindingId;
        public AudioClip Clip;
        [Range(0f, 1f)]
        public float Volume;
    }

    public sealed class CinematicDefinition : ScriptableObject
    {
        public float Duration = 2.5f;
        public CinematicPoseKey[] CameraKeys;
        public CinematicTransformTrack[] TransformTracks;
        public CinematicAudioCue[] AudioCues;

        public bool Validate(out string diagnostic)
        {
            if (float.IsNaN(Duration) || float.IsInfinity(Duration) || Duration <= 0f)
            {
                diagnostic = $"Invalid Duration: {Duration}. Duration must be a positive finite number.";
                return false;
            }

            if (CameraKeys == null || CameraKeys.Length == 0)
            {
                diagnostic = "Camera track cannot be null or empty.";
                return false;
            }

            if (Mathf.Abs(CameraKeys[0].Time) > 0.0001f)
            {
                diagnostic = $"Camera track first key must start at zero, but was {CameraKeys[0].Time}.";
                return false;
            }

            if (Mathf.Abs(CameraKeys[CameraKeys.Length - 1].Time - Duration) > 0.0001f)
            {
                diagnostic = $"Camera track last key must match Duration {Duration}, but was {CameraKeys[CameraKeys.Length - 1].Time}.";
                return false;
            }

            for (int i = 0; i < CameraKeys.Length; i++)
            {
                var key = CameraKeys[i];
                if (float.IsNaN(key.Time) || float.IsInfinity(key.Time) || key.Time < 0f || key.Time > Duration + 0.0001f)
                {
                    diagnostic = $"Camera key at index {i} has time outside valid range [0, {Duration}]: {key.Time}.";
                    return false;
                }

                if (i > 0 && key.Time <= CameraKeys[i - 1].Time)
                {
                    diagnostic = $"Camera key at index {i} has descending or duplicate time {key.Time} <= {CameraKeys[i - 1].Time}.";
                    return false;
                }

                if (!IsFinite(key.Position) || !IsFinite(key.EulerAngles))
                {
                    diagnostic = $"Camera key at index {i} has non-finite pose values.";
                    return false;
                }

                if (float.IsNaN(key.FieldOfView) || float.IsInfinity(key.FieldOfView) || key.FieldOfView < 1f || key.FieldOfView > 179f)
                {
                    diagnostic = $"Camera key at index {i} has invalid FOV {key.FieldOfView}. Must be between 1 and 179.";
                    return false;
                }
            }

            if (TransformTracks != null)
            {
                var seenBindings = new HashSet<string>(StringComparer.Ordinal);
                for (int t = 0; t < TransformTracks.Length; t++)
                {
                    var track = TransformTracks[t];
                    if (track == null)
                    {
                        diagnostic = $"Transform track at index {t} is null.";
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(track.BindingId))
                    {
                        diagnostic = $"Transform track at index {t} has empty or null binding ID.";
                        return false;
                    }

                    if (!seenBindings.Add(track.BindingId))
                    {
                        diagnostic = $"Duplicate transform track binding ID: '{track.BindingId}'.";
                        return false;
                    }

                    if (track.Keys != null)
                    {
                        for (int k = 0; k < track.Keys.Length; k++)
                        {
                            var key = track.Keys[k];
                            if (float.IsNaN(key.Time) || float.IsInfinity(key.Time) || key.Time < 0f || key.Time > Duration + 0.0001f)
                            {
                                diagnostic = $"Transform track '{track.BindingId}' key {k} has time outside range: {key.Time}.";
                                return false;
                            }

                            if (k > 0 && key.Time <= track.Keys[k - 1].Time)
                            {
                                diagnostic = $"Transform track '{track.BindingId}' key {k} has descending or duplicate time {key.Time}.";
                                return false;
                            }

                            if (!IsFinite(key.Position) || !IsFinite(key.EulerAngles))
                            {
                                diagnostic = $"Transform track '{track.BindingId}' key {k} has non-finite pose.";
                                return false;
                            }
                        }
                    }
                }
            }

            if (AudioCues != null)
            {
                for (int a = 0; a < AudioCues.Length; a++)
                {
                    var cue = AudioCues[a];
                    if (float.IsNaN(cue.Time) || float.IsInfinity(cue.Time) || cue.Time < 0f || cue.Time > Duration + 0.0001f)
                    {
                        diagnostic = $"Audio cue at index {a} has time outside range: {cue.Time}.";
                        return false;
                    }

                    if (float.IsNaN(cue.Volume) || float.IsInfinity(cue.Volume) || cue.Volume < 0f || cue.Volume > 1f)
                    {
                        diagnostic = $"Audio cue at index {a} has invalid volume {cue.Volume}. Must be in [0, 1].";
                        return false;
                    }
                }
            }

            diagnostic = null;
            return true;
        }

        private static bool IsFinite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
                   !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
                   !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        }
    }
}
