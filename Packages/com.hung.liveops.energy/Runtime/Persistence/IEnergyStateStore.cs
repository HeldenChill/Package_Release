namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Load outcome for <see cref="IEnergyStateStore.Load"/>. Reuses
    /// <see cref="EnergyStateDtoParseOutcome"/> semantics for the parse-failure cases
    /// and adds <see cref="NotFound"/> for the fresh-install case the mapper never sees.
    /// </summary>
    internal enum EnergyStateLoadStatus
    {
        NotFound,
        Loaded,
        Corrupt,
        UnsupportedVersion
    }

    /// <summary>
    /// Result of loading persisted Energy state. <see cref="State"/> is non-null only
    /// when <see cref="Status"/> is <see cref="EnergyStateLoadStatus.Loaded"/>.
    /// </summary>
    internal sealed class EnergyStateLoadResult
    {
        public EnergyStateLoadStatus Status { get; }
        public EnergyState State { get; }

        private EnergyStateLoadResult(EnergyStateLoadStatus status, EnergyState state)
        {
            Status = status;
            State = state;
        }

        public static EnergyStateLoadResult NotFound() =>
            new EnergyStateLoadResult(EnergyStateLoadStatus.NotFound, null);

        public static EnergyStateLoadResult Loaded(EnergyState state) =>
            new EnergyStateLoadResult(EnergyStateLoadStatus.Loaded, state);

        public static EnergyStateLoadResult Corrupt() =>
            new EnergyStateLoadResult(EnergyStateLoadStatus.Corrupt, null);

        public static EnergyStateLoadResult UnsupportedVersion() =>
            new EnergyStateLoadResult(EnergyStateLoadStatus.UnsupportedVersion, null);
    }

    /// <summary>
    /// Package composition seam for reading/writing the durable Energy state.
    /// Internal — exposed to consumers only through the public factory (Task 5).
    /// </summary>
    internal interface IEnergyStateStore
    {
        EnergyStateLoadResult Load();
        bool Save(EnergyState state);
    }
}
