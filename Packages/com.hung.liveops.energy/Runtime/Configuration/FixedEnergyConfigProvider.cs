namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// Test-only provider constructed directly from an <see cref="EnergyConfig"/> and version
    /// string, with no ScriptableObject involved. Used by later-task service tests.
    /// </summary>
    public sealed class FixedEnergyConfigProvider : IEnergyConfigProvider
    {
        private readonly EnergyConfig _config;
        private readonly string _version;

        public FixedEnergyConfigProvider(EnergyConfig config, string version)
        {
            _config = config;
            _version = version;
        }

        public EnergyConfigResult GetConfig()
        {
            return EnergyConfigResult.Ok(_config, _version);
        }
    }
}
