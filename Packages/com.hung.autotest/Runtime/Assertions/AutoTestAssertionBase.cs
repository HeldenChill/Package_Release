namespace Hung.AutoTest
{
    public abstract class AutoTestAssertionBase : IAutoTestAssertion
    {
        protected readonly AutoTestAssertionConfig config;

        public abstract string Id { get; }

        public AutoTestAssertionSeverity Severity
        {
            get { return config != null ? config.severity : AutoTestAssertionSeverity.Failure; }
        }

        protected AutoTestAssertionBase(AutoTestAssertionConfig config)
        {
            this.config = config;
        }

        public virtual void OnTestStarted(AutoTestContext context) { }

        public abstract AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context);
    }
}
