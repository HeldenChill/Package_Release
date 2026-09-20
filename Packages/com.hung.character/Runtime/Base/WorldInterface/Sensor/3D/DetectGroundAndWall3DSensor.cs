using UnityEngine;
namespace Dynamic.WorldInterface.Sensor
{
    using Data;
    using Gameplay.Character.WorldInterface;
    public class DetectGroundAndWall3DSensor : BaseSensor
    {
        [SerializeField] protected float groundCheckRadius;
        [SerializeField] protected float wallCheckRadius;
        [SerializeField] protected Transform groundCheck;
        [SerializeField] protected Transform wallCheck;

        [Header("Stability (Fix E - camera jitter)")]
        [Tooltip("Keep IsGrounded true for this long after the last real ground contact. " +
                 "Stops the flag flapping on step edges and corners, which was churning the " +
                 "state machine and producing camera jitter. 0 restores the old raw behaviour.")]
        [SerializeField] protected float groundedCoyoteTime = 0.08f;

        [Tooltip("Extra radius used only to KEEP ground contact once established, so entering " +
                 "and leaving ground use different thresholds instead of one flickering test.")]
        [SerializeField] protected float groundedExitPadding = 0.05f;

        Collider[] colliders;
        float lastGroundedTime = float.NegativeInfinity;
        bool wasGrounded;

        private void Awake()
        {
            colliders = new Collider[1];
        }

        /// <summary>
        /// Create a circle to check collide with ground or not
        /// Create a raycast to check collide with wall or not
        ///
        /// Ground contact is hysteresised: a slightly larger radius is used while already
        /// grounded, and contact persists for <see cref="groundedCoyoteTime"/> after the last
        /// real hit. Both exist because a single-sample binary test flips every tick at a
        /// corner, and every flip drove a state transition that wrote velocity.
        /// See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
        /// </summary>
        public override void UpdateState()
        {
            Physics.Raycast(new Ray(groundCheck.position, -groundCheck.up), out Data.TouchingGroundPoint, groundCheckRadius, layer);
            Data.Normal = groundCheck.up;

            float radius = wasGrounded ? groundCheckRadius + groundedExitPadding : groundCheckRadius;
            bool rawGrounded = Physics.OverlapSphereNonAlloc(groundCheck.position, radius, colliders, layer) > 0;

            if (rawGrounded) lastGroundedTime = Time.time;

            bool coyoteActive = groundedCoyoteTime > 0f && Time.time - lastGroundedTime <= groundedCoyoteTime;
            bool grounded = rawGrounded || coyoteActive;

            // Report the raw sample separately from the filtered one. A high raw-false count means
            // the capsule really is leaving the floor; a low one with high flips would mean the
            // filter itself is the problem.
            // Attribute to the owning character's root, so an Enemy's samples are not pooled with
            // the Player's in the probe's static counters.
            int ownerId = transform.root.gameObject.GetInstanceID();
            Hung.Base.MotionProbe.GroundSample(rawGrounded, !rawGrounded && coyoteActive, ownerId);

            Data.IsGrounded = grounded;
            wasGrounded = grounded;

            Data.IsTouchingWall = Physics.Raycast(new Ray(wallCheck.position, wallCheck.forward), out Data.TouchingWallPoint, wallCheckRadius, layer);

            // Probe choke point: counts transitions, not samples, so a clean walk reports ~0
            // flips and a corner reports a burst. That difference is the Fix E evidence.
            Hung.Base.MotionProbe.Sensors(Data.IsGrounded, Data.IsTouchingWall, ownerId);
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + transform.forward * wallCheckRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckRadius);
        }
    }
}
