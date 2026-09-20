using System;
using System.Collections.Generic;
using Gameplay.Character.WorldInterface;
using UnityEngine;

namespace Dynamic.WorldInterface.Sensor
{
    public class DetectSoundSensor : BaseSensor
    {
        [SerializeField]
        float detectSoundRadius;
        protected Collider[] colliders = new Collider[10];
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data, Parameter);
            Data.SoundColliders = new List<Collider>();
        }
        public override void UpdateState()
        {
            UpdateCircleState();
        }
        protected void UpdateCircleState()
        {
            Array.Clear(colliders, 0, colliders.Length);
            Data.SoundColliders.Clear();

            Physics.OverlapSphereNonAlloc(tf.position, detectSoundRadius, colliders, layer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Data.SoundColliders.Add(colliders[i]);
                }
            }
        }
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            UpdateCircleGizmos();
        }
        protected void UpdateCircleGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tf.position, detectSoundRadius);
        }
    }
}