using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    using Dynamic.WorldInterface.Data;
    using Gameplay.Character.WorldInterface;
    public class DetectGroundEdgeSensor : BaseSensor
    {
        [SerializeField]
        float checkLength;
        [SerializeField]
        protected Transform leftCheckPointPos;
        [SerializeField]
        protected Transform rightCheckPointPos;
        public DetectGroundEdgeData SensorData
        {
            get;
            protected set;
        }
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data, Parameter);
            SensorData = new DetectGroundEdgeData();
            Data.SensorDatas.Add(SensorData);
        }

        public override void UpdateState()
        {
            if(rightCheckPointPos.right.x > 0)
            {
                SensorData.RightEdgeDetected = Physics2D.Raycast(rightCheckPointPos.position, rightCheckPointPos.right, checkLength, layer);
                SensorData.LeftEdgeDetected = Physics2D.Raycast(leftCheckPointPos.position, leftCheckPointPos.right, checkLength, layer);
            }
            else
            {
                SensorData.RightEdgeDetected = Physics2D.Raycast(leftCheckPointPos.position, leftCheckPointPos.right, checkLength, layer);
                SensorData.LeftEdgeDetected = Physics2D.Raycast(rightCheckPointPos.position, rightCheckPointPos.right, checkLength, layer);
            }
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.DrawLine(rightCheckPointPos.position, rightCheckPointPos.position + rightCheckPointPos.right * checkLength);
            Gizmos.DrawLine(leftCheckPointPos.position, leftCheckPointPos.position + leftCheckPointPos.right * checkLength);
        }
    }
}