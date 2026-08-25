using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    [Serializable]
    public class StateMachine
    {
        protected BaseState currentState;
        protected Dictionary<STATE, BaseState> states;
        public bool IsDebug;
        [SerializeField]
        protected STATE currentStateId;
        public STATE CurrentState => currentState?.Id ?? STATE.NONE;
        public Dictionary<STATE, BaseState> States => states;

        public StateMachine()
        {
            states = new Dictionary<STATE, BaseState>();
        }

        public void Start(STATE id)
        {
            currentState = states[id];
            currentState?.Enter();
            currentStateId = id;
            if (IsDebug)
            {
                Debug.Log($"STATE MACHINE - START: {id}");
            }
        }

        public void Stop()
        {
            currentState?.Exit();
            currentState = null;
        }
        public void AddState(STATE id, BaseState state)
        {
            if(!states.ContainsKey(id))
            {
                states.Add(id, state);
                states[id]._OnStateChanged += ChangeState;
                states[id]._OnAddDecorState += AddDecorState;
                states[id]._OnRemoveDecorState += RemoveDecorState;
            }
        }
        public void RemoveState(STATE id)
        {
            if (states.ContainsKey(id))
            {
                states[id]._OnStateChanged -= ChangeState;
                states[id]._OnAddDecorState -= AddDecorState;
                states[id]._OnRemoveDecorState -= RemoveDecorState;
                states.Remove(id);
            }
        }
        public void AddDecorState(STATE id)
        {
            // Reached from BaseState._OnAddDecorState, i.e. from gameplay code rather than
            // setup, so an unregistered id must not throw out of the calling state.
            if (!states.TryGetValue(id, out BaseState state))
            {
                Debug.Log($"STATE MACHINE - CANNOT ADD DECOR: {id}");
                return;
            }
            switch (state.Type)
            {
                case STATE_TYPE.DECORATOR:
                    state.Decorator = currentState;
                    currentState = state;
                    currentState.Enter();
                    break;
            }
        }
        public void RemoveDecorState(STATE id)
        {
            if (!states.TryGetValue(id, out BaseState state))
            {
                Debug.Log($"STATE MACHINE - CANNOT REMOVE DECOR: {id}");
                return;
            }
            switch (state.Type)
            {
                case STATE_TYPE.DECORATOR:
                    if (currentState == null) return;
                    RemoveLinkedNode(currentState);
                    break;
            }

            void RemoveLinkedNode(BaseState checkState)
            {
                if (currentState.Id == id)
                {
                    currentState.Exit();
                    currentState = checkState.Decorator;
                    checkState.Decorator = null;
                    return;
                }
                while (checkState.Decorator != null)
                {
                    if (checkState.Decorator.Id == id)
                    {
                        BaseState link = checkState.Decorator;
                        checkState.Decorator = link.Decorator;
                        link.Exit();
                        link.Decorator = null;
                        break;
                    }
                    checkState = checkState.Decorator;
                }
            }
        }
        public void ChangeState(STATE id)
        {
            if (!states.ContainsKey(id))
            {
                Debug.Log($"STATE MACHINE - CANNOT CHANGE: {CurrentState} -> {id}");
                return;
            }
            if (IsDebug)
            {
                if(currentState == null)
                {
                    Debug.Log($"STATE MACHINE STOP!!");
                }
                else
                {
                    Debug.Log($"STATE MACHINE - CHANGE: {CurrentState} -> {id}");
                }
            }

            BaseState lastDecoratorState = FindFarestDecorState();
            if (lastDecoratorState == null)
            {
                currentState?.Exit();
                currentState = states[id];
                currentState?.Enter();
                currentStateId = id;
            }
            else
            {
                lastDecoratorState.Decorator?.Exit();
                lastDecoratorState.Decorator = states[id];
                lastDecoratorState.Decorator?.Enter();
            }

            BaseState FindFarestDecorState()
            {
                BaseState checkState = currentState;
                while (checkState != null)
                {
                    if (checkState.Decorator == null) return null;
                    if (checkState.Decorator.Type == STATE_TYPE.NORMAL)
                    {
                        return checkState;
                    }
                    checkState = checkState.Decorator;
                }
                return null;
            }
        }
        public void Update()
        {
            currentState?.Update();
        }
        public void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }
    }
}