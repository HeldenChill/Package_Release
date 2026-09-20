using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using Gameplay.Character.WorldInterface;
    using Hung.Utilities;
    using Dynamic.WorldInterface.Data;

    public class Type1Sensor : MultiRaycastSensor
    {
        [SerializeField] Transform positionCharacter;
        List<RaycastHit2D> detectInformation = new List<RaycastHit2D>();

        public Type1SensorData SensorData
        {
            get;
            protected set;
        }
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data,Parameter);
            SensorData = new Type1SensorData();
            Data.SensorDatas.Add(SensorData);
        }
        /// <summary>
        /// Observation:Create multi SRayCast(Raycast has ability to detect surface direction)
        /// Add the information to Data storage
        /// </summary>
        protected override void Observation()
        {
            detectInformation.Clear();
            for (float currentAngle = viewAngle - scanAngle; currentAngle <= viewAngle + scanAngle + 0.1f; currentAngle += 2 * scanAngle / numberOfRayCast)
            {
                //SRaycastHit2D hit = SRayCast2D.Raycast(eyePosition.position, MathHelper.AngleToVector(currentAngle, Parameter.ScanSensor.IsFaceRight), viewRange, dectectLayer);
                //detectInformation.Add(hit);
                RaycastHit2D hit = Physics2D.Raycast(eyePosition.position, MathHelper.AngleToVector(currentAngle, Data.PerceptionData.IsFaceRight), viewRange, layer);
                detectInformation.Add(hit);
            }

            foreach(var i in detectInformation)
            {
                //if (i.Hit)
                //{
                //    Data.Type1Sensor.Add(i);
                //}
                if (i)
                {
                    SensorData.Add(i);
                }
            }

            UpdateData();
        }
        /// <summary>
        /// Update the position of character on map tile(VectorInt position)
        /// </summary>

        private TilePosition dynamicVariable = new TilePosition(Vector2Int.zero);
        protected override void UpdateData()
        {
            if (Data.TouchingGround2DPoint)
            {
                SensorData.DynamicPosition = SensorData.GetPosition(TilePosition.CalculateIntPosition(Data.TouchingGround2DPoint.point));
            }
            else if (Data.TouchingWall2DPoint)
            {
                SensorData.DynamicPosition = SensorData.GetPosition(TilePosition.CalculateIntPosition(Data.TouchingWall2DPoint.point));
            }
            else
            {
                //Debug.Log("WorldInterfaceData:" + lastDynamicData);
                dynamicVariable.Position = TilePosition.CalculateIntPosition(positionCharacter.position);
                dynamicVariable.GlobalPosition = positionCharacter.position;
                SensorData.DynamicPosition = dynamicVariable;
            }
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            //if (eyePosition != null)
            //{
            //    for (int i = 0; i < detectInformation.Count; i++)
            //    {
            //        if (detectInformation[i].Hit)
            //        {
            //            Gizmos.color = Color.blue;
            //            Gizmos.DrawLine(eyePosition.position, detectInformation[i].Hit.point);

            //            Gizmos.color = Color.red;
            //            for (int j = 0; j < detectInformation[i].Hits.Length; j++)
            //            {
            //                if (detectInformation[i].Hits[j])
            //                {
            //                    Gizmos.DrawLine(detectInformation[i].PointCheck, detectInformation[i].Hits[j].point);
            //                    //Debug.Log(detectInformation[i].Hits[j].point);
            //                }
            //                else
            //                {
            //                    Gizmos.DrawLine(detectInformation[i].PointCheck, detectInformation[i].PointCheck + MathHelper.AngleToVector(90 * j, true) * SRayCast2D.LengthCheck);
            //                }
            //            }
            //        }
            //        else
            //        {
            //            Gizmos.color = Color.blue;
            //            float currentAngle = viewAngle - scanAngle + 2 * scanAngle * i / numberOfRayCast;
            //            if (Data)
            //            {
            //                Gizmos.DrawLine(eyePosition.position, eyePosition.position + (Vector3)(MathHelper.AngleToVector(currentAngle, Parameter.ScanSensor.IsFaceRight) * viewRange));
            //            }
            //            else
            //            {
            //                Gizmos.DrawLine(eyePosition.position, eyePosition.position + (Vector3)(MathHelper.AngleToVector(currentAngle, true) * viewRange));
            //            }

            //        }
            //    }
            //}
        }
    }
}
