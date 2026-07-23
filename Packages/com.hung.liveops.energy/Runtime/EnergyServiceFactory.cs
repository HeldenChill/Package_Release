namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// The only public way to construct an <see cref="IEnergyService"/>. Validates configuration
    /// and never falls back to a permissive/unlimited service when construction cannot proceed.
    /// </summary>
    public static class EnergyServiceFactory
    {
        /// <summary>
        /// Builds a locally-persisted Energy service from a config asset and a state file path.
        /// Returns a creation failure (never a working fallback service) if <paramref name="config"/>
        /// is missing or fails validation.
        /// </summary>
        /// <param name="config">Source config asset. A null reference is a creation failure.</param>
        /// <param name="stateFilePath">Path to the local persisted state file.</param>
        /// <param name="clock">Clock to use; defaults to <see cref="SystemClock"/> when null.</param>
        public static EnergyCreationResult CreateLocal(EnergyConfigSO config, string stateFilePath, IClock clock = null)
        {
            clock ??= new SystemClock();

            LocalEnergyConfigProvider configProvider = new LocalEnergyConfigProvider(config);
            EnergyConfigResult configResult = configProvider.GetConfig();
            if (!configResult.Success)
            {
                return EnergyCreationResult.Failed(configResult.ErrorMessage);
            }

            IEnergyStateStore store = new LocalEnergyStateStore(stateFilePath);

            IEnergyService service = new EnergyService(clock, store, configProvider);
            return EnergyCreationResult.Ready(service);
        }
    }
}
