namespace Hung.AutoTest
{
    public sealed class ScenarioStartedAssertion : AutoTestAssertionBase
    {
        public override string Id { get { return "ScenarioStartedAssertion"; } }

        public ScenarioStartedAssertion(AutoTestAssertionConfig config) : base(config) { }

        public override AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context)
        {
            if (snapshot == null || !snapshot.combat.scenarioManagerFound)
                return AutoTestAssertionResult.Failed(Id, "TestScenarioModeManager was not found.");

            if (context == null || context.CurrentCase == null || context.CurrentCase.scenario == null)
                return AutoTestAssertionResult.Failed(Id, "Current AutoTest case has no TestScenarioData.");

            if (context.ElapsedSeconds > config.timeoutSeconds && !snapshot.combat.scenarioAssigned)
                return AutoTestAssertionResult.Failed(Id, "Scenario did not start within timeout.");

            return AutoTestAssertionResult.Passed(Id);
        }
    }
}
