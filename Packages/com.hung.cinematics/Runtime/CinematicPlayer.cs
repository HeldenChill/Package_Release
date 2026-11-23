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

            // Validate only. The camera lease and transform snapshots are taken in Start, so a prepared
            // session neither freezes the camera nor restores targets to a stale pose (BUG-261114-ce05).
            if (!CinematicSession.TryCaptureSnapshots(definition, bindings, out _, out diagnostic))
            {
                return false;
            }

            if (!CinematicCameraLease.CanAcquire(bindings, out diagnostic))
            {
                return false;
            }

            session = new CinematicSession(this, definition, bindings);
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

