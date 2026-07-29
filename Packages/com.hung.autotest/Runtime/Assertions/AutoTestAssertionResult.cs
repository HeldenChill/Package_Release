using System;

namespace Hung.AutoTest
{
    public enum AutoTestAssertionStatus
    {
        Passed,
        Failed,
        Skipped,
        Inconclusive
    }

    [Serializable]
    public sealed class AutoTestAssertionResult
    {
        public string assertionId;
        public AutoTestAssertionStatus status;
        public string message;

        public static AutoTestAssertionResult Passed(string id)
        {
            return new AutoTestAssertionResult
            {
                assertionId = id,
                status = AutoTestAssertionStatus.Passed,
                message = string.Empty
            };
        }

        public static AutoTestAssertionResult Failed(string id, string message)
        {
            return new AutoTestAssertionResult
            {
                assertionId = id,
                status = AutoTestAssertionStatus.Failed,
                message = message
            };
        }
    }
}
