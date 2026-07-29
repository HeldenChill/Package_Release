namespace Hung.AutoTest
{
    /// <summary>
    /// Builds a RuntimeSnapshot from live game state. Game glue provides the
    /// implementation via AutoTestRunner.SnapshotBuilderFactory so the core
    /// runner never names game types.
    /// </summary>
    public interface IRuntimeSnapshotBuilder
    {
        RuntimeSnapshot Build(AutoTestContext context);
    }
}
