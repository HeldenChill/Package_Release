using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Character.Physic;
using Hung.Utilities.Timer;

namespace _Game.Character
{
    public class Physic2DModule : A2DPhysicModule
    {
        protected override void OnInit()
        {

        }
        public override void IgnoreCollision(bool value, float time)
        {
            
        }

        public override void SetActiveRBStimulate(bool val)
        {
            
        }

        public override void SetGravityScale(float val)
        {
           
        }

        public override void SetOriginalGravityScale()
        {
            
        }

        public override void SetVelocity(Vector2 vel)
        {
            rb.linearVelocity = vel;
        }

        public override void SetVelocity(Vector2 vel, float time)
        {           
            rb.linearVelocity = vel;
            TimerManager.Ins.WaitForTime(time, () => rb.linearVelocity = Vector2.zero);
        }

        public override void SetVelocity(Vector2 vel, int frame)
        {
            rb.linearVelocity = vel;
            TimerManager.Ins.WaitForFrame(frame, () => rb.linearVelocity = Vector2.zero);
        }

        public override void SetVelocityX(float velX)
        {
            rb.linearVelocity = new Vector2 (velX, rb.linearVelocity.y);
        }

        public override void SetVelocityX(float velX, float time)
        {
            rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
            TimerManager.Ins.WaitForTime(time, () => rb.linearVelocity = new Vector2(0, rb.linearVelocity.y));
        }

        public override void SetVelocityX(float velX, int frame)
        {
            rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
            TimerManager.Ins.WaitForFrame(frame, () => rb.linearVelocity = new Vector2(0, rb.linearVelocity.y));
        }

        public override void SetVelocityY(float velY)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velY);
        }

        public override void SetVelocityY(float velY, float time)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velY);
            TimerManager.Ins.WaitForTime(time, () => rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0));
        }

        public override void SetVelocityY(float velY, int frame)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velY);
            TimerManager.Ins.WaitForFrame(frame, () => rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0));
        }

        public override void UpdateData()
        {
            Data.RbVelocity = rb.linearVelocity;
        }

        public override void UpdateEvent(int type)
        {
           
        }
       
    }
}