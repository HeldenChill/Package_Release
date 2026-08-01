namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Internal wrapper around <see cref="EnergyResultOutcome"/> used by mutation delegates
    /// passed to <c>EnergyService.CommitCandidate</c>. Adds <see cref="ShouldPersist"/> so a
    /// mutation can report "nothing to do" (e.g. an idle <c>Reconcile()</c>) without the helper
    /// hitting the store or firing <c>Changed</c>. Not part of the public IEnergyService surface.
    /// </summary>
    internal readonly struct EnergyCommandOutcome
    {
        public EnergyResultOutcome Outcome { get; }
        public bool ShouldPersist { get; }

        public EnergyCommandOutcome(EnergyResultOutcome outcome, bool shouldPersist)
        {
            Outcome = outcome;
            ShouldPersist = shouldPersist;
        }

        public static EnergyCommandOutcome NoChange(EnergyResultOutcome outcome) =>
            new EnergyCommandOutcome(outcome, false);

        public static EnergyCommandOutcome Persist(EnergyResultOutcome outcome) =>
            new EnergyCommandOutcome(outcome, true);

        public static EnergyCommandOutcome PersistenceFailure =>
            new EnergyCommandOutcome(EnergyResultOutcome.PersistenceFailure, false);
    }
}
