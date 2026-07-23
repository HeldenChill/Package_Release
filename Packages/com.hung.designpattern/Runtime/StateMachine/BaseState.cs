using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.DesignPattern
{

    public enum STATE
    {
        NONE = -1,
        APPEAR = 0,
        IDLE = 1,
        MOVE = 2,
        SNAP = 3,
        ATTACK = 4,
        DISABLE = 5,
        DIE = 6,
        QUESTION = 7,
        OVERLAP = 8,
        STUN = 9,
        HIT = 10,
        WALK = 11,
        JUMP = 12,
        IN_AIR = 13,
        USING_SKILL = 14,
        RUN = 15,
        CROUCH = 16,
        VAULT = 17,
        TIRED = 18,
        CROWED_CONTROL = 100,
        KNOCK_BACK = 101,
        SLOW = 102,
        CHARM = 103,
        FREEZE = 105,
        NORMAL_ATTACK = 106,
        TELEPORTING = 107,
    }
    public enum STATE_TYPE
    {
        NONE = -1,
        NORMAL = 0,
        DECORATOR = 1,
    }
    [Serializable]
    public abstract class BaseState
    {
        public event Action<STATE> _OnStateChanged;
        public event Action<STATE> _OnAddDecorState;
        public event Action<STATE> _OnRemoveDecorState;
        public virtual BaseState Decorator { get; set; }
        public abstract STATE Id { get; }
        public virtual STATE_TYPE Type => STATE_TYPE.NORMAL;
        public abstract void Enter();
        public abstract bool Update();
        public virtual bool FixedUpdate() { return true; }
        public abstract void Exit();
        protected void ChangeState(STATE newState)
        {
            _OnStateChanged?.Invoke(newState);
        }
        protected void AddDecorState(STATE addState)
        {
            _OnAddDecorState?.Invoke(addState);
        }
        protected void RemoveDecorState(STATE removeState)
        {
            _OnRemoveDecorState?.Invoke(removeState);
        }
    }
}
