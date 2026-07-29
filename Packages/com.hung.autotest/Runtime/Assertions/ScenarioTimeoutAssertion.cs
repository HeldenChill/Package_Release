namespace Hung.AutoTest
{
    public sealed class ScenarioTimeoutAssertion : AutoTestAssertionBase
    {
        public override string Id { get { return "ScenarioTimeoutAssertion"; } }

        public ScenarioTimeoutAssertion(AutoTestAssertionConfig config) : base(config) { }

        public override AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context)
        {
            if (context == null || context.CurrentCase == null)
                return AutoTestAssertionResult.Passed(Id);

            if (context.ElapsedSeconds > context.CurrentCase.timeoutSeconds + 0.25f)
                return AutoTestAssertionResult.Failed(Id, "Scenario exceeded timeout: " + context.CurrentCase.timeoutSeconds + " seconds.");

            return AutoTestAssertionResult.Passed(Id);
        }
    }
}
