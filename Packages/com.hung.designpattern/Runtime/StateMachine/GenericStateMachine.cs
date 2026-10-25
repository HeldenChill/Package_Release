using System.Collections.Generic;

namespace Hung.DesignPattern
{
    /// <summary>Explicit validation outcomes; callback exceptions propagate.</summary>
    public enum StateStatus
    {
        /// <summary>Transition or registration succeeded.</summary>
        Success,
        /// <summary>State key already exists.</summary>
        DuplicateKey,
        /// <summary>State key was not found.</summary>
        MissingKey,
        /// <summary>State instance is null.</summary>
        NullState,
        /// <summary>State key is invalid or null.</summary>
        InvalidKey,
        /// <summary>State machine is not started.</summary>
        NotStarted,
        /// <summary>State machine is currently executing an exit lifecycle transition.</summary>
        Busy
    }

    /// <summary>Minimal product-key machine with explicit replacement and same-key re-entry.</summary>
    public sealed class StateMachine<TKey>
    {
        private readonly Dictionary<TKey, IState<TKey>> states = new Dictionary<TKey, IState<TKey>>();
        private TKey currentKey;
        private bool exiting;

        /// <summary>Whether a behavior is currently started.</summary>
        public bool IsStarted { get; private set; }

        /// <summary>The active instance, which may differ from a replaced registration.</summary>
        public IState<TKey> CurrentState { get; private set; }

        /// <summary>Add a new key; duplicates never replace.</summary>
        public StateStatus Register(TKey key, IState<TKey> state)
        {
            if (key is null) return StateStatus.InvalidKey;
            if (state == null) return StateStatus.NullState;
            if (states.ContainsKey(key)) return StateStatus.DuplicateKey;
            states.Add(key, state);
            return StateStatus.Success;
        }

        /// <summary>Replace only a registration; the active instance stays until a transition.</summary>
        public StateStatus Replace(TKey key, IState<TKey> state)
        {
            if (key is null) return StateStatus.InvalidKey;
            if (state == null) return StateStatus.NullState;
            if (!states.ContainsKey(key)) return StateStatus.MissingKey;
            states[key] = state;
            return StateStatus.Success;
        }

        /// <summary>Start once; repeated running starts ignore their supplied key.</summary>
        public StateStatus Start(TKey key)
        {
            if (exiting) return StateStatus.Busy;
            if (IsStarted) return StateStatus.Success;
            if (key is null) return StateStatus.InvalidKey;
            if (!states.TryGetValue(key, out var next)) return StateStatus.MissingKey;
            IsStarted = true;
            currentKey = key;
            CurrentState = next;
            next.Enter();
            return StateStatus.Success;
        }

        /// <summary>Exit once; repeated stopped stops succeed without callbacks.</summary>
        public StateStatus Stop()
        {
            if (exiting) return StateStatus.Busy;
            if (!IsStarted) return StateStatus.Success;
            exiting = true;
            try
            {
                CurrentState.Exit();
            }
            finally
            {
                exiting = false;
                CurrentState = null;
                IsStarted = false;
                currentKey = default;
            }
            return StateStatus.Success;
        }

        /// <summary>Transition with Exit then Enter, including same-key transitions.</summary>
        public StateStatus ChangeState(TKey key)
        {
            if (exiting) return StateStatus.Busy;
            if (key is null) return StateStatus.InvalidKey;
            if (!states.TryGetValue(key, out var next)) return StateStatus.MissingKey;
            if (!IsStarted) return StateStatus.NotStarted;
            exiting = true;
            try
            {
                CurrentState.Exit();
            }
            finally
            {
                exiting = false;
            }
            currentKey = key;
            CurrentState = next;
            next.Enter();
            return StateStatus.Success;
        }

        /// <summary>Get the active key without reserving a sentinel value in a product key type.</summary>
        public bool TryGetCurrentKey(out TKey key)
        {
            key = currentKey;
            return IsStarted;
        }

        /// <summary>Drive active behavior only.</summary>
        public void Update()
        {
            if (IsStarted) CurrentState.Update();
        }

        /// <summary>Drive active behavior only.</summary>
        public void FixedUpdate()
        {
            if (IsStarted) CurrentState.FixedUpdate();
        }
    }
}
