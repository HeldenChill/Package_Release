using UnityEngine;

namespace Hung.AutoTest
{
    public sealed class NoExceptionLogAssertion : AutoTestAssertionBase
    {
        public override string Id { get { return "NoExceptionLogAssertion"; } }

        public NoExceptionLogAssertion(AutoTestAssertionConfig config) : base(config) { }

        public override AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context)
        {
            if (context == null || context.Logs == null)
                return AutoTestAssertionResult.Passed(Id);

            for (int i = 0; i < context.Logs.Count; i++)
            {
                AutoTestLogEntry entry = context.Logs[i];
                if (entry.type == LogType.Exception || entry.type == LogType.Error || entry.type == LogType.Assert)
                    return AutoTestAssertionResult.Failed(Id, entry.type + ": " + entry.condition);
            }

            return AutoTestAssertionResult.Passed(Id);
        }
    }
}
