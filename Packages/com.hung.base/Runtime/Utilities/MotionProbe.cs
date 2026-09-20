using UnityEngine;

namespace Hung.Base
{
    /// <summary>
    /// Layer-1 static collection point for movement/camera instrumentation.
    /// Lives in Hung.Base so any layer (physics modules, logic states, sensors, the
    /// composition root's reporter) can write to it without creating an upward
    /// assembly reference. Nothing here logs; a higher-layer reporter reads and prints.
    ///
    /// Used by the player camera jitter investigation:
    /// Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
    ///
    /// The whole class compiles to nothing outside the editor and development builds,
    /// so leaving the call sites in place costs no release performance.
    /// ponytail: static fields, no interface, no DI. Delete the class and the call sites
    /// together when the jitter bug is closed.
    /// </summary>
    public static class MotionProbe
    {
        /// <summary>Master switch. A higher layer flips this on; call sites stay cheap when off.</summary>
        public static bool Enabled;

        // --- velocity write clock accounting (diagnosis defect 1 / Fix C) ---
        public static int VelocityWritesInFixed;
        public static int VelocityWritesOutsideFixed;
        public static string LastOffClockWriter = "(none)";
        public static Vector3 LastVelocityWritten;

        // --- state machine clock accounting ---
        public static int StateChangesInFixed;
        public static int StateChangesOutsideFixed;
        public static string LastStateName = "?";
        public static string LastOffClockStateChange = "(none)";

        // --- sensor stability (diagnosis defect 5 / Fix E) ---
        public static int GroundedFlips;
        public static int WallFlips;
        public static bool LastGrounded;
        public static bool LastWall;
        public static bool SensorSeeded;

        // --- deferral accounting (Fix C): how many render-clock writes were buffered ---
        public static int VelocityWritesDeferred;
        public static int VelocityWritesCoalesced;

        // --- vertical velocity health (Fix I) ---
        // PeakDownwardY is the whole point: a resting character must never accumulate downward
        // speed. Anything beyond a few m/s while PhysicsGroundContact is true means gravity is
        // being integrated with nothing consuming it.
        public static bool PhysicsGroundContact;
        public static float PeakDownwardY;
        public static float LastY;

        // --- sensor call accounting ---
        // SensorCalls counts how many times the ground sensor sampled in a window. Comparing it
        // against the window's physics-step count says whether the sensor runs once per step
        // (expected) or more often - which would make a raw flip count uninterpretable.
        public static int SensorCalls;

        /// <summary>
        /// Instance id of the character the probe should attribute readings to (the Player).
        /// Every character runs the same sensors and physics module, and these counters are static,
        /// so without this filter an Enemy's samples are pooled with the Player's - which showed up
        /// as an impossible "2 sensor calls per physics step".
        /// 0 means unfiltered (accept everything).
        /// </summary>
        public static int FocusInstanceId;

        /// <summary>True when the caller is the instance the probe is focused on.</summary>
        public static bool IsFocused(int instanceId)
        {
            return FocusInstanceId == 0 || FocusInstanceId == instanceId;
        }
        public static int RawGroundedTrue;
        public static int RawGroundedFalse;
        public static int CoyoteRescues;

        /// <summary>
        /// Record an assignment to Rigidbody.linearVelocity.
        /// Time.inFixedTimeStep is the whole point: it tells us whether the write happened
        /// on the physics clock (correct) or the render clock (defect 1).
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void VelocityWrite(Vector3 velocity, string writer, int instanceId = 0)
        {
            if (!Enabled) return;
            if (!IsFocused(instanceId)) return;
            LastVelocityWritten = velocity;
            if (Time.inFixedTimeStep)
            {
                VelocityWritesInFixed++;
            }
            else
            {
                VelocityWritesOutsideFixed++;
                LastOffClockWriter = $"{writer}@f{Time.frameCount}";
            }
        }

        /// <summary>Record a state transition and which clock it happened on.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void StateChange(string stateName)
        {
            if (!Enabled) return;
            LastStateName = stateName;
            if (Time.inFixedTimeStep)
            {
                StateChangesInFixed++;
            }
            else
            {
                StateChangesOutsideFixed++;
                LastOffClockStateChange = $"{stateName}@f{Time.frameCount}";
            }
        }

        /// <summary>
        /// Record ground/wall sensor booleans. Counts transitions, not samples, so a
        /// stable walk reports ~0 and a corner reports a burst.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Sensors(bool grounded, bool wall, int instanceId = 0)
        {
            if (!Enabled) return;
            if (!IsFocused(instanceId)) return;
            SensorCalls++;
            if (!SensorSeeded)
            {
                LastGrounded = grounded;
                LastWall = wall;
                SensorSeeded = true;
                return;
            }
            if (grounded != LastGrounded) GroundedFlips++;
            if (wall != LastWall) WallFlips++;
            LastGrounded = grounded;
            LastWall = wall;
        }

        /// <summary>
        /// Record that a render-clock velocity write was buffered rather than applied.
        /// <paramref name="coalesced"/> is true when an earlier buffered write was overwritten
        /// before it could flush, which means two states fought over velocity inside one frame.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void VelocityDeferred(bool coalesced)
        {
            if (!Enabled) return;
            VelocityWritesDeferred++;
            if (coalesced) VelocityWritesCoalesced++;
        }

        /// <summary>
        /// Record the physics module's own ground-contact flag and current vertical velocity.
        /// Tracks the worst downward speed seen, which is how runaway gravity accumulation shows up.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void GroundContact(bool grounded, float velocityY, int instanceId = 0)
        {
            if (!Enabled) return;
            if (!IsFocused(instanceId)) return;
            PhysicsGroundContact = grounded;
            LastY = velocityY;
            if (velocityY < PeakDownwardY) PeakDownwardY = velocityY;
        }

        /// <summary>Zero the per-window counters. Called by the reporter after each summary.</summary>
        /// <summary>
        /// Raw (pre-hysteresis) ground sample plus whether the coyote timer had to rescue it.
        /// Separating raw from filtered is the only way to tell "the sensor is noisy" from
        /// "the capsule genuinely left the floor".
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void GroundSample(bool rawGrounded, bool rescuedByCoyote, int instanceId = 0)
        {
            if (!Enabled) return;
            if (!IsFocused(instanceId)) return;
            if (rawGrounded) RawGroundedTrue++; else RawGroundedFalse++;
            if (rescuedByCoyote) CoyoteRescues++;
        }

        public static void ResetWindow()
        {
            PeakDownwardY = 0f;
            SensorCalls = 0;
            RawGroundedTrue = 0;
            RawGroundedFalse = 0;
            CoyoteRescues = 0;
            VelocityWritesInFixed = 0;
            VelocityWritesOutsideFixed = 0;
            StateChangesInFixed = 0;
            StateChangesOutsideFixed = 0;
            GroundedFlips = 0;
            WallFlips = 0;
            VelocityWritesDeferred = 0;
            VelocityWritesCoalesced = 0;
        }
    }
}
