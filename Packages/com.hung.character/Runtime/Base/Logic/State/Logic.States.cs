using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Gameplay.Character
{
    using System;
    using Gameplay.Character.Logic;
    using Hung.DesignPattern;
    #region BASE STATE

    #region GROUNDED STATE
    public abstract class GroundedState<D, P, E> : BaseLogicState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        protected GroundedState(D data, P parameter, E _event)
            : base(data, parameter, _event)
        {
        }

        public override void Enter()
        {
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_GROUNDED_ANIM_NAME, true);
        }
        public override bool Update()
        {
            if (!base.Update()) return false;
            if (!Parameter.WIData.IsGrounded)
            {
                ChangeState(STATE.IN_AIR);
                return false;
            }
            else
            {
                if (Parameter.NavData.Jump.Value)
                {
                    ChangeState(STATE.JUMP);
                    return false;
                }
            }
            return true;
        }
        public override void Exit()
        {
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_GROUNDED_ANIM_NAME, false);
        }
    }
    public abstract class IdleState<D, P, E> : GroundedState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        public override STATE Id => STATE.IDLE;

        public IdleState(D data, P parameter, E _event)
            : base(data, parameter, _event) { }
        public override void Enter()
        {
            base.Enter();
            // Fix C (camera jitter): this call arrives on the render clock when Enter() is
            // reached from Update(). LogicEvent.SetVelocity buffers it until the next
            // FixedUpdate, so no state needs its own deferral flag.
            // See Assets/_Game/_Docs/player-camera-jitter-diagnosis.md
            Event.SetVelocity(Vector3.zero);
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_IDLE_ANIM_NAME, true);
        }
        public override bool Update()
        {
            if (!base.Update()) return false;
            if (Parameter.NavData.MoveDirection.sqrMagnitude > 0.0001f)
            {
                ChangeState(STATE.WALK);
                return true;
            }
            return true;
        }
        public override void Exit()
        {
            base.Exit();
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_IDLE_ANIM_NAME, false);
        }


    }
    public abstract class MoveState<D, P, E> : GroundedState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        protected readonly Vector3 NORMAL_PLANE_2D = -Vector3.forward;
        public MoveState(D data, P parameter, E _event)
            : base(data, parameter, _event)
        {
        }

        public override STATE Id => STATE.WALK;

        public override void Enter()
        {
            base.Enter();
            Event.SetVelocity(Parameter.NavData.MoveDirection * Stats<CharacterStats>().Speed.Value);
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_RUN_ANIM_NAME, true);
        }

        public override void Exit()
        {
            base.Exit();
            Event.SetAnimBool(typeof(IdleState<D, P, E>), CONSTANTS.IS_RUN_ANIM_NAME, false);
        }

        public override bool Update()
        {
            if (!base.Update()) return false;
            if (Parameter.NavData.MoveDirection.sqrMagnitude < 0.0001f)
            {
                ChangeState(STATE.IDLE);
            }
            return true;
        }

        public override bool FixedUpdate()
        {
            if (Parameter.NavData.MoveDirection.sqrMagnitude < 0.0001f)
                return false;
            Vector3 inputMoveDirection = new Vector3(Parameter.NavData.MoveDirection.x, 0, Parameter.NavData.MoveDirection.z).normalized;
            //Vector3 moveDirection = Parameter.PhysicData.PerceptionData.Tf
            //    .TransformDirection(inputMoveDirection);
            // UpdateSkinRotation(inputMoveDirection);
            Event.SetVelocity(inputMoveDirection * Stats<CharacterStats>().Speed.Value);
            return base.FixedUpdate();
        }
    }
    #endregion
    public abstract class JumpState<D, P, E> : BaseLogicState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        bool isJumping = false;
        public JumpState(D data, P parameter, E _event)
            : base(data, parameter, _event)
        {
        }

        public override STATE Id => STATE.JUMP;

        public override void Enter()
        {
            Event.SetVelocity(Parameter.PerceptionData.Tf.up * Stats<CharacterStats>().JumpSpeed.Value);
            Event.SetAnimTrigger(typeof(IdleState<D, P, E>), CONSTANTS.JUMP_ANIM_NAME);
            isJumping = false;
        }

        public override void Exit()
        {

        }

        public override bool Update()
        {
            if (!base.Update()) return false;
            isJumping = !Parameter.WIData.IsGrounded || isJumping;
            if (!isJumping) return false;
            ChangeState(STATE.IN_AIR);
            return true;
        }
    }
    public class DieState<D, P, E> : BaseLogicState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        public DieState(D data, P parameter, E _event) : base(data, parameter, _event) { }

        public override STATE Id => STATE.DIE;

        public override void Enter()
        {
            Event.SetVelocityX(0);
            Event.OnDie();
        }

        public override void Exit()
        {

        }

        public override bool Update()
        {
            return true;
        }
    }
    public abstract class AirState<D, P, E> : BaseLogicState<D, P, E>
        where D : LogicData
        where P : LogicParameter
        where E : LogicEvent
    {
        public AirState(D data, P parameter, E _event) : base(data, parameter, _event) { }

        public override STATE Id => STATE.IN_AIR;

        public override void Enter()
        {
        }

        public override void Exit()
        {
        }

        public override bool Update()
        {
            if (!base.Update()) return false;
            if (Parameter.WIData.IsGrounded)
            {
                if (Parameter.NavData.MoveDirection.sqrMagnitude > 0.0001f)
                {
                    ChangeState(STATE.WALK);
                }
                else
                {
                    ChangeState(STATE.IDLE);
                }
            }
            return true;
        }

        public override bool FixedUpdate()
        {
            if (Parameter.NavData.MoveDirection.sqrMagnitude < 0.0001f)
                return false;
            Vector3 inputMoveDirection = new Vector3(Parameter.NavData.MoveDirection.x, 0, Parameter.NavData.MoveDirection.z).normalized;
            //Vector3 moveDirection = Parameter.PhysicData.PerceptionData.Tf
            //   .TransformDirection(inputMoveDirection);
            // UpdateSkinRotation(inputMoveDirection);
            Event.SetLocalVelocityXZ(inputMoveDirection * Stats<CharacterStats>().Speed.Value);
            return base.FixedUpdate();
        }
    }
    #endregion
    #region PLAYER STATE
    [Serializable]
    public class PlayerIdleState : IdleState<LogicData, LogicParameter, LogicEvent>
    {
        public PlayerIdleState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class PlayerMoveState : MoveState<LogicData, LogicParameter, LogicEvent>
    {
        public PlayerMoveState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class PlayerJumpState : JumpState<LogicData, LogicParameter, LogicEvent>
    {
        public PlayerJumpState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class PlayerAirState : AirState<LogicData, LogicParameter, LogicEvent>
    {
        public PlayerAirState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event) { }
    }
    [Serializable]
    public class PlayerDieState : DieState<LogicData, LogicParameter, LogicEvent>
    {
        public PlayerDieState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event) { }
    }
    #endregion
    #region ENEMY STATE
    [Serializable]
    public class EnemyIdleState : IdleState<LogicData, LogicParameter, LogicEvent>
    {
        // EnemyNavigationData NavData;
        // ALERT_STATE currentAlertState;
        public EnemyIdleState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {

        }

        public override STATE Id => STATE.IDLE;

        public override void Enter()
        {
            base.Enter();
            // currentAlertState = ALERT_STATE.NONE;
            // NavData = Parameter.NavData as EnemyNavigationData;
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override bool Update()
        {
            if (!base.Update()) return false;
            return true;
        }

    }
    [Serializable]
    public class EnemyMoveState : MoveState<LogicData, LogicParameter, LogicEvent>
    {
        public EnemyMoveState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class EnemyJumpState : JumpState<LogicData, LogicParameter, LogicEvent>
    {
        public EnemyJumpState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class EnemyDieState : DieState<LogicData, LogicParameter, LogicEvent>
    {
        public EnemyDieState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event)
        {
        }
    }
    [Serializable]
    public class EnemyAirState : AirState<LogicData, LogicParameter, LogicEvent>
    {
        public EnemyAirState(LogicData data, LogicParameter parameter, LogicEvent _event) : base(data, parameter, _event) { }
    }
    #endregion
}