using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay.Character.Logic
{
    using System;
    using Hung.DesignPattern;

    public class LogicEvent
    {
        /// <summary>
        /// Set <c>Velocity_x</c> for character in a period of time.
        /// </summary>
        public event Action<float> _SetVelocityX;
        public event Action<float, float> _SetVelocityXTime;
        public event Action<float, int> _SetVelocityXFrame;
        /// <summary>
        /// Set <c>Velocity_y</c> for character in a period of time.
        /// </summary>
        public event Action<float> _SetVelocityY;
        public event Action<float, float> _SetVelocityYTime;
        public event Action<float, int> _SetVelocityYFrame;
        /// <summary>
        /// Set <c>Velocity</c> for character in a period of time.
        /// </summary>
        public event Action<Vector3> _SetVelocity;
        public event Action<Vector3> _SetLocalVelocityXZ;
        public event Action<Vector3, ForceMode> _AddForce;
        public event Action<Vector2, float> _SetVelocityTime;
        public event Action<Vector2, int> _SetVelocityFrame;
        /// <summary>
        /// Set <c>GravityScale = 0</c> in character RigidBody.
        /// </summary>
        public event Action _DisableGravity;
        /// <summary>
        /// Set <c>GravityScale = originalGravityScale</c> in character RigidBody.
        /// </summary>
        public event Action _EnableGravity;

        //AnimModule.ActivateAfterImageEffect(0.02f, dashTime);
        public event Action<Type, float, float> _UseAfterEffect;
        public event Action<Type> _ShowAnimation;
        public event Action<Type> _HideAnimation;

        public event Action<Type, string> _PlayAnimation; //AnimModule.Activate(string)
        public event Action<Type, string> _ExitAnimation; //AnimModule.Deactivate(string)
        public event Action<Type, string, float> _SetAnimFloat; //AnimModule.UpdateFloatParameter(float)
        public event Action<Type, string> _SetAnimTrigger;//player.AnimModule.Trigger(nameTrigger);
        public event Action<Type, string, bool> _SetAnimBool;


        public event Action<bool, float> _IgnoreCollision;

        /// <summary>
        /// Set <c>Rotation</c> of character.
        /// </summary>
        public event Action<Quaternion> _SetSkinRotation;
        public event Action<Quaternion> _SetSkinLocalRotation;
        public event Action<float> _UpdateSensitivity;
        public event Action<STATE> _UpdateCameraState;
        public event Action _OnDie;
        public event Action<Vector3, float> _SetColliderSize;
        public event Action<Vector3, float> _SetColliderOffset;
        public event Action<Vector3> _SetLocalHeadPosition;
        public event Action<Vector3, float, float> _PlaySound;
        public event Action<Vector3> _LookTowardPosition;

        public void SetVelocityX(float speed)
        {
            _SetVelocityX?.Invoke(speed);
        }
        public void SetVelocityX(float speed, float time = -1f)
        {
            WarningInformation(_SetVelocityXTime, "SetVelocityX");
            _SetVelocityXTime?.Invoke(speed, time);
        }
        public void SetVelocityX(float speed, int frame)
        {
            WarningInformation(_SetVelocityXTime, "SetVelocityXFrame");
            _SetVelocityXFrame?.Invoke(speed, frame);
        }
        public void SetVelocityY(float speed)
        {
            _SetVelocityY?.Invoke(speed);
        }
        public void SetVelocityY(float speed, float time = -1f)
        {
            WarningInformation(_SetVelocityYTime, "SetVelocityY");
            _SetVelocityYTime?.Invoke(speed, time);
        }
        public void SetVelocityY(float speed, int frame)
        {
            WarningInformation(_SetVelocityYTime, "SetVelocityYFrame");
            _SetVelocityYFrame?.Invoke(speed, frame);
        }
        public void SetVelocity(Vector3 speed, float time = -1f)
        {
            _SetVelocityTime?.Invoke(speed, time);
        }
        public void SetVelocity(Vector3 speed, int frame)
        {
            _SetVelocityFrame?.Invoke(speed, frame);
        }
        #region Velocity write clock guard (Fix C - camera jitter)
        // rb.linearVelocity must only be assigned on the physics clock. State Enter() methods
        // are reached from the render-rate Update() loop, so a direct write there clobbers the
        // velocity the pending FixedUpdate step was about to integrate - which is jitter.
        //
        // Rather than make every state carry its own deferral flag, the two velocity setters
        // buffer a render-clock write here and FlushPendingVelocity() applies it at the start
        // of the next FixedUpdate. Writes already on the physics clock pass straight through.
        //
        // Last write wins inside a frame: a state that enters and exits within one render frame
        // should not produce two physics impulses.
        // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
        bool hasPendingVelocity;
        Vector3 pendingVelocity;
        bool hasPendingLocalVelocityXZ;
        Vector3 pendingLocalVelocityXZ;

        public void SetVelocity(Vector3 velocity)
        {
            if (!Time.inFixedTimeStep)
            {
                Hung.Base.MotionProbe.VelocityDeferred(hasPendingVelocity);
                pendingVelocity = velocity;
                hasPendingVelocity = true;
                return;
            }
            _SetVelocity?.Invoke(velocity);
        }
        public void SetLocalVelocityXZ(Vector3 velocity)
        {
            if (!Time.inFixedTimeStep)
            {
                Hung.Base.MotionProbe.VelocityDeferred(hasPendingLocalVelocityXZ);
                pendingLocalVelocityXZ = velocity;
                hasPendingLocalVelocityXZ = true;
                return;
            }
            _SetLocalVelocityXZ?.Invoke(velocity);
        }

        /// <summary>
        /// Apply any velocity buffered from the render clock. Called once per physics step by
        /// the owning Character before the state machine's FixedUpdate runs, so a state's own
        /// FixedUpdate write still takes precedence over a stale buffered one.
        /// </summary>
        public void FlushPendingVelocity()
        {
            if (hasPendingVelocity)
            {
                hasPendingVelocity = false;
                _SetVelocity?.Invoke(pendingVelocity);
            }
            if (hasPendingLocalVelocityXZ)
            {
                hasPendingLocalVelocityXZ = false;
                _SetLocalVelocityXZ?.Invoke(pendingLocalVelocityXZ);
            }
        }
        #endregion
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            _AddForce?.Invoke(force, mode);
        }

        public void DisableGravity()
        {
            _DisableGravity?.Invoke();
        }
        public void EnableGravity()
        {
            _EnableGravity?.Invoke();
        }
        public void UseAfterEffect(Type type, float timeBetweenImage, float time)
        {
            _UseAfterEffect?.Invoke(type, timeBetweenImage, time);
        }
        public void ShowAnimation(Type type)
        {
            _ShowAnimation?.Invoke(type);
        }
        public void HideAnimation(Type type)
        {
            _HideAnimation?.Invoke(type);
        }

        public void PlayAnimation(Type type, string name)
        {
            _PlayAnimation?.Invoke(type, name);
        }
        public void ExitAnimation(Type type, string name)
        {
            _ExitAnimation?.Invoke(type, name);
        }

        public void SetAnimFloat(Type type, string name, float value)
        {
            _SetAnimFloat?.Invoke(type, name, value);
        }

        public void SetAnimTrigger(Type type, string name)
        {
            _SetAnimTrigger?.Invoke(type, name);
        }
        public void SetAnimBool(Type type, string name, bool value)
        {
            _SetAnimBool?.Invoke(type, name, value);
        }
        public void UpdateCameraState(STATE state)
        {
            _UpdateCameraState?.Invoke(state);
        }

        public void OnDie()
        {
            _OnDie?.Invoke();
        }
        public void IgnoreCollision(bool value, float time = -1f)
        {
            _IgnoreCollision?.Invoke(value, time);
        }
        public void SetSkinRotation(Quaternion rotation)
        {
            _SetSkinRotation?.Invoke(rotation);
        }
        public void SetSkinLocalRotation(Quaternion rotation)
        {
            _SetSkinLocalRotation?.Invoke(rotation);
        }
        public void UpdateSensitivity(float input)
        {
            _UpdateSensitivity?.Invoke(input);
        }
        public void SetColliderSize(Vector3 size, float time = -1)
        {
            _SetColliderSize?.Invoke(size, time);
        }
        public void SetColliderOffset(Vector3 offset, float time = -1)
        {
            _SetColliderOffset?.Invoke(offset, time);
        }
        public void SetHeadLocalPosition(Vector3 localPosition)
        {
            _SetLocalHeadPosition?.Invoke(localPosition);
        }
        public void PlaySound(Vector3 position, float radius, float time)
        {
            _PlaySound?.Invoke(position, radius, time);
        }
        public void LookTowardPosition(Vector3 position)
        {
            _LookTowardPosition?.Invoke(position);
        }
        private void WarningInformation(Delegate action, string name)
        {
            if (action == null)
            {
                Debug.LogWarning(name + " not implement" + "DynamicLogicSystem");
            }
        }
    }
}