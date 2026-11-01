using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Cinematics
{
    [ExecuteAlways]
    public sealed class CinematicPlayer : MonoBehaviour
    {
        public bool AutoTick = true;
        private CinematicSession _activeSession;

        public bool TryPrepare(
            CinematicDefinition definition,
            CinematicBindings bindings,
            out CinematicSession session,
            out string diagnostic)
        {
            session = null;

            if (definition == null)
            {
                diagnostic = "CinematicDefinition is null.";
                return false;
            }

            if (bindings == null)
            {
                diagnostic = "CinematicBindings is null.";
                return false;
            }

            if (!definition.Validate(out diagnostic))
            {
                return false;
            }

            if (_activeSession != null && !_activeSession.IsTerminal)
            {
                diagnostic = "CinematicPlayer already has an active session.";
                return false;
            }

            // Validate transform bindings
            var snapshots = new List<CinematicSession.TransformSnapshot>();
            if (definition.TransformTracks != null && definition.TransformTracks.Length > 0)
            {
                if (bindings.Transforms == null)
                {
                    diagnostic = "CinematicBindings Transforms dictionary is null.";
                    return false;
                }

                for (int t = 0; t < definition.TransformTracks.Length; t++)
                {
                    var track = definition.TransformTracks[t];
                    if (track != null && !string.IsNullOrEmpty(track.BindingId))
                    {
                        if (!bindings.Transforms.TryGetValue(track.BindingId, out var tr) || tr == null)
                        {
                            diagnostic = $"Missing required transform binding for track '{track.BindingId}'.";
                            return false;
                        }

                        snapshots.Add(new CinematicSession.TransformSnapshot(tr, tr.position, tr.rotation));
                    }
                }
            }

            // Acquire camera lease
            if (!CinematicCameraLease.TryAcquire(bindings, out var lease, out diagnostic))
            {
                return false;
            }

            session = new CinematicSession(this, definition, bindings, lease, snapshots);
            _activeSession = session;
            diagnostic = null;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentException($"Invalid deltaTime: {deltaTime}. Must be a non-negative finite number.");
            }

            if (_activeSession != null && !_activeSession.IsTerminal)
            {
                _activeSession.Advance(deltaTime);
            }
        }

        private void Update()
        {
            if (AutoTick && Application.isPlaying)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (_activeSession != null && !_activeSession.IsTerminal)
            {
                _activeSession.Cancel();
            }
        }

        internal void OnSessionFinished(CinematicSession session)
        {
            if (_activeSession == session)
            {
                _activeSession = null;
            }
        }
    }
}

