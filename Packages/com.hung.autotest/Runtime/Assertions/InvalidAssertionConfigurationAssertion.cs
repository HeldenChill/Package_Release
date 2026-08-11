namespace Hung.AutoTest
{
    public sealed class InvalidAssertionConfigurationAssertion : IAutoTestAssertion
    {
        readonly AutoTestAssertionConfig config;
        readonly string diagnostic;

        public InvalidAssertionConfigurationAssertion(AutoTestAssertionConfig config, string diagnostic)
        {
            this.config = config;
            this.diagnostic = diagnostic;
        }

        public string Id => string.IsNullOrWhiteSpace(config?.assertionId)
            ? "autotest.assertion.invalid" : config.assertionId.Trim();

        public AutoTestAssertionSeverity Severity => config?.severity ?? AutoTestAssertionSeverity.Failure;

        public void OnTestStarted(AutoTestContext context) { }

        public AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context)
            => AutoTestAssertionResult.Failed(Id, diagnostic);
    }
}
