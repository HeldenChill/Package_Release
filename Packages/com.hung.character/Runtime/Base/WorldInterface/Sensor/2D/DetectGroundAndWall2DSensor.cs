using Dynamic.WorldInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using Data;
    using Gameplay.Character.WorldInterface;
    public class DetectGroundAndWall2DSensor : BaseSensor
    {
        [SerializeField] protected float groundCheckRadius;
        [SerializeField] protected float wallCheckRadius;
        [SerializeField] protected Transform groundCheck;
        [SerializeField] protected Transform wallCheck;

        /// <summary>
        /// Create a circle to check collide with ground or not
        /// Create a raycast to check collide with wall or not
        /// </summary>
        public override void UpdateState()
        {
            Data.TouchingGround2DPoint = Physics2D.Raycast(groundCheck.position, Vector2Int.down, groundCheckRadius, layer);
            Data.IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, layer);
            RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckRadius, layer);
            Data.IsTouchingWall = hit;
            Data.TouchingWall2DPoint = hit;               
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3)(transform.right * wallCheckRadius));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckRadius);
        }
    }
}

