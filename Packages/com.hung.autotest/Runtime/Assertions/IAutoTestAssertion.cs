namespace Hung.AutoTest
{
    public interface IAutoTestAssertion
    {
        string Id { get; }
        AutoTestAssertionSeverity Severity { get; }
        void OnTestStarted(AutoTestContext context);
        AutoTestAssertionResult Evaluate(RuntimeSnapshot snapshot, AutoTestContext context);
    }
}
