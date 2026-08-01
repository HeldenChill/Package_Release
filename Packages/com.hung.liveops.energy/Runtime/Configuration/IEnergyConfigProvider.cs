namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Result of asking a config provider for the current Energy configuration.
    /// Providers never throw for missing/invalid source data — invalid input is
    /// always reported through <see cref="Success"/> = false with an actionable
    /// <see cref="ErrorMessage"/>, never a permissive fallback config.
    /// </summary>
    public readonly struct EnergyConfigResult
    {
        public bool Success { get; }
        public EnergyConfig Config { get; }
        public string Version { get; }
        public string ErrorMessage { get; }

        private EnergyConfigResult(bool success, EnergyConfig config, string version, string errorMessage)
        {
            Success = success;
            Config = config;
            Version = version;
            ErrorMessage = errorMessage;
        }

        public static EnergyConfigResult Ok(EnergyConfig config, string version)
        {
            return new EnergyConfigResult(true, config, version, null);
        }

        public static EnergyConfigResult Failure(string errorMessage)
        {
            return new EnergyConfigResult(false, null, null, errorMessage);
        }
    }

    /// <summary>
    /// Produces a validated <see cref="EnergyConfig"/> plus a deterministic version string.
    /// Implementations must never let a source-validation problem crash the caller and must
    /// never return a permissive/unlimited config for missing or invalid source data.
    /// </summary>
    public interface IEnergyConfigProvider
    {
        EnergyConfigResult GetConfig();
    }
}
