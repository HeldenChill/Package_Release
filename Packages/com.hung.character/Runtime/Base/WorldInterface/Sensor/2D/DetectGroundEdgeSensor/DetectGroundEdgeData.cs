using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Data
{
    using Gameplay.Character.WorldInterface;

    public class DetectGroundEdgeData : SensorData
    {
        public bool LeftEdgeDetected;
        public bool RightEdgeDetected;
    }
}