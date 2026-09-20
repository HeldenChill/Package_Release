using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.Physic
{
    public class PhysicData : Data
    {
        public Vector3 RbVelocity;

        /// <summary>
        /// Ground contact, mirrored from the world-interface sensor that already owns this question
        /// (<c>WorldInterfaceData.IsGrounded</c>). The physics module needs it to decide whether a
        /// horizontal move request should hold the capsule down or let gravity keep accumulating.
        ///
        /// Mirrored rather than re-derived on purpose: an earlier attempt gave the physics module
        /// its own collision-callback grounded check, which disagreed with the sensor (the callbacks
        /// do not fire reliably under ContinuousSpeculative collision detection) and silently
        /// disabled the clamp. One grounded source, one answer.
        /// See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
        /// </summary>
        public bool IsGrounded;
    }
}