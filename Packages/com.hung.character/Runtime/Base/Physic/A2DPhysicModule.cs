using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay.Character.Physic
{   
    public abstract class A2DPhysicModule : Module<PhysicData, PhysicParameter>
    {
        [SerializeField]
        protected Rigidbody2D rb;
        [SerializeField]
        string objectLayerName;
        [SerializeField]
        string ignoreLayerName;
        protected int objectLayerId;
        protected int ignoreLayerId;
        protected float originGravityScale;
        public float OriginaGravitylScale => originGravityScale;
        public override void Initialize(PhysicData Data,PhysicParameter Parameter)
        {
            this.Data = Data;
            this.Parameter = Parameter;
            objectLayerId = LayerMask.NameToLayer(objectLayerName);
            ignoreLayerId = LayerMask.NameToLayer(ignoreLayerName);
        }
        protected abstract void OnInit();  
        public abstract void SetVelocity(Vector2 vel);
        public abstract void SetVelocity(Vector2 vel, float time);
        public abstract void SetVelocity(Vector2 vel, int frame);
        public abstract void SetVelocityX(float velX);
        public abstract void SetVelocityX(float velX, float time);
        public abstract void SetVelocityX(float velX, int frame);
        public abstract void SetVelocityY(float velY);
        public abstract void SetVelocityY(float velY, float time);
        public abstract void SetVelocityY(float velY, int frame);
        public abstract void SetActiveRBStimulate(bool val);
        public abstract void SetGravityScale(float val);
        public abstract void SetOriginalGravityScale();
        public abstract void IgnoreCollision(bool value, float time);
        public abstract void UpdateEvent(int type);
    }
}
