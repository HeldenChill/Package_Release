using System.Collections.Generic;

namespace Hung.Sync.Simulator
{
    /// <summary>Fault behaviors a script can force on a given attempt.</summary>
    public enum SyncFaultKind
    {
        /// <summary>Process normally.</summary>
        None = 0,

        /// <summary>Fail as if no network path exists.</summary>
        NetworkUnavailable = 1,

        /// <summary>Fail as if the request timed out.</summary>
        Timeout = 2,

        /// <summary>Fail as if the auth token expired.</summary>
        AuthExpired = 3,

        /// <summary>Fail as if the response were malformed.</summary>
        ProtocolError = 4,

        /// <summary>Return a revision conflict regardless of the real revision.</summary>
        ForceConflict = 5,

        /// <summary>Reject on a business rule.</summary>
        RejectBusinessRule = 6
    }

    /// <summary>
    /// Declarative, attempt-indexed fault plan. Behavior is keyed by attempt number rather than by
    /// wall-clock timing, so a scripted scenario reproduces exactly on every run.
    /// </summary>
    public sealed class SyncFaultScript
    {
        private readonly Dictionary<int, SyncFaultKind> byAttempt = new Dictionary<int, SyncFaultKind>();

        /// <summary>Schedules a fault for a one-based attempt index.</summary>
        public void At(int attemptIndex, SyncFaultKind kind) => byAttempt[attemptIndex] = kind;

        /// <summary>Returns the fault scheduled for an attempt, or <see cref="SyncFaultKind.None"/>.</summary>
        public SyncFaultKind ForAttempt(int attemptIndex)
            => byAttempt.TryGetValue(attemptIndex, out SyncFaultKind kind) ? kind : SyncFaultKind.None;

        /// <summary>Removes every scheduled fault.</summary>
        public void Clear() => byAttempt.Clear();
    }
}
