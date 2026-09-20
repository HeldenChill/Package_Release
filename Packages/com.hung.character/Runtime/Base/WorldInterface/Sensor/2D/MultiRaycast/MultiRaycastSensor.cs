using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using Hung.Utilities;
    using Gameplay.Character.WorldInterface;
    public class MultiRaycastSensor : BaseSensor
    {
        [SerializeField]
        protected Transform eyePosition;      
        [Min(0)]
        [SerializeField]
        protected float viewRange;
        [Range(1, 20)]
        [SerializeField]
        protected int numberOfRayCast;
        [Range(5, 180)]
        [SerializeField]
        protected float scanAngle;
        [Range(-180, 180)]
        [SerializeField]
        protected float viewAngle;
        [SerializeField]
        DATA_TYPE type;
        List<RaycastHit2D> detectInformation = new List<RaycastHit2D>();

        RaycastHit2D[] hits;
        private float increaseAngle = 0;
        private float currentAngle = 0;

        private void Awake()
        {
            increaseAngle = 2 * scanAngle / (numberOfRayCast - 1); //NOTE: Just for Editor
            hits = new RaycastHit2D[numberOfRayCast];
        }
        public override void UpdateState()
        {           
            Observation();
            UpdateData();
        }
        protected override void UpdateData()
        {
            switch (type)
            {
                case DATA_TYPE.GROUND:
                    Data.WallHit2D = detectInformation.AsReadOnly();
                    break;
                case DATA_TYPE.CHARACTER:
                    Data.CharacterHit2D = detectInformation.AsReadOnly();
                    break;

            }
        }
        protected virtual void Observation()
        {
            detectInformation.Clear();
            currentAngle = viewAngle - scanAngle;
            for (int i = 0; i < numberOfRayCast; i++)
            {             
                hits[i] = Physics2D.Raycast(eyePosition.position, MathHelper.AngleToVector(currentAngle, Data.PerceptionData.IsFaceRight), viewRange, layer);
                if (hits[i])
                {
                    detectInformation.Add(hits[i]);
                }
                currentAngle += increaseAngle;
            }
            //Debug.Log(detectInformation.Count);

        }
        protected override void OnDrawGizmos()
        {
            if (eyePosition != null && Parameter != null)
            {                
                for(int i = 0; i < numberOfRayCast; i++)
                {
                    if(hits[i])
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(eyePosition.position, hits[i].point);
                    }
                    else
                    {
                        Gizmos.color = Color.blue;
                        float currentAngle = viewAngle - scanAngle + increaseAngle * i;
                        Gizmos.DrawLine(eyePosition.position, eyePosition.position + (Vector3)(MathHelper.AngleToVector(currentAngle, Data.PerceptionData.IsFaceRight) * viewRange));
                    }
                }
            }

        }
        
    }
}