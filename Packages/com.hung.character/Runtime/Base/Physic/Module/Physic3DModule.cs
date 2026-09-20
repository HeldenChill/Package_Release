using UnityEngine;
using Gameplay.Character.Physic;
using Utilities;
using DG.Tweening;

namespace _Game.Character
{
    public class Physic3DModule : A3DPhysicModule
    {
        [SerializeField]
        ObjectRotation headRotation;
        [Header("Grounded vertical handling (Fix I - camera jitter)")]
        [Tooltip("Downward speed held while resting on a surface. Small negative keeps the capsule " +
                 "pressed onto the floor without letting gravity accumulate. 0 would let it drift " +
                 "off slopes and stairs.")]
        [SerializeField] float groundedStickSpeed = -1f;

        [Tooltip("Hard cap on downward speed so a long fall cannot reach absurd values. " +
                 "Real terminal velocity for a human is about 55 m/s.")]
        [SerializeField] float maxFallSpeed = -55f;

        string setVelocityTag;
        string setLocalVelocityTag;
        string SetVelocityTag => setVelocityTag ??= $"SetVelocity/{name}";
        string SetLocalVelocityTag => setLocalVelocityTag ??= $"SetLocalVelocityXZ/{name}";

        public override void SetVelocity(Vector3 velocity)
        {
            // Fix G/I (camera jitter): a purely horizontal move request must not destroy the
            // vertical velocity gravity has accumulated - but it must not let it accumulate
            // forever either.
            //
            // Movement states build `moveDirection` by projecting onto the XZ plane, so they pass
            // Y exactly 0 every physics step.
            //
            //   Original bug: assigning that Y=0 straight through re-zeroed gravity 90 times a
            //   second, so the capsule never settled - it fell one step, got zeroed, fell again.
            //   That vertical buzz was the dominant jitter term.
            //
            //   Fix G carried rb.linearVelocity.y forward instead. That removed the buzz (measured
            //   Y jerk fell to 0.002) but introduced a worse problem: resting contact does not zero
            //   linearVelocity.y in PhysX, it only stops the position advancing. So gravity kept
            //   adding -9.81*dt every step with nothing consuming it, and Y reached -240 m/s while
            //   standing still. At that speed one step wants to move 2.66 m - about 14x the ground
            //   probe's 0.192 m depth - which is why IsGrounded kept dropping out on flat floor.
            //
            //   Fix I: carry Y forward only while airborne, clamped to a real terminal velocity,
            //   and hold a small constant downward speed while grounded. The stick speed keeps the
            //   capsule on the floor across slopes and stairs without accumulating.
            //
            //   Fix J: grounded-ness comes from Data.IsGrounded, mirrored from the world-interface
            //   sensor. Fix I's first attempt used this module's own OnCollisionStay callbacks,
            //   which never fired under ContinuousSpeculative collision detection (Fix F) - so the
            //   clamp branch was never taken and Y still ran to -236 m/s. Two independent
            //   grounded sources disagreeing is exactly the trap this project has hit before.
            //
            // Callers that intend a vertical change (jump, knockback) pass a non-zero Y and are
            // left untouched. SetLocalVelocityXZ preserves Y the same way.
            // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
            if (Mathf.Approximately(velocity.y, 0f))
                velocity.y = ResolveVerticalVelocity();

            // Probe choke point: every assignment to rb.linearVelocity passes through here, so
            // this is where we can prove no write lands off the physics clock (Fix C).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Hung.Base.MotionProbe.Enabled)
                Hung.Base.MotionProbe.VelocityWrite(velocity, SetVelocityTag, OwnerId);
#endif
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// The vertical velocity a horizontal-only move request should keep.
        /// Grounded: a small constant downward stick speed, so gravity cannot accumulate while
        /// resting. Airborne: whatever gravity has built up, clamped to a real terminal velocity.
        /// Shared by both velocity setters so they cannot drift apart (Fix K).
        /// </summary>
        float ResolveVerticalVelocity()
        {
            if (Data != null && Data.IsGrounded) return groundedStickSpeed;
            return Mathf.Max(rb.linearVelocity.y, maxFallSpeed);
        }

        /// <summary>
        /// Root GameObject id, used to attribute probe readings to one character. The probe's
        /// counters are static, so Player and Enemy samples would otherwise be pooled.
        /// </summary>
        int OwnerId => transform.root.gameObject.GetInstanceID();

        void FixedUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Hung.Base.MotionProbe.Enabled)
                Hung.Base.MotionProbe.GroundContact(Data != null && Data.IsGrounded, rb.linearVelocity.y, OwnerId);
#endif
        }
        public override void SetLocalVelocityXZ(Vector3 velocity)
        {
            // Fix K (camera jitter): this path had no vertical clamp at all. It read
            // rb.linearVelocity.y raw and carried it forward unbounded, so a fall drove Y to
            // -236 m/s - the same runaway Fix I fixed in SetVelocity, still live in its sibling.
            // Fix I/J only guarded SetVelocity; the air state uses this method, which is why
            // peakDownY stayed at -236 even after physGround started reporting correctly.
            // Both setters now share ResolveVerticalVelocity so they cannot diverge again.
            // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
            velocity.y = ResolveVerticalVelocity();

            // Vector3 parallelComponent = Vector3.Project(rb.linearVelocity, Data.PerceptionData.Tf.up);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Hung.Base.MotionProbe.Enabled)
                Hung.Base.MotionProbe.VelocityWrite(velocity, SetLocalVelocityTag, OwnerId);
#endif
            rb.AddForce(velocity - rb.linearVelocity, ForceMode.VelocityChange);
        }
        public override void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            rb.AddForce(force, mode);
        }

        public override void UpdateData()
        {
            Data.RbVelocity = rb.linearVelocity;
        }

        public override void SetColliderSize(Vector3 size, float time = -1)
        {
            if (time > 0)
            {
                Vector3 origin = new Vector3(capsuleCollider.radius, capsuleCollider.height, 0);
                DOVirtual.Vector3(origin, size, time, (x) =>
                {
                    capsuleCollider.height = x.y;
                    capsuleCollider.radius = x.x;
                }).OnComplete(() =>
                {
                    capsuleCollider.height = size.y;
                    capsuleCollider.radius = size.x;
                });


            }
            else
            {
                capsuleCollider.height = size.y;
                capsuleCollider.radius = size.x;
            }

        }
        public override void SetColliderOffset(Vector3 offset, float time = -1)
        {
            if (time > 0)
            {
                DOVirtual.Vector3(capsuleCollider.center, offset, time, (x) =>
                {
                    capsuleCollider.center = x;
                }).OnComplete(() =>
                {
                    capsuleCollider.center = offset;
                });
            }
            else
            {
                capsuleCollider.center = offset;
            }
        }
        public override void LookTowardPosition(Vector3 position)
        {
            // DevLog.Log(DevId.Gameplay, "LookTowardPosition: " + position);
            headRotation.SetTowardPosition(position);
        }
    }
}