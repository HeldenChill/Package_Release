using UnityEngine;
namespace Dynamic.WorldInterface.Sensor
{
    using Gameplay.Character.WorldInterface;
    using System.Collections.Generic;
    using System;

    public abstract class Vision3DSensor : BaseSensor
    {
        [SerializeField]
        protected float visionDistance;
        protected Collider[] colliders = new Collider[10];
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data, Parameter);
            Data.HostileColliders = new List<Collider>();
        }
    }
}