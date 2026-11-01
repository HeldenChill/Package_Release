using System;
namespace Hung.AI
{
    /// <summary>Result of ticking a node. Aborted is only ever recorded as a last status; OnUpdate never returns it.</summary>
    public enum NodeStatus
    {
        None = 0,
        Running = 1,
        Success = 2,
        Failure = 3,
        Aborted = 4
    }

    /// <summary>Which running branches a condition may interrupt when its value changes.</summary>
    [Flags]
    public enum AbortType
    {
        None = 0,
        Self = 1,
        LowerPriority = 2,
        Both = Self | LowerPriority
    }
}
