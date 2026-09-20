using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.WorldInterface
{
    public enum SensorType
    {
        Detect = 0,
        Vision = 1
    }
    public abstract class BaseSensor : MonoBehaviour
    {
        protected enum DATA_TYPE
        {
            NONE = -1,
            PLAYER = 0,
            CHARACTER = 1,
            GROUND = 2,
            UNKNOWN = 100,
        }
        protected WorldInterfaceData Data;
        protected WorldInterfaceParameter Parameter;
        [SerializeField]
        protected LayerMask layer;
        [SerializeField]
        protected Transform tf;
        public virtual void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            this.Parameter = Parameter;
            this.Data = Data;
        }
        public abstract void UpdateState();
        protected virtual void UpdateData() { }
        public virtual void FixedUpdate() { }
        protected virtual void OnDrawGizmos() { }
    }
}