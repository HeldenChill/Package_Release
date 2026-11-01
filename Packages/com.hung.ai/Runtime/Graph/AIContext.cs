using System;
using System.Collections.Generic;
using UnityEngine;
namespace Hung.AI
{
    /// <summary>Per-agent state shared by every node during a tick.</summary>
    public sealed class AIContext
    {
        readonly HashSet<string> logged = new HashSet<string>();

        public AIContext(Blackboard blackboard, IAIMover mover = null, GameObject owner = null, int seed = 0)
        {
            Blackboard = blackboard;
            Mover = mover;
            Owner = owner;
            Random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public Blackboard Blackboard { get; }

        public IAIMover Mover { get; set; }

        public GameObject Owner { get; }

        /// <summary>Seconds of AI time since the runner started (sum of tick deltas).</summary>
        public float Time { get; internal set; }

        public float DeltaTime { get; internal set; }

        public System.Random Random { get; }

        /// <summary>Raised for every node tick and abort. Used by the debugger.</summary>
        public event Action<AINode, NodeStatus> NodeTicked;

        internal void NotifyTicked(AINode node, NodeStatus status) => NodeTicked?.Invoke(node, status);

        /// <summary>Logs a warning only the first time a token is seen for this agent.</summary>
        public void LogOnce(string token, string message)
        {
            if (logged.Add(token)) Debug.LogWarning(message, Owner);
        }
    }
}
