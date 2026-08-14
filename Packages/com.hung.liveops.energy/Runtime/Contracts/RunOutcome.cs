namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// The final result of a completed run, as reported by the caller to <see cref="IEnergyService.CompleteRun"/>.
    /// </summary>
    public enum RunOutcome
    {
        Win,
        Loss,
        Abandoned
    }
}
