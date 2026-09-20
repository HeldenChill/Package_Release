using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamic.WorldInterface.Data
{
    using Gameplay.Character.WorldInterface;
    public class Edge2SenorData : SensorData
    {
        public Vector2 Edge1Direction;
        public Vector2 Edge2Direction;

        public float Edge1Angle;
        public float Edge2Angle;
    }
}