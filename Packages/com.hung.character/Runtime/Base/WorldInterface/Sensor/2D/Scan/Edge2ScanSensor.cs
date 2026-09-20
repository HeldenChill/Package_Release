using Dynamic.WorldInterface.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Character.WorldInterface;
using Hung.Utilities;
namespace Dynamic.WorldInterface.Sensor
{
    public class Edge2ScanSensor : BaseSensor
    {
        [SerializeField] protected Transform eyePosition;
        [SerializeField] protected float viewRange;
        [SerializeField] protected float viewAngle;

        #region Variable
        private float edge1Deta = 0;
        private float edge2Deta = 0;
        private float middleAngle = 0;
        [Range(0f, 100f)]
        public float angleChangeVal = 2;
        [Range(0f, 10f)]
        [SerializeField]
        protected float dispersion = 0f;

        #endregion

        public Edge2SenorData SensorData
        {
            get; protected set;
        }
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data, Parameter);
            SensorData = new Edge2SenorData();
            Data.SensorDatas.Add(SensorData);

            middleAngle = transform.parent.rotation.eulerAngles.z;
            SensorData.Edge1Angle = viewAngle + middleAngle;
            SensorData.Edge2Angle = -viewAngle + middleAngle;
            edge1Deta = viewAngle;
            edge2Deta = -viewAngle;
        }
        public override void UpdateState()
        {
            Observation();
        }

        protected override void OnDrawGizmos()
        {
            if (Data != null && eyePosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(eyePosition.position, eyePosition.position + (Vector3)(MathHelper.AngleToVector(SensorData.Edge1Angle, Data.PerceptionData.IsFaceRight) * viewRange));
                Gizmos.DrawLine(eyePosition.position, eyePosition.position + (Vector3)(MathHelper.AngleToVector(SensorData.Edge2Angle, Data.PerceptionData.IsFaceRight) * viewRange));
            }

        }

        public virtual void Observation()
        {           
            if (Mathf.Abs(SensorData.Edge1Angle - SensorData.Edge2Angle) <= dispersion)
            {
                angleChangeVal = 0;
            }
            middleAngle = transform.parent.rotation.eulerAngles.z;
            edge1Deta += -angleChangeVal * Time.deltaTime;
            edge2Deta += angleChangeVal * Time.deltaTime;

            SensorData.Edge1Angle = middleAngle + edge1Deta;
            SensorData.Edge2Angle = middleAngle + edge2Deta;
            
            SensorData.Edge1Direction = MathHelper.AngleToVector(SensorData.Edge1Angle, Data.PerceptionData.IsFaceRight);
            SensorData.Edge2Direction = MathHelper.AngleToVector(SensorData.Edge2Angle, Data.PerceptionData.IsFaceRight);

            //RaycastHit2D hit;
            //hit = Physics2D.Raycast(eyePosition.position, SensorData.Edge1Direction, viewRange);

        }
    }
}