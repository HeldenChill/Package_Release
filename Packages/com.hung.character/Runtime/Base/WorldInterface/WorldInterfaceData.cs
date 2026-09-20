using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.WorldInterface
{
    using Gameplay.Character;
    public abstract class SensorData { }
    public class WorldInterfaceData : Data
    {
        //Can improve performance by check value change or not
        public bool IsGrounded;
        public bool IsTouchingWall;
        public bool IsTouchingCorner;
        public bool IsTouchingVaultCorner;
        public RaycastHit2D TouchingWall2DPoint;
        public RaycastHit2D TouchingGround2DPoint;
        public RaycastHit TouchingWallPoint;
        public RaycastHit TouchingGroundPoint;
        public Vector3 Normal;

        public IReadOnlyList<RaycastHit2D> WallHit2D;
        public IReadOnlyList<RaycastHit2D> CharacterHit2D;
        public IReadOnlyList<RaycastHit> WallHit3D;
        public IReadOnlyList<RaycastHit> CharacterHit3D;
        public List<Collider> HostileColliders;
        public List<Collider> SeenColliders;
        public List<Collider> SoundColliders;
        protected List<SensorData> sensorDatas;
        public List<SensorData> SensorDatas => sensorDatas ??= new List<SensorData>();
        public T GetSensorData<T>() where T : SensorData
        {
            return SensorDatas.Find(x => x is T) as T;
        }
    }
}