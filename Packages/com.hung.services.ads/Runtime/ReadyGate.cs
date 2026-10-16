using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Ads
{
    /// <summary>
    /// Runs an action now when ready, otherwise keeps it until <see cref="Release"/>. Vendor adapters use it to hold
    /// loads until their SDK has initialized, because the game initializes the SDK only after consent.
    /// </summary>
    public sealed class ReadyGate
    {
        readonly Func<bool> _isReady;
        readonly List<Action> _waiting = new List<Action>();

        /// <summary>Constructs a gate whose predicate must include every required readiness condition.</summary>
        public ReadyGate(Func<bool> isReady)
        {
            _isReady = isReady ?? throw new ArgumentNullException(nameof(isReady));
        }

        /// <summary>Actions kept for <see cref="Release"/>.</summary>
        public int Waiting => _waiting.Count;

        /// <summary>Runs <paramref name="action"/> now when ready, otherwise keeps it.</summary>
        public void Run(Action action)
        {
            if (action == null) return;
            if (_isReady()) { action(); return; }
            _waiting.Add(action);
        }

        /// <summary>Runs every kept action once, in order. One that throws is logged; the rest still run.</summary>
        public void Release()
        {
            if (!_isReady()) return;
            var actions = _waiting.ToArray();
            _waiting.Clear();
            foreach (var action in actions)
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
