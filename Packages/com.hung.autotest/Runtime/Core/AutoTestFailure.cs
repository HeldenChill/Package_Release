using System;
using UnityEngine;

namespace Hung.AutoTest
{
    [Serializable]
    public sealed class AutoTestFailure
    {
        public string category;
        public string assertionId;
        public string message;
        public string stackTrace;
        public AutoTestAssertionSeverity severity;
        public float realtime;
        public float elapsed;
        public int frame;

        /// <summary>Game-state snapshot summary captured the moment the failure was recorded — root-cause triage context.</summary>
        public string contextSummary;

        public static AutoTestFailure Create(
            string category,
            string assertionId,
            string message,
            AutoTestAssertionSeverity severity,
            AutoTestContext context,
            string stackTrace = null)
        {
            return new AutoTestFailure
            {
                category = category,
                assertionId = assertionId,
                message = message,
                stackTrace = stackTrace ?? string.Empty,
                severity = severity,
                realtime = Time.realtimeSinceStartup,
                elapsed = context != null ? context.ElapsedSeconds : 0f,
                frame = Time.frameCount
            };
        }
    }
}
