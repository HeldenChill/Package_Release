using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using Data;
    using Gameplay.Character.WorldInterface;
    public class DetectWallCornerSensor : BaseSensor
    {
        public float wallCheckRadius = 1f;
        [SerializeField] protected Transform cornerCheckAbove;
        [SerializeField] protected Transform cornerCheckBelow;
        protected List<Transform> mapCheckPoint;
        protected Collider2D playerCollider;

        /// <summary>
        /// This function will update data of the world interface system
        /// </summary>
        public override void UpdateState()
        {
            Data.IsTouchingCorner = !Physics2D.Raycast(cornerCheckAbove.position, transform.right, wallCheckRadius, layer)
                                        && Physics2D.Raycast(cornerCheckBelow.position, transform.right, wallCheckRadius, layer);
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cornerCheckAbove.position, cornerCheckAbove.position + (Vector3)(transform.right * wallCheckRadius));
            Gizmos.DrawLine(cornerCheckBelow.position, cornerCheckBelow.position + (Vector3)(transform.right * wallCheckRadius));
        }
    }
}
