using System.Collections;

namespace Hung.AutoTest
{
    public interface IAutoTestScenarioExecutor
    {
        /// <summary>Clear per-run cached state (called before each case).</summary>
        void ResetRuntimeState();
        IEnumerator Prepare(AutoTestCaseData testCase, AutoTestContext context);
        IEnumerator Run(AutoTestCaseData testCase, AutoTestContext context);
        IEnumerator Cleanup(AutoTestCaseData testCase, AutoTestContext context);
    }
}
