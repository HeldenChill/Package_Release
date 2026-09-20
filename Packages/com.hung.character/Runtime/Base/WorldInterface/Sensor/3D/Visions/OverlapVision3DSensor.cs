using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using System;
    public class OverlapVision3DSensor : Vision3DSensor
    {
        public enum TYPE
        {
            CIRCLE = 0,
            RECTANGLE = 1,
        }
        [SerializeField]
        TYPE type; 
        public override void UpdateState()
        {
            switch (type)
            {
                case TYPE.CIRCLE:
                    UpdateCircleState();
                    break;
                case TYPE.RECTANGLE:
                    break;
            }
        }

        protected override void OnDrawGizmos()
        {
            switch (type)
            {
                case TYPE.CIRCLE:
                    UpdateCircleGizmos();
                    break;
                case TYPE.RECTANGLE:
                    break;
            }
        }

        protected void UpdateCircleState()
        {
            Array.Clear(colliders, 0, colliders.Length);
            Data.HostileColliders.Clear();

            Physics.OverlapSphereNonAlloc(tf.position, visionDistance, colliders, layer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Data.HostileColliders.Add(colliders[i]);
                }
            }
        }
        protected void UpdateCircleGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(tf.position, visionDistance);
        } 
    }
}
